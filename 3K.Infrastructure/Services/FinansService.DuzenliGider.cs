using System.Data;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services
{
    public sealed partial class FinansService
    {
        public async Task<IReadOnlyList<FinansDuzenliIsModel>> DuzenliIslerAsync(
            bool sadeceAktif,
            CancellationToken cancellationToken)
        {
            var query = _context.Set<FinansDuzenliIs>().AsNoTracking();
            if (sadeceAktif) query = query.Where(x => x.Aktif);
            var entities = await query.OrderBy(x => x.IsAdi).ThenBy(x => x.Id).ToListAsync(cancellationToken);
            return entities.Select(MapRecurring).ToArray();
        }

        public async Task<FinansSayfaliSonuc<FinansDuzenliIsModel>> DuzenliIslerSayfaliAsync(
            bool sadeceAktif,
            string? arama,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var query = ApplyRecurringFilter(
                _context.Set<FinansDuzenliIs>().AsNoTracking(), sadeceAktif, arama);
            var count = await query.CountAsync(cancellationToken);
            var (page, size, skip) = NormalizePagination(pageNumber, pageSize, count, 100);
            var entities = await query
                .OrderBy(x => x.IsAdi)
                .ThenBy(x => x.Id)
                .Skip(skip)
                .Take(size)
                .ToListAsync(cancellationToken);

            return new FinansSayfaliSonuc<FinansDuzenliIsModel>
            {
                Items = entities.Select(MapRecurring).ToArray(),
                PageNumber = page,
                PageSize = size,
                TotalCount = count
            };
        }

        internal static IQueryable<FinansDuzenliIs> ApplyRecurringFilter(
            IQueryable<FinansDuzenliIs> query,
            bool sadeceAktif,
            string? arama)
        {
            if (sadeceAktif)
                query = query.Where(x => x.Aktif);
            if (string.IsNullOrWhiteSpace(arama))
                return query;

            var search = arama.Trim().ToLower();
            return query.Where(x =>
                x.IsAdi.ToLower().Contains(search) ||
                x.Musteri.ToLower().Contains(search) ||
                x.RaporGrubu.ToLower().Contains(search) ||
                (x.ManuelProjeNo != null && x.ManuelProjeNo.ToLower().Contains(search)) ||
                (x.ManuelProjeAdi != null && x.ManuelProjeAdi.ToLower().Contains(search)) ||
                (x.OzelIsTuru != null && x.OzelIsTuru.ToLower().Contains(search)));
        }

        public Task<FinansDuzenliIsModel> DuzenliIsOlusturAsync(
            FinansDuzenliIsKaydetModel model,
            CancellationToken cancellationToken)
            => ExecuteAtomicAsync(() => DuzenliIsOlusturCoreAsync(model, cancellationToken), cancellationToken);

        private async Task<FinansDuzenliIsModel> DuzenliIsOlusturCoreAsync(
            FinansDuzenliIsKaydetModel model,
            CancellationToken cancellationToken)
        {
            await ValidateRecurringReferencesAsync(model, cancellationToken);
            var entity = new FinansDuzenliIs();
            ApplyRecurring(entity, model);
            _context.Set<FinansDuzenliIs>().Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            AddAudit(nameof(FinansDuzenliIs), entity.Id, "Oluşturma", "*", null, "Düzenli iş oluşturuldu");
            await _context.SaveChangesAsync(cancellationToken);
            return MapRecurring(entity);
        }

        public async Task<FinansDuzenliIsModel?> DuzenliIsGuncelleAsync(
            int id,
            FinansDuzenliIsKaydetModel model,
            CancellationToken cancellationToken)
        {
            var entity = await _context.Set<FinansDuzenliIs>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null) return null;
            await ValidateRecurringReferencesAsync(model, cancellationToken);
            var auditBefore = CaptureAuditState(entity);
            ApplyRecurring(entity, model);
            AddAuditChanges(nameof(FinansDuzenliIs), entity, auditBefore);
            await _context.SaveChangesAsync(cancellationToken);
            return MapRecurring(entity);
        }

        public async Task<FinansDonemOlusturSonucModel> DuzenliIsDonemiOlusturAsync(
            DateTime referansTarihi,
            CancellationToken cancellationToken)
        {
            var period = FirstDayOfMonth(referansTarihi);
            var periodEnd = period.AddMonths(1).AddTicks(-1);
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var templates = await _context.Set<FinansDuzenliIs>()
                    .AsNoTracking()
                    .Include(x => x.Proje)
                    .Include(x => x.FinansUrun)
                    .Where(x => x.Aktif && x.BaslangicTarihi <= periodEnd && (!x.BitisTarihi.HasValue || x.BitisTarihi.Value >= period))
                    .OrderBy(x => x.Id)
                    .ToListAsync(cancellationToken);
                // Günlük arka plan görevi ayın başında çalışsa bile şablonu kendi
                // oluşturma gününden önce üretmez. Geçmiş dönem elle oluşturulurken
                // ayın tamamı işlenir.
                if (period.Year == TurkeyTime.Now.Year && period.Month == TurkeyTime.Now.Month)
                    templates = templates.Where(x => x.OlusturmaGunu <= referansTarihi.Day).ToList();

                var sourceIds = templates.Select(x => $"{x.Id}:{period:yyyyMM}").ToArray();
                var existingSourceIds = await _context.Set<FinansIsKaydi>()
                    .AsNoTracking()
                    .Where(x => x.KaynakTuru == "DuzenliIs" &&
                                x.KaynakKayitId != null &&
                                sourceIds.Contains(x.KaynakKayitId))
                    .Select(x => x.KaynakKayitId!)
                    .ToHashSetAsync(cancellationToken);
                var productIds = templates
                    .Where(x => x.FinansUrunId.HasValue)
                    .Select(x => x.FinansUrunId!.Value)
                    .Distinct()
                    .ToArray();
                var tariffs = await _context.Set<FinansFiyatTarifesi>()
                    .AsNoTracking()
                    .Where(x => productIds.Contains(x.FinansUrunId) &&
                                x.Aktif &&
                                x.GecerlilikBaslangici <= period &&
                                x.GecerlilikBitisi >= period)
                    .OrderByDescending(x => x.GecerlilikBaslangici)
                    .ThenByDescending(x => x.Id)
                    .ToListAsync(cancellationToken);
                var tariffByProduct = tariffs
                    .GroupBy(x => x.FinansUrunId)
                    .ToDictionary(x => x.Key, x => x.First());
                var worksToCreate = new List<(FinansIsKaydi Work, string SourceId)>();
                foreach (var template in templates)
                {
                    var sourceId = $"{template.Id}:{period:yyyyMM}";
                    if (existingSourceIds.Contains(sourceId))
                        continue;
                    var project = template.Proje;
                    var product = template.FinansUrun;
                    var pricingUnit = product?.FiyatlandirmaBirimi ?? FinansFiyatlandirmaBirimi.Adet;
                    var quantity = FinansMiktarKurallari.DuzenliIsMiktari(pricingUnit, template.Miktar);
                    var work = new FinansIsKaydi
                    {
                        ProjeId = template.ProjeId,
                        ProjeNo = project?.ProjeNo ?? template.ManuelProjeNo ?? "BAĞIMSIZ",
                        Musteri = project?.Musteri ?? template.ManuelProjeAdi ?? template.Musteri,
                        ManuelProjeMi = !template.ProjeId.HasValue,
                        IsTuru = template.IsTuru,
                        IsAdi = template.IsAdi,
                        OzelIsTuru = template.OzelIsTuru,
                        HesaplamaYontemi = template.HesaplamaYontemi,
                        RaporGrubu = template.RaporGrubu,
                        Aciklama = template.Aciklama,
                        Adet = quantity.Adet,
                        Birim = pricingUnit switch
                        {
                            FinansFiyatlandirmaBirimi.Metrekup => "m³",
                            FinansFiyatlandirmaBirimi.SabitTutar => "Sabit",
                            _ => template.Birim
                        },
                        BirimM3 = quantity.M3,
                        ToplamM3 = quantity.M3,
                        FinansUrunId = template.FinansUrunId,
                        UretimTarihi = period.AddDays(Math.Min(template.OlusturmaGunu, DateTime.DaysInMonth(period.Year, period.Month)) - 1),
                        FinansDonemi = period,
                        KayitTarihi = TurkeyTime.Now,
                        KaynakTuru = "DuzenliIs",
                        KaynakKayitId = sourceId,
                        KaynakAktif = true,
                        DuzenliIsId = template.Id
                    };
                    if (product is not null)
                    {
                        tariffByProduct.TryGetValue(product.Id, out var tariff);
                        work.FiyatlandirmaBirimiSnapshot = product.FiyatlandirmaBirimi;
                        work.BirimFiyatSnapshot = tariff?.BirimFiyat ?? template.BirimFiyat;
                        work.ParaBirimiSnapshot = tariff?.ParaBirimi ?? template.ParaBirimi;
                        work.KdvOraniSnapshot = tariff?.KdvOrani ?? template.KdvOrani;
                        work.TarifeYiliSnapshot = tariff?.Yil;
                    }
                    else
                    {
                        work.FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.Adet;
                        work.BirimFiyatSnapshot = template.BirimFiyat;
                        work.ParaBirimiSnapshot = template.ParaBirimi;
                        work.KdvOraniSnapshot = template.KdvOrani;
                    }
                    worksToCreate.Add((work, sourceId));
                }

                _context.Set<FinansIsKaydi>().AddRange(worksToCreate.Select(x => x.Work));
                await _context.SaveChangesAsync(cancellationToken);
                foreach (var item in worksToCreate)
                    AddAudit(nameof(FinansIsKaydi), item.Work.Id, "Dönem Oluşturma", "*", null, item.SourceId);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new FinansDonemOlusturSonucModel(templates.Count, worksToCreate.Count, referansTarihi);
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                ThrowIfPersistenceConflict(exception);
                throw;
            }
        }

        private async Task ValidateRecurringReferencesAsync(FinansDuzenliIsKaydetModel model, CancellationToken cancellationToken)
        {
            await ResolveProjectAsync(model.ProjeId, model.ManuelProjeNo, model.ManuelProjeAdi, model.Musteri, cancellationToken);
            if (!model.FinansUrunId.HasValue) return;
            if (!await _context.Set<FinansUrun>().AnyAsync(x => x.Id == model.FinansUrunId && x.Aktif, cancellationToken))
                throw new InvalidOperationException("Seçilen finans ürünü bulunamadı veya pasif.");
        }

        private static void ApplyRecurring(FinansDuzenliIs entity, FinansDuzenliIsKaydetModel model)
        {
            entity.ProjeId = model.ProjeId;
            entity.ManuelProjeNo = model.ManuelProjeNo?.Trim();
            entity.ManuelProjeAdi = model.ManuelProjeAdi?.Trim();
            entity.IsAdi = model.IsAdi.Trim();
            entity.IsTuru = model.IsTuru;
            entity.OzelIsTuru = model.OzelIsTuru?.Trim();
            entity.HesaplamaYontemi = model.HesaplamaYontemi;
            entity.RaporGrubu = model.RaporGrubu.Trim();
            entity.Musteri = model.Musteri.Trim();
            entity.Aciklama = model.Aciklama?.Trim();
            entity.TekrarSikligi = FinansTekrarSikligi.Aylik;
            entity.BaslangicTarihi = model.BaslangicTarihi.Date;
            entity.BitisTarihi = model.BitisTarihi?.Date;
            entity.OlusturmaGunu = model.OlusturmaGunu;
            entity.Miktar = model.Miktar;
            entity.Birim = model.Birim.Trim();
            entity.FinansUrunId = model.FinansUrunId;
            entity.BirimFiyat = model.BirimFiyat;
            entity.ParaBirimi = NormalizeCurrency(model.ParaBirimi);
            entity.KdvOrani = model.KdvOrani;
            entity.Aktif = model.Aktif;
        }

        private static FinansDuzenliIsModel MapRecurring(FinansDuzenliIs entity) => new()
        {
            Id = entity.Id,
            ProjeId = entity.ProjeId,
            ManuelProjeNo = entity.ManuelProjeNo,
            IsAdi = entity.IsAdi,
            IsTuru = entity.OzelIsTuru ?? entity.IsTuru.ToString(),
            HesaplamaYontemi = entity.HesaplamaYontemi,
            RaporGrubu = entity.RaporGrubu,
            Musteri = entity.Musteri,
            Aciklama = entity.Aciklama,
            TekrarSikligi = "Aylık",
            BaslangicTarihi = entity.BaslangicTarihi,
            BitisTarihi = entity.BitisTarihi,
            OlusturmaGunu = entity.OlusturmaGunu,
            Miktar = entity.Miktar,
            Birim = entity.Birim,
            FinansUrunId = entity.FinansUrunId,
            BirimFiyat = entity.BirimFiyat,
            ParaBirimi = entity.ParaBirimi,
            KdvOrani = entity.KdvOrani,
            Aktif = entity.Aktif
        };

        public async Task<FinansSayfaliSonuc<FinansGiderModel>> GiderlerAsync(
            FinansListeFiltre filtre,
            CancellationToken cancellationToken)
        {
            var query = _context.Set<FinansGider>()
                .AsNoTracking()
                .Include(x => x.Kategori)
                .Include(x => x.GiderKalemi)
                .Include(x => x.Proje)
                .AsQueryable();
            if (!filtre.IptalEdilenleriDahilEt) query = query.Where(x => !x.IptalEdildi);
            if (filtre.ProjeId.HasValue) query = query.Where(x => x.ProjeId == filtre.ProjeId);
            if (!string.IsNullOrWhiteSpace(filtre.ProjeNo))
            {
                var projectNo = filtre.ProjeNo.Trim();
                query = query.Where(x => x.ManuelProjeNo == projectNo || (x.Proje != null && x.Proje.ProjeNo == projectNo));
            }
            if (filtre.IsTuru.HasValue) query = query.Where(x => x.IsTuru == filtre.IsTuru);
            if (filtre.Baslangic.HasValue) query = query.Where(x => x.FinansDonemi >= filtre.Baslangic.Value.Date);
            if (filtre.Bitis.HasValue)
            {
                var end = filtre.Bitis.Value.Date.AddDays(1);
                query = query.Where(x => x.FinansDonemi < end);
            }
            if (!string.IsNullOrWhiteSpace(filtre.ParaBirimi)) query = query.Where(x => x.ParaBirimi == filtre.ParaBirimi.Trim().ToUpper());
            if (!string.IsNullOrWhiteSpace(filtre.Arama))
            {
                var search = filtre.Arama.Trim().ToLower();
                query = query.Where(x => x.Aciklama.ToLower().Contains(search) ||
                                         (x.FirmaVeyaKisi != null && x.FirmaVeyaKisi.ToLower().Contains(search)) ||
                                         x.Kategori.Ad.ToLower().Contains(search));
            }
            var count = await query.CountAsync(cancellationToken);
            var (page, size, skip) = NormalizePagination(
                filtre.PageNumber, filtre.PageSize, count, 250);
            var totals = await query
                .Where(x => !x.IptalEdildi)
                .GroupBy(x => x.ParaBirimi)
                .Select(x => new FinansParaToplamiModel(
                    x.Key,
                    x.Sum(y => y.Matrah),
                    x.Sum(y => y.KdvTutari),
                    x.Sum(y => y.ToplamTutar)))
                .OrderBy(x => x.ParaBirimi)
                .ToListAsync(cancellationToken);
            var entities = await query.OrderByDescending(x => x.Tarih).ThenByDescending(x => x.Id)
                .Skip(skip).Take(size).ToListAsync(cancellationToken);
            return new FinansSayfaliSonuc<FinansGiderModel>
            {
                Items = entities.Select(MapExpense).ToArray(),
                Toplamlar = totals,
                PageNumber = page,
                PageSize = size,
                TotalCount = count
            };
        }

        public Task<FinansGiderModel> GiderOlusturAsync(FinansGiderKaydetModel model, CancellationToken cancellationToken)
            => ExecuteAtomicAsync(() => GiderOlusturCoreAsync(model, cancellationToken), cancellationToken);

        private async Task<FinansGiderModel> GiderOlusturCoreAsync(FinansGiderKaydetModel model, CancellationToken cancellationToken)
        {
            await ValidateExpenseReferencesAsync(model, cancellationToken);
            var entity = new FinansGider();
            ApplyExpense(entity, model);
            _context.Set<FinansGider>().Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            AddAudit(nameof(FinansGider), entity.Id, "Oluşturma", "*", null, "Gider oluşturuldu");
            await _context.SaveChangesAsync(cancellationToken);
            return await GetExpenseAsync(entity.Id, cancellationToken);
        }

        public async Task<FinansGiderModel?> GiderGuncelleAsync(int id, FinansGiderKaydetModel model, CancellationToken cancellationToken)
        {
            var entity = await _context.Set<FinansGider>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null) return null;
            if (entity.IptalEdildi) throw new InvalidOperationException("İptal edilmiş gider güncellenemez.");
            await ValidateExpenseReferencesAsync(model, cancellationToken);
            var auditBefore = CaptureAuditState(entity);
            ApplyExpense(entity, model);
            AddAuditChanges(nameof(FinansGider), entity, auditBefore);
            await _context.SaveChangesAsync(cancellationToken);
            return await GetExpenseAsync(id, cancellationToken);
        }

        public async Task<bool> GiderIptalAsync(int id, string aciklama, CancellationToken cancellationToken)
        {
            var entity = await _context.Set<FinansGider>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null) return false;
            if (entity.IptalEdildi) return true;
            entity.IptalEdildi = true;
            entity.IptalTarihi = TurkeyTime.Now;
            entity.IptalAciklamasi = aciklama.Trim();
            AddAudit(nameof(FinansGider), id, "İptal", nameof(entity.IptalEdildi), false, true, aciklama);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> GiderGeriAlAsync(int id, CancellationToken cancellationToken)
        {
            var entity = await _context.Set<FinansGider>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null) return false;
            if (!entity.IptalEdildi) return true;
            entity.IptalEdildi = false;
            entity.IptalTarihi = null;
            entity.IptalAciklamasi = null;
            AddAudit(nameof(FinansGider), id, "Aktifleştirme", nameof(entity.IptalEdildi), true, false);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task ValidateExpenseReferencesAsync(FinansGiderKaydetModel model, CancellationToken cancellationToken)
        {
            if (model.ProjeId.HasValue && !string.IsNullOrWhiteSpace(model.ManuelProjeNo))
                throw new InvalidOperationException("Sistem projesi ile manuel proje aynı anda seçilemez.");
            if (!await _context.Set<FinansGiderKategori>().AnyAsync(x => x.Id == model.KategoriId && x.Aktif, cancellationToken))
                throw new InvalidOperationException("Gider kategorisi bulunamadı veya pasif.");
            if (model.GiderKalemiId.HasValue && !await _context.Set<FinansGiderKalemi>()
                    .AnyAsync(x => x.Id == model.GiderKalemiId && x.FinansGiderKategoriId == model.KategoriId && x.Aktif, cancellationToken))
                throw new InvalidOperationException("Gider kalemi kategoriyle eşleşmiyor veya pasif.");
            if (model.ProjeId.HasValue && !await _context.Set<Proje>().AnyAsync(x => x.Id == model.ProjeId, cancellationToken))
                throw new InvalidOperationException("Proje bulunamadı.");
        }

        private static void ApplyExpense(FinansGider entity, FinansGiderKaydetModel model)
        {
            var baseAmount = decimal.Round(model.Miktar * model.BirimFiyat, 2, MidpointRounding.AwayFromZero);
            decimal net;
            decimal vat;
            decimal gross;
            if (model.KdvDahil)
            {
                gross = baseAmount;
                net = model.KdvOrani <= 0 ? gross : decimal.Round(gross / (1 + model.KdvOrani / 100m), 2, MidpointRounding.AwayFromZero);
                vat = gross - net;
            }
            else
            {
                net = baseAmount;
                vat = decimal.Round(net * model.KdvOrani / 100m, 2, MidpointRounding.AwayFromZero);
                gross = net + vat;
            }
            entity.Tarih = model.Tarih;
            entity.FinansDonemi = FirstDayOfMonth(model.FinansDonemi);
            entity.FinansGiderKategoriId = model.KategoriId;
            entity.FinansGiderKalemiId = model.GiderKalemiId;
            entity.AltKategori = model.AltKategori?.Trim();
            entity.FirmaVeyaKisi = model.FirmaVeyaKisi?.Trim();
            entity.Aciklama = model.Aciklama.Trim();
            entity.Miktar = model.Miktar;
            entity.Birim = model.Birim.Trim();
            entity.BirimFiyat = model.BirimFiyat;
            entity.Tutar = baseAmount;
            entity.ParaBirimi = NormalizeCurrency(model.ParaBirimi);
            entity.KdvDahil = model.KdvDahil;
            entity.KdvOrani = model.KdvOrani;
            entity.Matrah = net;
            entity.KdvTutari = vat;
            entity.ToplamTutar = gross;
            entity.ProjeId = model.ProjeId;
            entity.ManuelProjeNo = model.ManuelProjeNo?.Trim();
            entity.IsTuru = model.IsTuru;
        }

        private async Task<FinansGiderModel> GetExpenseAsync(int id, CancellationToken cancellationToken)
        {
            var entity = await _context.Set<FinansGider>().AsNoTracking()
                .Include(x => x.Kategori).Include(x => x.GiderKalemi).Include(x => x.Proje)
                .FirstAsync(x => x.Id == id, cancellationToken);
            return MapExpense(entity);
        }

        private static FinansGiderModel MapExpense(FinansGider entity) => new()
        {
            Id = entity.Id,
            Tarih = entity.Tarih,
            FinansDonemi = entity.FinansDonemi,
            KategoriId = entity.FinansGiderKategoriId,
            Kategori = entity.Kategori.Ad,
            GiderKalemiId = entity.FinansGiderKalemiId,
            GiderKalemi = entity.GiderKalemi?.Ad,
            AltKategori = entity.AltKategori,
            FirmaVeyaKisi = entity.FirmaVeyaKisi,
            Aciklama = entity.Aciklama,
            Miktar = entity.Miktar,
            Birim = entity.Birim,
            BirimFiyat = entity.BirimFiyat,
            Tutar = entity.Tutar,
            ParaBirimi = entity.ParaBirimi,
            KdvDahil = entity.KdvDahil,
            KdvOrani = entity.KdvOrani,
            Matrah = entity.Matrah,
            KdvTutari = entity.KdvTutari,
            ToplamTutar = entity.ToplamTutar,
            ProjeId = entity.ProjeId,
            ProjeNo = entity.Proje?.ProjeNo ?? entity.ManuelProjeNo ?? string.Empty,
            IsTuru = entity.IsTuru,
            IptalEdildi = entity.IptalEdildi,
            IptalAciklamasi = entity.IptalAciklamasi
        };

        public async Task<IReadOnlyList<FinansGiderKategoriModel>> GiderKategorileriAsync(bool sadeceAktif, CancellationToken cancellationToken)
        {
            var query = _context.Set<FinansGiderKategori>().AsNoTracking();
            if (sadeceAktif) query = query.Where(x => x.Aktif);
            return await query.OrderBy(x => x.Ad).Select(x => new FinansGiderKategoriModel(x.Id, x.Ad, x.Aktif)).ToListAsync(cancellationToken);
        }

        public Task<FinansGiderKategoriModel> GiderKategoriOlusturAsync(FinansGiderKategoriKaydetModel model, CancellationToken cancellationToken)
            => ExecuteAtomicAsync(() => GiderKategoriOlusturCoreAsync(model, cancellationToken), cancellationToken);

        private async Task<FinansGiderKategoriModel> GiderKategoriOlusturCoreAsync(FinansGiderKategoriKaydetModel model, CancellationToken cancellationToken)
        {
            if (await _context.Set<FinansGiderKategori>().AnyAsync(x => x.Ad.ToLower() == model.Ad.Trim().ToLower(), cancellationToken))
                throw new InvalidOperationException("Aynı isimde gider kategorisi zaten var.");
            var entity = new FinansGiderKategori { Ad = model.Ad.Trim(), Aktif = model.Aktif };
            _context.Set<FinansGiderKategori>().Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            AddAudit(nameof(FinansGiderKategori), entity.Id, "Oluşturma", "*", null, entity.Ad);
            await _context.SaveChangesAsync(cancellationToken);
            return new FinansGiderKategoriModel(entity.Id, entity.Ad, entity.Aktif);
        }

        public Task<FinansGiderKategoriModel?> GiderKategoriGuncelleAsync(int id, FinansGiderKategoriKaydetModel model, CancellationToken cancellationToken)
            => ExecuteAtomicAsync(() => GiderKategoriGuncelleCoreAsync(id, model, cancellationToken), cancellationToken);

        private async Task<FinansGiderKategoriModel?> GiderKategoriGuncelleCoreAsync(int id, FinansGiderKategoriKaydetModel model, CancellationToken cancellationToken)
        {
            var entity = await _context.Set<FinansGiderKategori>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null) return null;
            if (await _context.Set<FinansGiderKategori>().AnyAsync(x => x.Id != id && x.Ad.ToLower() == model.Ad.Trim().ToLower(), cancellationToken))
                throw new InvalidOperationException("Aynı isimde gider kategorisi zaten var.");
            AddChangedAudit(nameof(FinansGiderKategori), id, nameof(entity.Ad), entity.Ad, model.Ad.Trim());
            AddChangedAudit(nameof(FinansGiderKategori), id, nameof(entity.Aktif), entity.Aktif, model.Aktif);
            entity.Ad = model.Ad.Trim();
            entity.Aktif = model.Aktif;
            await _context.SaveChangesAsync(cancellationToken);
            return new FinansGiderKategoriModel(entity.Id, entity.Ad, entity.Aktif);
        }

        public async Task<IReadOnlyList<FinansGiderKalemiModel>> GiderKalemleriAsync(int? kategoriId, bool sadeceAktif, CancellationToken cancellationToken)
        {
            var query = _context.Set<FinansGiderKalemi>().AsNoTracking();
            if (kategoriId.HasValue) query = query.Where(x => x.FinansGiderKategoriId == kategoriId);
            if (sadeceAktif) query = query.Where(x => x.Aktif);
            return await query.OrderBy(x => x.Ad)
                .Select(x => new FinansGiderKalemiModel(
                    x.Id, x.FinansGiderKategoriId, x.Kod, x.Ad, x.Aktif,
                    x.VarsayilanFirmaVeyaKisi, x.VarsayilanMiktar, x.VarsayilanBirim,
                    x.VarsayilanBirimFiyat, x.VarsayilanParaBirimi,
                    x.VarsayilanKdvDahil, x.VarsayilanKdvOrani))
                .ToListAsync(cancellationToken);
        }

        public Task<FinansGiderKalemiModel> GiderKalemiOlusturAsync(FinansGiderKalemiKaydetModel model, CancellationToken cancellationToken)
            => ExecuteAtomicAsync(() => GiderKalemiOlusturCoreAsync(model, cancellationToken), cancellationToken);

        private async Task<FinansGiderKalemiModel> GiderKalemiOlusturCoreAsync(FinansGiderKalemiKaydetModel model, CancellationToken cancellationToken)
        {
            if (!await _context.Set<FinansGiderKategori>().AnyAsync(x => x.Id == model.KategoriId && x.Aktif, cancellationToken))
                throw new InvalidOperationException("Gider kategorisi bulunamadı veya pasif.");
            var code = model.Kod.Trim().ToUpperInvariant();
            if (await _context.Set<FinansGiderKalemi>().AnyAsync(x => x.Kod == code, cancellationToken))
                throw new InvalidOperationException("Gider kalemi kodu benzersiz olmalıdır.");
            var entity = new FinansGiderKalemi
            {
                FinansGiderKategoriId = model.KategoriId,
                Kod = code,
                Ad = model.Ad.Trim(),
                Aktif = model.Aktif
            };
            ApplyExpenseItemDefaults(entity, model);
            _context.Set<FinansGiderKalemi>().Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            AddAudit(nameof(FinansGiderKalemi), entity.Id, "Oluşturma", "*", null, entity.Kod);
            await _context.SaveChangesAsync(cancellationToken);
            return MapExpenseItem(entity);
        }

        public Task<FinansGiderKalemiModel?> GiderKalemiGuncelleAsync(int id, FinansGiderKalemiKaydetModel model, CancellationToken cancellationToken)
            => ExecuteAtomicAsync(() => GiderKalemiGuncelleCoreAsync(id, model, cancellationToken), cancellationToken);

        private async Task<FinansGiderKalemiModel?> GiderKalemiGuncelleCoreAsync(int id, FinansGiderKalemiKaydetModel model, CancellationToken cancellationToken)
        {
            var entity = await _context.Set<FinansGiderKalemi>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null) return null;
            if (!await _context.Set<FinansGiderKategori>().AnyAsync(x => x.Id == model.KategoriId && x.Aktif, cancellationToken))
                throw new InvalidOperationException("Gider kategorisi bulunamadı veya pasif.");
            var code = model.Kod.Trim().ToUpperInvariant();
            if (await _context.Set<FinansGiderKalemi>().AnyAsync(x => x.Id != id && x.Kod == code, cancellationToken))
                throw new InvalidOperationException("Gider kalemi kodu benzersiz olmalıdır.");
            AddChangedAudit(nameof(FinansGiderKalemi), id, nameof(entity.FinansGiderKategoriId), entity.FinansGiderKategoriId, model.KategoriId);
            AddChangedAudit(nameof(FinansGiderKalemi), id, nameof(entity.Kod), entity.Kod, code);
            AddChangedAudit(nameof(FinansGiderKalemi), id, nameof(entity.Ad), entity.Ad, model.Ad.Trim());
            AddChangedAudit(nameof(FinansGiderKalemi), id, nameof(entity.Aktif), entity.Aktif, model.Aktif);
            entity.FinansGiderKategoriId = model.KategoriId;
            entity.Kod = code;
            entity.Ad = model.Ad.Trim();
            entity.Aktif = model.Aktif;
            var defaultsBefore = CaptureAuditState(entity);
            ApplyExpenseItemDefaults(entity, model);
            AddAuditChanges(nameof(FinansGiderKalemi), entity, defaultsBefore);
            await _context.SaveChangesAsync(cancellationToken);
            return MapExpenseItem(entity);
        }

        public Task<FinansGiderKalemiModel?> GideriKutuphaneyeKaydetAsync(
            int giderId,
            FinansGideriKutuphaneyeKaydetModel model,
            CancellationToken cancellationToken)
            => ExecuteAtomicAsync(() => GideriKutuphaneyeKaydetCoreAsync(giderId, model, cancellationToken), cancellationToken);

        private async Task<FinansGiderKalemiModel?> GideriKutuphaneyeKaydetCoreAsync(
            int giderId,
            FinansGideriKutuphaneyeKaydetModel model,
            CancellationToken cancellationToken)
        {
            var expense = await _context.Set<FinansGider>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == giderId, cancellationToken);
            if (expense is null) return null;

            return await GiderKalemiOlusturCoreAsync(new FinansGiderKalemiKaydetModel(
                expense.FinansGiderKategoriId,
                model.Kod,
                model.Ad,
                model.Aktif,
                expense.FirmaVeyaKisi,
                expense.Miktar,
                expense.Birim,
                expense.BirimFiyat,
                expense.ParaBirimi,
                expense.KdvDahil,
                expense.KdvOrani), cancellationToken);
        }

        private static void ApplyExpenseItemDefaults(FinansGiderKalemi entity, FinansGiderKalemiKaydetModel model)
        {
            entity.VarsayilanFirmaVeyaKisi = model.VarsayilanFirmaVeyaKisi?.Trim();
            entity.VarsayilanMiktar = model.VarsayilanMiktar;
            entity.VarsayilanBirim = model.VarsayilanBirim?.Trim();
            entity.VarsayilanBirimFiyat = model.VarsayilanBirimFiyat;
            entity.VarsayilanParaBirimi = string.IsNullOrWhiteSpace(model.VarsayilanParaBirimi)
                ? null
                : NormalizeCurrency(model.VarsayilanParaBirimi);
            entity.VarsayilanKdvDahil = model.VarsayilanKdvDahil;
            entity.VarsayilanKdvOrani = model.VarsayilanKdvOrani;
        }

        private static FinansGiderKalemiModel MapExpenseItem(FinansGiderKalemi entity)
            => new(
                entity.Id, entity.FinansGiderKategoriId, entity.Kod, entity.Ad, entity.Aktif,
                entity.VarsayilanFirmaVeyaKisi, entity.VarsayilanMiktar, entity.VarsayilanBirim,
                entity.VarsayilanBirimFiyat, entity.VarsayilanParaBirimi,
                entity.VarsayilanKdvDahil, entity.VarsayilanKdvOrani);
    }
}
