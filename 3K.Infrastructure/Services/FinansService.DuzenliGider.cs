using System.Data;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Models;
using _3K.Core.Exceptions;

namespace _3K.Infrastructure.Services
{
    public sealed partial class FinansService
    {
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

        public Task<FinansDuzenliIsModel?> DuzenliIsGuncelleAsync(
            int id,
            FinansDuzenliIsKaydetModel model,
            CancellationToken cancellationToken)
            => ExecuteAtomicAsync(() => DuzenliIsGuncelleCoreAsync(id, model, cancellationToken), cancellationToken);

        private async Task<FinansDuzenliIsModel?> DuzenliIsGuncelleCoreAsync(
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

        /// <summary>
        /// Aktif düzenli işlerin referans tarihine kadarki dönem kayıtlarını üretir. Her şablon kendi
        /// transaction'ında işlenir: iş kuralına takılan şablon (ör. çakışan tarife) atlanıp sonuçta
        /// raporlanır, diğer şablonların üretimini engellemez. Aynı dönem anahtarı iki kez üretilmez.
        /// </summary>
        public async Task<FinansDonemOlusturSonucModel> DuzenliIsDonemiOlusturAsync(DateTime referansTarihi, CancellationToken cancellationToken)
        {
            if (!FinansTarihKurallari.GecerliTarih(referansTarihi)) throw IsKuraliException.Cakisma("Geçerli referans tarihi girin.");
            var templates = await _context.Set<FinansDuzenliIs>().AsNoTracking().Include(x => x.Proje).Include(x => x.FinansUrun)
                .Where(x => x.Aktif && x.BaslangicTarihi <= referansTarihi.Date).OrderBy(x => x.Id).ToListAsync(cancellationToken);
            var created = 0;
            var hatalar = new List<string>();
            foreach (var template in templates)
            {
                try
                {
                    created += await ExecuteAtomicAsync(
                        () => DuzenliIsDonemleriniOlusturAsync(template, referansTarihi, cancellationToken), cancellationToken);
                }
                catch (IsKuraliException exception)
                {
                    // Geri alınan şablonun izlenen kayıtları sonraki şablonun kaydına karışmasın.
                    _context.ChangeTracker.Clear();
                    hatalar.Add($"#{template.Id} {template.IsAdi}: {exception.Message}");
                }
            }
            return new FinansDonemOlusturSonucModel(templates.Count, created, referansTarihi) { Hatalar = hatalar };
        }

        private async Task<int> DuzenliIsDonemleriniOlusturAsync(
            FinansDuzenliIs template,
            DateTime referansTarihi,
            CancellationToken cancellationToken)
        {
            var created = 0;
            var final = template.BitisTarihi.HasValue && template.BitisTarihi.Value < referansTarihi ? template.BitisTarihi.Value : referansTarihi;
            foreach (var due in DuzenliIsTarihleri(template.BaslangicTarihi, final, template.OlusturmaGunu))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var period = FirstDayOfMonth(due);
                var sourceId = $"{template.Id}:{period:yyyyMM}";
                if (await _context.Set<FinansIsKaydi>().AnyAsync(x => x.KaynakTuru == "DuzenliIs" && x.KaynakKayitId == sourceId, cancellationToken) ||
                    await _context.Set<FinansKaynakBastirma>().AnyAsync(x => x.KaynakTuru == "DuzenliIs" && x.KaynakKayitId == sourceId, cancellationToken))
                    continue;
                var product = template.FinansUrun;
                var pricing = product?.FiyatlandirmaBirimi ?? (template.HesaplamaYontemi is FinansHesaplamaYontemi.SabitAylik or FinansHesaplamaYontemi.DegiskenTutar
                    ? FinansFiyatlandirmaBirimi.SabitTutar : template.HesaplamaYontemi == FinansHesaplamaYontemi.Metrekup ? FinansFiyatlandirmaBirimi.Metrekup : FinansFiyatlandirmaBirimi.Adet);
                var quantity = FinansMiktarKurallari.DuzenliIsMiktari(pricing, template.Miktar);
                var tariff = product is null ? null : await FindTariffAsync(product.Id, due, cancellationToken);
                var work = new FinansIsKaydi
                {
                    ProjeId = template.ProjeId, ProjeNo = template.Proje?.ProjeNo ?? template.ManuelProjeNo ?? "BAĞIMSIZ",
                    Musteri = template.Proje?.Musteri ?? template.ManuelProjeAdi ?? template.Musteri, ManuelProjeMi = !template.ProjeId.HasValue,
                    IsTuru = template.IsTuru, IsAdi = template.IsAdi, OzelIsTuru = template.OzelIsTuru, HesaplamaYontemi = template.HesaplamaYontemi,
                    RaporGrubu = template.RaporGrubu, Aciklama = template.Aciklama, Adet = quantity.Adet,
                    Birim = pricing == FinansFiyatlandirmaBirimi.Metrekup ? "m³" : template.Birim,
                    BirimM3 = quantity.M3, ToplamM3 = quantity.M3, FinansUrunId = template.FinansUrunId,
                    UretimTarihi = due, FinansTarihi = due, FinansDonemi = period, KayitTarihi = TurkeyTime.Now,
                    KaynakTuru = "DuzenliIs", KaynakKayitId = sourceId, KaynakAktif = true, DuzenliIsId = template.Id,
                    FiyatlandirmaBirimiSnapshot = pricing, BirimFiyatSnapshot = tariff?.BirimFiyat ?? template.BirimFiyat,
                    ParaBirimiSnapshot = tariff?.ParaBirimi ?? template.ParaBirimi, KdvOraniSnapshot = tariff?.KdvOrani ?? template.KdvOrani,
                    TarifeYiliSnapshot = tariff?.Yil, TarifeIdSnapshot = tariff?.Id
                };
                _context.Add(work);
                await _context.SaveChangesAsync(cancellationToken);
                AddAudit(nameof(FinansIsKaydi), work.Id, "Dönem Oluşturma", "*", null, sourceId);
                created++;
            }
            await _context.SaveChangesAsync(cancellationToken);
            return created;
        }

