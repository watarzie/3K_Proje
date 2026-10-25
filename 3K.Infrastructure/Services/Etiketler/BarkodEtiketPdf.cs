using System.Globalization;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using _3K.Core.Helpers;

namespace _3K.Infrastructure.Services.Etiketler;

/// <summary>
/// Godex şablonunu etiket boyunda PDF'e çizer: her etiket bir sayfa (adet kadar tekrar), öğeler şablondaki noktalarla
/// (1 mm = 8 nokta). Godex iç fontları CG Triumvirate ölçüsündedir; PDF Arial (yoksa Lato) kullanır. Godex
/// sürücüsünde kâğıt boyu etiket boyuna (60×40 / 100×75 mm) ayarlanıp "gerçek boyut" ile basılır.
/// </summary>
internal static class BarkodEtiketPdf
{
    private static readonly string[] YaziTipi = ["Arial", "Lato"];
    /// <summary>Barkod altındaki okunur metnin boyu (pt) ve barkoda uzaklığı (nokta).</summary>
    private const float OkunurPunto = 10f;
    private const int OkunurAralik = 2;

    public static byte[] Olustur(IReadOnlyList<DoluEtiket> etiketler)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        return Document.Create(container =>
        {
            foreach (var etiket in etiketler)
                for (var i = 0; i < etiket.Adet; i++)
                    container.Page(page => Sayfa(page, etiket));
        }).GeneratePdf();
    }

    private static void Sayfa(PageDescriptor page, DoluEtiket etiket)
    {
        page.Size(etiket.GenislikMm, etiket.BoyMm, Unit.Millimetre);
        page.Margin(0);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(s => s.FontFamily(YaziTipi).FontColor(Colors.Black).LineHeight(1));
        page.Content().Layers(layers =>
        {
            layers.PrimaryLayer().Width(etiket.GenislikMm, Unit.Millimetre).Height(etiket.BoyMm, Unit.Millimetre);
            foreach (var oge in etiket.Ogeler)
                Ciz(layers, oge, etiket.SolKenar);
        });
    }

    private static void Ciz(LayersDescriptor katmanlar, EtiketOgesi oge, int solKenar)
    {
        switch (oge)
        {
            case MetinOgesi m:
                Konum(katmanlar.Layer(), m.X + solKenar, m.Y).Text(m.Metin).FontSize(Punto(m.Font));
                break;
            case BarkodOgesi b:
                var (cubuklar, toplamModul) = Code128.Cubuklar(b.Veri);
                var genislik = toplamModul * b.Dar;
                Konum(katmanlar.Layer(), b.X + solKenar, b.Y).Width(Mm(genislik), Unit.Millimetre).Height(Mm(b.Yukseklik), Unit.Millimetre)
                    .Svg(Svg(cubuklar, b.Dar, genislik, b.Yukseklik));
                // Godex okunur metni (readable=3) barkodun altına ortalar.
                Konum(katmanlar.Layer(), b.X + solKenar, b.Y + b.Yukseklik + OkunurAralik).Width(Mm(genislik), Unit.Millimetre)
                    .AlignCenter().Text(b.Veri).FontSize(OkunurPunto);
                break;
            case CizgiOgesi c:
                Konum(katmanlar.Layer(), c.X + solKenar, c.Y).Width(Mm(c.X1 - c.X + 1), Unit.Millimetre)
                    .Height(Mm(c.Y1 - c.Y + 1), Unit.Millimetre).Background(Colors.Black);
                break;
            default:
                throw new InvalidOperationException("Bilinmeyen etiket öğesi.");
        }
    }

    private static IContainer Konum(IContainer katman, int x, int y) =>
        katman.TranslateX(Mm(x), Unit.Millimetre).TranslateY(Mm(y), Unit.Millimetre).Unconstrained();

    private static string Svg(IReadOnlyList<(int Baslangic, int Genislik)> cubuklar, int dar, int genislik, int yukseklik)
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture,
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{genislik}\" height=\"{yukseklik}\" viewBox=\"0 0 {genislik} {yukseklik}\">");
        foreach (var (baslangic, cubuk) in cubuklar)
            sb.Append(CultureInfo.InvariantCulture,
                $"<rect x=\"{baslangic * dar}\" y=\"0\" width=\"{cubuk * dar}\" height=\"{yukseklik}\" fill=\"#000\"/>");
        return sb.Append("</svg>").ToString();
    }

    /// <summary>
    /// Godex fontunun PDF puntosu: basılı etikette metinler aynı puntodaki Arial'den ~%7 dardır
    /// (<see cref="GodexYaziOlcusu.Olcek"/>); ölçeksiz Arial "CASE NO:" ve "DESCRIPTION:" etiketlerini şablondaki değer
    /// konumunun üstüne taşır. Satır kaydırma da aynı ölçüyle hesaplanır.
    /// </summary>
    private static float Punto(char font) => GodexYaziOlcusu.Olcek * GodexYaziOlcusu.Punto(font);

    private static float Mm(int nokta) => nokta / GodexEtiketSablonu.NoktaMm;
}
