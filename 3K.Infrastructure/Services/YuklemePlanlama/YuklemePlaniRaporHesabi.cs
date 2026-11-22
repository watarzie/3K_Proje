using _3K.Core.Enums;
using _3K.Core.Models;
using _3K.YuklemeMotoru.Ekipman;
using _3K.YuklemeMotoru.Yerlesim;

namespace _3K.Infrastructure.Services.YuklemePlanlama;

/// <summary>
/// Araç bazlı yükleme planı raporunun sayıları (araç iç ölçüleri, kullanım, yüklü toplam, ağırlık merkezi, plan özeti)
/// ve sandık renkleri. Saf hesaplardır: plan modelinden ve ekipman teknik verisinden türetilir, hiçbir şey saklanmaz.
/// Ekipman teknik verisi motor projesinde olduğu için Core'da değil buradadır; ağırlık merkezi motorun hesabıdır
/// (<see cref="AgirlikMerkeziHesabi"/>), burada yeniden yazılmaz.
/// </summary>
internal static class YuklemePlaniRaporHesabi
{
    private const decimal Mm3M3 = 1_000_000_000m;

    // ===== Araç =====

    /// <summary>
    /// Aracın yükleme ölçüleri. Uzunluk bütün yükleme yüzeylerinin toplamıdır; genişlik ve iç yükseklik ana yüzeyindir
    /// (alanı en büyük yüzey; lowbed'de havuz). Tek yüzeyli araçta üçü de o yüzeyin ölçüsüdür. İç yükseklik açık
    /// platformda tanımlı değildir (boş). Kullanılabilir hacim ancak bütün yüzeylerin iç yüksekliği biliniyorsa vardır.
    /// </summary>
    internal sealed record AracOlculeri(
        int? IcUzunlukMm, int? IcGenislikMm, int? IcYukseklikMm, int YuzeySayisi, string? AnaYuzeyAdi, decimal? KullanilabilirHacimM3);

    internal static AracOlculeri Olculer(EkipmanTeknikVerisi veri)
    {
        if (veri.Yuzeyler.Count == 0)
            return new AracOlculeri(null, null, null, 0, null, null);
        var ana = veri.Yuzeyler.MaxBy(y => (long)y.UzunlukMm * y.GenislikMm)!;
        decimal? hacim = veri.Yuzeyler.All(y => y.KullanilabilirYukseklikMm != null)
            ? veri.Yuzeyler.Sum(y => (decimal)y.UzunlukMm * y.GenislikMm * y.KullanilabilirYukseklikMm!.Value) / Mm3M3
            : null;
        return new AracOlculeri(veri.Yuzeyler.Sum(y => y.UzunlukMm), ana.GenislikMm, ana.KullanilabilirYukseklikMm, veri.Yuzeyler.Count,
            string.IsNullOrWhiteSpace(ana.Ad) ? ana.Kod : ana.Ad, hacim);
    }

    // ===== Kullanım =====

    /// <summary>
    /// Yüklerin kapladığı zarf (en öndeki yükün önünden en arkadakinin arkasına, en soldakinden en sağdakine, en alttaki
    /// yükün tabanından en üsttekinin üstüne), yük ağırlığı ve kapasiteye oranı, yük hacmi ve kullanılabilir hacme oranı.
    /// Kapasite ya da kullanılabilir hacim bilinmiyorsa ilgili oran yazılmaz; kalan hacim yük hacmi kullanılabilir hacmi
    /// aşmıyorsa vardır (taşan yükte eksi kalan yazılmaz).
    /// </summary>
    internal sealed record AracKullanimi(
        int UzunlukMm, int GenislikMm, int YukseklikMm, decimal YukKg, decimal? AgirlikYuzde, decimal YukHacmiM3, decimal? HacimYuzde,
        decimal? KalanHacimM3);