        internal static IReadOnlyList<DateTime> DuzenliIsTarihleri(DateTime start, DateTime end, int day)
        {
            if (day is < 1 or > 31 || !FinansTarihKurallari.GecerliTarih(start) || !FinansTarihKurallari.GecerliTarih(end))
                throw IsKuraliException.Cakisma("Düzenli iş başlangıç/bitiş ve gününü kontrol edin.");
            var result = new List<DateTime>();
            for (var month = FirstDayOfMonth(start); month <= FirstDayOfMonth(end); month = month.AddMonths(1))
            {
                var due = month.AddDays(Math.Min(day, DateTime.DaysInMonth(month.Year, month.Month)) - 1);
                if (due < start.Date) due = start.Date;
                if (due <= end.Date) result.Add(due);
            }
            return result;
        }

        private async Task ValidateRecurringReferencesAsync(FinansDuzenliIsKaydetModel model, CancellationToken cancellationToken)
        {
            await ResolveProjectAsync(model.ProjeId, model.ManuelProjeNo, model.ManuelProjeAdi, model.Musteri, cancellationToken);
            if (!model.FinansUrunId.HasValue) return;
            if (!await _context.Set<FinansUrun>().AnyAsync(x => x.Id == model.FinansUrunId && x.Aktif, cancellationToken))
                throw IsKuraliException.Cakisma("Seçilen finans ürünü bulunamadı veya pasif.");
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
            query = ExpenseFilter(query, filtre);
            var count = await query.CountAsync(cancellationToken);
            var (page, size, skip) = NormalizePagination(
                filtre.PageNumber, filtre.PageSize, count, 250);
            var totals = await query
                .Where(x => !x.IptalEdildi && !x.AvansMi)
                .GroupBy(x => x.ParaBirimi)
                .Select(x => new FinansParaToplamiModel("", 0, 0, 0) { ParaBirimi = x.Key, NetTutar = x.Sum(y => y.Matrah), KdvTutari = x.Sum(y => y.KdvTutari), ToplamTutar = x.Sum(y => y.ToplamTutar) })
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

        public Task<FinansGiderModel?> GiderGuncelleAsync(int id, FinansGiderKaydetModel model, CancellationToken cancellationToken)
            => ExecuteAtomicAsync(() => GiderGuncelleCoreAsync(id, model, cancellationToken), cancellationToken);

        private async Task<FinansGiderModel?> GiderGuncelleCoreAsync(int id, FinansGiderKaydetModel model, CancellationToken cancellationToken)
        {
            var entity = await _context.Set<FinansGider>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null) return null;
            if (entity.IptalEdildi) throw IsKuraliException.Cakisma("İptal edilmiş gider güncellenemez.");
            await ValidateExpenseReferencesAsync(model, cancellationToken);
            var auditBefore = CaptureAuditState(entity);
            ApplyExpense(entity, model);
            AddAuditChanges(nameof(FinansGider), entity, auditBefore);
            await _context.SaveChangesAsync(cancellationToken);
            return await GetExpenseAsync(id, cancellationToken);
        }

        public Task<bool> GiderIptalAsync(int id, string aciklama, CancellationToken cancellationToken)
            => ExecuteAtomicAsync(() => GiderIptalCoreAsync(id, aciklama, cancellationToken), cancellationToken);

