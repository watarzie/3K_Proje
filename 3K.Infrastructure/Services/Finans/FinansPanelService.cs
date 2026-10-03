using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>Finans paneli, para hareketleri, genel arama, yaşlandırma ve denetim geçmişi (salt okunur).</summary>
internal sealed class FinansPanelService(AppDbContext context) : IFinansPanelService
{
    public async Task<FinansPanelModel> PanelAsync(DateTime baslangic, DateTime bitis, CancellationToken cancellationToken)
    {
        FinansTarihKurallari.AraligiDogrula(baslangic, bitis);
        var yearStart = new DateTime(bitis.Year, 1, 1);
        var loadStart = baslangic < yearStart ? baslangic.Date : yearStart;
        var end = bitis.Date.AddDays(1);
        var worksQuery = context.IsKaydiDetayQuery().Where(x => !x.IptalEdildi && x.KaynakAktif && x.FinansTarihi >= loadStart && x.FinansTarihi < end);
        if (await worksQuery.CountAsync(cancellationToken) > FinansSorgulari.RaporKayitSiniri) throw IsKuraliException.Cakisma("Panel aralığında 20.000'den fazla iş var; tarih aralığını daraltın.");
        var allWorks = await worksQuery.ToListAsync(cancellationToken);
        var expenseQuery = context.Set<FinansGider>().AsNoTracking().Include(x => x.Kategori).Include(x => x.Proje)
            .Where(x => !x.IptalEdildi && !x.AvansMi && x.FinansTarihi >= loadStart && x.FinansTarihi < end);
        if (await expenseQuery.CountAsync(cancellationToken) > FinansSorgulari.RaporKayitSiniri) throw IsKuraliException.Cakisma("Panel aralığında 20.000'den fazla gider var; tarih aralığını daraltın.");
        var allExpenses = await expenseQuery.ToListAsync(cancellationToken);
        var works = allWorks.Where(x => x.FinansTarihi >= baslangic.Date).Select(FinansModelEslemeleri.MapIsKaydi).ToArray();
        var expenses = allExpenses.Where(x => x.FinansTarihi >= baslangic.Date).ToArray();
        var currencies = await FinansSorgulari.OzetParaBirimleriAsync(context,
            allWorks.Select(x => x.ParaBirimiSnapshot).Concat(allExpenses.Select(x => x.ParaBirimi)), cancellationToken);
        var totals = currencies.Select(currency =>
        {
            var w = works.Where(x => x.ParaBirimi == currency).ToArray();
            var income = w.Sum(x => x.FaturalananNetTutar);
            var cost = expenses.Where(x => x.ParaBirimi == currency).Sum(x => x.Matrah);
            var eligible = w.Sum(x => x.NetTutar);
            return new FinansPanelParaModel(currency, eligible, w.Sum(x => x.SiparisNetTutar), w.Sum(x => x.KalanSiparisNetTutar), w.Sum(x => x.KalanFaturaNetTutar),
                income, cost, income - cost, eligible - cost, eligible == 0 ? null : FinansTutarKurallari.Para((eligible - cost) / eligible * 100m),
                allWorks.Where(x => x.FinansTarihi >= yearStart && x.ParaBirimiSnapshot == currency).Sum(FinansTutarKurallari.FaturaNet),
                allExpenses.Where(x => x.FinansTarihi >= yearStart && x.ParaBirimi == currency).Sum(x => x.Matrah));
        }).ToArray();
        var monthly = allWorks.GroupBy(x => new { Month = FinansTarihKurallari.AyBasi(x.FinansTarihi), Currency = x.ParaBirimiSnapshot })
            .Select(x => new FinansGrafikModel(x.Key.Month.ToString("yyyy-MM"), x.Key.Currency, x.Sum(FinansTutarKurallari.FaturaNet), 0m))
            .Concat(allExpenses.GroupBy(x => new { Month = FinansTarihKurallari.AyBasi(x.FinansTarihi), Currency = x.ParaBirimi })
                .Select(x => new FinansGrafikModel(x.Key.Month.ToString("yyyy-MM"), x.Key.Currency, 0m, x.Sum(y => y.Matrah))))
            .GroupBy(x => new { x.Grup, x.ParaBirimi }).Select(x => new FinansGrafikModel(x.Key.Grup, x.Key.ParaBirimi, x.Sum(y => y.Gelir), x.Sum(y => y.Gider))).OrderBy(x => x.Grup).ToArray();
        var types = works.GroupBy(x => new { x.IsTuru, x.ParaBirimi }).Select(x => new FinansGrafikModel(x.Key.IsTuru.ToString(), x.Key.ParaBirimi, x.Sum(y => y.FaturalananNetTutar), 0m)).ToArray();
        var costs = expenses.GroupBy(x => new { Category = x.Kategori.Ad, x.ParaBirimi }).Select(x => new FinansGrafikModel(x.Key.Category, x.Key.ParaBirimi, 0m, x.Sum(y => y.Matrah))).ToArray();
        var bugun = TurkeyTime.Now.Date;
        var uyariGunu = await FinansSorgulari.VadeUyariGunuAsync(context, cancellationToken);
        var (alacaklar, beklenenTahsilat) = await AlacakOzetiAsync(baslangic.Date, end, bugun, uyariGunu, currencies, cancellationToken);
        var yaslandirma = await YaslandirmaKovalariAsync(bugun, cancellationToken);
        var (sayilar, aylikSayilar) = await SayilarAsync(baslangic.Date, bitis.Date, bugun, uyariGunu, works, expenses.Length, cancellationToken);
        return new(baslangic.Date, bitis.Date, "İş ve gider finans tarihi; gelir=faturalanan net, tahmini kâr=uygun iş net bedeli−kayıtlı net gider; avans maliyet değildir.",
            totals, monthly, types, costs, works.Count(x => x.SiparisNetTutar == 0), works.Count(x => x.SiparisNetTutar > 0 && x.KalanSiparisNetTutar > 0),
            works.Count(x => x.NetTutar > 0 && x.KalanSiparisNetTutar == 0), works.Count(x => x.FaturalananNetTutar > 0 && x.Durum != FinansIsDurumu.Faturalandi), works.Count(x => x.Durum == FinansIsDurumu.Faturalandi),
            works.GroupBy(x => new { x.ProjeNo, x.ParaBirimi }).Select(x => new FinansGrafikModel(x.Key.ProjeNo, x.Key.ParaBirimi, x.Sum(y => y.FaturalananNetTutar),
                expenses.Where(g => (g.Proje?.ProjeNo ?? g.ManuelProjeNo ?? "") == x.Key.ProjeNo && g.ParaBirimi == x.Key.ParaBirimi).Sum(g => g.Matrah))).ToArray(),
            works.Where(x => x.IsTuru == FinansIsTuru.OzelIs).GroupBy(x => new { x.OzelIsTuru, x.ParaBirimi }).Select(x => new FinansGrafikModel(x.Key.OzelIsTuru ?? "Özel İş", x.Key.ParaBirimi, x.Sum(y => y.FaturalananNetTutar), 0)).ToArray(),
            ProjeDurumSayilari(allWorks.Where(x => x.FinansTarihi >= baslangic.Date)),
            alacaklar, beklenenTahsilat, yaslandirma, uyariGunu, sayilar, aylikSayilar,
            ProjeOzetleri(works, expenses), KategoriOzetleri(works));
    }

