namespace _3K.Core.Helpers;

/// <summary>
/// Code 128 kodlayıcı (B ve C alt kümeleri; yazdırılabilir ASCII 32–126). Godex <c>BQ</c> komutu gibi alt kümeyi
/// kendisi seçer: başta/sonda en az 4, ortada en az 6 rakamlık dizi C ile çift çift, kalanı B ile yazılır. PDF etiketi
/// çubukları bu desenden çizer; EZPL çıktısında barkodu yazıcının kendisi üretir.
/// </summary>
public static class Code128
{
    private const int KodB = 100;
    private const int KodC = 99;
    private const int BaslaB = 104;
    private const int BaslaC = 105;
    private const int Dur = 106;

    /// <summary>Her sembolün çubuk/boşluk genişlikleri (modül); 0–102 veri, 103–105 başlangıç, 106 bitiş.</summary>
    private static readonly string[] Desenler =
    [
        "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213",
        "221312", "231212", "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132",
        "221231", "213212", "223112", "312131", "311222", "321122", "321221", "312212", "322112", "322211",
        "212123", "212321", "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313",
        "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", "313121", "211331",
        "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111",
        "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214",
        "112412", "122114", "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111",
        "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", "421211", "212141",
        "214121", "412121", "111143", "111341", "131141", "114113", "114311", "411113", "411311", "113141",
        "114131", "311141", "411131", "211412", "211214", "211232", "2331112"
    ];

    /// <summary>Boş değilse ve her karakter yazdırılabilir ASCII (32–126) ise kodlanabilir.</summary>
    public static bool Kodlanabilir(string? veri) =>
        !string.IsNullOrEmpty(veri) && veri.All(c => c is >= ' ' and <= '~');

    /// <summary>Sembol değerleri: başlangıç, veri ve alt küme geçişleri, kontrol sembolü, bitiş.</summary>
    public static IReadOnlyList<int> Kodlar(string veri)
    {
        if (!Kodlanabilir(veri))
            throw new ArgumentException("Code 128 verisi boş olamaz ve yalnız yazdırılabilir ASCII içerebilir.", nameof(veri));

        var kodlar = new List<int>();
        int? kume = null;
        var i = 0;
        while (i < veri.Length)
        {
            var rakam = RakamDizisi(veri, i);
            var cGerekli = rakam >= 6 || (rakam >= 4 && (i == 0 || i + rakam == veri.Length));
            if (cGerekli)
            {
                if (kume != BaslaC)
                    kodlar.Add(kume is null ? BaslaC : KodC);
                kume = BaslaC;
                // Tek kalan rakam sonraki turda B ile yazılır.
                var cift = rakam - rakam % 2;
                for (var j = 0; j < cift; j += 2, i += 2)
                    kodlar.Add((veri[i] - '0') * 10 + (veri[i + 1] - '0'));
                continue;
            }

            if (kume != BaslaB)
                kodlar.Add(kume is null ? BaslaB : KodB);
            kume = BaslaB;
            kodlar.Add(veri[i] - ' ');
            i++;
        }

        var toplam = kodlar[0];
        for (var k = 1; k < kodlar.Count; k++)
            toplam += kodlar[k] * k;
        kodlar.Add(toplam % 103);
        kodlar.Add(Dur);
        return kodlar;
    }

    /// <summary>
    /// Siyah çubuklar (başlangıç modülü, genişlik modül) ve toplam genişlik; sessiz bölge dahil değildir. Çizen taraf
    /// modülü dar çubuk genişliğiyle (Godex <c>narrow</c>) çarpar.
    /// </summary>
    public static (IReadOnlyList<(int Baslangic, int Genislik)> Cubuklar, int ToplamModul) Cubuklar(string veri)
    {
        var cubuklar = new List<(int, int)>();
        var konum = 0;
        foreach (var kod in Kodlar(veri))
        {
            var desen = Desenler[kod];
            for (var k = 0; k < desen.Length; k++)
            {
                var genislik = desen[k] - '0';
                if (k % 2 == 0)
                    cubuklar.Add((konum, genislik));
                konum += genislik;
            }
        }
        return (cubuklar, konum);
    }

    private static int RakamDizisi(string veri, int baslangic)
    {
        var son = baslangic;
        while (son < veri.Length && char.IsAsciiDigit(veri[son]))
            son++;
        return son - baslangic;
    }
}