        private async Task<bool> GiderIptalCoreAsync(int id, string aciklama, CancellationToken cancellationToken)
        {
            Gerekce(aciklama);
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

        private async Task ValidateExpenseReferencesAsync(FinansGiderKaydetModel model, CancellationToken cancellationToken)
        {
            if (model.Miktar <= 0 || model.BirimFiyat < 0 || model.KdvOrani is < 0 or > 100 || !FinansTarihKurallari.GecerliTarih(model.Tarih) ||
                !FinansTarihKurallari.GecerliTarih(model.FinansDonemi) || (model.FinansTarihi.HasValue && !FinansTarihKurallari.GecerliTarih(model.FinansTarihi.Value)))
                throw IsKuraliException.Cakisma("Gider miktarı, fiyatı, KDV oranı ve tarihlerini kontrol edin.");
            if (NormalizeCurrency(model.ParaBirimi) is not ("EUR" or "USD" or "TRY")) throw IsKuraliException.Cakisma("Gider para birimi EUR, USD veya TRY olmalıdır.");
            if (model.MahsupEdilenAvansId.HasValue)
            {
                var advance = await _context.Set<FinansGider>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.MahsupEdilenAvansId, cancellationToken);
                if (model.AvansMi || advance is null || !advance.AvansMi || advance.IptalEdildi || advance.ParaBirimi != NormalizeCurrency(model.ParaBirimi) || advance.FirmaVeyaKisi != model.FirmaVeyaKisi?.Trim())
                    throw IsKuraliException.Cakisma("Avans mahsubu aynı kişi/para birimindeki aktif avansa bağlanmalıdır; avansın kendisi maliyete dahil edilmez.");
            }
            if (model.ProjeId.HasValue && !string.IsNullOrWhiteSpace(model.ManuelProjeNo))
                throw IsKuraliException.Cakisma("Sistem projesi ile manuel proje aynı anda seçilemez.");
            if (!await _context.Set<FinansGiderKategori>().AnyAsync(x => x.Id == model.KategoriId && x.Aktif, cancellationToken))
                throw IsKuraliException.Cakisma("Gider kategorisi bulunamadı veya pasif.");
            if (model.GiderKalemiId.HasValue && !await _context.Set<FinansGiderKalemi>()
                    .AnyAsync(x => x.Id == model.GiderKalemiId && x.FinansGiderKategoriId == model.KategoriId && x.Aktif, cancellationToken))
                throw IsKuraliException.Cakisma("Gider kalemi kategoriyle eşleşmiyor veya pasif.");
            if (model.ProjeId.HasValue && !await _context.Set<Proje>().AnyAsync(x => x.Id == model.ProjeId, cancellationToken))
                throw IsKuraliException.Cakisma("Proje bulunamadı.");
        }

        private static void ApplyExpense(FinansGider entity, FinansGiderKaydetModel model)
        {
            var baseAmount = FinansTutarKurallari.Para(model.Miktar * model.BirimFiyat);
            var amount = model.KdvDahil
                ? FinansTutarKurallari.KdvDahilHesapla(baseAmount, model.KdvOrani)
                : FinansTutarKurallari.Hesapla(model.Miktar, model.BirimFiyat, model.KdvOrani);
            entity.Tarih = model.Tarih;
            entity.FinansTarihi = (model.FinansTarihi ?? (FirstDayOfMonth(model.Tarih) == FirstDayOfMonth(model.FinansDonemi) ? model.Tarih : model.FinansDonemi)).Date;
            entity.FinansDonemi = FirstDayOfMonth(entity.FinansTarihi);
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
            entity.Matrah = amount.Net;
            entity.KdvTutari = amount.Kdv;
            entity.ToplamTutar = amount.Toplam;
            entity.ProjeId = model.ProjeId;
            entity.ManuelProjeNo = model.ManuelProjeNo?.Trim();
            entity.IsTuru = model.IsTuru;
            entity.BelgeNo = model.BelgeNo?.Trim();
            entity.AvansMi = model.AvansMi;
            entity.MahsupEdilenAvansId = model.MahsupEdilenAvansId;
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
            FinansTarihi = entity.FinansTarihi,
            BelgeNo = entity.BelgeNo,
            AvansMi = entity.AvansMi,
            MahsupEdilenAvansId = entity.MahsupEdilenAvansId,
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
                throw IsKuraliException.Cakisma("Aynı isimde gider kategorisi zaten var.");
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
                throw IsKuraliException.Cakisma("Aynı isimde gider kategorisi zaten var.");
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
                throw IsKuraliException.Cakisma("Gider kategorisi bulunamadı veya pasif.");
            var code = model.Kod.Trim().ToUpperInvariant();
            if (await _context.Set<FinansGiderKalemi>().AnyAsync(x => x.Kod == code, cancellationToken))
                throw IsKuraliException.Cakisma("Gider kalemi kodu benzersiz olmalıdır.");
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
                throw IsKuraliException.Cakisma("Gider kategorisi bulunamadı veya pasif.");
            var code = model.Kod.Trim().ToUpperInvariant();
            if (await _context.Set<FinansGiderKalemi>().AnyAsync(x => x.Id != id && x.Kod == code, cancellationToken))
                throw IsKuraliException.Cakisma("Gider kalemi kodu benzersiz olmalıdır.");
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