    /// <summary>
    /// Proje × para birimi detay dökümü; gider, aynı proje numarası ve para birimiyle yazılmış dönem giderleridir.
    /// Durum, projenin dönemdeki bütün kalemlerinden <see cref="FinansTutarKurallari.BirlesikDurum"/> ile türetilir.
    /// </summary>
    internal static IReadOnlyList<FinansPanelProjeOzetiModel> ProjeOzetleri(IReadOnlyCollection<FinansIsKaydiModel> works, IReadOnlyCollection<FinansGider> expenses)
    {
        var giderler = expenses.GroupBy(x => (Proje: x.Proje?.ProjeNo ?? x.ManuelProjeNo ?? "", x.ParaBirimi))
            .ToDictionary(x => x.Key, x => x.Sum(y => y.Matrah));
        return works.GroupBy(x => (x.ProjeNo, x.ParaBirimi)).Select(g =>
        {
            var durum = FinansTutarKurallari.BirlesikDurum(g.Select(x => new FinansKalemTutarlari(x.NetTutar, x.SiparisNetTutar, x.FaturalananNetTutar)));
            return new FinansPanelProjeOzetiModel(g.Key.ProjeNo, g.Select(x => x.Musteri).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "",
                g.Key.ParaBirimi, g.Count(), g.Sum(x => x.NetTutar), g.Sum(x => x.SiparisNetTutar), g.Sum(x => x.FaturalananNetTutar),
                g.Sum(x => x.KalanSiparisNetTutar), g.Sum(x => x.KalanFaturaNetTutar), giderler.GetValueOrDefault((g.Key.ProjeNo, g.Key.ParaBirimi)),
                durum.Siparis, durum.Fatura);
        }).OrderByDescending(x => x.IsBedeli).ThenBy(x => x.ProjeNo, StringComparer.Ordinal).ToArray();
    }

