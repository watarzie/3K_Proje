namespace _3K.Infrastructure.Services.Etiketler;

/// <summary>
/// Godex iç fontlarının (A 6, B 8, C 10, D 12 pt; CG Triumvirate = Helvetica/Arial ölçüsü) basılı genişliği. Şablondaki
/// sabit koordinatlara uzun metin (müşteri adı, varış yeri) sığmayınca satır kaydırmak için kullanılır; PDF de aynı
/// puntoyla çizer. Ölçü nokta (203 dpi) cinsindendir.
/// </summary>
internal static class GodexYaziOlcusu
{
    /// <summary>
    /// Sandık etiketi fotoğrafından ölçülen genişlik oranı (başlık, FB NO, CASE NO; Godex / Arial): basılı metin aynı
    /// puntodaki Arial'den ~%7 dardır.
    /// </summary>
    public const float Olcek = 0.93f;

    /// <summary>Helvetica genişlikleri (em'in binde biri), ' ' (32) – '~' (126).</summary>
    private static readonly short[] Genislikler =
    [
        278, 278, 355, 556, 556, 889, 667, 191, 333, 333, 389, 584, 278, 333, 278, 278, // ' ' – '/'
        556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 278, 278, 584, 584, 584, 556, // '0' – '?'
        1015, 667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833, 722, 778, // '@' – 'O'
        667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 278, 278, 278, 469, 556, // 'P' – '_'
        333, 556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833, 556, 556, // '`' – 'o'
        556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500, 334, 260, 334, 584       // 'p' – '~'
    ];

    /// <summary>EZPL kılavuzundaki punto (A 6, B 8, C 10, D 12).</summary>
    public static float Punto(char font) => font switch
    {
        'A' => 6f,
        'B' => 8f,
        'C' => 10f,
        'D' => 12f,
        _ => throw new InvalidOperationException($"Desteklenmeyen Godex fontu: {font}")
    };

    /// <summary>Metnin basılı genişliği (nokta).</summary>
    public static float Genislik(char font, string metin)
    {
        var binde = metin.Sum(c => c is >= ' ' and <= '~' ? Genislikler[c - ' '] : 556);
        return binde / 1000f * Punto(font) * Olcek / 72f * 203f;
    }

    /// <summary>
    /// Metni en fazla <paramref name="enFazlaSatir"/> satıra, kelime sınırından kaydırır; satıra sığmayan tek kelime
    /// harf harf bölünür. Satırlar yetmezse kalan metin atılır (etiket kenarından taşmaz). Boş metin tek boş satırdır.
    /// </summary>
    public static IReadOnlyList<string> Sar(char font, string metin, float genislik, int enFazlaSatir)
    {
        var satirlar = new List<string>();
        var satir = "";
        foreach (var kelime in metin.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var aday = satir.Length == 0 ? kelime : satir + " " + kelime;
            if (Genislik(font, aday) <= genislik)
            {
                satir = aday;
                continue;
            }
            if (satir.Length > 0)
                satirlar.Add(satir);
            satir = kelime;
            // Satıra tek başına sığmayan kelime harf harf bölünür.
            while (Genislik(font, satir) > genislik && satir.Length > 1)
            {
                var kesim = satir.Length - 1;
                while (kesim > 1 && Genislik(font, satir[..kesim]) > genislik)
                    kesim--;
                satirlar.Add(satir[..kesim]);
                satir = satir[kesim..];
            }
        }
        satirlar.Add(satir);
        if (satirlar.Count <= enFazlaSatir)
            return satirlar;
        return satirlar.Take(enFazlaSatir).ToList();
    }
}
