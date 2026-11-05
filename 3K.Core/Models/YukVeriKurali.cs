namespace _3K.Core.Models;

/// <summary>
/// Yükün ölçü ve ağırlık verisi hesaplanabilir mi (Simülasyon isterleri 5). Ölçüsü veya ağırlığı eksik, sıfır ya da
/// geçersiz yük hesaplanmaz, satırında mesaj gösterilir; eksik veri tahminle doldurulmaz. Sıfır, kaynakta boş ölçünün
/// karşılığıdır ve eksik sayılır.
/// </summary>
public static class YukVeriKurali
{
    public const string OlcuGiriniz = "Ölçü giriniz.";
    public const string AgirlikGiriniz = "Ağırlık giriniz.";
    public const string GecerliOlcuGiriniz = "Geçerli bir ölçü giriniz.";
    public const string GecerliAgirlikGiriniz = "Geçerli bir ağırlık giriniz.";

    /// <summary>Bir kenar en fazla 50 m olabilir; üstü birim hatası sayılır (veride en uzun yük 14,2 m).</summary>
    public const decimal EnFazlaOlcuMm = 50_000m;

    /// <summary>Bir yük en fazla 1.000 t olabilir; üstü birim hatası sayılır (veride en ağır yük 351 t).</summary>
    public const decimal EnFazlaAgirlikKg = 1_000_000m;

    /// <summary>Satırda gösterilecek veri sorunları; boşsa yük hesaplanabilir.</summary>
    public static IReadOnlyList<string> Sorunlar(decimal? boyMm, decimal? enMm, decimal? yukseklikMm, decimal? brutKg)
    {
        var sorunlar = new List<string>(2);
        decimal?[] olculer = [boyMm, enMm, yukseklikMm];
        if (olculer.Any(o => o is null or 0))
            sorunlar.Add(OlcuGiriniz);
        else if (olculer.Any(o => o < 0 || o > EnFazlaOlcuMm))
            sorunlar.Add(GecerliOlcuGiriniz);

        if (brutKg is null or 0)
            sorunlar.Add(AgirlikGiriniz);
        else if (brutKg < 0 || brutKg > EnFazlaAgirlikKg)
            sorunlar.Add(GecerliAgirlikGiriniz);

        return sorunlar;
    }

    /// <summary>Elle girilen ölçü: boş bırakılabilir (satırda "Ölçü giriniz."), girildiyse sıfırdan büyük ve sınır içinde.</summary>
    public static bool ElleOlcuGecerli(decimal? olcuMm) => olcuMm is null || (olcuMm > 0 && olcuMm <= EnFazlaOlcuMm);

    /// <summary>Elle girilen ağırlık: boş bırakılabilir, girildiyse sıfırdan büyük ve sınır içinde.</summary>
    public static bool ElleAgirlikGecerli(decimal? agirlikKg) => agirlikKg is null || (agirlikKg > 0 && agirlikKg <= EnFazlaAgirlikKg);
}