    /// <summary>Kategori × para birimi detay dökümü (kategori kuralı <see cref="FinansKategoriKurali"/>).</summary>
    internal static IReadOnlyList<FinansPanelKategoriOzetiModel> KategoriOzetleri(IReadOnlyCollection<FinansIsKaydiModel> works)
        => works.GroupBy(x => (x.Kategori, x.ParaBirimi)).Select(g => new FinansPanelKategoriOzetiModel(g.Key.Kategori, g.Key.ParaBirimi, g.Count(),
                g.Select(x => x.ProjeNo).Distinct(StringComparer.OrdinalIgnoreCase).Count(), g.Sum(x => x.NetTutar), g.Sum(x => x.SiparisNetTutar), g.Sum(x => x.FaturalananNetTutar)))
            .OrderBy(x => x.Kategori).ThenBy(x => x.ParaBirimi, StringComparer.Ordinal).ToArray();

    /// <summary>
    /// Genel durum sayıları ve aylık adet grafiği: PO sipariş tarihine, fatura fatura tarihine, iş kalemi finans
    /// tarihine göre seçili aralıkta sayılır; iptaller hariçtir. Ödeme kırılımı aralıktaki faturaların bugünkü durumudur.
    /// </summary>
    private async Task<(FinansPanelSayilariModel Sayilar, IReadOnlyList<FinansPanelAylikSayiModel> Aylik)> SayilarAsync(
        DateTime baslangic,
        DateTime bitis,
        DateTime bugun,
        int uyariGunu,
        IReadOnlyCollection<FinansIsKaydiModel> works,
        int giderKaydi,
        CancellationToken cancellationToken)
    {
        var bitisHaric = bitis.AddDays(1);
        var siparisler = await context.Set<FinansSiparis>().AsNoTracking()
            .Where(x => !x.IptalEdildi && x.SiparisTarihi >= baslangic && x.SiparisTarihi < bitisHaric)
            .Select(x => new { x.SiparisTarihi, x.Durum }).ToListAsync(cancellationToken);
        var faturalar = await context.Set<FinansFatura>().AsNoTracking()
            .Where(x => !x.IptalEdildi && x.FaturaTarihi >= baslangic && x.FaturaTarihi < bitisHaric)
            .Select(x => new { x.FaturaTarihi, x.VadeTarihi, x.OdendiMi, x.OdemeTarihi }).ToListAsync(cancellationToken);
        var odenmemis = faturalar.Where(x => !x.OdendiMi).ToList();
        var sayilar = new FinansPanelSayilariModel(
            works.Count,
            works.Select(x => x.ProjeNo).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            siparisler.Count,
            siparisler.Count(x => x.Durum == FinansSiparisDurumu.Acik),
            siparisler.Count(x => x.Durum == FinansSiparisDurumu.KismiFaturalandi),
            siparisler.Count(x => x.Durum == FinansSiparisDurumu.Faturalandi),
            faturalar.Count,
            faturalar.Count - odenmemis.Count,
            odenmemis.Count,
            odenmemis.Count(x => FinansVadeKurallari.Durum(false, x.VadeTarihi, null, bugun, uyariGunu) == FinansOdemeDurumu.VadesiGecti),
            giderKaydi);

        var isAylari = works.GroupBy(x => FinansTarihKurallari.AyBasi(x.FinansTarihi)).ToDictionary(x => x.Key, x => x.Count());
        var poAylari = siparisler.GroupBy(x => FinansTarihKurallari.AyBasi(x.SiparisTarihi)).ToDictionary(x => x.Key, x => x.Count());
        var faturaAylari = faturalar.GroupBy(x => FinansTarihKurallari.AyBasi(x.FaturaTarihi)).ToDictionary(x => x.Key, x => x.Count());
        var veriAylari = isAylari.Keys.Concat(poAylari.Keys).Concat(faturaAylari.Keys).ToList();
        var aylik = new List<FinansPanelAylikSayiModel>();
        if (veriAylari.Count > 0)
        {
            // Grafik ilk veri ayından aralık sonuna kesintisizdir; boş aylar sıfır görünür.
            var sonAy = FinansTarihKurallari.AyBasi(bitis);
            for (var ay = veriAylari.Min(); ay <= sonAy; ay = ay.AddMonths(1))
                aylik.Add(new FinansPanelAylikSayiModel(ay.ToString("yyyy-MM"),
                    isAylari.GetValueOrDefault(ay), poAylari.GetValueOrDefault(ay), faturaAylari.GetValueOrDefault(ay)));
        }
        return (sayilar, aylik);
    }

