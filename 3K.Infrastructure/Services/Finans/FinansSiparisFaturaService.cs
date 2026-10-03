using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>Sipariş (PO) ve fatura kayıtları: kapasite kontrollü dağıtım, tutar revizyonu, belge mutabakatı, iptal ve durum güncellemesi.</summary>
internal sealed class FinansSiparisFaturaService(
    AppDbContext context,
    FinansIslemYurutucu islem,
    FinansDenetimKaydi denetim) : IFinansSiparisFaturaService
{
    public async Task<FinansSayfaliSonuc<FinansSiparisModel>> SiparislerAsync(
        FinansListeFiltre filtre,
        CancellationToken cancellationToken)
    {
        var query = SiparisDetayQuery();
        var scopedLines = ScopeOrderLines(context.Set<FinansSiparisKalemi>().AsNoTracking(), filtre);
        query = query.Where(x => x.Kalemler.Any(l => scopedLines.Select(s => s.Id).Contains(l.Id)));
        if (!filtre.IptalEdilenleriDahilEt) query = query.Where(x => !x.IptalEdildi);
        query = ApplyFaturalamaBekleyenFilter(query, filtre.FaturalamaBekleyen);
        if (filtre.Baslangic.HasValue) query = query.Where(x => x.SiparisTarihi >= filtre.Baslangic.Value.Date);
        if (filtre.Bitis.HasValue)
        {
            var end = filtre.Bitis.Value.Date.AddDays(1);
            query = query.Where(x => x.SiparisTarihi < end);
        }
        if (filtre.ProjeId.HasValue) query = query.Where(x => x.Kalemler.Any(y => y.FinansIsKaydi.ProjeId == filtre.ProjeId));
        if (!string.IsNullOrWhiteSpace(filtre.ProjeNo)) query = query.Where(x => x.Kalemler.Any(y => y.FinansIsKaydi.ProjeNo == filtre.ProjeNo));
        if (filtre.IsTuru.HasValue) query = query.Where(x => x.Kalemler.Any(y => y.FinansIsKaydi.IsTuru == filtre.IsTuru));
        if (filtre.Durum.HasValue) query = query.Where(x => x.Kalemler.Any(y => y.FinansIsKaydi.Durum == filtre.Durum));
        if (filtre.SiparisDurumu.HasValue) query = query.Where(x => x.Durum == filtre.SiparisDurumu);
        if (!string.IsNullOrWhiteSpace(filtre.ParaBirimi))
        {
            var currency = filtre.ParaBirimi.Trim().ToUpperInvariant();
            query = query.Where(x => x.Kalemler.Any(y => y.ParaBirimiSnapshot == currency));
        }
        if (!string.IsNullOrWhiteSpace(filtre.PoNumarasi))
        {
            var po = filtre.PoNumarasi.Trim().ToLower();
            query = query.Where(x => x.PoNumarasi.ToLower().Contains(po));
        }
        if (!string.IsNullOrWhiteSpace(filtre.TalepEden))
        {
            var requester = filtre.TalepEden.Trim().ToLower();
            query = query.Where(x => x.Kalemler.Any(y =>
                (y.FinansIsKaydi.TalepEdenKisi != null && y.FinansIsKaydi.TalepEdenKisi.ToLower().Contains(requester)) ||
                (y.FinansIsKaydi.TalepEdenBolum != null && y.FinansIsKaydi.TalepEdenBolum.ToLower().Contains(requester))));
        }
        if (!string.IsNullOrWhiteSpace(filtre.Arama))
        {
            var search = filtre.Arama.Trim().ToLower();
            query = query.Where(x => x.PoNumarasi.ToLower().Contains(search) ||
                                     x.KayitNo.ToLower().Contains(search) ||
                                     x.Kalemler.Any(y => y.FinansIsKaydi.ProjeNo.ToLower().Contains(search)));
        }

        var page = Math.Max(1, filtre.PageNumber);
        var size = Math.Clamp(filtre.PageSize, 1, 250);
        var count = await query.CountAsync(cancellationToken);
        var entities = await query.OrderByDescending(x => x.SiparisTarihi).ThenByDescending(x => x.Id)
            .Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken);
        var siparisToplamlari = await IsSiparisToplamlariAsync(entities, cancellationToken);
        return new FinansSayfaliSonuc<FinansSiparisModel>
        {
            Items = entities.Select(x => FinansSiparisFaturaEslemeleri.MapSiparis(x, siparisToplamlari, filtre)).ToArray(),
            Toplamlar = await query.Where(x => !x.IptalEdildi).SelectMany(x => x.Kalemler)
                .Where(x => scopedLines.Select(s => s.Id).Contains(x.Id)).GroupBy(x => x.ParaBirimiSnapshot)
                .Select(g => new FinansParaToplamiModel(g.Key, g.Sum(x => x.NetTutarSnapshot), g.Sum(x => x.KdvTutariSnapshot), g.Sum(x => x.ToplamTutarSnapshot))).ToArrayAsync(cancellationToken),
            PageNumber = page,
            PageSize = size,
            TotalCount = count
        };
    }

    /// <summary>
    /// PO açma ekranının seçicisi: siparişe bağlanabilecek işler proje × kategori × para birimi (üretim),
    /// düzenli iş × para birimi veya tek özel iş olarak gruplanır. Sandıklar satır olarak dönmez; grubun
    /// kalem kimlikleri dağıtım sırasıyla (finans tarihi, kimlik) verilir.
    /// </summary>
    public async Task<FinansSayfaliSonuc<FinansSiparisGrubuModel>> SiparisGruplariAsync(
        FinansSiparisGrupFiltre filtre,
        CancellationToken cancellationToken)
    {
        var query = context.IsKaydiDetayQuery().Where(x => !x.IptalEdildi && x.KaynakAktif);
        if (filtre.Baslangic.HasValue) query = query.Where(x => x.FinansTarihi >= filtre.Baslangic.Value.Date);
        if (filtre.Bitis.HasValue)
        {
            var end = filtre.Bitis.Value.Date.AddDays(1);
            query = query.Where(x => x.FinansTarihi < end);
        }
        if (!string.IsNullOrWhiteSpace(filtre.ParaBirimi))
        {
            var currency = filtre.ParaBirimi.Trim().ToUpperInvariant();
            query = query.Where(x => x.ParaBirimiSnapshot == currency);
        }
        if (filtre.ProjeId.HasValue) query = query.Where(x => x.ProjeId == filtre.ProjeId);
        if (filtre.DuzenliIsId.HasValue) query = query.Where(x => x.DuzenliIsId == filtre.DuzenliIsId);
        if (filtre.IsKaydiIds is { Count: > 0 })
        {
            var ids = filtre.IsKaydiIds.Distinct().Take(2000).ToArray();
            query = query.Where(x => ids.Contains(x.Id));
        }
        query = FinansSorgulari.KategoriFiltresi(query, filtre.Kategoriler);
        if (!string.IsNullOrWhiteSpace(filtre.Arama)) query = FinansSorgulari.AramaFiltresi(query, filtre.Arama);
        if (filtre.SadeceSiparisBekleyen) query = query.Where(FinansSorgulari.SiparisBekleyenIfadesi());
        if (await query.CountAsync(cancellationToken) > FinansSorgulari.RaporKayitSiniri)
            throw IsKuraliException.Cakisma("Seçim 20.000 işi aşıyor; dönem, kategori veya proje filtresini daraltın.");

        var groups = SiparisGruplariniOlustur(await query.ToListAsync(cancellationToken));
        var (page, size, skip) = FinansSorgulari.NormalizePagination(filtre.PageNumber, filtre.PageSize, groups.Count, 100);
        return new FinansSayfaliSonuc<FinansSiparisGrubuModel>
        {
            Items = groups.Skip(skip).Take(size).ToArray(),
            Toplamlar = groups.GroupBy(x => x.ParaBirimi).OrderBy(x => x.Key)
                .Select(x => new FinansParaToplamiModel(x.Key, x.Sum(y => y.KalanSiparisNetTutar), 0, x.Sum(y => y.KalanSiparisNetTutar)))
                .ToArray(),
            PageNumber = page,
            PageSize = size,
            TotalCount = groups.Count
        };
    }

    /// <summary>İşleri PO seçicisi gruplarına ayırır; grup durumu <see cref="FinansTutarKurallari.BirlesikDurum"/> ile türetilir.</summary>
    internal static IReadOnlyList<FinansSiparisGrubuModel> SiparisGruplariniOlustur(IEnumerable<FinansIsKaydi> works)
        => works
            .GroupBy(SiparisGrupAnahtari)
            .Select(group =>
            {
                var items = group.OrderBy(x => x.FinansTarihi).ThenBy(x => x.Id).ToList();
                var first = items[0];
                var kategori = FinansKategoriKurali.Kategori(first.IsTuru, first.DuzenliIsId);
                var amounts = items.Select(FinansTutarKurallari.KalemTutarlari).ToList();
                var durum = FinansTutarKurallari.BirlesikDurum(amounts);
                var volumes = items.Select(FinansModelEslemeleri.Hacim).ToList();
                return new FinansSiparisGrubuModel
                {
                    Anahtar = group.Key,
                    ProjeId = first.ProjeId,
                    ProjeNo = first.ProjeNo,
                    Musteri = first.Musteri,
                    Kategori = kategori,
                    IsTuru = first.IsTuru,
                    DuzenliIsId = first.DuzenliIsId,
                    Ad = FinansKategoriKurali.UretimKategorisi(kategori) ? FinansKategoriKurali.Ad(kategori) : first.IsAdi,
                    ParaBirimi = first.ParaBirimiSnapshot,
                    IlkTarih = items.Min(x => x.FinansTarihi),
                    SonTarih = items.Max(x => x.FinansTarihi),
                    KalemSayisi = items.Count,
                    IsKaydiIds = items.Select(x => x.Id).ToArray(),
                    NetTutar = amounts.Sum(x => x.Net),
                    SiparisNetTutar = amounts.Sum(x => x.Siparis),
                    KalanSiparisNetTutar = amounts.Sum(x => Math.Max(0, x.Net - x.Siparis)),
                    FaturalananNetTutar = amounts.Sum(x => x.Fatura),
                    AhsapM3 = volumes.Sum(x => x.Ahsap),
                    KontraM3 = volumes.Sum(x => x.Kontra),
                    SiparisKarsilanma = durum.Siparis,
                    FaturaKarsilanma = durum.Fatura,
                    FiyatBekleyenKalem = durum.FiyatBekleyenKalem
                };
            })
            .OrderBy(x => x.ProjeNo, StringComparer.Ordinal)
            .ThenBy(x => x.Kategori)
            .ThenBy(x => x.IlkTarih)
            .ThenBy(x => x.Anahtar, StringComparer.Ordinal)
            .ToArray();

    /// <summary>Üretim işi proje × iş türü × para birimi, düzenli iş dönemi iş × para birimi, özel iş tek başına gruplanır.</summary>
    internal static string SiparisGrupAnahtari(FinansIsKaydi work)
    {
        if (work.DuzenliIsId.HasValue) return $"D:{work.DuzenliIsId}:{work.ParaBirimiSnapshot}";
        if (work.IsTuru == FinansIsTuru.OzelIs) return $"O:{work.Id}";
        var project = work.ProjeId.HasValue ? $"P:{work.ProjeId}" : $"M:{work.ProjeNo}\u001f{work.Musteri}";
        return $"{project}:{(int)work.IsTuru}:{work.ParaBirimiSnapshot}";
    }

    internal static IQueryable<FinansSiparis> ApplyFaturalamaBekleyenFilter(
        IQueryable<FinansSiparis> query,
        bool faturalamaBekleyen)
        => !faturalamaBekleyen
            ? query
            : query.Where(x =>
                !x.IptalEdildi &&
                (x.Durum == FinansSiparisDurumu.Acik ||
                 x.Durum == FinansSiparisDurumu.KismiFaturalandi));

    public async Task<FinansSiparisModel?> SiparisGetirAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await SiparisDetayQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null
            ? null
            : FinansSiparisFaturaEslemeleri.MapSiparis(entity, await IsSiparisToplamlariAsync([entity], cancellationToken));
    }

    public Task<FinansSiparisModel?> SiparisGuncelleAsync(
        int id,
        FinansSiparisGuncelleModel model,
        CancellationToken cancellationToken)
        => islem.AtomikAsync(() => SiparisGuncelleCoreAsync(id, model, cancellationToken), cancellationToken);

    private async Task<FinansSiparisModel?> SiparisGuncelleCoreAsync(
        int id,
        FinansSiparisGuncelleModel model,
        CancellationToken cancellationToken)
    {
        var entity = await SiparisDetayQuery(true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return null;
        if (entity.IptalEdildi)
            throw IsKuraliException.Cakisma("İptal edilmiş sipariş güncellenemez.");

        var po = PoNumarasiNormalize(model.PoNumarasi);
        await PoNumarasiKullanilabilirMiAsync(po, id, cancellationToken);

        var auditBefore = FinansDenetimKaydi.AnlikGoruntu(entity);
        entity.PoNumarasi = po;
        entity.SiparisTarihi = model.SiparisTarihi;
        entity.Aciklama = model.Aciklama?.Trim();
        if (model.Kalemler is not null)
        {
            FinansGerekceKurali.Dogrula(model.Gerekce);
            if (model.Kalemler.Count == 0 || model.Kalemler.Select(x => x.IsKaydiId).Distinct().Count() != model.Kalemler.Count)
                throw IsKuraliException.Cakisma("Tekil mevcut PO kalemlerini seçin.");
            foreach (var requested in model.Kalemler)
            {
                var line = entity.Kalemler.SingleOrDefault(x => x.FinansIsKaydiId == requested.IsKaydiId)
                    ?? throw IsKuraliException.Cakisma("Revizyon yalnız bu PO'nun mevcut kalemlerinde yapılabilir.");
                if (!requested.NetTutar.HasValue) throw IsKuraliException.Cakisma("Revizyon net tutarı zorunludur.");
                var work = await context.IsKaydiDetayQuery(true).FirstAsync(x => x.Id == requested.IsKaydiId, cancellationToken);
                var used = work.SiparisKalemleri.Where(x => x.Id != line.Id && !x.FinansSiparis.IptalEdildi).Sum(x => x.NetTutarSnapshot);
                FinansTutarKurallari.Kapasite(requested.NetTutar.Value, used, FinansTutarKurallari.IsNet(work), "PO revizyonu");
                var money = FinansTutarKurallari.Hesapla(1, requested.NetTutar.Value, line.KdvOraniSnapshot);
                var billed = line.FaturaKalemleri.Where(x => !x.FinansFatura.IptalEdildi).ToArray();
                if (billed.Sum(x => x.NetTutarSnapshot) > money.Net || billed.Sum(x => x.KdvTutariSnapshot) > money.Kdv || billed.Sum(x => x.ToplamTutarSnapshot) > money.Toplam)
                    throw IsKuraliException.Cakisma("PO tutarı aktif faturalanan net/KDV/brüt tutarının altına indirilemez.");
                var beforeLine = FinansDenetimKaydi.AnlikGoruntu(line);
                line.NetTutarSnapshot = money.Net; line.KdvTutariSnapshot = money.Kdv; line.ToplamTutarSnapshot = money.Toplam;
                line.TutarBazli = true;
                denetim.DegisiklikleriEkle(nameof(FinansSiparisKalemi), line, beforeLine);
            }
            denetim.Ekle(nameof(FinansSiparis), id, "Tutar Revizyonu", "Gerekçe", null, null, model.Gerekce);
            await context.SaveChangesAsync(cancellationToken);
            RefreshOrderStatus(entity);
            await RefreshWorkStatusesAsync(entity.Kalemler.Select(x => x.FinansIsKaydiId), cancellationToken);
        }
        denetim.DegisiklikleriEkle(nameof(FinansSiparis), entity, auditBefore);
        await context.SaveChangesAsync(cancellationToken);
        return await SiparisGetirAsync(id, cancellationToken);
    }

    public Task<FinansSiparisModel> SiparisOlusturAsync(
        FinansSiparisOlusturModel model,
        CancellationToken cancellationToken)
        => islem.AtomikAsync(() => SiparisOlusturCoreAsync(model, cancellationToken), cancellationToken);

    private async Task<FinansSiparisModel> SiparisOlusturCoreAsync(
        FinansSiparisOlusturModel model,
        CancellationToken cancellationToken)
    {
        var po = PoNumarasiNormalize(model.PoNumarasi);
        await PoNumarasiKullanilabilirMiAsync(po, null, cancellationToken);
        var dagitimlar = await SiparisDagitimlariniHazirlaAsync(model.Kalemler, model.Gruplar, cancellationToken);

        var order = new FinansSiparis
        {
            KayitNo = NewDocumentNo("SIP"),
            PoNumarasi = po,
            SiparisTarihi = model.SiparisTarihi,
            Aciklama = model.Aciklama?.Trim(),
            ParaBirimi = string.IsNullOrWhiteSpace(model.ParaBirimi) ? null : FinansParaBirimiKurali.Normalize(model.ParaBirimi)
        };
        foreach (var (work, requested) in dagitimlar)
            SiparisKalemiBagla(order, work, requested);

        if (model.BelgeNetTutar.HasValue &&
            FinansTutarKurallari.Para(model.BelgeNetTutar.Value) != order.Kalemler.Sum(x => x.NetTutarSnapshot))
            throw IsKuraliException.Cakisma("PO tutarı dağıtılan kalemlerin net toplamına eşit olmalıdır.");
        context.Set<FinansSiparis>().Add(order);
        await context.SaveChangesAsync(cancellationToken);
        denetim.Ekle(nameof(FinansSiparis), order.Id, "Oluşturma", "*", null, $"PO: {order.PoNumarasi}");
        await RefreshWorkStatusesAsync(dagitimlar.Select(x => x.Work.Id), cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return (await SiparisGetirAsync(order.Id, cancellationToken))!;
    }

    public Task<FinansSiparisModel?> SiparisKalemEkleAsync(
        int id,
        FinansSiparisKalemEkleModel model,
        CancellationToken cancellationToken)
        => islem.AtomikAsync(() => SiparisKalemEkleCoreAsync(id, model, cancellationToken), cancellationToken);

    /// <summary>
    /// Aktif PO'ya yeni işleri bağlar veya PO'daki işin tutarını artırır. Proje ilerledikçe üretilen
    /// kalemler aynı müşteri PO numarasına bu yolla eklenir; PO tutarı dağıtımların toplamıdır.
    /// </summary>
    private async Task<FinansSiparisModel?> SiparisKalemEkleCoreAsync(
        int id,
        FinansSiparisKalemEkleModel model,
        CancellationToken cancellationToken)
    {
        var order = await SiparisDetayQuery(true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (order is null) return null;
        if (order.IptalEdildi)
            throw IsKuraliException.Cakisma("İptal edilmiş PO'ya kalem eklenemez.");
        var dagitimlar = await SiparisDagitimlariniHazirlaAsync(model.Kalemler ?? [], model.Gruplar, cancellationToken);
        var oncekiNet = order.Kalemler.Sum(x => x.NetTutarSnapshot);
        foreach (var (work, requested) in dagitimlar)
            SiparisKalemiBagla(order, work, requested);
        await context.SaveChangesAsync(cancellationToken);
        denetim.Ekle(nameof(FinansSiparis), order.Id, "Kalem Ekleme", "NetTutar", oncekiNet,
            order.Kalemler.Sum(x => x.NetTutarSnapshot), $"{dagitimlar.Count} iş kalemi");
        RefreshOrderStatus(order);
        await RefreshWorkStatusesAsync(dagitimlar.Select(x => x.Work.Id), cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return await SiparisGetirAsync(id, cancellationToken);
    }

    /// <summary>
    /// Tek tek ve grup düzeyinde istenen dağıtımları kalem bazında birleştirir. Grup tutarı grubun
    /// kalemlerine finans tarihi sırasıyla <see cref="FinansDagitimKurallari"/> ile dağıtılır; aynı iş
    /// bir istekte yalnız bir kez yer alabilir.
    /// </summary>
    private async Task<IReadOnlyList<(FinansIsKaydi Work, FinansSiparisDagitimModel Requested)>> SiparisDagitimlariniHazirlaAsync(
        IReadOnlyList<FinansSiparisDagitimModel> kalemler,
        IReadOnlyList<FinansSiparisGrupDagitimModel>? gruplar,
        CancellationToken cancellationToken)
    {
        gruplar ??= [];
        var tumIds = kalemler.Select(x => x.IsKaydiId).Concat(gruplar.SelectMany(x => x.IsKaydiIds)).ToArray();
        if (tumIds.Length == 0) throw IsKuraliException.Cakisma("Sipariş en az bir iş kalemi içermelidir.");
        if (tumIds.Distinct().Count() != tumIds.Length)
            throw IsKuraliException.Cakisma("Aynı iş kaydı bir siparişte birden fazla kez dağıtılamaz.");
        var works = await context.IsKaydiDetayQuery(true).Where(x => tumIds.Contains(x.Id)).ToListAsync(cancellationToken);
        if (works.Count != tumIds.Length)
            throw IsKuraliException.Cakisma("Sipariş kalemlerinden en az biri bulunamadı.");
        var byId = works.ToDictionary(x => x.Id);

        var sonuc = kalemler.Select(x => (byId[x.IsKaydiId], x)).ToList();
        foreach (var grup in gruplar)
        {
            var grupIsleri = grup.IsKaydiIds.Select(x => byId[x])
                .OrderBy(x => x.FinansTarihi).ThenBy(x => x.Id).ToList();
            foreach (var work in grupIsleri)
                SiparisIcinAktifMi(work);
            var paylar = FinansDagitimKurallari.Dagit(grup.NetTutar,
                grupIsleri.Select(x => new FinansDagitimKapasitesi(x.Id,
                    FinansTutarKurallari.IsNet(x) - FinansTutarKurallari.SiparisNet(x))).ToArray(),
                "Sipariş");
            sonuc.AddRange(paylar.Select(x => (byId[x.Id], new FinansSiparisDagitimModel(x.Id, 0, 0, NetTutar: x.Tutar))));
        }
        return sonuc;
    }

    private static void SiparisIcinAktifMi(FinansIsKaydi work)
    {
        if (work.IptalEdildi || !work.KaynakAktif)
            throw IsKuraliException.Cakisma($"{work.IsAdi} aktif olmadığı için siparişe eklenemez.");
        if (FinansTutarKurallari.IsNet(work) <= 0)
            throw IsKuraliException.Cakisma($"{work.IsAdi} kalemi için geçerli fiyat bulunamadı.");
    }

    /// <summary>
    /// İşi PO'ya bağlar: iş PO'da zaten varsa satır tutarı artırılır, yoksa yeni satır açılır. PO tek para
    /// birimlidir, dağıtım işin kalan net kapasitesini aşamaz ve fiyat işin snapshot'ından alınır.
    /// </summary>
    private static void SiparisKalemiBagla(FinansSiparis order, FinansIsKaydi work, FinansSiparisDagitimModel requested)
    {
        SiparisIcinAktifMi(work);
        var productId = requested.FinansUrunId ?? work.FinansUrunId;
        var pricingUnit = work.FiyatlandirmaBirimiSnapshot;
        var unitPrice = requested.BirimFiyat ?? work.BirimFiyatSnapshot;
        var currency = string.IsNullOrWhiteSpace(requested.ParaBirimi)
            ? work.ParaBirimiSnapshot
            : FinansParaBirimiKurali.Normalize(requested.ParaBirimi);
        var vat = requested.KdvOrani ?? work.KdvOraniSnapshot;
        if (productId != work.FinansUrunId || unitPrice != work.BirimFiyatSnapshot || currency != work.ParaBirimiSnapshot || vat != work.KdvOraniSnapshot)
        {
            throw IsKuraliException.Cakisma("PO işin fiyat snapshot'ını kullanır. Önce ayrı fiyatlandırma işlemiyle iş bedelini düzeltin.");
        }
        order.ParaBirimi ??= currency;
        if (order.ParaBirimi != currency)
            throw IsKuraliException.Cakisma("Tek PO yalnız aynı para birimindeki işleri içerebilir.");

        var activeLines = work.SiparisKalemleri.Where(x => !x.FinansSiparis.IptalEdildi).ToList();
        if (activeLines.Any(x => x.FiyatlandirmaBirimiSnapshot != pricingUnit))
            throw IsKuraliException.Cakisma("Kısmi siparişte fiyatlandırma birimi değiştirilemez.");
        var existing = order.Kalemler.SingleOrDefault(x => x.FinansIsKaydiId == work.Id);
        if (existing is not null && !requested.NetTutar.HasValue)
            throw IsKuraliException.Cakisma("PO'da bulunan işe yalnız net tutar olarak ek dağıtım yapılabilir.");
        var distribution = requested.NetTutar.HasValue
            ? new FinansDagitimMiktari(0, 0)
            : FinansMiktarKurallari.DagitimiNormalizeEt(
            pricingUnit,
            requested.Adet,
            requested.M3,
            work.Adet,
            work.ToplamM3,
            activeLines.Sum(x => x.Adet),
            activeLines.Sum(x => x.M3),
            activeLines.Count > 0,
            "Sipariş");
        var pricingQuantity = FinansTutarKurallari.FiyatlandirmaMiktari(pricingUnit, distribution.Adet, distribution.M3);
        var requestedNet = requested.NetTutar ?? FinansTutarKurallari.Hesapla(pricingQuantity, unitPrice, vat).Net;
        FinansTutarKurallari.Kapasite(requestedNet, activeLines.Sum(x => x.NetTutarSnapshot), FinansTutarKurallari.IsNet(work), "Sipariş");
        if (existing is not null)
        {
            var total = FinansTutarKurallari.Hesapla(1, existing.NetTutarSnapshot + requestedNet, existing.KdvOraniSnapshot);
            existing.NetTutarSnapshot = total.Net;
            existing.KdvTutariSnapshot = total.Kdv;
            existing.ToplamTutarSnapshot = total.Toplam;
            existing.TutarBazli = true;
            return;
        }
        var money = FinansTutarKurallari.Hesapla(1, requestedNet, vat);
        order.Kalemler.Add(new FinansSiparisKalemi
        {
            FinansIsKaydiId = work.Id,
            FinansIsKaydi = work,
            FinansSiparis = order,
            Adet = distribution.Adet,
            M3 = distribution.M3,
            TutarBazli = requested.NetTutar.HasValue,
            FinansUrunId = productId,
            FiyatlandirmaBirimiSnapshot = pricingUnit,
            BirimFiyatSnapshot = unitPrice,
            ParaBirimiSnapshot = currency,
            KdvOraniSnapshot = vat,
            NetTutarSnapshot = money.Net,
            KdvTutariSnapshot = money.Kdv,
            ToplamTutarSnapshot = money.Toplam
        });
    }

    internal static string PoNumarasiNormalize(string poNumarasi) => poNumarasi.Trim().ToUpperInvariant();

    /// <summary>PO-1: PO numarası iptal edilmemiş siparişler arasında benzersizdir (büyük/küçük harf duyarsız).</summary>
    private async Task PoNumarasiKullanilabilirMiAsync(string po, int? haricId, CancellationToken cancellationToken)
    {
        if (po.Length == 0) throw IsKuraliException.Cakisma("PO numarası zorunludur.");
        if (await context.Set<FinansSiparis>().AnyAsync(x => x.PoNumarasi == po && !x.IptalEdildi &&
                (!haricId.HasValue || x.Id != haricId.Value), cancellationToken))
            throw IsKuraliException.Cakisma("Bu PO numarası aktif bir siparişte kullanılıyor. Yeni işleri mevcut PO'ya kalem olarak ekleyin.");
    }

    public Task<bool> SiparisIptalAsync(int id, string aciklama, CancellationToken cancellationToken)
        => islem.AtomikAsync(() => SiparisIptalCoreAsync(id, aciklama, cancellationToken), cancellationToken);

    private async Task<bool> SiparisIptalCoreAsync(int id, string aciklama, CancellationToken cancellationToken)
    {
        FinansGerekceKurali.Dogrula(aciklama);
        var entity = await SiparisDetayQuery(true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;
        if (entity.IptalEdildi) return true;
        if (entity.Kalemler.SelectMany(x => x.FaturaKalemleri).Any(x => !x.FinansFatura.IptalEdildi))
            throw IsKuraliException.Cakisma("Aktif faturası bulunan sipariş iptal edilemez. Önce faturaları iptal edin.");
        entity.IptalEdildi = true;
        entity.IptalTarihi = TurkeyTime.Now;
        entity.IptalAciklamasi = aciklama.Trim();
        entity.Durum = FinansSiparisDurumu.IptalEdildi;
        denetim.Ekle(nameof(FinansSiparis), id, "İptal", nameof(entity.IptalEdildi), false, true, aciklama);
        await RefreshWorkStatusesAsync(entity.Kalemler.Select(x => x.FinansIsKaydiId), cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<FinansSayfaliSonuc<FinansFaturaModel>> FaturalarAsync(
        FinansListeFiltre filtre,
        CancellationToken cancellationToken)
    {
        var query = ApplyInvoiceStatusFilter(FaturaDetayQuery(), filtre);
        var scopedLines = ScopeOrderLines(context.Set<FinansSiparisKalemi>().AsNoTracking(), filtre);
        query = query.Where(x => x.Kalemler.Any(l => scopedLines.Select(s => s.Id).Contains(l.FinansSiparisKalemiId)));
        if (!string.IsNullOrWhiteSpace(filtre.FaturaNumarasi)) query = query.Where(x => x.FaturaNumarasi.ToLower().Contains(filtre.FaturaNumarasi.ToLower()));
        if (filtre.Baslangic.HasValue) query = query.Where(x => x.FaturaTarihi >= filtre.Baslangic.Value.Date);
        if (filtre.Bitis.HasValue)
        {
            var end = filtre.Bitis.Value.Date.AddDays(1);
            query = query.Where(x => x.FaturaTarihi < end);
        }
        if (filtre.ProjeId.HasValue) query = query.Where(x => x.Kalemler.Any(y => y.FinansSiparisKalemi.FinansIsKaydi.ProjeId == filtre.ProjeId));
        if (!string.IsNullOrWhiteSpace(filtre.ProjeNo)) query = query.Where(x => x.Kalemler.Any(y => y.FinansSiparisKalemi.FinansIsKaydi.ProjeNo == filtre.ProjeNo));
        if (filtre.IsTuru.HasValue) query = query.Where(x => x.Kalemler.Any(y => y.FinansSiparisKalemi.FinansIsKaydi.IsTuru == filtre.IsTuru));
        if (filtre.Durum.HasValue) query = query.Where(x => x.Kalemler.Any(y => y.FinansSiparisKalemi.FinansIsKaydi.Durum == filtre.Durum));
        if (filtre.SiparisDurumu.HasValue) query = query.Where(x => x.FinansSiparis.Durum == filtre.SiparisDurumu);
        if (!string.IsNullOrWhiteSpace(filtre.ParaBirimi))
        {
            var currency = filtre.ParaBirimi.Trim().ToUpperInvariant();
            query = query.Where(x => x.Kalemler.Any(y => y.FinansSiparisKalemi.ParaBirimiSnapshot == currency));
        }
        if (!string.IsNullOrWhiteSpace(filtre.PoNumarasi))
        {
            var po = filtre.PoNumarasi.Trim().ToLower();
            query = query.Where(x => x.FinansSiparis.PoNumarasi.ToLower().Contains(po));
        }
        if (!string.IsNullOrWhiteSpace(filtre.TalepEden))
        {
            var requester = filtre.TalepEden.Trim().ToLower();
            query = query.Where(x => x.Kalemler.Any(y =>
                (y.FinansSiparisKalemi.FinansIsKaydi.TalepEdenKisi != null && y.FinansSiparisKalemi.FinansIsKaydi.TalepEdenKisi.ToLower().Contains(requester)) ||
                (y.FinansSiparisKalemi.FinansIsKaydi.TalepEdenBolum != null && y.FinansSiparisKalemi.FinansIsKaydi.TalepEdenBolum.ToLower().Contains(requester))));
        }
        if (!string.IsNullOrWhiteSpace(filtre.Arama))
        {
            var search = filtre.Arama.Trim().ToLower();
            query = query.Where(x => x.FaturaNumarasi.ToLower().Contains(search) ||
                                     x.KayitNo.ToLower().Contains(search) ||
                                     x.FinansSiparis.PoNumarasi.ToLower().Contains(search));
        }
        var bugun = TurkeyTime.Now.Date;
        var uyariGunu = await FinansSorgulari.VadeUyariGunuAsync(context, cancellationToken);
        query = FinansSorgulari.OdemeFiltresi(query, filtre.OdemeDurumu, bugun, uyariGunu);
        var page = Math.Max(1, filtre.PageNumber);
        var size = Math.Clamp(filtre.PageSize, 1, 250);
        var count = await query.CountAsync(cancellationToken);
        // Vade takibinde en acil fatura üstte; normal listede en yeni fatura.
        var ordered = filtre.OdemeDurumu is FinansOdemeFiltresi.Odenmedi or FinansOdemeFiltresi.VadesiGecen or FinansOdemeFiltresi.VadesiYaklasan
            ? query.OrderBy(x => x.VadeTarihi).ThenBy(x => x.Id)
            : query.OrderByDescending(x => x.FaturaTarihi).ThenByDescending(x => x.Id);
        var entities = await ordered.Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken);
        return new FinansSayfaliSonuc<FinansFaturaModel>
        {
            Items = entities.Select(x => FinansSiparisFaturaEslemeleri.MapFatura(x, bugun, uyariGunu, filtre)).ToArray(),
            Toplamlar = await FinansSorgulari.BuildInvoiceTotalsQuery(query.Where(x => !x.IptalEdildi).SelectMany(x => x.Kalemler)
                .Where(x => scopedLines.Select(l => l.Id).Contains(x.FinansSiparisKalemiId)))
                .ToArrayAsync(cancellationToken),
            PageNumber = page,
            PageSize = size,
            TotalCount = count
        };
    }

    internal static IQueryable<FinansFatura> ApplyInvoiceStatusFilter(
        IQueryable<FinansFatura> query,
        FinansListeFiltre filtre)
    {
        if (filtre.FaturaDurumu == FinansFaturaDurumu.IptalEdildi)
            return query.Where(x => x.IptalEdildi && x.Durum == FinansFaturaDurumu.IptalEdildi);
        if (filtre.FaturaDurumu == FinansFaturaDurumu.Aktif)
            return query.Where(x => !x.IptalEdildi && x.Durum == FinansFaturaDurumu.Aktif);
        return filtre.IptalEdilenleriDahilEt ? query : query.Where(x => !x.IptalEdildi);
    }

    public async Task<FinansFaturaModel?> FaturaGetirAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await FaturaDetayQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : await MapFaturaAsync(entity, cancellationToken);
    }

    private async Task<FinansFaturaModel> MapFaturaAsync(FinansFatura entity, CancellationToken cancellationToken)
        => FinansSiparisFaturaEslemeleri.MapFatura(entity, TurkeyTime.Now.Date,
            await FinansSorgulari.VadeUyariGunuAsync(context, cancellationToken));

    public Task<FinansFaturaModel?> OdemeKaydetAsync(int id, FinansOdemeKaydetModel model, CancellationToken cancellationToken)
        => islem.AtomikAsync(() => OdemeKaydetCoreAsync(id, model, cancellationToken), cancellationToken);

    /// <summary>
    /// Tahsilat dışarıdan gelir; kullanıcı faturayı ödendi olarak işaretler (tutar faturanın tamamıdır). İşaretin
    /// kaldırılması gerekçe ister; ikisi de denetim kaydına yazılır. Aynı durum tekrar gönderilirse değişiklik yapılmaz.
    /// </summary>
    private async Task<FinansFaturaModel?> OdemeKaydetCoreAsync(int id, FinansOdemeKaydetModel model, CancellationToken cancellationToken)
    {
        var invoice = await context.Set<FinansFatura>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (invoice is null) return null;
        if (invoice.IptalEdildi) throw IsKuraliException.Cakisma("İptal edilmiş faturanın ödemesi işaretlenemez.");
        var odemeTarihi = FinansVadeKurallari.OdemeTarihiDogrula(model.Odendi, model.OdemeTarihi, TurkeyTime.Now.Date);
        var aciklama = string.IsNullOrWhiteSpace(model.Aciklama) ? null : model.Aciklama.Trim();
        if (aciklama?.Length > FinansGerekceKurali.EnUzun) throw IsKuraliException.Cakisma("Ödeme açıklaması en fazla 1000 karakter olabilir.");
        if (invoice.OdendiMi && !model.Odendi) FinansGerekceKurali.Dogrula(aciklama);
        if (invoice.OdendiMi == model.Odendi && invoice.OdemeTarihi == odemeTarihi && invoice.OdemeAciklamasi == aciklama)
            return await FaturaGetirAsync(id, cancellationToken);

        var before = FinansDenetimKaydi.AnlikGoruntu(invoice);
        var islemAdi = model.Odendi == invoice.OdendiMi ? "Ödeme Güncelleme" : model.Odendi ? "Ödeme Kaydı" : "Ödeme Geri Alma";
        invoice.OdendiMi = model.Odendi;
        invoice.OdemeTarihi = odemeTarihi;
        invoice.OdemeAciklamasi = aciklama;
        denetim.Ekle(nameof(FinansFatura), id, islemAdi, nameof(invoice.OdendiMi), before[nameof(invoice.OdendiMi)], invoice.OdendiMi, aciklama);
        denetim.DegisiklikleriEkle(nameof(FinansFatura), invoice, before);
        await context.SaveChangesAsync(cancellationToken);
        return await FaturaGetirAsync(id, cancellationToken);
    }

    public async Task<FinansVadeUyarilariModel> VadeUyarilariAsync(int enFazlaKalem, CancellationToken cancellationToken)
    {
        var bugun = TurkeyTime.Now.Date;
        var uyariGunu = await FinansSorgulari.VadeUyariGunuAsync(context, cancellationToken);
        var sinir = bugun.AddDays(uyariGunu + 1);
        var query = context.Set<FinansFatura>().AsNoTracking().Where(x => !x.IptalEdildi && !x.OdendiMi && x.VadeTarihi < sinir);
        var gecen = await query.CountAsync(x => x.VadeTarihi < bugun, cancellationToken);
        var yaklasan = await query.CountAsync(x => x.VadeTarihi >= bugun, cancellationToken);
        var satirlar = await FinansSorgulari.AlacakSatirlariAsync(
            query.OrderBy(x => x.VadeTarihi).ThenBy(x => x.Id).Take(Math.Clamp(enFazlaKalem, 1, 100)), cancellationToken);
        return new FinansVadeUyarilariModel(uyariGunu, gecen, yaklasan, satirlar
            .OrderBy(x => x.VadeTarihi).ThenBy(x => x.Id)
            .Select(x => new FinansVadeUyarisiModel(x.Id, x.FaturaNumarasi, x.PoNumarasi, x.ProjeNo, x.FaturaTarihi, x.VadeTarihi,
                FinansVadeKurallari.VadeFarkiGun(false, x.VadeTarihi, null, bugun),
                FinansVadeKurallari.Durum(false, x.VadeTarihi, null, bugun, uyariGunu), x.ParaBirimi, x.Toplam))
            .ToArray());
    }

    public Task<FinansFaturaModel?> FaturaGuncelleAsync(
        int id,
        FinansFaturaGuncelleModel model,
        CancellationToken cancellationToken)
        => islem.AtomikAsync(() => FaturaGuncelleCoreAsync(id, model, cancellationToken), cancellationToken);

    private async Task<FinansFaturaModel?> FaturaGuncelleCoreAsync(
        int id,
        FinansFaturaGuncelleModel model,
        CancellationToken cancellationToken)
    {
        var entity = await FaturaDetayQuery(true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return null;
        if (entity.IptalEdildi)
            throw IsKuraliException.Cakisma("İptal edilmiş fatura güncellenemez.");

        var invoiceNo = model.FaturaNumarasi.Trim().ToUpperInvariant();
        if (await context.Set<FinansFatura>().AnyAsync(x => x.Id != id && x.FaturaNumarasi == invoiceNo, cancellationToken))
            throw IsKuraliException.Cakisma("Bu fatura numarası daha önce kullanılmış; finansal belge numaraları tekrar kullanılamaz.");

        var auditBefore = FinansDenetimKaydi.AnlikGoruntu(entity);
        entity.FaturaNumarasi = invoiceNo;
        entity.FaturaTarihi = model.FaturaTarihi;
        entity.Aciklama = model.Aciklama?.Trim();
        entity.VadeTarihi = FinansVadeKurallari.VadeDogrula(model.FaturaTarihi, model.VadeTarihi ?? entity.VadeTarihi);
        if (model.Kalemler is not null)
        {
            if (entity.OdendiMi)
                throw IsKuraliException.Cakisma("Ödendi olarak işaretli faturanın tutarı değiştirilemez; önce ödeme işaretini kaldırın.");
            FinansGerekceKurali.Dogrula(model.Gerekce);
            if (model.BelgeMutabakatiniKoru) throw IsKuraliException.Cakisma("Kalem revizyonunda eski belge tutarları korunamaz; gerçek belge toplamlarını yeniden girin.");
            if (model.Kalemler.Count == 0 || model.Kalemler.Select(x => x.SiparisKalemiId).Distinct().Count() != model.Kalemler.Count)
                throw IsKuraliException.Cakisma("Tekil mevcut fatura kalemlerini seçin.");
            var order = await SiparisDetayQuery(true).FirstAsync(x => x.Id == entity.FinansSiparisId, cancellationToken);
            if (order.IptalEdildi) throw IsKuraliException.Cakisma("Pasif PO faturası değiştirilemez.");
            foreach (var requested in model.Kalemler)
            {
                var line = entity.Kalemler.SingleOrDefault(x => x.FinansSiparisKalemiId == requested.SiparisKalemiId)
                    ?? throw IsKuraliException.Cakisma("Revizyon yalnız bu faturanın mevcut kalemlerinde yapılabilir.");
                var poLine = order.Kalemler.Single(x => x.Id == line.FinansSiparisKalemiId);
                var others = poLine.FaturaKalemleri.Where(x => x.Id != line.Id && !x.FinansFatura.IptalEdildi).ToArray();
                if (!requested.NetTutar.HasValue) throw IsKuraliException.Cakisma("Revizyon net tutarı zorunludur.");
                var net = requested.NetTutar.Value;
                FinansTutarKurallari.Kapasite(net, others.Sum(x => x.NetTutarSnapshot), poLine.NetTutarSnapshot, "Fatura revizyonu");
                var money = FinansTutarKurallari.FaturaKalemiTutari(net, poLine.KdvOraniSnapshot,
                    poLine.NetTutarSnapshot, poLine.KdvTutariSnapshot, others.Sum(x => x.NetTutarSnapshot), others.Sum(x => x.KdvTutariSnapshot));
                if (money.Kdv < 0 || money.Toplam + others.Sum(x => x.ToplamTutarSnapshot) > poLine.ToplamTutarSnapshot)
                    throw IsKuraliException.Cakisma("Fatura revizyonu PO KDV/brüt bakiyesini aşamaz.");
                var beforeLine = FinansDenetimKaydi.AnlikGoruntu(line);
                line.NetTutarSnapshot = money.Net; line.KdvTutariSnapshot = money.Kdv; line.ToplamTutarSnapshot = money.Toplam; line.TutarBazli = true;
                denetim.DegisiklikleriEkle(nameof(FinansFaturaKalemi), line, beforeLine);
            }
            denetim.Ekle(nameof(FinansFatura), id, "Tutar Revizyonu", "Gerekçe", null, null, model.Gerekce);
            await context.SaveChangesAsync(cancellationToken);
            RefreshOrderStatus(order);
            await RefreshWorkStatusesAsync(order.Kalemler.Select(x => x.FinansIsKaydiId), cancellationToken);
        }
        ApplyInvoiceDocumentReconciliationForUpdate(entity, model);
        denetim.DegisiklikleriEkle(nameof(FinansFatura), entity, auditBefore);
        await context.SaveChangesAsync(cancellationToken);
        return await FaturaGetirAsync(id, cancellationToken);
    }

    public Task<FinansFaturaModel> FaturaOlusturAsync(
        FinansFaturaOlusturModel model,
        CancellationToken cancellationToken)
        => islem.AtomikAsync(() => FaturaOlusturCoreAsync(model, cancellationToken), cancellationToken);

    private async Task<FinansFaturaModel> FaturaOlusturCoreAsync(
        FinansFaturaOlusturModel model,
        CancellationToken cancellationToken)
    {
        var invoiceNo = model.FaturaNumarasi.Trim().ToUpperInvariant();
        if (await context.Set<FinansFatura>().AnyAsync(x => x.FaturaNumarasi == invoiceNo, cancellationToken))
            throw IsKuraliException.Cakisma("Bu fatura numarası daha önce kullanılmış; finansal belge numaraları tekrar kullanılamaz.");
        var order = await SiparisDetayQuery(true).FirstOrDefaultAsync(x => x.Id == model.SiparisId, cancellationToken)
            ?? throw IsKuraliException.Cakisma("Sipariş bulunamadı.");
        if (order.IptalEdildi) throw IsKuraliException.Cakisma("İptal edilmiş sipariş faturalanamaz.");
        if (order.Kalemler.Select(x => x.ParaBirimiSnapshot).Distinct().Count() != 1)
            throw IsKuraliException.Cakisma("Fatura oluşturmak için PO satırlarının para birimleri aynı olmalıdır.");
        var istenenKalemler = FaturaKalemleriniHazirla(order, model.Kalemler, model.Gruplar);

        var vadeTarihi = model.VadeTarihi.HasValue
            ? FinansVadeKurallari.VadeDogrula(model.FaturaTarihi, model.VadeTarihi.Value)
            : FinansVadeKurallari.VadeTarihi(model.FaturaTarihi, await FinansSorgulari.VarsayilanVadeGunuAsync(context, cancellationToken));
        var invoice = new FinansFatura
        {
            FinansSiparisId = order.Id,
            KayitNo = NewDocumentNo("FAT"),
            FaturaNumarasi = invoiceNo,
            FaturaTarihi = model.FaturaTarihi,
            VadeTarihi = vadeTarihi,
            Aciklama = model.Aciklama?.Trim()
        };
        foreach (var requested in istenenKalemler)
        {
            var line = order.Kalemler.FirstOrDefault(x => x.Id == requested.SiparisKalemiId)
                ?? throw IsKuraliException.Cakisma("Fatura kalemi siparişe ait değil.");
            var activeInvoiceLines = line.FaturaKalemleri.Where(x => !x.FinansFatura.IptalEdildi).ToList();
            var distribution = requested.NetTutar.HasValue
                ? new FinansDagitimMiktari(0, 0)
                : FinansMiktarKurallari.DagitimiNormalizeEt(
                line.FiyatlandirmaBirimiSnapshot,
                requested.Adet,
                requested.M3,
                line.Adet,
                line.M3,
                activeInvoiceLines.Sum(x => x.Adet),
                activeInvoiceLines.Sum(x => x.M3),
                activeInvoiceLines.Count > 0,
                "Fatura");
            var pricingQuantity = FinansTutarKurallari.FiyatlandirmaMiktari(line.FiyatlandirmaBirimiSnapshot, distribution.Adet, distribution.M3);
            var requestedNet = requested.NetTutar ?? FinansTutarKurallari.Hesapla(pricingQuantity, line.BirimFiyatSnapshot, line.KdvOraniSnapshot).Net;
            FinansTutarKurallari.Kapasite(requestedNet, activeInvoiceLines.Sum(x => x.NetTutarSnapshot), line.NetTutarSnapshot, "Fatura");
            var money = FinansTutarKurallari.FaturaKalemiTutari(requestedNet, line.KdvOraniSnapshot, line.NetTutarSnapshot,
                line.KdvTutariSnapshot, activeInvoiceLines.Sum(x => x.NetTutarSnapshot), activeInvoiceLines.Sum(x => x.KdvTutariSnapshot));
            if (money.Kdv < 0 || money.Toplam + activeInvoiceLines.Sum(x => x.ToplamTutarSnapshot) > line.ToplamTutarSnapshot)
                throw IsKuraliException.Cakisma("Fatura KDV/brüt tutarı PO kaleminin kalanını aşamaz.");
            invoice.Kalemler.Add(new FinansFaturaKalemi
            {
                FinansSiparisKalemiId = line.Id,
                FinansSiparisKalemi = line,
                FinansFatura = invoice,
                TutarBazli = requested.NetTutar.HasValue,
                Adet = distribution.Adet,
                M3 = distribution.M3,
                NetTutarSnapshot = money.Net,
                KdvTutariSnapshot = money.Kdv,
                ToplamTutarSnapshot = money.Toplam
            });
        }

        ApplyInvoiceDocumentReconciliation(
            invoice,
            model.BelgeParaBirimi,
            model.BelgeNetTutar,
            model.BelgeKdvTutari,
            model.BelgeToplamTutar,
            model.MutabakatAciklamasi);

        context.Set<FinansFatura>().Add(invoice);
        await context.SaveChangesAsync(cancellationToken);
        denetim.Ekle(nameof(FinansFatura), invoice.Id, "Oluşturma", "*", null, $"Fatura: {invoice.FaturaNumarasi}");
        RefreshOrderStatus(order);
        await RefreshWorkStatusesAsync(order.Kalemler.Select(x => x.FinansIsKaydiId), cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return await MapFaturaAsync(await FaturaDetayQuery().FirstAsync(x => x.Id == invoice.Id, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Fatura kalemlerini tek tek ve grup düzeyinde istenen tutarlardan kurar. Grup tutarı grubun PO
    /// kalemlerine satır sırasıyla, her satırın faturalanmamış net bakiyesini aşmadan dağıtılır. Bütün
    /// kalemler seçilen PO'ya ait olmalıdır; PO'suz iş faturalanamaz.
    /// </summary>
    internal static IReadOnlyList<FinansFaturaKalemiOlusturModel> FaturaKalemleriniHazirla(
        FinansSiparis order,
        IReadOnlyList<FinansFaturaKalemiOlusturModel> kalemler,
        IReadOnlyList<FinansFaturaGrupKalemiModel>? gruplar)
    {
        gruplar ??= [];
        var tumIds = kalemler.Select(x => x.SiparisKalemiId).Concat(gruplar.SelectMany(x => x.SiparisKalemiIds)).ToArray();
        if (tumIds.Length == 0) throw IsKuraliException.Cakisma("En az bir fatura kalemi seçilmelidir.");
        if (tumIds.Distinct().Count() != tumIds.Length)
            throw IsKuraliException.Cakisma("Aynı sipariş kalemi faturada birden fazla kez kullanılamaz.");
        var byId = order.Kalemler.ToDictionary(x => x.Id);
        if (tumIds.Any(id => !byId.ContainsKey(id)))
            throw IsKuraliException.Cakisma("Fatura kalemi siparişe ait değil.");

        var sonuc = kalemler.ToList();
        foreach (var grup in gruplar)
        {
            var satirlar = grup.SiparisKalemiIds.Select(x => byId[x]).OrderBy(x => x.Id).ToArray();
            var paylar = FinansDagitimKurallari.Dagit(grup.NetTutar,
                satirlar.Select(x => new FinansDagitimKapasitesi(x.Id,
                    x.NetTutarSnapshot - FinansTutarKurallari.FaturaNet(x))).ToArray(),
                "Fatura");
            sonuc.AddRange(paylar.Select(x => new FinansFaturaKalemiOlusturModel(x.Id, 0, 0, x.Tutar)));
        }
        return sonuc;
    }

    public Task<bool> FaturaIptalAsync(int id, string aciklama, CancellationToken cancellationToken)
        => islem.AtomikAsync(() => FaturaIptalCoreAsync(id, aciklama, cancellationToken), cancellationToken);

    private async Task<bool> FaturaIptalCoreAsync(int id, string aciklama, CancellationToken cancellationToken)
    {
        FinansGerekceKurali.Dogrula(aciklama);
        var invoice = await FaturaDetayQuery(true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (invoice is null) return false;
        if (invoice.IptalEdildi) return true;
        if (invoice.OdendiMi)
            throw IsKuraliException.Cakisma("Ödendi olarak işaretli fatura iptal edilemez; önce ödeme işaretini kaldırın.");
        invoice.IptalEdildi = true;
        invoice.IptalTarihi = TurkeyTime.Now;
        invoice.IptalAciklamasi = aciklama.Trim();
        invoice.Durum = FinansFaturaDurumu.IptalEdildi;
        denetim.Ekle(nameof(FinansFatura), id, "İptal", nameof(invoice.IptalEdildi), false, true, aciklama);
        var order = await SiparisDetayQuery(true).FirstAsync(x => x.Id == invoice.FinansSiparisId, cancellationToken);
        RefreshOrderStatus(order);
        await RefreshWorkStatusesAsync(order.Kalemler.Select(x => x.FinansIsKaydiId), cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private void RefreshOrderStatus(FinansSiparis order)
    {
        var oldStatus = order.Durum;
        order.Durum = FinansTutarKurallari.SiparisDurumu(order);
        denetim.DegisiklikEkle(nameof(FinansSiparis), order.Id, nameof(order.Durum), oldStatus, order.Durum);
    }

    private async Task RefreshWorkStatusesAsync(IEnumerable<int> workIds, CancellationToken cancellationToken)
    {
        var ids = workIds.Distinct().ToArray();
        if (ids.Length == 0)
            return;
        var works = await context.IsKaydiDetayQuery(true).Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);
        foreach (var work in works)
        {
            var oldStatus = work.Durum;
            work.Durum = FinansTutarKurallari.Durum(work);
            denetim.DegisiklikEkle(nameof(FinansIsKaydi), work.Id, nameof(FinansIsKaydi.Durum), oldStatus, work.Durum);
        }
    }

    private IQueryable<FinansSiparis> SiparisDetayQuery(bool tracking = false)
    {
        var query = context.Set<FinansSiparis>()
            .Include(x => x.Kalemler)
                .ThenInclude(x => x.FinansIsKaydi)
            .Include(x => x.Kalemler)
                .ThenInclude(x => x.FinansUrun)
            .Include(x => x.Kalemler)
                .ThenInclude(x => x.FaturaKalemleri)
                    .ThenInclude(x => x.FinansFatura)
            .Include(x => x.Giderler.Where(g => !g.IptalEdildi && !g.AvansMi))
            .AsSplitQuery();

        return tracking ? query : query.AsNoTracking();
    }

    /// <summary>
    /// İşlerin bütün aktif PO'larda siparişe bağlanan net toplamı (<see cref="FinansTutarKurallari.SiparisNet(FinansIsKaydi)"/>);
    /// PO satırında işin kalan sipariş bakiyesi bundan hesaplanır.
    /// </summary>
    private async Task<IReadOnlyDictionary<int, decimal>> IsSiparisToplamlariAsync(
        IEnumerable<FinansSiparis> orders,
        CancellationToken cancellationToken)
    {
        var ids = orders.SelectMany(x => x.Kalemler).Select(x => x.FinansIsKaydiId).Distinct().ToArray();
        if (ids.Length == 0) return new Dictionary<int, decimal>();
        return await context.Set<FinansSiparisKalemi>().AsNoTracking()
            .Where(x => ids.Contains(x.FinansIsKaydiId) && !x.FinansSiparis.IptalEdildi)
            .GroupBy(x => x.FinansIsKaydiId)
            .Select(x => new { x.Key, Toplam = x.Sum(y => y.NetTutarSnapshot) })
            .ToDictionaryAsync(x => x.Key, x => x.Toplam, cancellationToken);
    }

    private IQueryable<FinansFatura> FaturaDetayQuery(bool tracking = false)
    {
        var query = context.Set<FinansFatura>()
            .Include(x => x.FinansSiparis)
            .Include(x => x.Kalemler)
                .ThenInclude(x => x.FinansSiparisKalemi)
                    .ThenInclude(x => x.FinansIsKaydi)
            .Include(x => x.Kalemler)
                .ThenInclude(x => x.FinansSiparisKalemi)
                    .ThenInclude(x => x.FinansUrun)
            .AsSplitQuery();

        return tracking ? query : query.AsNoTracking();
    }

    internal static void ApplyInvoiceDocumentReconciliationForUpdate(
        FinansFatura invoice,
        FinansFaturaGuncelleModel model)
    {
        if (model.BelgeMutabakatiniKoru)
            return;

        ApplyInvoiceDocumentReconciliation(
            invoice,
            model.BelgeParaBirimi,
            model.BelgeNetTutar,
            model.BelgeKdvTutari,
            model.BelgeToplamTutar,
            model.MutabakatAciklamasi);
    }

    private static void ApplyInvoiceDocumentReconciliation(
        FinansFatura invoice,
        string? documentCurrency,
        decimal? documentNet,
        decimal? documentVat,
        decimal? documentTotal,
        string? reconciliationNote)
    {
        var hasAnyDocumentValue = !string.IsNullOrWhiteSpace(documentCurrency) ||
                                  documentNet.HasValue ||
                                  documentVat.HasValue ||
                                  documentTotal.HasValue;
        if (!hasAnyDocumentValue)
        {
            invoice.BelgeParaBirimiSnapshot = null;
            invoice.BelgeNetTutarSnapshot = null;
            invoice.BelgeKdvTutariSnapshot = null;
            invoice.BelgeToplamTutarSnapshot = null;
            invoice.MutabakatFarkiSnapshot = 0;
            invoice.MutabakatAciklamasi = null;
            return;
        }

        if (string.IsNullOrWhiteSpace(documentCurrency) ||
            !documentNet.HasValue ||
            !documentVat.HasValue ||
            !documentTotal.HasValue)
            throw IsKuraliException.Cakisma("Belge para birimi, net, KDV ve brüt toplam alanları birlikte girilmelidir.");

        if (documentNet.Value < 0 || documentVat.Value < 0 || documentTotal.Value < 0)
            throw IsKuraliException.Cakisma("Belge tutarları negatif olamaz.");
        if (!FinansTutarKurallari.BelgeToplamiDengeli(documentNet.Value, documentVat.Value, documentTotal.Value))
            throw IsKuraliException.Cakisma("Belge net + KDV toplamı brüt toplamla eşleşmelidir.");

        var lineCurrencies = invoice.Kalemler
            .Select(x => FinansParaBirimiKurali.Normalize(x.FinansSiparisKalemi.ParaBirimiSnapshot))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (lineCurrencies.Length != 1)
            throw IsKuraliException.Cakisma("Farklı para birimindeki fatura kalemleri için tek belge toplamı girilemez.");

        var normalizedCurrency = FinansParaBirimiKurali.Normalize(documentCurrency);
        if (!string.Equals(lineCurrencies[0], normalizedCurrency, StringComparison.OrdinalIgnoreCase))
            throw IsKuraliException.Cakisma("Belge para birimi fatura kalemlerinin para birimiyle eşleşmelidir.");

        var calculatedTotal = invoice.Kalemler.Sum(x => x.ToplamTutarSnapshot);
        if (FinansTutarKurallari.Para(documentNet.Value) != invoice.Kalemler.Sum(x => x.NetTutarSnapshot) ||
            FinansTutarKurallari.Para(documentVat.Value) != invoice.Kalemler.Sum(x => x.KdvTutariSnapshot) ||
            FinansTutarKurallari.Para(documentTotal.Value) != calculatedTotal)
            throw IsKuraliException.Cakisma("Belge net/KDV/brüt tutarları satır toplamlarıyla eşleşmelidir. Farkı PO kapasitesini aşmadan satırlara dağıtın.");
        var difference = decimal.Round(documentTotal.Value - calculatedTotal, 2, MidpointRounding.AwayFromZero);
        var normalizedNote = reconciliationNote?.Trim();
        if (Math.Abs(difference) > 0.02m && string.IsNullOrWhiteSpace(normalizedNote))
            throw IsKuraliException.Cakisma("Belge ile hesaplanan brüt toplam arasındaki fark için mutabakat açıklaması zorunludur.");

        invoice.BelgeParaBirimiSnapshot = normalizedCurrency;
        invoice.BelgeNetTutarSnapshot = decimal.Round(documentNet.Value, 2, MidpointRounding.AwayFromZero);
        invoice.BelgeKdvTutariSnapshot = decimal.Round(documentVat.Value, 2, MidpointRounding.AwayFromZero);
        invoice.BelgeToplamTutarSnapshot = decimal.Round(documentTotal.Value, 2, MidpointRounding.AwayFromZero);
        invoice.MutabakatFarkiSnapshot = difference;
        invoice.MutabakatAciklamasi = string.IsNullOrWhiteSpace(normalizedNote) ? null : normalizedNote;
    }

    private static IQueryable<FinansSiparisKalemi> ScopeOrderLines(IQueryable<FinansSiparisKalemi> query, FinansListeFiltre filtre)
    {
        if (filtre.ProjeId.HasValue) query = query.Where(x => x.FinansIsKaydi.ProjeId == filtre.ProjeId);
        if (!string.IsNullOrWhiteSpace(filtre.ProjeNo)) query = query.Where(x => x.FinansIsKaydi.ProjeNo == filtre.ProjeNo);
        if (filtre.IsTuru.HasValue) query = query.Where(x => x.FinansIsKaydi.IsTuru == filtre.IsTuru);
        if (filtre.SandikCinsi.HasValue) query = query.Where(x => x.FinansIsKaydi.SandikCinsi == filtre.SandikCinsi);
        if (!string.IsNullOrWhiteSpace(filtre.Firma)) query = query.Where(x => x.FinansIsKaydi.Musteri.ToLower().Contains(filtre.Firma.ToLower()));
        if (!string.IsNullOrWhiteSpace(filtre.ParaBirimi)) query = query.Where(x => x.ParaBirimiSnapshot == filtre.ParaBirimi.Trim().ToUpper());
        if (!string.IsNullOrWhiteSpace(filtre.FaturaNumarasi)) query = query.Where(x => x.FaturaKalemleri.Any(f => f.FinansFatura.FaturaNumarasi.ToLower().Contains(filtre.FaturaNumarasi.ToLower())));
        return query;
    }

    private static string NewDocumentNo(string prefix)
        => $"{prefix}-{TurkeyTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..Math.Min(31, prefix.Length + 1 + 14 + 1 + 32)];
}
