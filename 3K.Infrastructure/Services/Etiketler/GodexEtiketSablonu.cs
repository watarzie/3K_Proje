using System.Text;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services.Etiketler;

/// <summary>Etiket öğesi; koordinatlar Godex noktasıdır (203 dpi, 1 mm = 8 nokta), sol kenar boşluğu hariç.</summary>
internal abstract record EtiketOgesi;

/// <summary>Godex iç fontuyla metin (A 6 pt, B 8 pt, C 10 pt, D 12 pt); y metnin üst kenarıdır.</summary>
internal sealed record MetinOgesi(char Font, int X, int Y, string Metin) : EtiketOgesi;

/// <summary>Code 128 (Godex <c>BQ</c>, alt küme yazıcı seçer); okunur metin barkodun altında ortalıdır.</summary>
internal sealed record BarkodOgesi(int X, int Y, int Dar, int Genis, int Yukseklik, string Veri) : EtiketOgesi;

/// <summary>Dolu dikdörtgen çizgi (Godex <c>Lo</c>): sol üst ve sağ alt köşe dahil.</summary>
internal sealed record CizgiOgesi(int X, int Y, int X1, int Y1) : EtiketOgesi;

/// <summary>Doldurulmuş etiket: boy/genişlik mm, sol kenar (Godex <c>^R</c>) nokta, adet (<c>^P</c>).</summary>
internal sealed record DoluEtiket(int BoyMm, int GenislikMm, int SolKenar, int Adet, IReadOnlyList<EtiketOgesi> Ogeler);

/// <summary>
/// Kullanıcının Godex şablonları (Label_Sandik / Label_Urun, EZPL) birebir: koordinat, font, barkod ve çizgiler. Tek
/// fark ürün etiketinin başlığıdır: şablondaki "ALSTOM GRID / ATT / GEBZE" yerine sandık etiketindeki gibi
/// "Grid Solutions ATT / Gebze" (26.10.2026 kullanıcı kararı). PDF de aynı öğeleri çizer.
/// </summary>
internal static class GodexEtiketSablonu
{
    public const string Baslik = "Grid Solutions ATT / Gebze";
    /// <summary>203 dpi yazıcıda milimetredeki nokta.</summary>
    public const float NoktaMm = 8f;

    /// <summary>Sandık etiketinin sağ sütunu (CLIENT NAME / DISPATCH DESTINATION): x = 424'ten etiket kenarına (790).</summary>
    public const int SandikSagSutunGenisligi = 366;
    /// <summary>
    /// Ürün etiketinde müşteri adına kalan genişlik: x = 106'dan varış yerinin başladığı 264'e (Grid etiketinde de
    /// "MOE POC-IRAQ" varış yerine dayanır).
    /// </summary>
    public const int UrunMusteriGenisligi = 158;
    /// <summary>Ürün etiketinde varış yerine kalan genişlik: x = 264'ten etiket kenarına (^R16 ile 458).</summary>
    public const int UrunLokasyonGenisligi = 194;

    /// <summary>
    /// Sandık etiketi, 100×75 mm (^Q75,3 / ^W100 / ^R0). "CLIENT NAME: müşteri" sağ sütuna sığarsa şablon birebir; sığmazsa
    /// "CLIENT NAME:" kendi satırına iner, müşteri en fazla iki satıra kayar ve DISPATCH DESTINATION altına kayar
    /// (veride 44 karaktere kadar müşteri adı var; şablon kısa ada göre çizilmiş).
    /// </summary>
    public static DoluEtiket Sandik(SandikEtiketi e)
    {
        List<EtiketOgesi> ogeler =
        [
            new BarkodOgesi(134, 140, 4, 10, 174, e.Barkod),
            new MetinOgesi('D', 20, 496, "FB NO:"),
            new MetinOgesi('D', 15, 542, "CASE NO:"),
            new MetinOgesi('D', 171, 494, e.SeriNo),
            new MetinOgesi('D', 169, 542, e.SandikNo),
            new MetinOgesi('D', 228, 401, "Case Label / Koli Etiketi"),
        ];
        var lokasyon = GodexYaziOlcusu.Sar('B', e.Lokasyon, SandikSagSutunGenisligi, 1)[0];
        var tekSatir = "CLIENT NAME: " + e.Musteri;
        if (GodexYaziOlcusu.Genislik('B', tekSatir) <= SandikSagSutunGenisligi)
        {
            ogeler.Add(new MetinOgesi('B', 423, 493, tekSatir));
            ogeler.AddRange(SandikAlt(526, lokasyon));
        }
        else
        {
            var musteri = GodexYaziOlcusu.Sar('B', e.Musteri, SandikSagSutunGenisligi, 2);
            ogeler.Add(new MetinOgesi('B', 424, 470, "CLIENT NAME:"));
            ogeler.AddRange(musteri.Select((satir, i) => new MetinOgesi('B', 424, 493 + 23 * i, satir)));
            ogeler.AddRange(SandikAlt(493 + 23 * musteri.Count + 6, lokasyon));
        }
        return new(75, 100, 0, e.Adet, ogeler);

        IEnumerable<EtiketOgesi> SandikAlt(int y, string lokasyon) =>
        [
            new CizgiOgesi(14, 385, 776, 396),
            new MetinOgesi('D', 242, 48, Baslik),
            new CizgiOgesi(15, 452, 777, 463),
            new MetinOgesi('C', 651, 99, e.Tarih),
            new MetinOgesi('B', 424, y, "DISPATCH DESTINATION:"),
            new MetinOgesi('B', 424, y + 26, lokasyon),
        ];
    }

