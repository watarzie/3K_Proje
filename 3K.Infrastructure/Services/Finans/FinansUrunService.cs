using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Exceptions;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>Finans ürünleri: güncel tarife, üretim kaydı eşleşmeleri ve kayıttan sonra fiyatsız işlerin yeniden fiyatlanması.</summary>
internal sealed class FinansUrunService(
    AppDbContext context,
    FinansIslemYurutucu islem,
    FinansDenetimKaydi denetim,
    FinansFiyatBelirleyici fiyatlar) : IFinansUrunService
{
    public async Task<FinansSayfaliSonuc<FinansUrunModel>> UrunlerSayfaliAsync(
        bool sadeceAktif,
        DateTime? tarifeTarihi,
        string? arama,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var tariffDate = (tarifeTarihi ?? TurkeyTime.Now).Date;
        var query = ApplyProductFilter(
            context.Set<FinansUrun>().AsNoTracking(), sadeceAktif, arama);
        var count = await query.CountAsync(cancellationToken);
        var (page, size, skip) = FinansSorgulari.NormalizePagination(pageNumber, pageSize, count, 100);
        var entities = await WithProductPageDetails(query)
            .AsSplitQuery()
            .OrderBy(x => x.Sira)
            .ThenBy(x => x.Ad)
            .ThenBy(x => x.Id)
            .Skip(skip)
            .Take(size)
            .ToListAsync(cancellationToken);

        return new FinansSayfaliSonuc<FinansUrunModel>
        {
            Items = entities.Select(x => MapProduct(x, tariffDate)).ToArray(),
            PageNumber = page,
            PageSize = size,
            TotalCount = count
        };
    }

    /// <summary>Ürünü eşleşmeleri ve bütün aktif tarife sürümleriyle yükler; güncel fiyat eşlemede seçilir.</summary>
    internal static IQueryable<FinansUrun> WithProductPageDetails(IQueryable<FinansUrun> query)
        => query
            .Include(x => x.Eslesmeler)
            .Include(x => x.FiyatTarifeleri.Where(tariff => tariff.Aktif));

    internal static IQueryable<FinansUrun> ApplyProductFilter(
        IQueryable<FinansUrun> query,
        bool sadeceAktif,
        string? arama)
    {
        if (sadeceAktif)
            query = query.Where(x => x.Aktif);
        if (string.IsNullOrWhiteSpace(arama))
            return query;

        var search = arama.Trim().ToLower();
        return query.Where(x =>
            x.Kod.ToLower().Contains(search) ||
            x.Ad.ToLower().Contains(search) ||
            x.Eslesmeler.Any(y =>
                (y.SandikAdi != null && y.SandikAdi.ToLower().Contains(search)) ||
                (y.SandikTipi != null && y.SandikTipi.ToLower().Contains(search))));
    }

    public Task<FinansUrunModel> UrunOlusturAsync(FinansUrunKaydetModel model, CancellationToken cancellationToken)
        => islem.AtomikAsync(() => UrunOlusturCoreAsync(model, cancellationToken), cancellationToken);

    private async Task<FinansUrunModel> UrunOlusturCoreAsync(FinansUrunKaydetModel model, CancellationToken cancellationToken)
    {
        var code = model.Kod.Trim().ToUpperInvariant();
        if (await context.Set<FinansUrun>().AnyAsync(x => x.Kod == code, cancellationToken))
            throw IsKuraliException.Cakisma("Finans ürün kodu benzersiz olmalıdır.");
        var entity = new FinansUrun
        {
            Kod = code,
            Ad = model.Ad.Trim(),
            FiyatlandirmaBirimi = model.FiyatlandirmaBirimi,
            Aktif = model.Aktif,
            Sira = model.Sira
        };
        foreach (var match in model.Eslesmeler)
            entity.Eslesmeler.Add(NewMatch(match));
        var newTariff = await ApplyDirectPriceAsync(entity, model, cancellationToken);
        context.Set<FinansUrun>().Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        denetim.Ekle(nameof(FinansUrun), entity.Id, "Oluşturma", "*", null, entity.Kod);
        if (newTariff is not null) AddTariffCreationAudit(newTariff);
        await context.SaveChangesAsync(cancellationToken);
        await BackfillUnpricedWorksAsync(cancellationToken);
        return await ProductByIdAsync(entity.Id, TurkeyTime.Now.Date, cancellationToken)
            ?? throw IsKuraliException.Cakisma("Kaydedilen finans ürünü tekrar okunamadı.");
    }

    public Task<FinansUrunModel?> UrunGuncelleAsync(int id, FinansUrunKaydetModel model, CancellationToken cancellationToken)
        => islem.AtomikAsync(() => UrunGuncelleCoreAsync(id, model, cancellationToken), cancellationToken);

    private async Task<FinansUrunModel?> UrunGuncelleCoreAsync(int id, FinansUrunKaydetModel model, CancellationToken cancellationToken)
    {
        var tariffDate = TurkeyTime.Now.Date;
        var entity = await WithProductPageDetails(
                context.Set<FinansUrun>().Where(x => x.Id == id))
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return null;
        var code = model.Kod.Trim().ToUpperInvariant();
        if (await context.Set<FinansUrun>().AnyAsync(x => x.Id != id && x.Kod == code, cancellationToken))
            throw IsKuraliException.Cakisma("Finans ürün kodu benzersiz olmalıdır.");
        var requestedExistingIds = model.Eslesmeler.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToHashSet();
        var actualIds = entity.Eslesmeler.Select(x => x.Id).ToHashSet();
        if (requestedExistingIds.Except(actualIds).Any())
            throw IsKuraliException.Cakisma("Ürün eşleşmelerinden en az biri bu ürüne ait değil veya bulunamadı.");
        var auditBefore = FinansDenetimKaydi.AnlikGoruntu(entity);
        entity.Kod = code;
        entity.Ad = model.Ad.Trim();
        entity.FiyatlandirmaBirimi = model.FiyatlandirmaBirimi;
        entity.Aktif = model.Aktif;
        entity.Sira = model.Sira;
        var newTariff = await ApplyDirectPriceAsync(entity, model, cancellationToken);
        denetim.DegisiklikleriEkle(nameof(FinansUrun), entity, auditBefore);

        var inputIds = model.Eslesmeler.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToHashSet();
        foreach (var existing in entity.Eslesmeler)
        {
            var input = model.Eslesmeler.FirstOrDefault(x => x.Id == existing.Id);
            if (input is null)
            {
                denetim.DegisiklikEkle(nameof(FinansUrunEslesmesi), existing.Id, nameof(existing.Aktif), existing.Aktif, false);
                existing.Aktif = false;
                continue;
            }
            var matchAuditBefore = FinansDenetimKaydi.AnlikGoruntu(existing);
            ApplyMatch(existing, input);
            denetim.DegisiklikleriEkle(nameof(FinansUrunEslesmesi), existing, matchAuditBefore);
        }
        var newMatches = model.Eslesmeler.Where(x => !x.Id.HasValue || !inputIds.Contains(x.Id.Value))
            .Select(NewMatch)
            .ToList();
        foreach (var newMatch in newMatches)
            entity.Eslesmeler.Add(newMatch);

        await context.SaveChangesAsync(cancellationToken);
        foreach (var newMatch in newMatches)
            denetim.Ekle(nameof(FinansUrunEslesmesi), newMatch.Id, "Oluşturma", "*", null, $"FinansUrunId={entity.Id}");
        if (newTariff is not null) AddTariffCreationAudit(newTariff);
        if (newMatches.Count > 0 || newTariff is not null)
            await context.SaveChangesAsync(cancellationToken);
        await BackfillUnpricedWorksAsync(cancellationToken);
        return await ProductByIdAsync(id, tariffDate, cancellationToken);
    }

    private async Task<FinansUrunModel?> ProductByIdAsync(
        int id,
        DateTime tariffDate,
        CancellationToken cancellationToken)
    {
        var entity = await BuildProductByIdQuery(
                context.Set<FinansUrun>().AsNoTracking(), id)
            .AsSplitQuery()
            .SingleOrDefaultAsync(cancellationToken);
        return entity is null ? null : MapProduct(entity, tariffDate);
    }

    internal static IQueryable<FinansUrun> BuildProductByIdQuery(
        IQueryable<FinansUrun> query,
        int id)
        => WithProductPageDetails(query.Where(x => x.Id == id));

    public Task<bool> UrunPasiflestirAsync(int id, CancellationToken cancellationToken)
        => islem.AtomikAsync(() => UrunPasiflestirCoreAsync(id, cancellationToken), cancellationToken);

    private async Task<bool> UrunPasiflestirCoreAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await context.Set<FinansUrun>().Include(x => x.Eslesmeler)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;
        if (!entity.Aktif) return true;
        entity.Aktif = false;
        foreach (var match in entity.Eslesmeler) match.Aktif = false;
        denetim.Ekle(nameof(FinansUrun), id, "Pasifleştirme", nameof(entity.Aktif), true, false);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<FinansYenidenFiyatlamaSonucModel?> YenidenFiyatlaAsync(int id, string aciklama, CancellationToken cancellationToken)
        => islem.AtomikAsync(() => YenidenFiyatlaCoreAsync(id, aciklama, cancellationToken), cancellationToken);

    /// <summary>
    /// D4: tarife değişikliği açık kalemleri kendiliğinden değiştirmez. Bu işlem ürüne bağlı, hiç PO'ya bağlanmamış,
    /// fiyatı elle girilmemiş aktif üretim kalemlerini finans tarihlerinde geçerli tarife sürümüyle yeniden fiyatlar.
    /// Manuel ve düzenli işlerin fiyatı korunur; tarihinde tarifesi olmayan kalem değişmez. Her değişiklik denetime yazılır.
    /// </summary>
    private async Task<FinansYenidenFiyatlamaSonucModel?> YenidenFiyatlaCoreAsync(int id, string aciklama, CancellationToken cancellationToken)
    {
        FinansGerekceKurali.Dogrula(aciklama);
        var urun = await context.Set<FinansUrun>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (urun is null) return null;
        var works = await context.IsKaydiDetayQuery(true)
            .Where(x => x.FinansUrunId == id && x.KaynakAktif && !x.IptalEdildi && !x.FinansMiktariManuel && !x.ManuelNetTutar.HasValue &&
                        x.KaynakTuru != FinansKaynakTurleri.Manuel && x.KaynakTuru != FinansKaynakTurleri.DuzenliIs &&
                        !x.SiparisKalemleri.Any())
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var fiyatlanan = 0;
        foreach (var work in works)
        {
            var tariff = await fiyatlar.GecerliTarifeAsync(id, work.FinansTarihi, cancellationToken);
            if (tariff is null || (work.TarifeIdSnapshot == tariff.Id && work.BirimFiyatSnapshot == tariff.BirimFiyat &&
                                   work.ParaBirimiSnapshot == tariff.ParaBirimi && work.KdvOraniSnapshot == tariff.KdvOrani &&
                                   work.FiyatlandirmaBirimiSnapshot == urun.FiyatlandirmaBirimi))
                continue;
            var before = FinansDenetimKaydi.AnlikGoruntu(work);
            work.FiyatlandirmaBirimiSnapshot = urun.FiyatlandirmaBirimi;
            work.BirimFiyatSnapshot = tariff.BirimFiyat;
            work.ParaBirimiSnapshot = tariff.ParaBirimi;
            work.KdvOraniSnapshot = tariff.KdvOrani;
            work.TarifeYiliSnapshot = tariff.Yil;
            work.TarifeIdSnapshot = tariff.Id;
            work.Durum = FinansTutarKurallari.Durum(work);
            denetim.Ekle(nameof(FinansIsKaydi), work.Id, "Yeniden Fiyatlandırma", nameof(work.TarifeIdSnapshot),
                before[nameof(work.TarifeIdSnapshot)], tariff.Id, aciklama.Trim());
            denetim.DegisiklikleriEkle(nameof(FinansIsKaydi), work, before);
            fiyatlanan++;
        }
        await context.SaveChangesAsync(cancellationToken);
        return new FinansYenidenFiyatlamaSonucModel(id, works.Count, fiyatlanan);
    }

    private static FinansUrunEslesmesi NewMatch(FinansUrunEslesmesiModel model)
    {
        var entity = new FinansUrunEslesmesi();
        ApplyMatch(entity, model);
        return entity;
    }

    /// <summary>
    /// Ürün formundaki fiyatı tarife sürümü olarak uygular (<see cref="FinansTarifeKurallari"/>). Aynı gün
    /// başlayan sürüm, onunla fiyatlanmış iş kaydı varsa düzeltilmez; geçmiş fiyat snapshot'ı korunur.
    /// Yeni sürüm, kimlik aldıktan sonra oluşturma denetimi yazılabilsin diye döndürülür.
    /// </summary>
    private async Task<FinansFiyatTarifesi?> ApplyDirectPriceAsync(
        FinansUrun entity,
        FinansUrunKaydetModel model,
        CancellationToken cancellationToken)
    {
        var plan = FiyatPlani(entity, model);
        if (plan is null) return null;
        await FinansSorgulari.ParaBirimiSecimiAsync(context, model.ParaBirimi ?? "EUR",
            entity.FiyatTarifeleri.Where(x => x.Aktif).Select(x => x.ParaBirimi), cancellationToken);
        if (plan.Islem == FinansTarifeIslemi.AyniGunSurumunuDuzelt && plan.Mevcut!.Id > 0 &&
            await context.Set<FinansIsKaydi>().AnyAsync(x => x.TarifeIdSnapshot == plan.Mevcut.Id, cancellationToken))
            throw IsKuraliException.Cakisma(
                "Bu tarih için geçerli tarife sürümüyle fiyatlanmış kayıtlar var; geçmiş fiyat değiştirilemez. " +
                "Yeni fiyatı sonraki bir geçerlilik tarihiyle girin.");
        return PlaniUygula(entity, plan, model);
    }

    /// <summary>Fiyat alanlarını doğrular ve sürüm planını çıkarır; fiyat gönderilmediyse null döner.</summary>
    internal static FinansTarifePlani? FiyatPlani(FinansUrun entity, FinansUrunKaydetModel model)
    {
        if (!model.BirimFiyat.HasValue)
            return null;
        if (model.BirimFiyat.Value < 0)
            throw IsKuraliException.Cakisma("Birim fiyat sıfırdan küçük olamaz.");
        if (model.KdvOrani is < 0 or > 100)
            throw IsKuraliException.Cakisma("KDV oranı 0-100 aralığında olmalıdır.");
        var effective = (model.GecerlilikBaslangici ?? TurkeyTime.Now).Date;
        if (!FinansTarihKurallari.GecerliTarih(effective))
            throw IsKuraliException.Cakisma("Tarife geçerlilik başlangıcı geçerli bir tarih olmalıdır.");
        return FinansTarifeKurallari.Planla(entity.FiyatTarifeleri, effective, model.BirimFiyat.Value,
            FinansParaBirimiKurali.Normalize(model.ParaBirimi ?? "EUR"), model.KdvOrani ?? 0m);
    }

    /// <summary>Sürüm planını ürün tarifelerine uygular ve değişiklikleri denetime yazar.</summary>
    internal FinansFiyatTarifesi? PlaniUygula(FinansUrun entity, FinansTarifePlani plan, FinansUrunKaydetModel model)
    {
        var price = model.BirimFiyat!.Value;
        var currency = FinansParaBirimiKurali.Normalize(model.ParaBirimi ?? "EUR");
        var vat = model.KdvOrani ?? 0m;
        switch (plan.Islem)
        {
            case FinansTarifeIslemi.DegisiklikYok:
                return null;
            case FinansTarifeIslemi.AyniGunSurumunuDuzelt:
                var tariff = plan.Mevcut!;
                denetim.DegisiklikEkle(nameof(FinansFiyatTarifesi), tariff.Id, nameof(tariff.BirimFiyat), tariff.BirimFiyat, price);
                denetim.DegisiklikEkle(nameof(FinansFiyatTarifesi), tariff.Id, nameof(tariff.ParaBirimi), tariff.ParaBirimi, currency);
                denetim.DegisiklikEkle(nameof(FinansFiyatTarifesi), tariff.Id, nameof(tariff.KdvOrani), tariff.KdvOrani, vat);
                tariff.BirimFiyat = price;
                tariff.ParaBirimi = currency;
                tariff.KdvOrani = vat;
                return null;
            case FinansTarifeIslemi.SurumuKapatVeYeniAc:
                var closing = plan.Mevcut!;
                var newEnd = plan.YeniBaslangic.AddDays(-1);
                denetim.DegisiklikEkle(nameof(FinansFiyatTarifesi), closing.Id, nameof(closing.GecerlilikBitisi), closing.GecerlilikBitisi, newEnd);
                closing.GecerlilikBitisi = newEnd;
                break;
        }
        var created = new FinansFiyatTarifesi
        {
            Yil = plan.YeniBaslangic.Year,
            GecerlilikBaslangici = plan.YeniBaslangic,
            GecerlilikBitisi = plan.YeniBitis,
            Aktif = true,
            BirimFiyat = price,
            ParaBirimi = currency,
            KdvOrani = vat
        };
        entity.FiyatTarifeleri.Add(created);
        return created;
    }

    private void AddTariffCreationAudit(FinansFiyatTarifesi tariff)
        => denetim.Ekle(nameof(FinansFiyatTarifesi), tariff.Id, "Oluşturma", "*", null,
            $"FinansUrunId={tariff.FinansUrunId}; {FinansDenetimKaydi.DegerMetni(tariff.BirimFiyat)} {tariff.ParaBirimi}; KDV {FinansDenetimKaydi.DegerMetni(tariff.KdvOrani)}");

    private static void ApplyMatch(FinansUrunEslesmesi entity, FinansUrunEslesmesiModel model)
    {
        entity.IsTuru = model.IsTuru;
        entity.SandikAdi = model.SandikAdi?.Trim();
        entity.SandikTipi = model.SandikTipi?.Trim();
        entity.Boy = model.Boy;
        entity.En = model.En;
        entity.Yukseklik = model.Yukseklik;
        entity.IcSandikSablonId = model.IcSandikSablonId;
        entity.Aktif = model.Aktif;
    }

    private static FinansUrunModel MapProduct(FinansUrun entity, DateTime tariffDate)
    {
        var tariff = entity.FiyatTarifeleri.Where(x => x.Aktif && x.GecerlilikBaslangici.Date <= tariffDate && x.GecerlilikBitisi.Date >= tariffDate)
            .OrderByDescending(x => x.GecerlilikBaslangici).ThenByDescending(x => x.Id).FirstOrDefault();
        return new FinansUrunModel
        {
            Id = entity.Id,
            Kod = entity.Kod,
            Ad = entity.Ad,
            FiyatlandirmaBirimi = entity.FiyatlandirmaBirimi,
            Aktif = entity.Aktif,
            Sira = entity.Sira,
            GuncelBirimFiyat = tariff?.BirimFiyat,
            GuncelParaBirimi = tariff?.ParaBirimi,
            GuncelKdvOrani = tariff?.KdvOrani,
            GuncelGecerlilikBaslangici = tariff?.GecerlilikBaslangici,
            GuncelGecerlilikBitisi = tariff?.GecerlilikBitisi,
            Tarifeler = entity.FiyatTarifeleri.Where(x => x.Aktif)
                .OrderBy(x => x.GecerlilikBaslangici).ThenBy(x => x.Id)
                .Select(x => new FinansTarifeSurumuModel(x.Id, x.GecerlilikBaslangici, x.GecerlilikBitisi, x.BirimFiyat, x.ParaBirimi, x.KdvOrani))
                .ToArray(),
            Eslesmeler = entity.Eslesmeler.OrderBy(x => x.Id).Select(x => new FinansUrunEslesmesiModel(
                x.Id, x.IsTuru, x.SandikAdi, x.SandikTipi, x.Boy, x.En, x.Yukseklik, x.IcSandikSablonId, x.Aktif)).ToArray()
        };
    }

    /// <summary>
    /// Ürün kayıt işlemlerinin transaction'ı içinde, üretimden gelen fiyatsız ve siparişsiz işleri
    /// finans tarihlerinde geçerli tarife sürümüyle fiyatlar (F12). Fiyatı olan işler yeniden
    /// fiyatlanmaz; manuel ve düzenli işlerin elle girilen fiyat/para birimi bilgisi korunur.
    /// </summary>
    private async Task BackfillUnpricedWorksAsync(CancellationToken cancellationToken)
    {
        var works = await context.IsKaydiDetayQuery(true)
            .Where(x => x.KaynakAktif && !x.IptalEdildi && !x.FinansMiktariManuel &&
                        x.KaynakTuru != FinansKaynakTurleri.Manuel && x.KaynakTuru != FinansKaynakTurleri.DuzenliIs &&
                        x.BirimFiyatSnapshot <= 0 && !x.TarifeYiliSnapshot.HasValue &&
                        !x.SiparisKalemleri.Any())
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
        foreach (var work in works)
        {
            var oldProduct = work.FinansUrunId;
            var oldUnit = work.FiyatlandirmaBirimiSnapshot;
            var oldPrice = work.BirimFiyatSnapshot;
            var oldCurrency = work.ParaBirimiSnapshot;
            var oldVat = work.KdvOraniSnapshot;
            var oldYear = work.TarifeYiliSnapshot;
            await fiyatlar.OtomatikFiyatUygulaAsync(work, cancellationToken);
            denetim.DegisiklikEkle(nameof(FinansIsKaydi), work.Id, nameof(work.FinansUrunId), oldProduct, work.FinansUrunId);
            denetim.DegisiklikEkle(nameof(FinansIsKaydi), work.Id, nameof(work.FiyatlandirmaBirimiSnapshot), oldUnit, work.FiyatlandirmaBirimiSnapshot);
            denetim.DegisiklikEkle(nameof(FinansIsKaydi), work.Id, nameof(work.BirimFiyatSnapshot), oldPrice, work.BirimFiyatSnapshot);
            denetim.DegisiklikEkle(nameof(FinansIsKaydi), work.Id, nameof(work.ParaBirimiSnapshot), oldCurrency, work.ParaBirimiSnapshot);
            denetim.DegisiklikEkle(nameof(FinansIsKaydi), work.Id, nameof(work.KdvOraniSnapshot), oldVat, work.KdvOraniSnapshot);
            denetim.DegisiklikEkle(nameof(FinansIsKaydi), work.Id, nameof(work.TarifeYiliSnapshot), oldYear, work.TarifeYiliSnapshot);
        }
        await context.SaveChangesAsync(cancellationToken);
    }
}