    internal static decimal YukHacmiM3(IEnumerable<PlanYukKonumu> konumlar) =>
        konumlar.Sum(k => (decimal)k.BoyMm * k.EnMm * k.YukseklikMm) / Mm3M3;

    internal static AracKullanimi Kullanim(PlanAraci a, AracOlculeri olcu)
    {
        var k = a.Konumlar;
        var hacim = YukHacmiM3(k);
        decimal? agirlikYuzde = a.KapasiteKg is > 0 ? a.ToplamKg * 100 / a.KapasiteKg.Value : null;
        decimal? hacimYuzde = olcu.KullanilabilirHacimM3 is > 0 ? hacim * 100 / olcu.KullanilabilirHacimM3.Value : null;
        decimal? kalan = olcu.KullanilabilirHacimM3 is { } kullanilabilir && hacim <= kullanilabilir ? kullanilabilir - hacim : null;
        return k.Count == 0
            ? new AracKullanimi(0, 0, 0, a.ToplamKg, agirlikYuzde, 0, hacimYuzde, kalan)
            : new AracKullanimi(
                k.Max(x => x.XMm + x.BoyMm) - k.Min(x => x.XMm),
                k.Max(x => x.YMm + x.EnMm) - k.Min(x => x.YMm),
                k.Max(x => x.ZMm + x.YukseklikMm) - k.Min(x => x.ZMm),
                a.ToplamKg, agirlikYuzde, hacim, hacimYuzde, kalan);
    }

    // ===== Toplam (yüklü araç) =====

    /// <summary>
    /// Yüklü aracın ölçüleri (araç + taşma; motorun sakladığı değerler), brüt ağırlık ve üstten taşma. Brüt ağırlık dara
    /// teknik veride varsa yazılır (dara tahmin edilmez).
    /// </summary>
    internal sealed record AracToplami(int UzunlukMm, int GenislikMm, int YukseklikMm, decimal? BrutKg, int UsttenTasmaMm);

    internal static AracToplami Toplam(PlanAraci a, EkipmanTeknikVerisi veri) =>
        new(a.YukluUzunlukMm, a.YukluGenislikMm, a.YukluYukseklikMm, veri.DaraKg is { } dara ? dara + a.ToplamKg : null, UsttenTasmaMm(a, veri));

    /// <summary>
    /// Yükün aracın dış yüksekliğinden (yan duvar, uç duvar) yukarı çıkan kısmı. Dış yükseklik bütün yükleme
    /// yüzeylerinin üstündeyse anlamlıdır: dış yüksekliği güvertenin kendisi olan platformda (lowbed) her yük "üstte"
    /// olacağı için taşma sayılmaz.
    /// </summary>
    internal static int UsttenTasmaMm(PlanAraci a, EkipmanTeknikVerisi veri)
    {
        if (veri.DisOlcu is not { } dis || a.Konumlar.Count == 0 || veri.Yuzeyler.Count == 0 ||
            dis.YukseklikMm <= veri.Yuzeyler.Max(y => y.YerdenYukseklikMm))
            return 0;
        return Math.Max(0, a.Konumlar.Max(k => k.ZMm + k.YukseklikMm) - dis.YukseklikMm);
    }

    /// <summary>Araçta kasa dışına (sol, sağ, ön, arka) ya da üstten taşan yük var mı (ölçü dışı yük).</summary>
    internal static bool OlcuDisi(PlanAraci a, EkipmanTeknikVerisi veri) =>
        a.SolTasmaMm > 0 || a.SagTasmaMm > 0 || a.OnTasmaMm > 0 || a.ArkaTasmaMm > 0 || UsttenTasmaMm(a, veri) > 0;

    // ===== Ağırlık merkezi =====

