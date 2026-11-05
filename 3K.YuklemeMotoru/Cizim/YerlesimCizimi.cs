using System.Globalization;
using System.Text;
using _3K.YuklemeMotoru.Ekipman;

namespace _3K.YuklemeMotoru.Cizim;

/// <summary>Çizimdeki yük kutusu (ekipman koordinatları, mm). Etiket rapordaki yük listesinin numarasıdır.</summary>
public sealed record CizimKutusu(string Etiket, int XMm, int YMm, int ZMm, int BoyMm, int EnMm, int YukseklikMm, string Renk, bool Sorunlu);

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
/// izometrik. Ekipman koordinatları: X önden arkaya, Y soldan sağa, Z yerden yukarı (mm). Yükler proje rengiyle ve
/// listedeki numarasıyla çizilir (kimlik yalnız renge bağlı değildir); uygunsuz yük kırmızıdır. Yön (ÖN/ARKA, SOL/SAĞ)
/// ve yükleme açıklıkları işaretlenir. Bilinmeyen ekipman verisi çizilmez.
/// </summary>
public static class YerlesimCizimi
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private const string YuzeyRengi = "#cbd5e1";
    private const string DisRengi = "#64748b";
    private const string AciklikRengi = "#16a34a";
    private const string EngelRengi = "#dc2626";
    private const string SorunRengi = "#dc2626";

    public static string Svg(EkipmanTeknikVerisi ekipman, IReadOnlyList<CizimKutusu> kutular, CizimGorunumu gorunum) => gorunum switch
    {
        CizimGorunumu.Ustten => Ustten(ekipman, kutular),
        CizimGorunumu.Yandan => Yandan(ekipman, kutular),
        CizimGorunumu.Arkadan => Arkadan(ekipman, kutular),
        _ => Izometrik(ekipman, kutular)
    };

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

        public void Yazi(double x, double y, string metin, double boyut, string renk = "#0f172a", bool kalin = true, string hiza = "middle") =>
            icerik.Append($"<text x='{F(x)}' y='{F(y)}' font-size='{F(boyut)}' font-family='Lato, Arial, sans-serif' text-anchor='{hiza}' dominant-baseline='middle' fill='{renk}'{(kalin ? " font-weight='bold'" : "")}>{Kacis(metin)}</text>");

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

    /// <summary>Etiket yazı boyutu: kutunun küçük kenarına göre, okunur aralıkta.</summary>
    private static double EtiketBoyu(double a, double b, double olcek) => Math.Clamp(Math.Min(a, b) * 0.45, olcek * 2.2, olcek * 5);

    private static string KutuDolgusu(CizimKutusu k) => k.Sorunlu ? SorunRengi : k.Renk;

    // ===== Üstten: X sağa (ön solda), Y yukarı doğru azalır (sol kenar altta) =====
    private static string Ustten(EkipmanTeknikVerisi e, IReadOnlyList<CizimKutusu> kutular)
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
        foreach (var k in kutular.OrderBy(k => k.ZMm + k.YukseklikMm))
        {
            t.Dikdortgen(k.XMm, SvgY(k.YMm + k.EnMm), k.BoyMm, k.EnMm, KutuDolgusu(k), k.Sorunlu ? "#7f1d1d" : "#1e293b", c * (k.Sorunlu ? 0.5 : 0.2), 0.88);
            t.Yazi(k.XMm + k.BoyMm / 2.0, SvgY(k.YMm + k.EnMm / 2.0), k.Etiket, EtiketBoyu(k.BoyMm, k.EnMm, c), "#ffffff");
        }

        t.Yazi(-g * 0.09, SvgY(g / 2.0), "ÖN", c * 3.2);
        t.Yazi(u + g * 0.1, SvgY(g / 2.0), "ARKA", c * 3.2);
        t.Yazi(u / 2.0, SvgY(minY) + g * 0.07, "SOL", c * 2.6, DisRengi);
        t.Yazi(u / 2.0, SvgY(maxY) - g * 0.07, "SAĞ", c * 2.6, DisRengi);
        return t.Bitir();
    }

    // ===== Yandan (sol taraftan bakış): X sağa, Z yukarı =====
    private static string Yandan(EkipmanTeknikVerisi e, IReadOnlyList<CizimKutusu> kutular)
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
        foreach (var k in kutular.OrderByDescending(k => k.YMm))
        {
            t.Dikdortgen(k.XMm, SvgZ(k.ZMm + k.YukseklikMm), k.BoyMm, k.YukseklikMm, KutuDolgusu(k), k.Sorunlu ? "#7f1d1d" : "#1e293b", c * (k.Sorunlu ? 0.5 : 0.2), 0.88);
            t.Yazi(k.XMm + k.BoyMm / 2.0, SvgZ(k.ZMm + k.YukseklikMm / 2.0), k.Etiket, EtiketBoyu(k.BoyMm, k.YukseklikMm, c), "#ffffff");
        }
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
    private static string Arkadan(EkipmanTeknikVerisi e, IReadOnlyList<CizimKutusu> kutular)
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
        // Arkadan bakış: öndeki (küçük X) yük önce çizilir.
        foreach (var k in kutular.OrderBy(k => k.XMm))
        {
            t.Dikdortgen(k.YMm, SvgZ(k.ZMm + k.YukseklikMm), k.EnMm, k.YukseklikMm, KutuDolgusu(k), k.Sorunlu ? "#7f1d1d" : "#1e293b", c * (k.Sorunlu ? 0.5 : 0.2), 0.88);
            t.Yazi(k.YMm + k.EnMm / 2.0, SvgZ(k.ZMm + k.YukseklikMm / 2.0), k.Etiket, EtiketBoyu(k.EnMm, k.YukseklikMm, c), "#ffffff");
        }
        if (e.Acikliklar.FirstOrDefault(a => a.Taraf == AciklikTarafi.Arka) is { GenislikMm: { } ag, YukseklikMm: { } ah })
        {
            var taban = e.Yuzeyler.Count == 0 ? 0 : e.Yuzeyler.Min(y => y.YerdenYukseklikMm);
            t.Dikdortgen((g - ag) / 2.0, SvgZ(taban + ah), ag, ah, "none", AciklikRengi, c * 0.6);
        }
        t.Yazi(minY - g * 0.14, SvgZ(ust / 2.0), "SOL", c * 5.5);
        t.Yazi(maxY + g * 0.14, SvgZ(ust / 2.0), "SAĞ", c * 5.5);
        return t.Bitir();
    }

    // ===== İzometrik: arka-sağ-üstten bakış =====
    private static readonly double Cos30 = Math.Cos(Math.PI / 6);

    private static (double X, double Y) Iz(double x, double y, double z) => ((x - y) * Cos30, (x + y) * 0.5 - z);

    private static string Izometrik(EkipmanTeknikVerisi e, IReadOnlyList<CizimKutusu> kutular)
    {
        var (u, g, h) = Olculer(e);
        var ust = Math.Max(h, kutular.Count == 0 ? 0 : kutular.Max(k => k.ZMm + k.YukseklikMm));
        var noktalar = new List<(double X, double Y)>
        {
            Iz(0, 0, 0), Iz(u, 0, 0), Iz(0, g, 0), Iz(u, g, 0), Iz(0, 0, ust), Iz(u, 0, ust), Iz(0, g, ust), Iz(u, g, ust)
        };
        foreach (var k in kutular)
        {
            noktalar.Add(Iz(k.XMm, k.YMm, k.ZMm));
            noktalar.Add(Iz(k.XMm + k.BoyMm, k.YMm + k.EnMm, k.ZMm + k.YukseklikMm));
            noktalar.Add(Iz(k.XMm, k.YMm + k.EnMm, k.ZMm + k.YukseklikMm));
            noktalar.Add(Iz(k.XMm + k.BoyMm, k.YMm, k.ZMm));
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

        // Ressam sırası: uzaktaki (küçük X+Y) ve alttaki kutu önce.
        foreach (var k in kutular.OrderBy(k => k.XMm + k.YMm).ThenBy(k => k.ZMm))
        {
            double x0 = k.XMm, y0 = k.YMm, z0 = k.ZMm, x1 = k.XMm + k.BoyMm, y1 = k.YMm + k.EnMm, z1 = k.ZMm + k.YukseklikMm;
            var dolgu = KutuDolgusu(k);
            var cizgi = k.Sorunlu ? "#7f1d1d" : "#1e293b";
            t.Cokgen([Iz(x1, y0, z0), Iz(x1, y1, z0), Iz(x1, y1, z1), Iz(x1, y0, z1)], Koyu(dolgu, 0.72), cizgi, c * 0.15);
            t.Cokgen([Iz(x0, y1, z0), Iz(x1, y1, z0), Iz(x1, y1, z1), Iz(x0, y1, z1)], Koyu(dolgu, 0.86), cizgi, c * 0.15);
            t.Cokgen([Iz(x0, y0, z1), Iz(x1, y0, z1), Iz(x1, y1, z1), Iz(x0, y1, z1)], dolgu, cizgi, c * 0.15);
            var merkez = Iz((x0 + x1) / 2, (y0 + y1) / 2, z1);
            t.Yazi(merkez.X, merkez.Y, k.Etiket, EtiketBoyu(k.BoyMm, k.EnMm, c) * 0.8, "#ffffff");
        }
        var on = Iz(0, g / 2.0, ust);
        var arka = Iz(u, g / 2.0, ust);
        t.Yazi(on.X, on.Y - c * 5, "ÖN", c * 4.2);
        t.Yazi(arka.X, arka.Y - c * 5, "ARKA", c * 4.2);
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