    /// <summary>
    /// Ürün etiketi, 60×40 mm (^Q40,3 / ^W60 / ^R16). Müşteri adı DESTINATION sütununa kadar sığarsa şablon birebir;
    /// sığmazsa küçük fontla (A) en fazla iki satıra kayar, satır çizgisinin üstünde kalır.
    /// </summary>
    public static DoluEtiket Urun(UrunEtiketi e)
    {
        List<EtiketOgesi> musteri;
        if (GodexYaziOlcusu.Genislik('B', e.Musteri) <= UrunMusteriGenisligi)
            musteri = [new MetinOgesi('B', 106, 189, e.Musteri)];
        else
        {
            var satirlar = GodexYaziOlcusu.Sar('A', e.Musteri, UrunMusteriGenisligi, 2);
            musteri = satirlar.Count == 1
                ? [new MetinOgesi('A', 106, 193, satirlar[0])]
                : [.. satirlar.Select((satir, i) => new MetinOgesi('A', 106, 184 + 15 * i, satir))];
        }
        return new(40, 60, 16, e.Adet,
        [
            // Şablondaki iki başlık (AC,25,18 "ALSTOM GRID" / AC,241,14 "ATT / GEBZE") yerine tek başlık: C fontunda
            // ~345 nokta genişliğinde, 480 noktalık etikete ^R16 kenarıyla ortalanır.
            new MetinOgesi('C', 51, 16, Baslik),
            new BarkodOgesi(20, 68, 2, 5, 62, e.Barkod),
            new MetinOgesi('B', 13, 163, "FB NO:"),
            new MetinOgesi('A', 12, 221, "TANIM:"),
            new MetinOgesi('A', 12, 241, "DESCRIPTION:"),
            new MetinOgesi('B', 12, 191, "CLIENT:"),
            .. musteri,
            new MetinOgesi('A', 12, 269, "REMARKS:"),
            new MetinOgesi('B', 95, 164, e.SeriNo),
            new MetinOgesi('A', 127, 219, e.Tanim),
            new MetinOgesi('A', 102, 269, e.Remarks),
            new CizgiOgesi(2, 216, 438, 217),
            new CizgiOgesi(0, 262, 436, 263),
            new MetinOgesi('A', 264, 170, "DESTINATION:"),
            new MetinOgesi('A', 264, 189, GodexYaziOlcusu.Sar('A', e.Lokasyon, UrunLokasyonGenisligi, 1)[0]),
            new MetinOgesi('A', 128, 240, e.Aciklama),
        ]);
    }

    /// <summary>
    /// Etiketler EZPL komutu olarak (her etiket kendi kurulumuyla, adedi ^P ile); ASCII, CRLF. Dosya yazıcıya ham
    /// gönderilir (GoLabel veya <c>copy /b dosya.prn \\bilgisayar\yazici</c>).
    /// </summary>
    public static byte[] Ezpl(IEnumerable<DoluEtiket> etiketler)
    {
        var sb = new StringBuilder();
        foreach (var etiket in etiketler)
        {
            Satir(sb, $"^Q{etiket.BoyMm},3");
            Satir(sb, $"^W{etiket.GenislikMm}");
            Satir(sb, "^H5");
            Satir(sb, $"^P{etiket.Adet}");
            Satir(sb, "^S2");
            Satir(sb, "^AD");
            Satir(sb, "^C1");
            Satir(sb, $"^R{etiket.SolKenar}");
            Satir(sb, "~Q+0");
            Satir(sb, "^O0");
            Satir(sb, "^D0");
            Satir(sb, "^E12");
            Satir(sb, "~R200");
            Satir(sb, "^XSET,ROTATION,0");
            Satir(sb, "^L");
            Satir(sb, "Dy2-me-dd");
            Satir(sb, "Th:m:s");
            foreach (var oge in etiket.Ogeler)
            {
                Satir(sb, oge switch
                {
                    MetinOgesi m => $"A{m.Font},{m.X},{m.Y},1,1,0,0,{m.Metin}",
                    BarkodOgesi b => $"BQ,{b.X},{b.Y},{b.Dar},{b.Genis},{b.Yukseklik},0,3,{b.Veri}",
                    CizgiOgesi c => $"Lo,{c.X},{c.Y},{c.X1},{c.Y1}",
                    _ => throw new InvalidOperationException("Bilinmeyen etiket öğesi.")
                });
            }
            Satir(sb, "E");
        }
        // Metinler BarkodEtiketKurali.Metin ile ASCII'ye çevrilmiştir; ASCII dışı karakter "?" olur.
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    private static void Satir(StringBuilder sb, string satir) => sb.Append(satir).Append("\r\n");
}
