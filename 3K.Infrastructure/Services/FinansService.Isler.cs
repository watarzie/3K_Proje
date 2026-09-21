using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services
{
    public sealed partial class FinansService
    {
        public async Task<FinansSayfaliSonuc<FinansIsKaydiModel>> IsKayitlariAsync(
            FinansListeFiltre filtre,
            CancellationToken cancellationToken)
        {
            var pageNumber = Math.Max(1, filtre.PageNumber);
            var pageSize = Math.Clamp(filtre.PageSize, 1, 250);
            var query = ApplyFilter(IsKaydiDetayQuery(), filtre);
            var count = await query.CountAsync(cancellationToken);
            var items = await query.OrderByDescending(x => x.FinansDonemi)
                .ThenByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new FinansSayfaliSonuc<FinansIsKaydiModel>
            {
                Items = items.Select(MapIsKaydi).ToArray(),
                Toplamlar = await WorkMoneyTotalsAsync(query.Where(x => !x.IptalEdildi && x.KaynakAktif), cancellationToken),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = count
            };
        }

        public async Task<IReadOnlyList<FinansIsKaydiModel>> IsKayitlariSecimAsync(
            IReadOnlyCollection<int> ids,
            CancellationToken cancellationToken)
        {
            var normalizedIds = NormalizeWorkSelectionIds(ids);
            if (normalizedIds.Count == 0)
                return Array.Empty<FinansIsKaydiModel>();

            var entities = await IsKaydiDetayQuery()
                .Where(x => normalizedIds.Contains(x.Id))
                .ToListAsync(cancellationToken);
            var byId = entities.ToDictionary(x => x.Id);

            return normalizedIds
                .Where(byId.ContainsKey)
                .Select(id => MapIsKaydi(byId[id]))
                .ToArray();
        }

        internal static IReadOnlyList<int> NormalizeWorkSelectionIds(IReadOnlyCollection<int> ids)
        {
            ArgumentNullException.ThrowIfNull(ids);
            if (ids.Any(x => x <= 0))
                throw new InvalidOperationException("İş kaydı seçiminde yalnız pozitif kimlikler kullanılabilir.");

            var normalized = ids.Distinct().ToArray();
            if (normalized.Length > 2000)
                throw new InvalidOperationException("Tek işlemde en fazla 2000 iş kaydı seçilebilir.");

            return normalized;
        }

        public async Task<FinansIsKaydiModel?> IsKaydiGetirAsync(int id, CancellationToken cancellationToken)
        {
            var entity = await IsKaydiDetayQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null) return null;
            var model = MapIsKaydi(entity);
            if (entity.SablonSurumId is not { } versionId) return model;
            // Düzenleme, şablonun bugünkü son sürümünü değil kaydın değişmez sürümünü açar.
            var version = await _context.Set<FinansIsSablonSurumu>().AsNoTracking().Include(x => x.Sablon)
                .FirstOrDefaultAsync(x => x.Id == versionId, cancellationToken);
            return version is null ? model : model with
            {
                Sablon = new FinansSablonModel(version.FinansIsSablonuId, version.Sablon.Kod, version.Sablon.Ad,
                    version.Sablon.Aktif, version.Id, version.Surum,
                    System.Text.Json.JsonSerializer.Deserialize<FinansSablonAlanModel[]>(version.AlanlarJson)!)
            };
        }

        public async Task<FinansSayfaliSonuc<FinansProjeOzetModel>> ProjelerAsync(
            FinansListeFiltre filtre,
            CancellationToken cancellationToken)
        {
            var pageNumber = Math.Max(1, filtre.PageNumber);
            var pageSize = Math.Clamp(filtre.PageSize, 1, 250);

            // Önce yalnız proje anahtarlarını SQL tarafında gruplayıp sayfala. Eski
            // akış tüm iş/sipariş/fatura graph'ını belleğe alıp daha sonra sayfalıyordu.
            var baseQuery = ApplyFilter(
                    _context.Set<FinansIsKaydi>().AsNoTracking(),
                    filtre)
                .Where(x => x.KaynakAktif);
            var totalCount = await baseQuery
                .Select(x => new { x.ProjeId, x.ProjeNo, x.Musteri })
                .Distinct()
                .CountAsync(cancellationToken);
            var pageKeys = await BuildProjectPageQuery(baseQuery, pageNumber, pageSize)
                .ToListAsync(cancellationToken);

            if (pageKeys.Count == 0)
            {
                return new FinansSayfaliSonuc<FinansProjeOzetModel>
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount
                };
            }

            // MapProjectSummary mevcut finansal miktar/snapshot kurallarını korur.
            // Bunun için gereken detay graph'ı yalnız seçilmiş sayfadaki proje
            // anahtarlarıyla sınırlandırılır; sorgu sayısı sayfadaki kayıt adedine
            // göre artmaz (AsSplitQuery sabit sayıda SQL üretir, N+1 oluşturmaz).
            var projectIds = pageKeys
                .Where(x => x.ProjeId.HasValue)
                .Select(x => x.ProjeId!.Value)
                .Distinct()
                .ToArray();
            var manualProjectNos = pageKeys
                .Where(x => !x.ProjeId.HasValue)
                .Select(x => x.ProjeNo)
                .Distinct()
                .ToArray();
            // Dönem/arama proje seçer; tamamlanma bütün aktif alt işleri kapsar.
            var pageEntityIds = _context.Set<FinansIsKaydi>().Where(x => !x.IptalEdildi && x.KaynakAktif)
                .Where(x =>
                    (x.ProjeId.HasValue && projectIds.Contains(x.ProjeId.Value)) ||
                    (!x.ProjeId.HasValue && manualProjectNos.Contains(x.ProjeNo)))
                .Select(x => x.Id);
            var detailQuery = IsKaydiDetayQuery()
                .Where(x => pageEntityIds.Contains(x.Id));
            var pageEntities = await detailQuery
                .OrderByDescending(x => x.UretimTarihi)
                .ThenByDescending(x => x.Id)
                .ToListAsync(cancellationToken);
            var groupedEntities = pageEntities
                .GroupBy(x => (x.ProjeId, x.ProjeNo, x.Musteri))
                .ToDictionary(x => x.Key, x => x.ToList());
            var items = pageKeys
                .Select(key => groupedEntities.TryGetValue((key.ProjeId, key.ProjeNo, key.Musteri), out var entities)
                    ? MapProjectSummary(key.ProjeId, key.ProjeNo, key.Musteri, entities)
                    : null)
                .Where(x => x is not null)
                .Select(x => x!)
                .ToArray();

            return new FinansSayfaliSonuc<FinansProjeOzetModel>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<FinansSayfaliSonuc<FinansProjeSecenekModel>> ProjeSecenekleriAsync(
            string? arama,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 250);
            var query = _context.Projeler.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(arama))
            {
                var normalized = arama.Trim().ToLower();
                query = query.Where(x =>
                    x.ProjeNo.ToLower().Contains(normalized) ||
                    x.Musteri.ToLower().Contains(normalized));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderBy(x => x.ProjeNo)
                .ThenBy(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new FinansProjeSecenekModel(x.Id, x.ProjeNo, x.Musteri))
                .ToListAsync(cancellationToken);

            return new FinansSayfaliSonuc<FinansProjeSecenekModel>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        internal static IQueryable<FinansProjeSayfaAnahtari> BuildProjectPageQuery(
            IQueryable<FinansIsKaydi> query,
            int pageNumber,
            int pageSize)
            => query
                .GroupBy(x => new { x.ProjeId, x.ProjeNo, x.Musteri })
                .Select(group => new
                {
                    group.Key.ProjeId,
                    group.Key.ProjeNo,
                    group.Key.Musteri,
                    SonUretimeAlmaTarihi = group.Max(x => x.UretimTarihi)
                })
                .OrderByDescending(x => x.SonUretimeAlmaTarihi)
                .ThenBy(x => x.ProjeNo)
                .ThenBy(x => x.Musteri)
                .Skip((Math.Max(1, pageNumber) - 1) * Math.Clamp(pageSize, 1, 250))
                .Take(Math.Clamp(pageSize, 1, 250))
                .Select(x => new FinansProjeSayfaAnahtari(
                    x.ProjeId,
                    x.ProjeNo,
                    x.Musteri,
                    x.SonUretimeAlmaTarihi));

        private static FinansProjeOzetModel MapProjectSummary(int? projeId, string projeNo, string musteri, List<FinansIsKaydi> entities)
        {
            var normalEntities = entities.Where(x => !x.IptalEdildi && x.KaynakAktif).ToList();
            var normalModels = normalEntities.Select(MapIsKaydi).ToList();
            var allOrderLines = normalEntities.SelectMany(x => x.SiparisKalemleri).Where(x => !x.FinansSiparis.IptalEdildi).ToList();
            var invoiceLines = allOrderLines.SelectMany(x => x.FaturaKalemleri).Where(x => !x.FinansFatura.IptalEdildi).ToList();
            var toplamM3 = normalModels.Where(x => x.IsTuru != FinansIsTuru.SarfKereste).Sum(x => x.ToplamM3);
            var tarife = normalModels.OrderBy(x => x.Id).FirstOrDefault(x => x.BirimFiyat > 0);
            var tarifeEksik = normalModels.Count == 0 || normalModels.Any(x => !x.FiyatlandirmaHazir);
            var totals = normalModels.GroupBy(x => x.ParaBirimi).Select(g => new FinansParaToplamiModel(
                g.Key, g.Sum(x => x.NetTutar), g.Sum(x => x.KdvTutari), g.Sum(x => x.ToplamTutar))).ToArray();
            var netTutar = totals.Length == 1 ? totals[0].NetTutar : 0;
            var kdvTutari = totals.Length == 1 ? totals[0].KdvTutari : 0;
            var allCompleted = normalModels.Count > 0 && normalModels.All(x => x.Durum == FinansIsDurumu.Faturalandi);
            var invoiceWaitingOrder = allOrderLines.Select(x => x.FinansSiparis)
                .Where(x => x.Durum is FinansSiparisDurumu.Acik or FinansSiparisDurumu.KismiFaturalandi)
                .OrderBy(x => x.SiparisTarihi).FirstOrDefault();

            return new FinansProjeOzetModel
            {
                ProjeId = projeId,
                ProjeNo = projeNo,
                Musteri = musteri,
                ToplamIsAdedi = normalModels.Count,
                ToplamSandikAdedi = normalModels.Where(x => x.IsTuru != FinansIsTuru.SarfKereste).Sum(x => x.Adet),
                ToplamM3 = toplamM3,
                SiparisAcikM3 = normalModels.Sum(x => x.SiparisM3),
                SiparisBekleyenM3 = normalModels.Sum(x => x.SiparisBekleyenM3),
                FaturalananM3 = normalModels.Sum(x => x.FaturalananM3),
                FaturaBekleyenM3 = Math.Max(0, normalModels.Sum(x => x.SiparisM3 - x.FaturalananM3)),
                SonUretimeAlmaTarihi = normalModels.Count == 0 ? null : normalModels.Max(x => (DateTime?)x.UretimTarihi),
                GenelDurum = allCompleted ? "Tamamlandı" : normalModels.Any(x => x.Durum != FinansIsDurumu.SiparisBekliyor) ? "Devam Ediyor" : "Sipariş Bekliyor",
                Tutarlar = totals,
                BirimFiyat = tarife?.BirimFiyat ?? 0m,
                ParaBirimi = tarife?.ParaBirimi ?? "EUR",
                KdvOrani = tarife?.KdvOrani ?? 0m,
                NetTutar = netTutar,
                KdvTutari = kdvTutari,
                ToplamTutar = netTutar + kdvTutari,
                TarifeEksik = tarifeEksik,
                PoNumaralari = allOrderLines.Select(x => x.FinansSiparis.PoNumarasi).Distinct().Order().ToArray(),
                FaturaNumaralari = invoiceLines.Select(x => x.FinansFatura.FaturaNumarasi).Distinct().Order().ToArray(),
                FaturaBekleyenSiparisId = invoiceWaitingOrder?.Id
            };
        }

        public Task<FinansIsKaydiModel> IsKaydiOlusturAsync(
            FinansIsKaydiKaydetModel model,
            CancellationToken cancellationToken)
            => ExecuteAtomicAsync(() => IsKaydiOlusturCoreAsync(model, cancellationToken), cancellationToken);

        private async Task<FinansIsKaydiModel> IsKaydiOlusturCoreAsync(
            FinansIsKaydiKaydetModel model,
            CancellationToken cancellationToken)
        {
            var project = await ResolveProjectAsync(model.ProjeId, model.ManuelProjeNo, model.ManuelProjeAdi, model.Musteri, cancellationToken);
            var entity = new FinansIsKaydi
            {
                ProjeId = project.ProjeId,
                ProjeNo = project.ProjeNo,
                Musteri = project.Musteri,
                ManuelProjeMi = project.Manuel,
                IsTuru = model.IsTuru,
                IsAdi = model.IsAdi.Trim(),
                OzelIsTuru = model.OzelIsTuru?.Trim(),
                HesaplamaYontemi = model.HesaplamaYontemi,
                RaporGrubu = model.RaporGrubu?.Trim(),
                Aciklama = model.Aciklama?.Trim(),
                TalepEdenKisi = model.TalepEdenKisi?.Trim(),
                TalepEdenBolum = model.TalepEdenBolum?.Trim(),
                SandikNo = model.SandikNo?.Trim(),
                SandikAdi = model.SandikAdi?.Trim(),
                SandikTipi = model.SandikTipi?.Trim(),
                Boy = model.Boy,
                En = model.En,
                Yukseklik = model.Yukseklik,
                IcSandikSablonId = model.IcSandikSablonId,
                Adet = model.Adet,
                Birim = model.Birim.Trim(),
                BirimM3 = model.BirimM3,
                ToplamM3 = decimal.Round(model.Adet * model.BirimM3, 6),
                UretimTarihi = model.UretimTarihi,
                FinansDonemi = FirstDayOfMonth(model.FinansDonemi),
                FinansTarihi = model.FinansDonemi.Date,
                FinansTarihiManuel = true,
                FinansMiktariManuel = true,
                KayitTarihi = TurkeyTime.Now,
                KaynakTuru = "Manuel",
                KaynakAktif = true
            };
            await ApplyPriceSnapshotAsync(entity, model.FinansUrunId, model.ManuelBirimFiyat, model.ParaBirimi, model.KdvOrani, entity.FinansTarihi, cancellationToken);
            _context.Set<FinansIsKaydi>().Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            if (entity.IsTuru == FinansIsTuru.OzelIs && string.IsNullOrWhiteSpace(entity.SandikNo))
            {
                entity.SandikNo = $"OZL-{entity.Id:D6}";
                await _context.SaveChangesAsync(cancellationToken);
            }
            AddAudit(nameof(FinansIsKaydi), entity.Id, "Oluşturma", "*", null, "Kayıt oluşturuldu");
            await _context.SaveChangesAsync(cancellationToken);
            return (await IsKaydiGetirAsync(entity.Id, cancellationToken))!;
        }

        public async Task<FinansIsKaydiModel?> IsKaydiGuncelleAsync(
            int id,
            FinansIsKaydiKaydetModel model,
            CancellationToken cancellationToken)
        {
            var entity = await IsKaydiDetayQuery(true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null) return null;
            if (!string.Equals(entity.KaynakTuru, "Manuel", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Üretim kaynaklı kayıtlar manuel olarak değiştirilemez; üretim modülünden senkronize edilmelidir.");
            if (entity.IptalEdildi)
                throw new InvalidOperationException("İptal edilmiş kayıt güncellenemez. Önce kaydı geri alın.");
            var auditBefore = CaptureAuditState(entity);
            var previousProductId = entity.FinansUrunId;
            var previousFinancePeriod = entity.FinansDonemi;
            if (model.Adet != entity.Adet || model.BirimM3 != entity.BirimM3 || model.FinansUrunId != entity.FinansUrunId ||
                (model.ManuelBirimFiyat.HasValue && model.ManuelBirimFiyat != entity.BirimFiyatSnapshot) ||
                (!string.IsNullOrWhiteSpace(model.ParaBirimi) && NormalizeCurrency(model.ParaBirimi) != entity.ParaBirimiSnapshot) ||
                (model.KdvOrani.HasValue && model.KdvOrani != entity.KdvOraniSnapshot))
                throw new InvalidOperationException("Finansal miktar/fiyat alanları gerekçeli fiyatlandırma komutuyla değiştirilmelidir.");
            if (FirstDayOfMonth(model.FinansDonemi) != entity.FinansDonemi)
                throw new InvalidOperationException("Finans tarihi ayrı finans tarihi değiştirme işlemiyle güncellenmelidir.");
            if (entity.SiparisKalemleri.Count > 0 && (model.ManuelBirimFiyat != null && model.ManuelBirimFiyat != entity.BirimFiyatSnapshot ||
                model.Adet != entity.Adet || model.BirimM3 != entity.BirimM3 || model.FinansUrunId != entity.FinansUrunId))
                throw new InvalidOperationException("Belgesi bulunan işin miktar/fiyat geçmişi genel düzenlemeden değiştirilemez.");

            var activeOrderLines = entity.SiparisKalemleri.Where(x => !x.FinansSiparis.IptalEdildi).ToList();
            var orderedAdet = activeOrderLines.Sum(x => x.Adet);
            var orderedM3 = activeOrderLines.Sum(x => x.M3);
            var newTotalM3 = decimal.Round(model.Adet * model.BirimM3, 6);
            var activePricingUnit = activeOrderLines.Select(x => x.FiyatlandirmaBirimiSnapshot).Distinct().Count() == 1
                ? activeOrderLines[0].FiyatlandirmaBirimiSnapshot
                : entity.FiyatlandirmaBirimiSnapshot;
            if (FinansMiktarKurallari.KapasiteAsiliyor(
                    activePricingUnit,
                    model.Adet,
                    newTotalM3,
                    orderedAdet,
                    orderedM3,
                    activeOrderLines.Count))
                throw new InvalidOperationException("Miktar, daha önce siparişe bağlanan miktarın altına düşürülemez.");

            var project = await ResolveProjectAsync(model.ProjeId, model.ManuelProjeNo, model.ManuelProjeAdi, model.Musteri, cancellationToken);

            entity.ProjeId = project.ProjeId;
            entity.ProjeNo = project.ProjeNo;
            entity.Musteri = project.Musteri;
            entity.ManuelProjeMi = project.Manuel;
            entity.IsTuru = model.IsTuru;
            entity.IsAdi = model.IsAdi.Trim();
            entity.OzelIsTuru = model.OzelIsTuru?.Trim();
            entity.HesaplamaYontemi = model.HesaplamaYontemi;
            entity.RaporGrubu = model.RaporGrubu?.Trim();
            entity.Aciklama = model.Aciklama?.Trim();
            entity.TalepEdenKisi = model.TalepEdenKisi?.Trim();
            entity.TalepEdenBolum = model.TalepEdenBolum?.Trim();
            entity.SandikNo = model.SandikNo?.Trim();
            entity.SandikAdi = model.SandikAdi?.Trim();
            entity.SandikTipi = model.SandikTipi?.Trim();
            entity.Boy = model.Boy;
            entity.En = model.En;
            entity.Yukseklik = model.Yukseklik;
            entity.IcSandikSablonId = model.IcSandikSablonId;
            entity.Adet = model.Adet;
            entity.Birim = model.Birim.Trim();
            entity.BirimM3 = model.BirimM3;
            entity.ToplamM3 = newTotalM3;
            entity.UretimTarihi = model.UretimTarihi;
            entity.FinansDonemi = FirstDayOfMonth(model.FinansDonemi);
            var explicitPriceOverride = model.ManuelBirimFiyat.HasValue ||
                                        !string.IsNullOrWhiteSpace(model.ParaBirimi) ||
                                        model.KdvOrani.HasValue;
            var shouldReprice = previousProductId != model.FinansUrunId ||
                                explicitPriceOverride;
            if (shouldReprice)
                await ApplyPriceSnapshotAsync(entity, model.FinansUrunId, model.ManuelBirimFiyat, model.ParaBirimi, model.KdvOrani, entity.FinansDonemi, cancellationToken);
            if (activeOrderLines.Count > 0 && entity.FiyatlandirmaBirimiSnapshot != activePricingUnit)
                throw new InvalidOperationException("Aktif siparişi bulunan işin fiyatlandırma birimi değiştirilemez.");
            entity.Durum = DetermineWorkStatus(entity);
            AddAuditChanges(nameof(FinansIsKaydi), entity, auditBefore);
            await _context.SaveChangesAsync(cancellationToken);
            return await IsKaydiGetirAsync(id, cancellationToken);
        }

        public async Task<bool> IsKaydiIptalAsync(int id, string aciklama, CancellationToken cancellationToken)
        {
            Gerekce(aciklama);
            var entity = await IsKaydiDetayQuery(true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null) return false;
            if (entity.IptalEdildi) return true;
            if (entity.SiparisKalemleri.Any(x => !x.FinansSiparis.IptalEdildi))
                throw new InvalidOperationException("Aktif siparişi bulunan iş iptal edilemez. Önce bağlı siparişleri iptal edin.");
            entity.IptalEdildi = true;
            entity.IptalTarihi = TurkeyTime.Now;
            entity.IptalAciklamasi = aciklama.Trim();
            entity.Durum = FinansIsDurumu.IptalEdildi;
            AddAudit(nameof(FinansIsKaydi), id, "İptal", nameof(entity.IptalEdildi), false, true, aciklama);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> IsKaydiGeriAlAsync(int id, CancellationToken cancellationToken)
        {
            var entity = await IsKaydiDetayQuery(true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null) return false;
            if (!entity.IptalEdildi) return true;
            if (!string.Equals(entity.KaynakTuru, "Manuel", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Üretim kaynaklı kayıtlar Finans ekranından aktifleştirilemez; işlemi Üretim modülünden yönetin.");
            entity.IptalEdildi = false;
            entity.IptalTarihi = null;
            entity.IptalAciklamasi = null;
            entity.KaynakAktif = true;
            entity.Durum = DetermineWorkStatus(entity);
            AddAudit(nameof(FinansIsKaydi), id, "Aktifleştirme", nameof(entity.IptalEdildi), true, false);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<FinansSenkronizasyonSonucModel> UretimKayitlariniAktarAsync(
            IReadOnlyList<FinansUretimAktarimModel> modeller,
            CancellationToken cancellationToken)
        {
            if (modeller.Count == 0) return new FinansSenkronizasyonSonucModel(0, 0, 0);
            modeller = modeller.SelectMany(model => model.SarfM3.HasValue && model.KaynakBileseni == "NET"
                ? new[] { model, model with { KaynakBileseni = "SARF", IsTuru = FinansIsTuru.SarfKereste,
                    IsAdi = $"Sarf Kereste - {model.SandikNo}", BirimM3 = model.Adet > 0 ? model.SarfM3.Value / model.Adet : 0,
                    KaynakAktif = model.KaynakAktif && model.UretimM3Hesaplanabilir && model.SarfM3 > 0, SarfM3 = null } }
                : new[] { model }).ToArray();
            var duplicate = modeller.GroupBy(x => new { Tur = x.KaynakTuru.Trim().ToUpperInvariant(), Id = x.KaynakKayitId.Trim(), x.KaynakBileseni })
                .FirstOrDefault(x => x.Count() > 1);
            if (duplicate is not null)
                throw new InvalidOperationException("Aynı aktarım paketinde yinelenen üretim kaynak anahtarı bulunuyor.");

            IDbContextTransaction? transaction = null;
            if (_context.Database.CurrentTransaction is null)
                transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var created = 0;
                var updated = 0;
                var deactivated = 0;
                foreach (var model in modeller)
                {
                    var sourceType = model.KaynakTuru.Trim().ToUpperInvariant();
                    var sourceId = model.KaynakKayitId.Trim();
                    var component = model.KaynakBileseni.Trim().ToUpperInvariant();
                    if (sourceType.Length == 0 || sourceId.Length == 0)
                        throw new InvalidOperationException("Üretim aktarımında kaynak türü ve kaynak kayıt kimliği zorunludur.");

                    var entity = await IsKaydiDetayQuery(true).FirstOrDefaultAsync(
                        x => x.KaynakTuru == sourceType && x.KaynakKayitId == sourceId && x.KaynakBileseni == component,
                        cancellationToken);
                    if (entity is null)
                    {
                        if (await _context.Set<FinansKaynakBastirma>().AnyAsync(x => x.KaynakTuru == sourceType && x.KaynakKayitId == sourceId && x.KaynakBileseni == component, cancellationToken))
                            continue;
                        // Üretime hiç alınmamış/pasif bir kaynak Finans'ta hayalet
                        // kayıt oluşturmamalı. Daha önce aktarılmış kayıtlarda false
                        // güncellemesi aşağıdaki mevcut-entity akışında işlenir.
                        if (!model.KaynakAktif)
                            continue;
                        entity = new FinansIsKaydi
                        {
                            KaynakTuru = sourceType,
                            KaynakKayitId = sourceId,
                            KaynakBileseni = component,
                            KayitTarihi = TurkeyTime.Now
                        };
                        ApplyProductionValues(entity, model);
                        // Üretim verisi her değiştiğinde fiyatı yeniden hesaplamak tarihsel
                        // snapshot'ı bozar. Yalnız daha önce eşleşmeyen/fiyatlanmayan ve henüz
                        // siparişe bağlanmayan kayıt, sonradan tanımlanan tarifeyle tamamlanır.
                        if (entity.BirimFiyatSnapshot <= 0 &&
                            !entity.TarifeYiliSnapshot.HasValue &&
                            !entity.SiparisKalemleri.Any(x => !x.FinansSiparis.IptalEdildi))
                        {
                            await TryApplyAutomaticPriceAsync(entity, cancellationToken);
                        }
                        _context.Set<FinansIsKaydi>().Add(entity);
                        await _context.SaveChangesAsync(cancellationToken);
                        AddAudit(nameof(FinansIsKaydi), entity.Id, "Üretim Aktarımı", "*", null, "Kayıt oluşturuldu");
                        created++;
                    }
                    else
                    {
                        var auditBefore = CaptureAuditState(entity);
                        var oldActive = entity.KaynakAktif;
                        var activeOrderLines = entity.SiparisKalemleri.Where(x => !x.FinansSiparis.IptalEdildi).ToList();
                        var orderedAdet = activeOrderLines.Sum(x => x.Adet);
                        var orderedM3 = activeOrderLines.Sum(x => x.M3);
                        var requestedM3 = decimal.Round(model.Adet * model.BirimM3, 6);
                        var pricingUnit = activeOrderLines.Select(x => x.FiyatlandirmaBirimiSnapshot).Distinct().Count() == 1
                            ? activeOrderLines[0].FiyatlandirmaBirimiSnapshot
                            : entity.FiyatlandirmaBirimiSnapshot;
                        if (!entity.FinansMiktariManuel && model.KaynakAktif && FinansMiktarKurallari.KapasiteAsiliyor(
                                pricingUnit,
                                model.Adet,
                                requestedM3,
                                orderedAdet,
                                orderedM3,
                                activeOrderLines.Count))
                            throw new InvalidOperationException($"{sourceType}/{sourceId} üretim miktarı aktif sipariş miktarının altına düşürülemez.");
                        // İptal edilmiş olsa bile oluşturulmuş bir PO, fiyat snapshot'ının
                        // tarihsel olarak kilitlendiği anlamına gelir. Yalnız aktif PO'ya
                        // bakmak, iptalden sonraki senkronizasyonda eski fiyatı değiştirebilirdi.
                        var hasFinancialHistory = entity.SiparisKalemleri.Count > 0;
                        var priceMatchChanged = ProductionPriceMatchChanged(entity, model);
                        if (model.KaynakAktif && hasFinancialHistory && priceMatchChanged)
                            throw new InvalidOperationException(
                                $"{sourceType}/{sourceId} kaydının tür/eşleşme veya finans dönemi değişikliği mevcut PO fiyat snapshot'ını etkiliyor. " +
                                "Önce finansal belgeleri uzlaştırın ya da mevcut kaydı koruyun.");

                        ApplyProductionValues(entity, model);
                        if (model.KaynakAktif && FinansTutarKurallari.SiparisNet(entity) > FinansTutarKurallari.IsNet(entity))
                            throw new InvalidOperationException("Kaynak güncellemesi aktif PO net toplamını iş bedelinin üzerine çıkaramaz.");
                        if (!hasFinancialHistory && !entity.FinansMiktariManuel && priceMatchChanged && entity.BirimFiyatSnapshot <= 0)
                        {
                            ResetAutomaticPriceSnapshot(entity);
                            await TryApplyAutomaticPriceAsync(entity, cancellationToken);
                        }
                        else if (entity.BirimFiyatSnapshot <= 0 &&
                                 !entity.FinansMiktariManuel &&
                                 !entity.TarifeYiliSnapshot.HasValue &&
                                 !hasFinancialHistory)
                        {
                            await TryApplyAutomaticPriceAsync(entity, cancellationToken);
                        }
                        AddAuditChanges(nameof(FinansIsKaydi), entity, auditBefore);
                        if (oldActive && !entity.KaynakAktif && hasFinancialHistory)
                        {
                            AddAudit(
                                nameof(FinansIsKaydi),
                                entity.Id,
                                "Üretim Kaynağı Pasifleştirme",
                                nameof(entity.KaynakAktif),
                                true,
                                false,
                                "Kayda bağlı PO/fatura geçmişi korundu; üretim iptali finansal belgeleri değiştirmedi.");
                        }
                        if (oldActive && !entity.KaynakAktif) deactivated++;
                        else updated++;
                    }

                    entity.Durum = DetermineWorkStatus(entity);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                if (transaction is not null) await transaction.CommitAsync(cancellationToken);
                return new FinansSenkronizasyonSonucModel(created, updated, deactivated);
            }
            catch (Exception exception)
            {
                if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
                ThrowIfPersistenceConflict(exception);
                throw;
            }
            finally
            {
                if (transaction is not null) await transaction.DisposeAsync();
            }
        }

        private static void ApplyProductionValues(FinansIsKaydi entity, FinansUretimAktarimModel model)
        {
            var finansIptali = entity.IptalEdildi && entity.KaynakAktif;
            entity.ProjeId = model.ProjeId;
            entity.ProjeNo = model.ProjeNo.Trim();
            entity.Musteri = model.Musteri.Trim();
            entity.ManuelProjeMi = !model.ProjeId.HasValue;
            entity.IsTuru = model.IsTuru;
            entity.IsAdi = model.IsAdi.Trim();
            entity.Aciklama = model.Aciklama?.Trim();
            entity.TalepEdenKisi = model.TalepEdenKisi?.Trim();
            entity.TalepEdenBolum = model.TalepEdenBolum?.Trim();
            entity.SandikNo = model.SandikNo?.Trim();
            entity.SandikAdi = model.SandikAdi?.Trim();
            entity.SandikTipi = model.SandikTipi?.Trim();
            entity.Boy = model.Boy;
            entity.En = model.En;
            entity.Yukseklik = model.Yukseklik;
            entity.IcSandikSablonId = model.IcSandikSablonId;
            entity.SandikCinsi = model.SandikCinsi;
            if (!entity.FinansMiktariManuel)
            {
                entity.Adet = model.Adet;
                entity.Birim = model.BirimM3 > 0 ? "m³" : "Adet";
                entity.BirimM3 = model.UretimM3Hesaplanabilir ? model.BirimM3 : 0;
                entity.ToplamM3 = decimal.Round(entity.Adet * entity.BirimM3, 6);
            }
            entity.UretimTarihi = model.UretimTarihi;
            if (!entity.FinansTarihiManuel)
            {
                entity.FinansTarihi = model.UretimTarihi.Date;
                entity.FinansDonemi = FirstDayOfMonth(model.FinansDonemi);
            }
            entity.KaynakAktif = model.KaynakAktif;
            if (!finansIptali)
            {
                entity.IptalEdildi = !model.KaynakAktif;
                entity.IptalTarihi = model.KaynakAktif ? null : TurkeyTime.Now;
                entity.IptalAciklamasi = model.KaynakAktif ? null : "Üretim kaynağı pasifleştirildi.";
            }
        }

        private static bool ProductionPriceMatchChanged(FinansIsKaydi entity, FinansUretimAktarimModel model)
            => entity.IsTuru != model.IsTuru ||
               !string.Equals(entity.SandikAdi?.Trim(), model.SandikAdi?.Trim(), StringComparison.OrdinalIgnoreCase) ||
               !string.Equals(entity.SandikTipi?.Trim(), model.SandikTipi?.Trim(), StringComparison.OrdinalIgnoreCase) ||
               entity.Boy != model.Boy || entity.En != model.En || entity.Yukseklik != model.Yukseklik ||
               entity.IcSandikSablonId != model.IcSandikSablonId;

        private static void ResetAutomaticPriceSnapshot(FinansIsKaydi entity)
        {
            entity.FinansUrunId = null;
            entity.FiyatlandirmaBirimiSnapshot = entity.BirimM3 > 0
                ? FinansFiyatlandirmaBirimi.Metrekup
                : FinansFiyatlandirmaBirimi.Adet;
            entity.BirimFiyatSnapshot = 0;
            entity.ParaBirimiSnapshot = "EUR";
            entity.KdvOraniSnapshot = 0;
            entity.TarifeYiliSnapshot = null;
        }

        private async Task TryApplyAutomaticPriceAsync(FinansIsKaydi entity, CancellationToken cancellationToken)
        {
            var matchQuery = _context.Set<FinansUrunEslesmesi>()
                .AsNoTracking()
                .Include(x => x.FinansUrun)
                .Where(x => x.Aktif && x.FinansUrun.Aktif && x.IsTuru == entity.IsTuru);
            var matches = await matchQuery.OrderBy(x => x.FinansUrun.Sira).ThenBy(x => x.Id).ToListAsync(cancellationToken);
            var match = matches
                .Where(x => string.IsNullOrWhiteSpace(x.SandikAdi) || string.Equals(x.SandikAdi, entity.SandikAdi, StringComparison.OrdinalIgnoreCase))
                .Where(x => string.IsNullOrWhiteSpace(x.SandikTipi) || string.Equals(x.SandikTipi, entity.SandikTipi, StringComparison.OrdinalIgnoreCase))
                .Where(x => !x.IcSandikSablonId.HasValue || x.IcSandikSablonId == entity.IcSandikSablonId)
                .Where(x => !x.Boy.HasValue || x.Boy == entity.Boy)
                .Where(x => !x.En.HasValue || x.En == entity.En)
                .Where(x => !x.Yukseklik.HasValue || x.Yukseklik == entity.Yukseklik)
                .OrderByDescending(x => MatchSpecificity(x))
                .ThenBy(x => x.FinansUrun.Sira)
                .FirstOrDefault();

            if (match is null)
            {
                entity.FinansUrunId = null;
                entity.FiyatlandirmaBirimiSnapshot = entity.BirimM3 > 0 ? FinansFiyatlandirmaBirimi.Metrekup : FinansFiyatlandirmaBirimi.Adet;
                entity.BirimFiyatSnapshot = 0;
                entity.ParaBirimiSnapshot = "EUR";
                entity.KdvOraniSnapshot = 0;
                entity.TarifeYiliSnapshot = null;
                return;
            }

            entity.FinansUrunId = match.FinansUrunId;
            entity.FiyatlandirmaBirimiSnapshot = match.FinansUrun.FiyatlandirmaBirimi;
            var tariff = await FindTariffAsync(match.FinansUrunId, entity.FinansTarihi, cancellationToken);
            if (tariff is null)
            {
                entity.BirimFiyatSnapshot = 0;
                entity.ParaBirimiSnapshot = "EUR";
                entity.KdvOraniSnapshot = 0;
                entity.TarifeYiliSnapshot = null;
                return;
            }

            entity.BirimFiyatSnapshot = tariff.BirimFiyat;
            entity.ParaBirimiSnapshot = tariff.ParaBirimi;
            entity.KdvOraniSnapshot = tariff.KdvOrani;
            entity.TarifeYiliSnapshot = tariff.Yil;
            entity.TarifeIdSnapshot = tariff.Id;
        }

        private static int MatchSpecificity(FinansUrunEslesmesi value)
            => (string.IsNullOrWhiteSpace(value.SandikAdi) ? 0 : 1) +
               (string.IsNullOrWhiteSpace(value.SandikTipi) ? 0 : 1) +
               (value.IcSandikSablonId.HasValue ? 1 : 0) +
               (value.Boy.HasValue ? 1 : 0) +
               (value.En.HasValue ? 1 : 0) +
               (value.Yukseklik.HasValue ? 1 : 0);

        private static DateTime FirstDayOfMonth(DateTime value) => new(value.Year, value.Month, 1);
    }

    internal sealed record FinansProjeSayfaAnahtari(
        int? ProjeId,
        string ProjeNo,
        string Musteri,
        DateTime SonUretimeAlmaTarihi);
}