    /// <summary>
    /// Alacak özeti (D3, F3), para birimi bazında: ödenmemiş faturalar bugünün durumudur (seçili dönemden bağımsız);
    /// zamanında/geç ödenen sayıları ve ödenen tutar, ödeme tarihi seçili aralıkta olan faturalardır. Beklenen
    /// tahsilat ödenmemiş faturaların vade ayına göre dağılımıdır.
    /// </summary>
    private async Task<(IReadOnlyList<FinansAlacakOzetiModel> Ozet, IReadOnlyList<FinansGrafikModel> BeklenenTahsilat)> AlacakOzetiAsync(
        DateTime baslangic,
        DateTime bitisHaric,
        DateTime bugun,
        int uyariGunu,
        IReadOnlyList<string> paraBirimleri,
        CancellationToken cancellationToken)
    {
        var query = context.Set<FinansFatura>().AsNoTracking().Where(x => !x.IptalEdildi &&
            (!x.OdendiMi || (x.OdemeTarihi >= baslangic && x.OdemeTarihi < bitisHaric)));
        if (await query.CountAsync(cancellationToken) > FinansSorgulari.RaporKayitSiniri)
            throw IsKuraliException.Cakisma("Panel aralığında 20.000'den fazla fatura var; tarih aralığını daraltın.");
        var satirlar = await FinansSorgulari.AlacakSatirlariAsync(query, cancellationToken);
        var odenmemis = satirlar.Where(x => !x.OdendiMi).ToList();
        var odenen = satirlar.Where(x => x.OdendiMi).ToList();
        var ozet = paraBirimleri.Union(satirlar.Select(x => x.ParaBirimi), StringComparer.Ordinal).Select(currency =>
        {
            var acik = odenmemis.Where(x => x.ParaBirimi == currency).ToList();
            var gecen = acik.Where(x => FinansVadeKurallari.Durum(false, x.VadeTarihi, null, bugun, uyariGunu) == FinansOdemeDurumu.VadesiGecti).ToList();
            var yaklasan = acik.Where(x => FinansVadeKurallari.Durum(false, x.VadeTarihi, null, bugun, uyariGunu) == FinansOdemeDurumu.VadesiYaklasiyor).ToList();
            var kapanan = odenen.Where(x => x.ParaBirimi == currency).ToList();
            var zamaninda = kapanan.Count(x => FinansVadeKurallari.Durum(true, x.VadeTarihi, x.OdemeTarihi, bugun, uyariGunu) == FinansOdemeDurumu.ZamanindaOdendi);
            return new FinansAlacakOzetiModel(currency, acik.Sum(x => x.Toplam), acik.Count, gecen.Sum(x => x.Toplam), gecen.Count,
                yaklasan.Sum(x => x.Toplam), yaklasan.Count, zamaninda, kapanan.Count - zamaninda, kapanan.Sum(x => x.Toplam));
        }).ToArray();
        var beklenen = odenmemis.GroupBy(x => new { Ay = FinansTarihKurallari.AyBasi(x.VadeTarihi), x.ParaBirimi })
            .Select(x => new FinansGrafikModel(x.Key.Ay.ToString("yyyy-MM"), x.Key.ParaBirimi, x.Sum(y => y.Toplam), 0m))
            .OrderBy(x => x.Grup).ThenBy(x => x.ParaBirimi).ToArray();
        return (ozet, beklenen);
    }