    /// <summary>
    /// Yüklerin ortak ağırlık merkezi (ekipman koordinatları, mm): X ön uçtan, Y sol kenardan, <see cref="ZYerdenMm"/>
    /// yerden. <see cref="ZMm"/> raporda yazılan yüksekliktir: araçta tek yükleme seviyesi varsa yükleme yüzeyinden
    /// (<see cref="ZYuzeyden"/>), yüzeyler farklı yükseklikteyse (lowbed boynu ve havuzu) yerden ölçülür.
    /// </summary>
    internal sealed record AracAgirlikMerkezi(int XMm, int YMm, int ZMm, bool ZYuzeyden, int ZYerdenMm);

    /// <summary>
    /// Motorun hesabıyla (<see cref="AgirlikMerkeziHesabi"/>) ağırlık merkezi; yük yoksa ya da toplam ağırlık sıfırsa
    /// null. Boyuna değer için aracın saklanan <see cref="PlanAraci.AgirlikMerkeziXMm"/> alanı varsa o yazılır (aynı
    /// tanım; raporda ekrandakinden farklı bir X çıkmaz).
    /// </summary>
    internal static AracAgirlikMerkezi? AgirlikMerkezi(PlanAraci a, EkipmanTeknikVerisi veri)
    {
        var nokta = AgirlikMerkeziHesabi.Hesapla(a.Konumlar.Select(k =>
            new AgirlikliKutu(k.BrutKg, k.XMm, k.YMm, k.ZMm, k.BoyMm, k.EnMm, k.YukseklikMm)));
        if (nokta is not { } n)
            return null;
        var seviyeler = veri.Yuzeyler.Select(y => y.YerdenYukseklikMm).Distinct().ToList();
        var yuzeyden = seviyeler.Count == 1;
        return new AracAgirlikMerkezi(a.AgirlikMerkeziXMm ?? n.XMm, n.YMm, yuzeyden ? n.ZMm - seviyeler[0] : n.ZMm, yuzeyden, n.ZMm);
    }

    // ===== Sandık renkleri =====

    /// <summary>
    /// Sandık renkleri: baskıda ayırt edilen, açık ve koyu tonları sırayla giden palet. Kırmızı yoktur (uygunsuz yükün
    /// vurgusudur).
    /// </summary>
    internal static readonly IReadOnlyList<string> SandikPaleti =
    [
        "#1f77b4", "#ffbb78", "#2ca02c", "#c5b0d5", "#8c564b", "#9edae5", "#e377c2", "#dbdb8d", "#393b79", "#98df8a",
        "#ff7f0e", "#aec7e8", "#637939", "#f7b6d2", "#17becf", "#c49c94", "#9467bd", "#bcbd22", "#7f7f7f", "#e7ba52"
    ];

    /// <summary>İki yük bu mesafeden yakınsa (üç eksende de) komşu sayılır ve aynı rengi almaz.</summary>
    internal const int KomsulukMesafesiMm = 250;

    internal static bool Komsu(PlanYukKonumu a, PlanYukKonumu b) =>
        a.XMm < b.XMm + b.BoyMm + KomsulukMesafesiMm && b.XMm < a.XMm + a.BoyMm + KomsulukMesafesiMm &&
        a.YMm < b.YMm + b.EnMm + KomsulukMesafesiMm && b.YMm < a.YMm + a.EnMm + KomsulukMesafesiMm &&
        a.ZMm < b.ZMm + b.YukseklikMm + KomsulukMesafesiMm && b.ZMm < a.ZMm + a.YukseklikMm + KomsulukMesafesiMm;

