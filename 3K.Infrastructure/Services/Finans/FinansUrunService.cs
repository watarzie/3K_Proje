using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
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

    public async Task<IReadOnlyList<FinansUrunSecenekModel>> UrunSecenekleriAsync(
        string? arama,
        FinansIsTuru? isTuru,
        DateTime? tarih,
        CancellationToken cancellationToken)
    {
        var gun = (tarih ?? TurkeyTime.Now).Date;
        var query = context.Set<FinansUrun>().AsNoTracking().Where(x => x.Aktif);
        if (isTuru.HasValue)
            query = query.Where(x => x.Eslesmeler.Any(e => e.Aktif && e.IsTuru == isTuru));
        if (!string.IsNullOrWhiteSpace(arama))
        {
            var desen = FinansSorgulari.LiteralSearchPattern(arama);
            query = query.Where(x => EF.Functions.ILike(x.Kod, desen, "\\") || EF.Functions.ILike(x.Ad, desen, "\\"));
        }
        var satirlar = await query.OrderBy(x => x.Sira).ThenBy(x => x.Ad)
            .Take(50)
            .Select(x => new
            {
                x.Id, x.Kod, x.Ad, x.FiyatlandirmaBirimi,
                Tarife = x.FiyatTarifeleri
                    .Where(t => t.Aktif && t.GecerlilikBaslangici.Date <= gun && t.GecerlilikBitisi.Date >= gun)
                    .OrderByDescending(t => t.GecerlilikBaslangici).ThenByDescending(t => t.Id)
                    .Select(t => new { t.BirimFiyat, t.ParaBirimi, t.KdvOrani })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
        return satirlar.Select(x => new FinansUrunSecenekModel(x.Id, x.Kod, x.Ad, x.FiyatlandirmaBirimi,
            x.Tarife?.BirimFiyat, x.Tarife?.ParaBirimi, x.Tarife?.KdvOrani)).ToArray();
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
            .Where(x => (x.FinansUrunId == id || x.AhsapFinansUrunId == id) && x.KaynakAktif && !x.IptalEdildi && !x.FinansMiktariManuel && !x.ManuelNetTutar.HasValue &&
                        x.KaynakTuru != FinansKaynakTurleri.Manuel && x.KaynakTuru != FinansKaynakTurleri.DuzenliIs &&
                        !x.SiparisKalemleri.Any())
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var fiyatlanan = 0;
        foreach (var work in works)
        {
            var tariff = await fiyatlar.GecerliTarifeAsync(id, work.FinansTarihi, cancellationToken);
            if (work.AhsapFinansUrunId == id && work.FinansUrunId != id)
            {
                // Malzemeli işin ahşap tarafı: yalnız ahşap fiyatı yenilenir (para birimi kontra tarafıyla aynı olmalı).
                if (tariff is null || work.AhsapTarifeIdSnapshot == tariff.Id && work.AhsapBirimFiyatSnapshot == tariff.BirimFiyat ||
                    !string.Equals(tariff.ParaBirimi, work.ParaBirimiSnapshot, StringComparison.OrdinalIgnoreCase))
                    continue;
                var ahsapOnce = FinansDenetimKaydi.AnlikGoruntu(work);
                work.AhsapBirimFiyatSnapshot = tariff.BirimFiyat;
                work.AhsapTarifeIdSnapshot = tariff.Id;
                work.Durum = FinansTutarKurallari.Durum(work);
                denetim.Ekle(nameof(FinansIsKaydi), work.Id, "Yeniden Fiyatlandırma", nameof(work.AhsapTarifeIdSnapshot),
                    ahsapOnce[nameof(work.AhsapTarifeIdSnapshot)], tariff.Id, aciklama.Trim());
                denetim.DegisiklikleriEkle(nameof(FinansIsKaydi), work, ahsapOnce);
                fiyatlanan++;
                continue;
            }
            if (tariff is null || (work.TarifeIdSnapshot == tariff.Id && work.BirimFiyatSnapshot == tariff.BirimFiyat &&
                                   work.ParaBirimiSnapshot == tariff.ParaBirimi && work.KdvOraniSnapshot == tariff.KdvOrani &&
                                   work.FiyatlandirmaBirimiSnapshot == urun.FiyatlandirmaBirimi))
                continue;
            var before = FinansDenetimKaydi.AnlikGoruntu(work);
            // Malzemeli işte kontra tarafı her zaman m³ ile fiyatlanır; birim ürünün biriminden alınmaz.
            work.FiyatlandirmaBirimiSnapshot = work.AhsapBirimFiyatSnapshot.HasValue ? FinansFiyatlandirmaBirimi.Metrekup : urun.FiyatlandirmaBirimi;
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

    public async Task<FinansTarifeDuzeltmeOnizlemeModel?> TarifeDuzeltmeOnizleAsync(
        int urunId,
        int tarifeId,
        decimal birimFiyat,
        decimal kdvOrani,
        CancellationToken cancellationToken)
    {
        var plan = await TarifeDuzeltmePlaniAsync(urunId, tarifeId, tracking: false, cancellationToken);
        if (plan is null) return null;
        var (urun, surum, kalemler) = plan.Value;
        FinansTarifeDuzeltmeKurali.Dogrula(surum, birimFiyat, kdvOrani);

        var siniflanan = kalemler.Select(kalem => (Kalem: kalem, Neden: FinansTarifeDuzeltmeKurali.KorunmaNedeni(kalem, surum))).ToList();
        var duzeltilecek = siniflanan.Where(x => x.Neden is null).Select(x => x.Kalem).ToList();
        var satirlar = siniflanan
            .OrderBy(x => x.Neden is null)
            .ThenBy(x => x.Kalem.FinansTarihi)
            .ThenBy(x => x.Kalem.Id)
            .Select(x => new FinansTarifeDuzeltmeKalemModel(x.Kalem.Id, x.Kalem.ProjeNo, x.Kalem.IsAdi, x.Kalem.FinansTarihi,
                x.Kalem.KaynakTuru, x.Neden is null, x.Neden, FinansTutarKurallari.IsNet(x.Kalem)))
            .ToList();
        return new FinansTarifeDuzeltmeOnizlemeModel
        {
            TarifeId = surum.Id,
            UrunId = urun.Id,
            UrunAdi = urun.Ad,
            ParaBirimi = surum.ParaBirimi,
            GecerlilikBaslangici = surum.GecerlilikBaslangici,
            GecerlilikBitisi = surum.GecerlilikBitisi,
            Mevcut = new(surum.BirimFiyat, surum.KdvOrani, duzeltilecek.Sum(FinansTutarKurallari.IsNet)),
            Yeni = new(birimFiyat, kdvOrani, duzeltilecek.Sum(kalem => FinansTarifeDuzeltmeKurali.YeniNet(kalem, birimFiyat, surum.Id))),
            DuzeltilecekKalemSayisi = duzeltilecek.Count,
            KorunanKalemSayisi = satirlar.Count - duzeltilecek.Count,
            Kalemler = satirlar.Take(FinansTarifeDuzeltmeOnizlemeModel.EnFazlaKalem).ToList(),
            KalemListesiKisaltildi = satirlar.Count > FinansTarifeDuzeltmeOnizlemeModel.EnFazlaKalem
        };
    }

    public Task<FinansTarifeDuzeltmeSonucModel?> TarifeDuzeltAsync(
        int urunId,
        int tarifeId,
        FinansTarifeDuzeltModel model,
        CancellationToken cancellationToken)
        => islem.AtomikAsync(() => TarifeDuzeltCoreAsync(urunId, tarifeId, model, cancellationToken), cancellationToken);

    private async Task<FinansTarifeDuzeltmeSonucModel?> TarifeDuzeltCoreAsync(
        int urunId,
        int tarifeId,
        FinansTarifeDuzeltModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        FinansGerekceKurali.Dogrula(model.Aciklama);
        var plan = await TarifeDuzeltmePlaniAsync(urunId, tarifeId, tracking: true, cancellationToken);
        if (plan is null) return null;
        var (_, surum, kalemler) = plan.Value;
        FinansTarifeDuzeltmeKurali.Dogrula(surum, model.BirimFiyat, model.KdvOrani);
        var aciklama = model.Aciklama.Trim();

        // Uygunluk sürümün düzeltilmeden önceki değerleriyle belirlenir; sürüm ondan sonra değişir.
        var duzeltilecek = kalemler.Where(kalem => FinansTarifeDuzeltmeKurali.KorunmaNedeni(kalem, surum) is null).ToList();
        void Duzeltme(string varlik, int id, string alan, decimal eski, decimal yeni)
        {
            if (eski != yeni) denetim.Ekle(varlik, id, "Tarife Düzeltme", alan, eski, yeni, aciklama);
        }

        Duzeltme(nameof(FinansFiyatTarifesi), surum.Id, nameof(surum.BirimFiyat), surum.BirimFiyat, model.BirimFiyat);
        Duzeltme(nameof(FinansFiyatTarifesi), surum.Id, nameof(surum.KdvOrani), surum.KdvOrani, model.KdvOrani);
        surum.BirimFiyat = model.BirimFiyat;
        surum.KdvOrani = model.KdvOrani;

        foreach (var kalem in duzeltilecek)
        {
            var eskiDurum = kalem.Durum;
            // Malzemeli işte sürüm ahşap tarafındaysa yalnız ahşap fiyatı düzeltilir (KDV kontra tarafından gelir).
            if (kalem.AhsapTarifeIdSnapshot == surum.Id && kalem.AhsapBirimFiyatSnapshot.HasValue)
            {
                Duzeltme(nameof(FinansIsKaydi), kalem.Id, nameof(kalem.AhsapBirimFiyatSnapshot), kalem.AhsapBirimFiyatSnapshot.Value, model.BirimFiyat);
                kalem.AhsapBirimFiyatSnapshot = model.BirimFiyat;
            }
            if (kalem.TarifeIdSnapshot == surum.Id)
            {
                Duzeltme(nameof(FinansIsKaydi), kalem.Id, nameof(kalem.BirimFiyatSnapshot), kalem.BirimFiyatSnapshot, model.BirimFiyat);
                Duzeltme(nameof(FinansIsKaydi), kalem.Id, nameof(kalem.KdvOraniSnapshot), kalem.KdvOraniSnapshot, model.KdvOrani);
                kalem.BirimFiyatSnapshot = model.BirimFiyat;
                kalem.KdvOraniSnapshot = model.KdvOrani;
            }
            kalem.Durum = FinansTutarKurallari.Durum(kalem);
            denetim.DegisiklikEkle(nameof(FinansIsKaydi), kalem.Id, nameof(kalem.Durum), eskiDurum, kalem.Durum);
        }

        await context.SaveChangesAsync(cancellationToken);
        return new FinansTarifeDuzeltmeSonucModel(surum.Id, duzeltilecek.Count, kalemler.Count - duzeltilecek.Count);
    }

    /// <summary>
    /// Ürün, ürüne ait aktif sürüm ve o sürümün snapshot'ıyla fiyatlanmış iptal edilmemiş kalemler (PO/fatura
    /// satırlarıyla). Ürün ya da sürüm yoksa null döner.
    /// </summary>
    private async Task<(FinansUrun Urun, FinansFiyatTarifesi Surum, List<FinansIsKaydi> Kalemler)?> TarifeDuzeltmePlaniAsync(
        int urunId,
        int tarifeId,
        bool tracking,
        CancellationToken cancellationToken)
    {
        var urun = await context.Set<FinansUrun>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == urunId, cancellationToken);
        if (urun is null) return null;
        var surumSorgusu = context.Set<FinansFiyatTarifesi>().Where(x => x.Id == tarifeId && x.FinansUrunId == urunId && x.Aktif);
        var surum = await (tracking ? surumSorgusu : surumSorgusu.AsNoTracking()).FirstOrDefaultAsync(cancellationToken);
        if (surum is null) return null;
        var kalemler = await context.IsKaydiDetayQuery(tracking)
            .Where(x => (x.TarifeIdSnapshot == tarifeId || x.AhsapTarifeIdSnapshot == tarifeId) && !x.IptalEdildi && x.KaynakAktif)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
        return (urun, surum, kalemler);
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
        // Katlanır sandık boyutla eşleşmez; eski boyut değerleri temizlenir (kontrplak gibi ahşap/kontra m³ ile fiyatlanır).
        entity.Boy = null;
        entity.En = null;
        entity.Yukseklik = null;
        entity.IcSandikSablonId = model.IcSandikSablonId;
        entity.Malzeme = model.Malzeme;
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
                x.Id, x.IsTuru, x.SandikAdi, x.SandikTipi, x.IcSandikSablonId, x.Malzeme, x.Aktif)).ToArray()
        };
    }

    /// <summary>
    /// Ürün kayıt işlemlerinin transaction'ı içinde, üretimden gelen fiyatsız ve siparişsiz işleri
    /// finans tarihlerinde geçerli tarife sürümüyle fiyatlar (F12). Fiyatı olan işler yeniden
    /// fiyatlanmaz; manuel ve düzenli işlerin elle girilen fiyat/para birimi bilgisi korunur. Malzemeli fiyatlanıp ahşap
    /// tarifesi henüz bulunamamış (ahşap fiyatı 0) işler de tamamlanır; böylece ahşap tarifesi sonradan tanımlanabilir.
    /// </summary>
    private async Task BackfillUnpricedWorksAsync(CancellationToken cancellationToken)
    {
        var works = await context.IsKaydiDetayQuery(true)
            .Where(x => x.KaynakAktif && !x.IptalEdildi && !x.FinansMiktariManuel &&
                        x.KaynakTuru != FinansKaynakTurleri.Manuel && x.KaynakTuru != FinansKaynakTurleri.DuzenliIs &&
                        (x.BirimFiyatSnapshot <= 0 && !x.TarifeYiliSnapshot.HasValue ||
                         x.AhsapBirimFiyatSnapshot.HasValue && !x.AhsapTarifeIdSnapshot.HasValue) &&
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
            var oldAhsapPrice = work.AhsapBirimFiyatSnapshot;
            var oldAhsapTariff = work.AhsapTarifeIdSnapshot;
            await fiyatlar.OtomatikFiyatUygulaAsync(work, cancellationToken);
            work.Durum = FinansTutarKurallari.Durum(work);
            denetim.DegisiklikEkle(nameof(FinansIsKaydi), work.Id, nameof(work.AhsapBirimFiyatSnapshot), oldAhsapPrice, work.AhsapBirimFiyatSnapshot);
            denetim.DegisiklikEkle(nameof(FinansIsKaydi), work.Id, nameof(work.AhsapTarifeIdSnapshot), oldAhsapTariff, work.AhsapTarifeIdSnapshot);
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
