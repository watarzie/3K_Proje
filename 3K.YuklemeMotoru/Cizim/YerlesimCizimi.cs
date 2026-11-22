using System.Globalization;
using System.Text;
using _3K.YuklemeMotoru.Ekipman;

namespace _3K.YuklemeMotoru.Cizim;

/// <summary>Çizimdeki yük kutusu (ekipman koordinatları, mm). Etiket yükün sandık numarası ya da kodudur (rapordaki yük listesiyle aynı).</summary>
public sealed record CizimKutusu(string Etiket, int XMm, int YMm, int ZMm, int BoyMm, int EnMm, int YukseklikMm, string Renk, bool Sorunlu);

/// <summary>Çizimde işaretlenen nokta (ekipman koordinatları, mm): yüklerin ortak ağırlık merkezi.</summary>
public sealed record CizimNoktasi(double XMm, double YMm, double ZMm);

/// <summary>Çizimin bakış yönü.</summary>
public enum CizimGorunumu
{
    Ustten = 1,
    Yandan = 2,
    Arkadan = 3,
    Izometrik = 4
}

/// <summary>
/// Yerleşimin SVG çizimi (araç bazlı yükleme planı raporu, isterler 29): üstten, yandan (soldan bakış), arkadan ve
/// 3B (arka-sağ-üstten eğik bakış). Ekipman koordinatları: X önden arkaya, Y soldan sağa, Z yerden yukarı (mm). Yükler
/// çağıranın verdiği renkle (raporda sandık başına ayrı renk) ve sandık numarasıyla çizilir (kimlik yalnız renge bağlı
/// değildir); etiket yazısı zemine göre açık ya da koyu seçilir, uygunsuz yük kırmızıdır. Yön (ÖN/ARKA, SOL/SAĞ) ve
/// yükleme açıklıkları işaretlenir; ağırlık merkezi verilirse dört dilimli daireyle gösterilir. Bilinmeyen ekipman
/// verisi çizilmez.
/// </summary>
public static class YerlesimCizimi
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private const string YuzeyRengi = "#cbd5e1";
    private const string DisRengi = "#64748b";
    private const string AciklikRengi = "#16a34a";
    private const string EngelRengi = "#dc2626";
    private const string SorunRengi = "#dc2626";

    public static string Svg(EkipmanTeknikVerisi ekipman, IReadOnlyList<CizimKutusu> kutular, CizimGorunumu gorunum,
        CizimNoktasi? agirlikMerkezi = null) => gorunum switch
    {
        CizimGorunumu.Ustten => Ustten(ekipman, kutular, agirlikMerkezi),
        CizimGorunumu.Yandan => Yandan(ekipman, kutular, agirlikMerkezi),
        CizimGorunumu.Arkadan => Arkadan(ekipman, kutular, agirlikMerkezi),
        _ => Izometrik(ekipman, kutular, agirlikMerkezi)
    };

    /// <summary>Ağırlık merkezi işaretinin tek başına çizimi (rapordaki açıklama satırı için).</summary>
    public static string AgirlikMerkeziIsareti()
    {
        var t = new Tuval(-10, -10, 10, 10);
        t.AgirlikMerkezi(0, 0, 9);
        return t.Bitir();
    }

    private static string F(double d) => Math.Round(d, 1).ToString(Inv);

    private static (int Uzunluk, int Genislik, int Yukseklik) Olculer(EkipmanTeknikVerisi e)
    {
        var u = e.DisOlcu?.UzunlukMm ?? (e.Yuzeyler.Count == 0 ? 0 : e.Yuzeyler.Max(y => y.BaslangicXMm + y.UzunlukMm));
        var g = e.DisOlcu?.GenislikMm ?? (e.Yuzeyler.Count == 0 ? 0 : e.Yuzeyler.Max(y => y.GenislikMm));
        var h = e.DisOlcu?.YukseklikMm ?? (e.Yuzeyler.Count == 0 ? 0 : e.Yuzeyler.Max(y => y.YerdenYukseklikMm + (y.KullanilabilirYukseklikMm ?? 0)));
        return (u, g, h);
    }

    private sealed class Tuval(double minX, double minY, double maxX, double maxY)
    {
        private readonly StringBuilder icerik = new();
        public double Olcek => Math.Max(maxX - minX, maxY - minY) / 100.0;

        public void Ekle(string oge) => icerik.Append(oge);

        public void Dikdortgen(double x, double y, double g, double h, string dolgu, string cizgi, double kalinlik, double opaklik = 1, string? kesik = null) =>
            icerik.Append($"<rect x='{F(x)}' y='{F(y)}' width='{F(g)}' height='{F(h)}' fill='{dolgu}' fill-opacity='{F(opaklik)}' stroke='{cizgi}' stroke-width='{F(kalinlik)}'{(kesik == null ? "" : $" stroke-dasharray='{kesik}'")}/>");

        public void Cizgi(double x1, double y1, double x2, double y2, string renk, double kalinlik) =>
            icerik.Append($"<line x1='{F(x1)}' y1='{F(y1)}' x2='{F(x2)}' y2='{F(y2)}' stroke='{renk}' stroke-width='{F(kalinlik)}'/>");

        public void Cokgen(IEnumerable<(double X, double Y)> noktalar, string dolgu, string cizgi, double kalinlik) =>
            icerik.Append($"<polygon points='{string.Join(" ", noktalar.Select(n => $"{F(n.X)},{F(n.Y)}"))}' fill='{dolgu}' stroke='{cizgi}' stroke-width='{F(kalinlik)}' stroke-linejoin='round'/>");

        /// <summary>
        /// Yazı (x, y) noktasına ortalanır. Düşey ortalama taban çizgisi kaydırılarak yapılır (büyük harf yüksekliğinin
        /// yarısı): PDF'e çizen SVG işleyici "dominant-baseline" özelliğini uygulamaz, yazı yukarıda kalırdı.
        /// </summary>
        public void Yazi(double x, double y, string metin, double boyut, string renk = "#0f172a", bool kalin = true, string hiza = "middle") =>
            icerik.Append($"<text x='{F(x)}' y='{F(y + boyut * 0.35)}' font-size='{F(boyut)}' font-family='Lato, Arial, sans-serif' text-anchor='{hiza}' fill='{renk}'{(kalin ? " font-weight='bold'" : "")}>{Kacis(metin)}</text>");

        /// <summary>
        /// Ağırlık merkezi işareti: dört dilimli daire (karşılıklı iki dilim koyu). Beyaz zemin ve koyu çerçeve her
        /// kutu renginin üstünde seçilir.
        /// </summary>
        public void AgirlikMerkezi(double x, double y, double r)
        {
            icerik.Append($"<g id='{AgirlikMerkeziKimligi}'><circle cx='{F(x)}' cy='{F(y)}' r='{F(r)}' fill='#ffffff' stroke='#0f172a' stroke-width='{F(r * 0.18)}'/>");
            icerik.Append($"<path d='M{F(x)},{F(y)} L{F(x + r)},{F(y)} A{F(r)},{F(r)} 0 0 0 {F(x)},{F(y - r)} Z' fill='#0f172a'/>");
            icerik.Append($"<path d='M{F(x)},{F(y)} L{F(x - r)},{F(y)} A{F(r)},{F(r)} 0 0 0 {F(x)},{F(y + r)} Z' fill='#0f172a'/></g>");
        }

        public string Bitir()
        {
            var pay = Olcek * 6;
            return $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='{F(minX - pay)} {F(minY - pay)} {F(maxX - minX + 2 * pay)} {F(maxY - minY + 2 * pay)}'>{icerik}</svg>";
        }
    }

    /// <summary>XML metin kaçışı; XML'de geçersiz kontrol karakterleri atılır.</summary>
    private static string Kacis(string metin) =>
        new string(metin.Where(c => !char.IsControl(c)).ToArray())
            .Replace("&", "&amp;", StringComparison.Ordinal).Replace("<", "&lt;", StringComparison.Ordinal).Replace(">", "&gt;", StringComparison.Ordinal);

    /// <summary>
    /// Etiket yazı boyutu: kutunun küçük kenarına göre okunur aralıkta; uzun etiket (sandık kodu) kutunun yazı yönündeki
    /// kenarına sığacak kadar küçülür (komşu kutunun üstüne taşmasın). Alt sınır okunurluk içindir; çok dar kutuda yazı
    /// kenardan taşabilir.
    /// </summary>
    private static double EtiketBoyu(string etiket, double yaziYonu, double dik, double olcek)
    {
        var sigan = yaziYonu * 0.9 / (Math.Max(etiket.Length, 1) * 0.6);
        return Math.Max(Math.Min(Math.Clamp(Math.Min(yaziYonu, dik) * 0.45, olcek * 2.2, olcek * 5), sigan), olcek * 1.0);
    }

    private static string KutuDolgusu(CizimKutusu k) => k.Sorunlu ? SorunRengi : k.Renk;

    /// <summary>SVG'de ağırlık merkezi işaretinin kimliği.</summary>
    public const string AgirlikMerkeziKimligi = "agirlik-merkezi";

    private const string KoyuYazi = "#0f172a";
    private const string AcikYazi = "#ffffff";

    /// <summary>
    /// Etiket yazısının rengi: açık zeminde koyu, koyu zeminde beyaz (algılanan parlaklığa göre). #rrggbb dışındaki
    /// renkte beyaz kalır.
    /// </summary>
    public static string YaziRengi(string dolgu)
    {
        if (dolgu.Length != 7 || dolgu[0] != '#' ||
            !int.TryParse(dolgu.AsSpan(1), NumberStyles.HexNumber, Inv, out var rgb))
            return AcikYazi;
        var parlaklik = (0.299 * ((rgb >> 16) & 0xff) + 0.587 * ((rgb >> 8) & 0xff) + 0.114 * (rgb & 0xff)) / 255.0;
        return parlaklik > 0.6 ? KoyuYazi : AcikYazi;
    }

    /// <summary>Düz görünümdeki kutu: çizim dikdörtgeni (SVG koordinatları).</summary>
    private readonly record struct DuzKutu(CizimKutusu Kutu, double X, double Y, double G, double H);

    /// <summary>Yazının kapladığı alan tahmininde harf genişliği (yazı boyunun katı; kalın yazıda büyük harf ve rakam).</summary>
    private const double HarfGenisligi = 0.62;

    /// <summary>Etiket olduğu yere sığmıyorsa (işaret ya da öndeki yük) sırayla denenen küçültme oranları.</summary>
    private static readonly double[] EtiketKucultmeleri = [1, 0.75, 0.55];

    /// <summary>Düz görünümde ağırlık merkezi işaretinin yeri ve yarıçapı (SVG koordinatları).</summary>
    private readonly record struct Isaret(double X, double Y, double R);

    /// <summary>
    /// Düz (üstten, yandan, arkadan) görünümde kutular verilen ressam sırasıyla çizilir (son çizilen bakana en yakın
    /// olandır). Etiketler bütün kutulardan sonra ve yalnız yazı alanı daha yakın bir kutuyla örtülmeyen kutuya yazılır:
    /// arkada ya da altta kalan yükün numarası öndeki yükün üstünde görünmez. Ağırlık merkezi işareti etiketin üstüne
    /// geliyorsa etiket kutunun içinde işaretin üstüne, altına, soluna ya da sağına kaydırılır (gerekirse küçültülerek);
    /// hiçbirine sığmıyorsa yazılmaz (işaretin altında yarım kalan numara okunmaz). İşaret en son çizilir.
    /// </summary>
    private static void DuzKutulariCiz(Tuval t, IReadOnlyList<DuzKutu> sirali, Isaret? isaret)
    {
        var c = t.Olcek;
        foreach (var d in sirali)
            t.Dikdortgen(d.X, d.Y, d.G, d.H, KutuDolgusu(d.Kutu), d.Kutu.Sorunlu ? "#7f1d1d" : "#1e293b", c * (d.Kutu.Sorunlu ? 0.5 : 0.2), 0.88);
        for (var i = 0; i < sirali.Count; i++)
        {
            var d = sirali[i];
            var tamBoyut = EtiketBoyu(d.Kutu.Etiket, d.G, d.H, c);
            foreach (var kucultme in EtiketKucultmeleri)
            {
                // Küçültme yalnız işarete yer açmak içindir; okunur alt sınırın altına inilmez.
                var boyut = tamBoyut * kucultme;
                if (kucultme < 1 && boyut < c)
                    break;
                var yariG = Math.Max(d.Kutu.Etiket.Length, 1) * HarfGenisligi * boyut / 2;
                var yariH = boyut * 0.55;
                var mx = d.X + d.G / 2;
                var my = d.Y + d.H / 2;
                if (isaret is { } im && Math.Abs(im.X - mx) < yariG + im.R && Math.Abs(im.Y - my) < yariH + im.R)
                {
                    (double X, double Y)[] adaylar =
                    [
                        (mx, im.Y - im.R - yariH * 1.5), (mx, im.Y + im.R + yariH * 1.5),
                        (im.X - im.R - yariG * 1.1, my), (im.X + im.R + yariG * 1.1, my)
                    ];
                    var sigan = adaylar.Where(a => a.X - yariG >= d.X && a.X + yariG <= d.X + d.G &&
                                                   a.Y - yariH * 1.1 >= d.Y && a.Y + yariH * 1.1 <= d.Y + d.H).ToList();
                    if (sigan.Count == 0)
                        continue;
                    (mx, my) = sigan[0];
                }
                // Yazı alanının kutunun içinde kalan kısmı: dar kutudan taşan yazı komşuyu "örtülü" saydırmaz.
                var sol = Math.Max(mx - yariG, d.X);
                var sag = Math.Min(mx + yariG, d.X + d.G);
                var ust = Math.Max(my - yariH, d.Y);
                var alt = Math.Min(my + yariH, d.Y + d.H);
                var ortulu = false;
                for (var j = i + 1; j < sirali.Count && !ortulu; j++)
                    ortulu = sol < sirali[j].X + sirali[j].G && sirali[j].X < sag && ust < sirali[j].Y + sirali[j].H && sirali[j].Y < alt;
                if (!ortulu)
                    t.Yazi(mx, my, d.Kutu.Etiket, boyut, YaziRengi(KutuDolgusu(d.Kutu)));
                break;
            }
        }
        if (isaret is { } son)
            t.AgirlikMerkezi(son.X, son.Y, son.R);
    }

    // ===== Üstten: X sağa (ön solda), Y yukarı doğru azalır (sol kenar altta) =====
    private static string Ustten(EkipmanTeknikVerisi e, IReadOnlyList<CizimKutusu> kutular, CizimNoktasi? agirlikMerkezi)
    {
        var (u, g, _) = Olculer(e);
        double SvgY(double y) => g - y;
        var minX = Math.Min(0, kutular.Count == 0 ? 0 : kutular.Min(k => k.XMm));
        var maxX = Math.Max(u, kutular.Count == 0 ? 0 : kutular.Max(k => k.XMm + k.BoyMm));
        var minY = Math.Min(0, kutular.Count == 0 ? 0 : kutular.Min(k => k.YMm));
        var maxY = Math.Max(g, kutular.Count == 0 ? 0 : kutular.Max(k => k.YMm + k.EnMm));
        var t = new Tuval(minX - g * 0.18, SvgY(maxY) - g * 0.12, maxX + g * 0.18, SvgY(minY) + g * 0.12);
        var c = t.Olcek;

        t.Dikdortgen(0, SvgY(g), u, g, "none", DisRengi, c * 0.3);
        foreach (var y in e.Yuzeyler)
        {
            var yBas = (g - y.GenislikMm) / 2.0;
            t.Dikdortgen(y.BaslangicXMm, SvgY(yBas + y.GenislikMm), y.UzunlukMm, y.GenislikMm, YuzeyRengi, DisRengi, c * 0.15);
        }
        foreach (var a in e.Acikliklar)
        {
            var bas = a.BaslangicXMm ?? 0;
            var son = a.UzunlukMm is { } ul ? bas + ul : u;
            switch (a.Taraf)
            {
                case AciklikTarafi.Arka: t.Cizgi(u, SvgY(g), u, SvgY(0), AciklikRengi, c * 0.9); break;
                case AciklikTarafi.On: t.Cizgi(0, SvgY(g), 0, SvgY(0), AciklikRengi, c * 0.9); break;
                case AciklikTarafi.Sol: t.Cizgi(bas, SvgY(0), son, SvgY(0), AciklikRengi, c * 0.9); break;
                case AciklikTarafi.Sag: t.Cizgi(bas, SvgY(g), son, SvgY(g), AciklikRengi, c * 0.9); break;
                case AciklikTarafi.Ust:
                    var ag = a.GenislikMm ?? g;
                    t.Dikdortgen(bas, SvgY((g + ag) / 2.0), son - bas, ag, "none", AciklikRengi, c * 0.4, 1, F(c * 1.5));
                    break;
            }
        }
        foreach (var en in e.Engeller)
            t.Dikdortgen(en.XMm, SvgY(en.YMm + en.GenislikMm), en.UzunlukMm, en.GenislikMm, EngelRengi, EngelRengi, c * 0.15, en.Sokulebilir ? 0.25 : 0.55);

        // Üstteki kat en son çizilir (görünen yük).
        DuzKutulariCiz(t, kutular.OrderBy(k => k.ZMm + k.YukseklikMm)
                .Select(k => new DuzKutu(k, k.XMm, SvgY(k.YMm + k.EnMm), k.BoyMm, k.EnMm)).ToList(),
            agirlikMerkezi is { } am ? new Isaret(am.XMm, SvgY(am.YMm), c * 1.7) : null);

        t.Yazi(-g * 0.09, SvgY(g / 2.0), "ÖN", c * 3.2);
        t.Yazi(u + g * 0.1, SvgY(g / 2.0), "ARKA", c * 3.2);
        t.Yazi(u / 2.0, SvgY(minY) + g * 0.07, "SOL", c * 2.6, DisRengi);
        t.Yazi(u / 2.0, SvgY(maxY) - g * 0.07, "SAĞ", c * 2.6, DisRengi);
        return t.Bitir();
    }

    // ===== Yandan (sol taraftan bakış): X sağa, Z yukarı =====
    private static string Yandan(EkipmanTeknikVerisi e, IReadOnlyList<CizimKutusu> kutular, CizimNoktasi? agirlikMerkezi)
    {
        var (u, _, h) = Olculer(e);
        var ust = Math.Max(h, kutular.Count == 0 ? 0 : kutular.Max(k => k.ZMm + k.YukseklikMm));
        double SvgZ(double z) => ust - z;
        var minX = Math.Min(0, kutular.Count == 0 ? 0 : kutular.Min(k => k.XMm));
        var maxX = Math.Max(u, kutular.Count == 0 ? 0 : kutular.Max(k => k.XMm + k.BoyMm));
        var t = new Tuval(minX - ust * 0.25, -ust * 0.08, maxX + ust * 0.3, ust * 1.05);
        var c = t.Olcek;

        t.Cizgi(minX - ust * 0.2, SvgZ(0), maxX + ust * 0.25, SvgZ(0), "#94a3b8", c * 0.3);
        if (e.DisOlcu != null)
            t.Dikdortgen(0, SvgZ(h), u, h, "none", DisRengi, c * 0.25, 1, F(c * 1.2));
        foreach (var y in e.Yuzeyler)
        {
            var kalinlik = Math.Min(120, Math.Max(40, y.YerdenYukseklikMm));
            t.Dikdortgen(y.BaslangicXMm, SvgZ(y.YerdenYukseklikMm), y.UzunlukMm, kalinlik, YuzeyRengi, DisRengi, c * 0.15);
            if (y.KullanilabilirYukseklikMm is { } ic)
                t.Dikdortgen(y.BaslangicXMm, SvgZ(y.YerdenYukseklikMm + ic), y.UzunlukMm, ic, "none", "#0e7490", c * 0.15, 1, F(c * 0.8));
        }
        foreach (var d in e.Dingiller)
            t.Ekle($"<circle cx='{F(d.XMm)}' cy='{F(SvgZ(450))}' r='450' fill='#1f2937'/>");
        if (e.KingPin is { } kp)
            t.Cokgen([(kp.XMm - 150, SvgZ(0) - 250), (kp.XMm + 150, SvgZ(0) - 250), (kp.XMm, SvgZ(0) - 520)], "#eab308", "#a16207", c * 0.15);
        foreach (var en in e.Engeller)
            t.Dikdortgen(en.XMm, SvgZ(en.ZMm + en.YukseklikMm), en.UzunlukMm, en.YukseklikMm, EngelRengi, EngelRengi, c * 0.15, en.Sokulebilir ? 0.25 : 0.55);

        // Soldan bakış: uzaktaki (büyük Y) yük önce çizilir.
        DuzKutulariCiz(t, kutular.OrderByDescending(k => k.YMm)
                .Select(k => new DuzKutu(k, k.XMm, SvgZ(k.ZMm + k.YukseklikMm), k.BoyMm, k.YukseklikMm)).ToList(),
            agirlikMerkezi is { } am ? new Isaret(am.XMm, SvgZ(am.ZMm), c * 1.7) : null);
        foreach (var a in e.Acikliklar.Where(a => a.Taraf is AciklikTarafi.Arka or AciklikTarafi.On))
        {
            var x = a.Taraf == AciklikTarafi.Arka ? u : 0;
            var taban = e.Yuzeyler.Count == 0 ? 0 : e.Yuzeyler.Min(y => y.YerdenYukseklikMm);
            t.Cizgi(x, SvgZ(taban), x, SvgZ(taban + (a.YukseklikMm ?? 0)), AciklikRengi, c * 0.9);
        }

        t.Yazi(minX - ust * 0.13, SvgZ(ust / 2.0), "ÖN", c * 3.2);
        t.Yazi(maxX + ust * 0.16, SvgZ(ust / 2.0), "ARKA", c * 3.2);
        return t.Bitir();
    }

    // ===== Arkadan (arkada durup öne bakış): Y sağa (sol kenar solda), Z yukarı =====
    private static string Arkadan(EkipmanTeknikVerisi e, IReadOnlyList<CizimKutusu> kutular, CizimNoktasi? agirlikMerkezi)
    {
        var (_, g, h) = Olculer(e);
        var ust = Math.Max(h, kutular.Count == 0 ? 0 : kutular.Max(k => k.ZMm + k.YukseklikMm));
        double SvgZ(double z) => ust - z;
        var minY = Math.Min(0, kutular.Count == 0 ? 0 : kutular.Min(k => k.YMm));
        var maxY = Math.Max(g, kutular.Count == 0 ? 0 : kutular.Max(k => k.YMm + k.EnMm));
        var t = new Tuval(minY - g * 0.3, -ust * 0.08, maxY + g * 0.3, ust * 1.05);
        var c = t.Olcek;

        t.Cizgi(minY - g * 0.2, SvgZ(0), maxY + g * 0.2, SvgZ(0), "#94a3b8", c * 0.3);
        if (e.DisOlcu != null)
            t.Dikdortgen(0, SvgZ(h), g, h, "none", DisRengi, c * 0.25, 1, F(c * 1.2));
        foreach (var y in e.Yuzeyler)
        {
            var yBas = (g - y.GenislikMm) / 2.0;
            t.Dikdortgen(yBas, SvgZ(y.YerdenYukseklikMm), y.GenislikMm, Math.Min(120, Math.Max(40, y.YerdenYukseklikMm)), YuzeyRengi, DisRengi, c * 0.15);
        }
        // Arkadan bakış: öndeki (küçük X) yük önce çizilir; arkadaki yük en son çizilir ve görünür.
        DuzKutulariCiz(t, kutular.OrderBy(k => k.XMm + k.BoyMm)
                .Select(k => new DuzKutu(k, k.YMm, SvgZ(k.ZMm + k.YukseklikMm), k.EnMm, k.YukseklikMm)).ToList(),
            agirlikMerkezi is { } am ? new Isaret(am.YMm, SvgZ(am.ZMm), c * 2.6) : null);
        if (e.Acikliklar.FirstOrDefault(a => a.Taraf == AciklikTarafi.Arka) is { GenislikMm: { } ag, YukseklikMm: { } ah })
        {
            var taban = e.Yuzeyler.Count == 0 ? 0 : e.Yuzeyler.Min(y => y.YerdenYukseklikMm);
            t.Dikdortgen((g - ag) / 2.0, SvgZ(taban + ah), ag, ah, "none", AciklikRengi, c * 0.6);
        }
        t.Yazi(minY - g * 0.14, SvgZ(ust / 2.0), "SOL", c * 5.5);
        t.Yazi(maxY + g * 0.14, SvgZ(ust / 2.0), "SAĞ", c * 5.5);
        return t.Bitir();
    }

    // ===== 3B: arka-sağ-üstten eğik bakış =====
    // Boyuna eksen yataya yakın, enine eksen daha dik çizilir: uzun araç sayfa genişliğini kullanır ve sağ yan yüz ile
    // arka yüz birlikte görünür. Bakış doğrultusu üç eksende de artı yöndedir (ressam sırası buna dayanır).
    private static readonly double BoyCos = Math.Cos(Math.PI * 8 / 180);
    private static readonly double BoySin = Math.Sin(Math.PI * 8 / 180);
    private static readonly double EnCos = Math.Cos(Math.PI * 32 / 180);
    private static readonly double EnSin = Math.Sin(Math.PI * 32 / 180);

    private static (double X, double Y) Iz(double x, double y, double z) => (x * BoyCos - y * EnCos, x * BoySin + y * EnSin - z);

    /// <summary>
    /// 3B görünümde üst yüz etiketinin yazı boyu: yatay yazı (genişliği harf sayısına bağlı dikdörtgen) üst yüzün eğik
    /// paralelkenarına ortasından sığacak kadar. Okunur alt sınırın altına düşüyorsa etiket yazılmaz (null): kutudan taşıp
    /// komşu etiketlerle üst üste binmesindense yük öbür görünümlerdeki ve tablodaki numarasıyla bulunur.
    /// </summary>
    private static double? UstYuzYaziBoyu(string etiket, double boy, double en, double olcek)
    {
        if (boy <= 0 || en <= 0)
            return null;
        // Üst yüzün kenar vektörleri: boyuna (ux, uy), enine (-vx, vy). Yazı dikdörtgeninin köşeleri yüzün içinde kalmalı.
        double ux = boy * BoyCos, uy = boy * BoySin, vx = en * EnCos, vy = en * EnSin;
        var harf = Math.Max(etiket.Length, 1) * HarfGenisligi;
        var sigan = 0.85 * (ux * vy + uy * vx) / Math.Max(vy * harf + vx, ux + uy * harf);
        return sigan < olcek * UstYuzEnKucukYazi ? null : Math.Min(sigan, olcek * 5);
    }

    /// <summary>3B görünümde üst yüz etiketinin okunur alt sınırı (çizim ölçeğinin katı).</summary>
    private const double UstYuzEnKucukYazi = 0.75;

    /// <summary>Üst yüzde etiket için denenen yerler: orta, sonra enine ve boyuna kaydırılmış (yüzün kenar boyunun katı).</summary>
    private static readonly (double U, double V)[] UstYuzKaymalari =
        [(0, 0), (0, -0.2), (0, 0.2), (-0.2, 0), (0.2, 0), (0, -0.32), (0, 0.32), (-0.32, 0), (0.32, 0)];

    /// <summary>
    /// 3B görünümde çizim sırası (uzaktaki önce). Bir kutu, öbürünün boyuna arkasında, enine sağında ya da üstündeyse
    /// bakana daha yakındır ve sonra çizilir; iki kutu birbirine göre çelişen yönlerde ayrıksa ekranda örtüşmezler ve
    /// sıraları serbesttir. Sıra bu ilişkilerden kurulur (kutu ölçüleri farklıyken tek bir uzaklık değeriyle sıralamak
    /// arkadaki yükü öndekinin üstüne çizebilir). Elle yerleşimde iç içe geçen kutular ilişki üretmez; döngü kalırsa
    /// kalanlar ön-sol-alt köşe sırasıyla çizilir.
    /// </summary>
    public static IReadOnlyList<CizimKutusu> RessamSirasi(IReadOnlyList<CizimKutusu> kutular)
    {
        var temel = kutular.OrderBy(k => k.XMm + k.YMm).ThenBy(k => k.ZMm).ToList();
        var n = temel.Count;
        // Yakin(a, b): a, b'ye göre bakana daha yakın bir yarı uzayda.
        static bool Yakin(CizimKutusu a, CizimKutusu b) =>
            a.XMm >= b.XMm + b.BoyMm || a.YMm >= b.YMm + b.EnMm || a.ZMm >= b.ZMm + b.YukseklikMm;
        var sonrakiler = new List<int>[n];
        var bekleyen = new int[n];
        for (var i = 0; i < n; i++)
            sonrakiler[i] = [];
        for (var i = 0; i < n; i++)
        {
            for (var j = i + 1; j < n; j++)
            {
                var jYakin = Yakin(temel[j], temel[i]);
                var iYakin = Yakin(temel[i], temel[j]);
                if (jYakin == iYakin)
                    continue;
                var (once, sonra) = jYakin ? (i, j) : (j, i);
                sonrakiler[once].Add(sonra);
                bekleyen[sonra]++;
            }
        }
        var sonuc = new List<CizimKutusu>(n);
        var cizildi = new bool[n];
        while (sonuc.Count < n)
        {
            var secilen = -1;
            for (var i = 0; i < n && secilen < 0; i++)
                if (!cizildi[i] && bekleyen[i] == 0)
                    secilen = i;
            if (secilen < 0)
                secilen = Array.IndexOf(cizildi, false);
            cizildi[secilen] = true;
            sonuc.Add(temel[secilen]);
            foreach (var sonraki in sonrakiler[secilen])
                bekleyen[sonraki]--;
        }
        return sonuc;
    }

    /// <summary>Kutunun 3B görünümdeki dış çizgisi: görünen üç yüzün birleşimi (dışbükey altıgen, köşeleri sırayla).</summary>
    private static (double X, double Y)[] Siluet(CizimKutusu k)
    {
        double x0 = k.XMm, y0 = k.YMm, z0 = k.ZMm, x1 = k.XMm + k.BoyMm, y1 = k.YMm + k.EnMm, z1 = k.ZMm + k.YukseklikMm;
        return [Iz(x1, y0, z0), Iz(x1, y1, z0), Iz(x0, y1, z0), Iz(x0, y1, z1), Iz(x0, y0, z1), Iz(x1, y0, z1)];
    }

    /// <summary>Nokta dışbükey çokgenin içinde mi (kenar üstü sayılmaz: yan yana duran kutular birbirini örtmez).</summary>
    private static bool Icinde((double X, double Y)[] cokgen, double x, double y)
    {
        var isaret = 0;
        for (var i = 0; i < cokgen.Length; i++)
        {
            var a = cokgen[i];
            var b = cokgen[(i + 1) % cokgen.Length];
            var carpim = (b.X - a.X) * (y - a.Y) - (b.Y - a.Y) * (x - a.X);
            if (Math.Abs(carpim) < 1e-6)
                return false;
            var yon = carpim > 0 ? 1 : -1;
            if (isaret != 0 && yon != isaret)
                return false;
            isaret = yon;
        }
        return true;
    }

    private static string Izometrik(EkipmanTeknikVerisi e, IReadOnlyList<CizimKutusu> kutular, CizimNoktasi? agirlikMerkezi)
    {
        var (u, g, h) = Olculer(e);
        var ust = Math.Max(h, kutular.Count == 0 ? 0 : kutular.Max(k => k.ZMm + k.YukseklikMm));
        var noktalar = new List<(double X, double Y)>
        {
            Iz(0, 0, 0), Iz(u, 0, 0), Iz(0, g, 0), Iz(u, g, 0), Iz(0, 0, ust), Iz(u, 0, ust), Iz(0, g, ust), Iz(u, g, ust)
        };
        foreach (var k in kutular)
        {
            // Kutunun sekiz köşesi de çizim alanına girer (eğik bakışta uç noktalar farklı köşelerdedir).
            foreach (var x in (double[])[k.XMm, k.XMm + k.BoyMm])
                foreach (var y in (double[])[k.YMm, k.YMm + k.EnMm])
                    foreach (var z in (double[])[k.ZMm, k.ZMm + k.YukseklikMm])
                        noktalar.Add(Iz(x, y, z));
        }
        var t = new Tuval(noktalar.Min(n => n.X), noktalar.Min(n => n.Y), noktalar.Max(n => n.X), noktalar.Max(n => n.Y));
        var c = t.Olcek;

        foreach (var y in e.Yuzeyler)
        {
            var yBas = (g - y.GenislikMm) / 2.0;
            var z = y.YerdenYukseklikMm;
            t.Cokgen([Iz(y.BaslangicXMm, yBas, z), Iz(y.BaslangicXMm + y.UzunlukMm, yBas, z),
                Iz(y.BaslangicXMm + y.UzunlukMm, yBas + y.GenislikMm, z), Iz(y.BaslangicXMm, yBas + y.GenislikMm, z)], YuzeyRengi, DisRengi, c * 0.15);
        }
        if (e.DisOlcu != null)
        {
            // Dış ölçü tel kafes (hafif, kesikli).
            (double, double, double)[] k8 = [(0, 0, 0), (u, 0, 0), (u, g, 0), (0, g, 0), (0, 0, h), (u, 0, h), (u, g, h), (0, g, h)];
            int[][] kenarlar = [[0, 1], [1, 2], [2, 3], [3, 0], [4, 5], [5, 6], [6, 7], [7, 4], [0, 4], [1, 5], [2, 6], [3, 7]];
            foreach (var kenar in kenarlar)
            {
                var a = Iz(k8[kenar[0]].Item1, k8[kenar[0]].Item2, k8[kenar[0]].Item3);
                var b = Iz(k8[kenar[1]].Item1, k8[kenar[1]].Item2, k8[kenar[1]].Item3);
                t.Ekle($"<line x1='{F(a.X)}' y1='{F(a.Y)}' x2='{F(b.X)}' y2='{F(b.Y)}' stroke='{DisRengi}' stroke-width='{F(c * 0.15)}' stroke-dasharray='{F(c)}'/>");
            }
        }

        var sirali = RessamSirasi(kutular);
        foreach (var k in sirali)
        {
            double x0 = k.XMm, y0 = k.YMm, z0 = k.ZMm, x1 = k.XMm + k.BoyMm, y1 = k.YMm + k.EnMm, z1 = k.ZMm + k.YukseklikMm;
            var dolgu = KutuDolgusu(k);
            var cizgi = k.Sorunlu ? "#7f1d1d" : "#1e293b";
            t.Cokgen([Iz(x1, y0, z0), Iz(x1, y1, z0), Iz(x1, y1, z1), Iz(x1, y0, z1)], Koyu(dolgu, 0.72), cizgi, c * 0.15);
            t.Cokgen([Iz(x0, y1, z0), Iz(x1, y1, z0), Iz(x1, y1, z1), Iz(x0, y1, z1)], Koyu(dolgu, 0.86), cizgi, c * 0.15);
            t.Cokgen([Iz(x0, y0, z1), Iz(x1, y0, z1), Iz(x1, y1, z1), Iz(x0, y1, z1)], dolgu, cizgi, c * 0.15);
        }
        // Etiketler bütün kutulardan sonra yazılır. Yazı alanına daha yakın bir kutu ya da ağırlık merkezi işareti
        // giriyorsa etiket üst yüzün içinde kaydırılır; görünen bir yer yoksa (üst yüzü örtülü yük) yazılmaz: yarım
        // görünen numara başka bir sandık numarası gibi okunabilir.
        var isaretNoktasi = agirlikMerkezi is { } am ? Iz(am.XMm, am.YMm, am.ZMm) : ((double X, double Y)?)null;
        var isaretYaricapi = c * 1.5;
        var siluetler = sirali.Select(Siluet).ToArray();
        for (var i = 0; i < sirali.Count; i++)
        {
            var k = sirali[i];
            if (UstYuzYaziBoyu(k.Etiket, k.BoyMm, k.EnMm, c) is not { } tamBoyut)
                continue;
            var merkez = Iz(k.XMm + k.BoyMm / 2.0, k.YMm + k.EnMm / 2.0, k.ZMm + k.YukseklikMm);
            // Üst yüzün kenar vektörleri (boyuna u, enine v); etiketin yeri yüzün ortasından u ve v boyunca kaydırılır.
            double ux = k.BoyMm * BoyCos, uy = k.BoyMm * BoySin, vx = -k.EnMm * EnCos, vy = k.EnMm * EnSin;
            var det = ux * vy - uy * vx;
            var yazildi = false;
            foreach (var kucultme in EtiketKucultmeleri)
            {
                var yaziBoyu = tamBoyut * kucultme;
                if (yazildi || (kucultme < 1 && yaziBoyu < c * UstYuzEnKucukYazi))
                    break;
                var yariG = Math.Max(k.Etiket.Length, 1) * HarfGenisligi * yaziBoyu / 2;
                var yariH = yaziBoyu / 2;
                bool YuzIcinde(double dx, double dy)
                {
                    foreach (var kx in (double[])[-yariG, yariG])
                    {
                        foreach (var ky in (double[])[-yariH, yariH])
                        {
                            double x = dx + kx, y = dy + ky;
                            if (Math.Abs((x * vy - y * vx) / det) > 0.5 || Math.Abs((ux * y - uy * x) / det) > 0.5)
                                return false;
                        }
                    }
                    return true;
                }
                bool IsarettenUzak(double dx, double dy) =>
                    isaretNoktasi is not { } nokta ||
                    Math.Abs(nokta.X - merkez.X - dx) >= yariG + isaretYaricapi || Math.Abs(nokta.Y - merkez.Y - dy) >= yariH * 1.3 + isaretYaricapi;
                bool Gorunur(double dx, double dy)
                {
                    for (var j = i + 1; j < siluetler.Length; j++)
                    {
                        for (var ax = -1; ax <= 1; ax++)
                            for (var ay = -1; ay <= 1; ay++)
                                if (Icinde(siluetler[j], merkez.X + dx + ax * yariG, merkez.Y + dy + ay * yariH))
                                    return false;
                    }
                    return true;
                }
                foreach (var (ku, kv) in UstYuzKaymalari)
                {
                    double dx = ku * ux + kv * vx, dy = ku * uy + kv * vy;
                    if (!YuzIcinde(dx, dy) || !IsarettenUzak(dx, dy) || !Gorunur(dx, dy))
                        continue;
                    t.Yazi(merkez.X + dx, merkez.Y + dy, k.Etiket, yaziBoyu, YaziRengi(KutuDolgusu(k)));
                    yazildi = true;
                    break;
                }
            }
        }
        // Ağırlık merkezi yükün içindedir; görünsün diye bütün kutuların üstüne çizilir.
        if (isaretNoktasi is { } isaretYeri)
            t.AgirlikMerkezi(isaretYeri.X, isaretYeri.Y, isaretYaricapi);
        var on = Iz(0, g / 2.0, ust);
        var arka = Iz(u, g / 2.0, ust);
        t.Yazi(on.X, on.Y - c * 6.5, "ÖN", c * 4.2);
        t.Yazi(arka.X, arka.Y - c * 6.5, "ARKA", c * 4.2);
        return t.Bitir();
    }

    /// <summary>Rengi karartır (yan yüzler için); #rrggbb dışındaki değer olduğu gibi döner.</summary>
    private static string Koyu(string renk, double oran)
    {
        if (renk.Length != 7 || renk[0] != '#')
            return renk;
        int Kanal(int i) => (int)(Convert.ToInt32(renk.Substring(i, 2), 16) * oran);
        return $"#{Kanal(1):x2}{Kanal(3):x2}{Kanal(5):x2}";
    }
}
