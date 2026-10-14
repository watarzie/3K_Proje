namespace _3K.Core.Models;

/// <summary>
/// Proje fotoğraf sayfası bağlantısı: yalnız mutlak http/https adresi kabul edilir (javascript:, data:, dosya yolu ve
/// boşluk içeren adres reddedilir), en fazla <see cref="EnFazlaUzunluk"/> karakter. Şemasız yazılan adres
/// (ör. foto.ornek.com/pa727) https kabul edilir; boş değer bağlantıyı kaldırır.
/// Ön yüz aynası: <c>features/projeler/proje-listesi/proje-fotograf-baglantisi.ts</c>.
/// </summary>
public static class ProjeFotografBaglantisiKurali
{
    public const int EnFazlaUzunluk = 1000;

    /// <summary>Kırpılmış adres; şema yoksa https:// eklenir; boşsa null (bağlantı yok).</summary>
    public static string? Normalize(string? baglanti)
    {
        if (string.IsNullOrWhiteSpace(baglanti))
            return null;
        var deger = baglanti.Trim();
        return deger.Contains("://", StringComparison.Ordinal) ? deger : "https://" + deger;
    }

    /// <summary>Boş (kaldırma) veya geçerli bir http/https adresi mi.</summary>
    public static bool Gecerli(string? baglanti)
    {
        var deger = Normalize(baglanti);
        return deger is null || (deger.Length <= EnFazlaUzunluk
            && !deger.Any(char.IsWhiteSpace)
            && Uri.TryCreate(deger, UriKind.Absolute, out var adres)
            && (adres.Scheme == Uri.UriSchemeHttps || adres.Scheme == Uri.UriSchemeHttp)
            && !string.IsNullOrWhiteSpace(adres.Host));
    }
}