    /// <summary>
    /// Araçtaki her yüke bir renk (girdi sırasıyla; rapor yükleme sırasını verir). Belirlenimcidir: aynı yerleşim hep
    /// aynı renkleri üretir. Renk paletten sırayla alınır; daha önce renklenmiş komşusunun rengi atlanır. Palet
    /// yük sayısından küçükse renkler tekrar eder (kimlik sandık numarasındadır); bütün palet komşularda kullanılmışsa
    /// sıradaki renk verilir.
    /// </summary>
    internal static IReadOnlyList<string> SandikRenkleri(IReadOnlyList<PlanYukKonumu> yukler)
    {
        var renkler = new string[yukler.Count];
        for (var i = 0; i < yukler.Count; i++)
        {
            var komsuRenkleri = new HashSet<string>(StringComparer.Ordinal);
            for (var j = 0; j < i; j++)
                if (Komsu(yukler[i], yukler[j]))
                    komsuRenkleri.Add(renkler[j]);
            renkler[i] = SandikPaleti[i % SandikPaleti.Count];
            for (var d = 0; d < SandikPaleti.Count; d++)
            {
                var aday = SandikPaleti[(i + d) % SandikPaleti.Count];
                if (komsuRenkleri.Contains(aday))
                    continue;
                renkler[i] = aday;
                break;
            }
        }
        return renkler;
    }

    // ===== Plan =====

    /// <summary>
    /// Üstüne yük konmayan yüklerin anahtarları: plan yük satırındaki izin (<see cref="PlanYukSatiri.UsteKonabilirMi"/>
    /// false). Satırı kalmamış konum (sandığı sonradan silinmiş) işaretlenmez.
    /// </summary>
    internal static IReadOnlySet<string> UstuneKonmazlar(YuklemePlaniDetayi plan) =>
        plan.Yukler
            .Where(y => !y.UsteKonabilirMi && (y.Kaynak != PlanYukKaynagi.Sandik || y.SandikId != null))
            .Select(y => YuklemePlaniKurali.YukAnahtari(y.Kaynak, y.SandikId, y.ManuelGrupNo, y.BirimNo))
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Yüklerin adları (yük anahtarı → ad): sandıkta sandık kaydının adı (plana kopyalanan), elle eklenen yükte
    /// açıklaması. Adı boş yük ve satırı kalmamış konum listede yoktur.
    /// </summary>
    internal static IReadOnlyDictionary<string, string> YukAdlari(YuklemePlaniDetayi plan) =>
        plan.Yukler
            .Where(y => !string.IsNullOrWhiteSpace(y.Aciklama) && (y.Kaynak != PlanYukKaynagi.Sandik || y.SandikId != null))
            .GroupBy(y => YuklemePlaniKurali.YukAnahtari(y.Kaynak, y.SandikId, y.ManuelGrupNo, y.BirimNo), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().Aciklama!.Trim(), StringComparer.Ordinal);

    /// <summary>
    /// Planın özeti (ilk sayfa): kullanılan araç ve ölçü dışı yük taşıyan araç sayısı, araca yüklenen yük sayısı (ve
    /// bunların kaçı sandık), toplam yük ağırlığı ve hacmi. Araca atanamayan yükler sayılmaz.
    /// </summary>
    internal sealed record PlanOzeti(int AracSayisi, int OlcuDisiAracSayisi, int YukSayisi, int SandikSayisi, decimal ToplamKg, decimal ToplamHacimM3);

    internal static PlanOzeti Ozet(YuklemePlaniDetayi plan, Func<PlanAraci, EkipmanTeknikVerisi> teknikVeri)
    {
        var araclar = plan.Yerlesim?.Araclar ?? [];
        var sandiklar = plan.Yukler
            .Where(y => y.Kaynak == PlanYukKaynagi.Sandik && y.SandikId != null)
            .Select(y => YuklemePlaniKurali.YukAnahtari(y.Kaynak, y.SandikId, y.ManuelGrupNo, y.BirimNo))
            .ToHashSet(StringComparer.Ordinal);
        var konumlar = araclar.SelectMany(a => a.Konumlar).ToList();
        return new PlanOzeti(
            araclar.Count,
            araclar.Count(a => OlcuDisi(a, teknikVeri(a))),
            konumlar.Count,
            konumlar.Count(k => sandiklar.Contains(k.YukAnahtari)),
            araclar.Sum(a => a.ToplamKg),
            YukHacmiM3(konumlar));
    }
}