    /// <summary>Panel yaşlandırma eşikleri (R1 §5); kovalar kümülatiftir.</summary>
    internal static readonly int[] YaslandirmaEsikleri = [15, 30, 60, 90];

    /// <summary>
    /// Sipariş ve fatura bekleyen kalemlerin 15+/30+/60+/90+ gün kovaları. Kova, aynı eşikle çağrılan
    /// <see cref="YaslandirmaAsync"/> listesinin (tarih filtresiz, bugün itibarıyla) sayısı ve tutarıdır.
    /// </summary>
    private async Task<IReadOnlyList<FinansYaslandirmaKovasiModel>> YaslandirmaKovalariAsync(DateTime bugun, CancellationToken cancellationToken)
    {
        var bekleyenler = await BekleyenKalemlerAsync(bugun, new FinansListeFiltre(), cancellationToken);
        return FinansBekleyenAsamalari.Tumu.SelectMany(asama => YaslandirmaEsikleri.Select(esik =>
        {
            var kova = bekleyenler.Where(x => x.Asama == asama && x.Gun >= esik).ToList();
            return new FinansYaslandirmaKovasiModel(asama, esik, kova.Count, kova.GroupBy(x => x.ParaBirimi).OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => new FinansParaToplamiModel(x.Key, x.Sum(y => y.KalanNetTutar ?? 0), 0, x.Sum(y => y.KalanNetTutar ?? 0))).ToArray());
        })).ToArray();
    }

    /// <summary>Proje birimlerinin birleşik sipariş/fatura durumuna göre sayılması (F4).</summary>
    internal static FinansProjeDurumSayilariModel ProjeDurumSayilari(IEnumerable<FinansIsKaydi> works)
    {
        var durumlar = works
            .Where(x => x.IsTuru != FinansIsTuru.OzelIs && !x.IptalEdildi && x.KaynakAktif)
            .GroupBy(x => FinansAylikOzetService.ProjectUnitKey(x.ProjeId, x.ProjeNo, x.Musteri))
            .Select(x => FinansTutarKurallari.BirlesikDurum(x.Select(FinansTutarKurallari.KalemTutarlari)))
            .ToList();
        var fiyatli = durumlar.Where(x => x.GerekliKalem > 0).ToList();
        return new FinansProjeDurumSayilariModel(
            durumlar.Count,
            fiyatli.Count(x => x.Siparis == FinansKarsilanma.Yok),
            fiyatli.Count(x => x.Siparis == FinansKarsilanma.Kismi),
            fiyatli.Count(x => x.Siparis == FinansKarsilanma.Tam),
            fiyatli.Count(x => x.Fatura == FinansKarsilanma.Yok),
            fiyatli.Count(x => x.Fatura == FinansKarsilanma.Kismi),
            fiyatli.Count(x => x.Fatura == FinansKarsilanma.Tam),
            fiyatli.Count(x => x.Durum == FinansIsDurumu.Faturalandi),
            durumlar.Count(x => x.FiyatBekleyenKalem > 0));
    }

    public async Task<FinansSayfaliSonuc<FinansHareketModel>> HareketlerAsync(FinansListeFiltre filtre, string? tur, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(tur) && tur is not ("IsKaydi" or "Siparis" or "Fatura" or "Gider"))
            throw IsKuraliException.Cakisma("Hareket türü geçerli değil.");
        var page = Math.Max(1, filtre.PageNumber); var size = Math.Clamp(filtre.PageSize, 1, 250);
        var take = checked(page * size);
        if (take > FinansSorgulari.RaporKayitSiniri) throw IsKuraliException.Cakisma("Bu sayfa için filtreyi daraltın; en fazla 20.000 hareket taranabilir.");
        var workQuery = FinansSorgulari.ApplyFilter(context.Set<FinansIsKaydi>().AsNoTracking(), filtre);
        var expenseQuery = FinansSorgulari.ExpenseFilter(context.Set<FinansGider>().AsNoTracking(), filtre);
        var linkedWorkIds = FinansSorgulari.ApplyFilter(context.Set<FinansIsKaydi>().AsNoTracking(), filtre with { Baslangic = null, Bitis = null }).Select(x => x.Id);
        var orderQuery = context.Set<FinansSiparis>().AsNoTracking().Where(x => x.Kalemler.Any(k => linkedWorkIds.Contains(k.FinansIsKaydiId)));
        var invoiceQuery = context.Set<FinansFatura>().AsNoTracking().Where(x => x.Kalemler.Any(k => linkedWorkIds.Contains(k.FinansSiparisKalemi.FinansIsKaydiId)));
        if (!filtre.IptalEdilenleriDahilEt) { orderQuery = orderQuery.Where(x => !x.IptalEdildi); invoiceQuery = invoiceQuery.Where(x => !x.IptalEdildi); }
        if (filtre.Baslangic.HasValue) { orderQuery = orderQuery.Where(x => x.SiparisTarihi >= filtre.Baslangic.Value.Date); invoiceQuery = invoiceQuery.Where(x => x.FaturaTarihi >= filtre.Baslangic.Value.Date); }
        if (filtre.Bitis.HasValue) { var end = filtre.Bitis.Value.Date.AddDays(1); orderQuery = orderQuery.Where(x => x.SiparisTarihi < end); invoiceQuery = invoiceQuery.Where(x => x.FaturaTarihi < end); }
        if (!string.IsNullOrWhiteSpace(tur))
        {
            if (tur != "IsKaydi") workQuery = workQuery.Where(x => false);
            if (tur != "Gider") expenseQuery = expenseQuery.Where(x => false);
            if (tur != "Siparis") orderQuery = orderQuery.Where(x => false);
            if (tur != "Fatura") invoiceQuery = invoiceQuery.Where(x => false);
        }
        var count = await workQuery.CountAsync(cancellationToken) + await expenseQuery.CountAsync(cancellationToken) +
            await orderQuery.CountAsync(cancellationToken) + await invoiceQuery.CountAsync(cancellationToken);
        // Sıralama ve sınır domain sorgusunda uygulanır; record constructor üstüne ORDER BY EF tarafından çevrilemez.
        var works = await workQuery.OrderByDescending(x => x.FinansTarihi).ThenByDescending(x => x.Id).Take(take)
            .Select(FinansSorgulari.IsNetIle((x, net) => new FinansHareketModel("IsKaydi", x.Id, x.ProjeNo, x.IsAdi, x.UretimTarihi,
                x.FinansTarihi, x.FinansDonemi, x.ParaBirimiSnapshot, net, x.Durum.ToString()))).ToListAsync(cancellationToken);
        var expenses = await expenseQuery.OrderByDescending(x => x.FinansTarihi).ThenByDescending(x => x.Id).Take(take)
            .Select(x => new FinansHareketModel("Gider", x.Id, x.Proje != null ? x.Proje.ProjeNo : x.ManuelProjeNo ?? "", x.Aciklama,
                x.Tarih, x.FinansTarihi, x.FinansDonemi, x.ParaBirimi, x.Matrah, x.IptalEdildi ? "İptal" : x.AvansMi ? "Avans" : "Gider")).ToListAsync(cancellationToken);
        var orders = await orderQuery.OrderByDescending(x => x.SiparisTarihi).ThenByDescending(x => x.Id).Take(take)
            .Select(x => new FinansHareketModel("Siparis", x.Id, "", x.PoNumarasi, x.SiparisTarihi, x.SiparisTarihi, x.SiparisTarihi,
                x.ParaBirimi ?? "", x.Kalemler.Sum(k => k.NetTutarSnapshot), x.Durum.ToString())).ToListAsync(cancellationToken);
        var invoices = await invoiceQuery.OrderByDescending(x => x.FaturaTarihi).ThenByDescending(x => x.Id).Take(take)
            .Select(x => new FinansHareketModel("Fatura", x.Id, "", x.FaturaNumarasi, x.FaturaTarihi, x.FaturaTarihi, x.FaturaTarihi,
                x.BelgeParaBirimiSnapshot ?? x.FinansSiparis.ParaBirimi ?? "", x.Kalemler.Sum(k => k.NetTutarSnapshot), x.Durum.ToString())).ToListAsync(cancellationToken);
        return new() { Items = works.Concat(expenses).Concat(orders).Concat(invoices).OrderByDescending(x => x.FinansTarihi).ThenBy(x => x.Tur).ThenByDescending(x => x.Id)
            .Skip((page - 1) * size).Take(size).ToArray(), PageNumber = page, PageSize = size, TotalCount = count };
    }

    public Task<FinansSayfaliSonuc<FinansIsKaydiModel>> GenelAramaAsync(FinansListeFiltre filtre, CancellationToken cancellationToken)
        => IsKayitlariAsync(filtre with { Baslangic = null, Bitis = null }, cancellationToken);

    private async Task<FinansSayfaliSonuc<FinansIsKaydiModel>> IsKayitlariAsync(
        FinansListeFiltre filtre,
        CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(1, filtre.PageNumber);
        var pageSize = Math.Clamp(filtre.PageSize, 1, 250);
        var query = FinansSorgulari.ApplyFilter(context.IsKaydiDetayQuery(), filtre);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.FinansDonemi)
            .ThenByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new FinansSayfaliSonuc<FinansIsKaydiModel>
        {
            Items = items.Select(FinansModelEslemeleri.MapIsKaydi).ToArray(),
            Toplamlar = await WorkMoneyTotalsAsync(query.Where(x => !x.IptalEdildi && x.KaynakAktif), cancellationToken),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = count
        };
    }

    private static async Task<IReadOnlyList<FinansParaToplamiModel>> WorkMoneyTotalsAsync(IQueryable<FinansIsKaydi> query, CancellationToken cancellationToken)
        => await query.Select(FinansSorgulari.IsNetIle((x, net) => new { Currency = x.ParaBirimiSnapshot, Vat = x.KdvOraniSnapshot, Net = net }))
            .GroupBy(x => x.Currency).Select(g => new FinansParaToplamiModel(g.Key, g.Sum(x => x.Net),
                g.Sum(x => decimal.Round(x.Net * x.Vat / 100m, 2)), g.Sum(x => x.Net + decimal.Round(x.Net * x.Vat / 100m, 2))))
            .ToArrayAsync(cancellationToken);

    public async Task<FinansSayfaliSonuc<FinansBekleyenModel>> YaslandirmaAsync(DateTime referansTarihi, int minimumGun, string? asama, FinansListeFiltre filtre, CancellationToken cancellationToken)
    {
        if (minimumGun < 0 || minimumGun > 36600) throw IsKuraliException.Cakisma("Geçerli bekleme gününü girin.");
        if (asama is not null && !FinansBekleyenAsamalari.Tumu.Contains(asama))
            throw IsKuraliException.Cakisma("Bekleme aşaması Sipariş veya Fatura olmalıdır.");
        var items = (await BekleyenKalemlerAsync(referansTarihi, filtre, cancellationToken))
            .Where(x => x.Gun >= minimumGun && (asama is null || x.Asama == asama)).ToList();
        var page = Math.Max(1, filtre.PageNumber); var size = Math.Clamp(filtre.PageSize, 1, 250);
        return new() { Items = items.OrderByDescending(x => x.Gun).ThenBy(x => x.IsKaydiId).Skip((page - 1) * size).Take(size).ToArray(),
            Toplamlar = items.GroupBy(x => x.ParaBirimi).Select(x => new FinansParaToplamiModel(x.Key, x.Sum(y => y.KalanNetTutar ?? 0), 0, x.Sum(y => y.KalanNetTutar ?? 0))).ToArray(),
            PageNumber = page, PageSize = size, TotalCount = items.Count };
    }

    /// <summary>
    /// Sipariş bekleyen (iş kaydı oluştuğundan beri) ve fatura bekleyen (PO tarihinden beri) kalemler; bekleme
    /// süresi referans tarihine göre hesaplanır. Finans tarihi filtresi uygulanmaz, bütün birikmiş iş taranır.
    /// </summary>
    private async Task<List<FinansBekleyenModel>> BekleyenKalemlerAsync(DateTime referansTarihi, FinansListeFiltre filtre, CancellationToken cancellationToken)
    {
        var query = FinansSorgulari.ApplyFilter(context.IsKaydiDetayQuery(), filtre with { Baslangic = null, Bitis = null, IptalEdilenleriDahilEt = false })
            .Where(x => x.Durum != FinansIsDurumu.Faturalandi);
        if (await query.CountAsync(cancellationToken) > FinansSorgulari.RaporKayitSiniri) throw IsKuraliException.Cakisma("Yaşlandırma için proje veya iş türü filtresini daraltın (en fazla 20.000 iş).");
        var works = await query.ToListAsync(cancellationToken);
        var items = new List<FinansBekleyenModel>();
        foreach (var work in works)
        {
            var remaining = FinansTutarKurallari.IsNet(work) - FinansTutarKurallari.SiparisNet(work);
            var age = Math.Max(0, (referansTarihi.Date - work.KayitTarihi.Date).Days);
            if (remaining > 0) items.Add(new(work.Id, null, work.ProjeNo, work.IsAdi, null, FinansBekleyenAsamalari.Siparis, work.KayitTarihi.Date, age, YasGrubu(age), work.ParaBirimiSnapshot, remaining));
            foreach (var line in work.SiparisKalemleri.Where(x => !x.FinansSiparis.IptalEdildi))
            {
                remaining = line.NetTutarSnapshot - FinansTutarKurallari.FaturaNet(line);
                age = Math.Max(0, (referansTarihi.Date - line.FinansSiparis.SiparisTarihi.Date).Days);
                if (remaining > 0) items.Add(new(work.Id, line.Id, work.ProjeNo, work.IsAdi, line.FinansSiparis.PoNumarasi, FinansBekleyenAsamalari.Fatura, line.FinansSiparis.SiparisTarihi.Date, age, YasGrubu(age), line.ParaBirimiSnapshot, remaining));
            }
        }
        return items;
    }

    internal static string YasGrubu(int days) => days >= 90 ? "90+" : days >= 60 ? "60–89" : days >= 30 ? "30–59" : days >= 15 ? "15–29" : "0–14";

    public async Task<FinansSayfaliSonuc<FinansDegisiklikModel>> DegisiklikGecmisiAsync(
        string? varlikTuru,
        int? varlikId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = context.Set<FinansDegisiklikGecmisi>().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(varlikTuru))
        {
            var canonical = varlikTuru.Trim() switch { "IsKaydi" => nameof(FinansIsKaydi), "Siparis" => nameof(FinansSiparis), "Fatura" => nameof(FinansFatura), "Gider" => nameof(FinansGider), var value => value };
            query = query.Where(x => x.VarlikTuru == canonical);
        }
        if (varlikId.HasValue) query = query.Where(x => x.VarlikId == varlikId);
        var page = Math.Max(1, pageNumber);
        var size = Math.Clamp(pageSize, 1, 250);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedDate).ThenByDescending(x => x.Id)
            .Skip((page - 1) * size).Take(size)
            .Select(x => new FinansDegisiklikModel(x.Id, x.VarlikTuru, x.VarlikId, x.Islem, x.AlanAdi,
                x.EskiDeger, x.YeniDeger, x.Aciklama, x.CreatedDate, x.IslemYapan, x.IslemGrubu, x.Referans))
            .ToListAsync(cancellationToken);
        return new FinansSayfaliSonuc<FinansDegisiklikModel>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = count
        };
    }
}
