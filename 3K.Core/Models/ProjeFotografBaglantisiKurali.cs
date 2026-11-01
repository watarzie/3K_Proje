using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>
/// Proje fotoğraf bağlantıları: yalnız mutlak http/https adresi kabul edilir (javascript:, data:, dosya yolu ve
/// boşluk içeren adres reddedilir), en fazla <see cref="EnFazlaUzunluk"/> karakter. Şemasız yazılan adres
/// (ör. foto.ornek.com/pa727) https kabul edilir. Açıklama isteğe bağlıdır, en fazla <see cref="AciklamaEnFazlaUzunluk"/>
/// karakter. Bir projede her türden (fotoğraf, gerçekleşen çeki) en fazla <see cref="EnFazlaBaglanti"/> bağlantı olur;
/// aynı adres projede türü ne olursa olsun iki kez eklenmez.
/// Ön yüz aynası: <c>features/projeler/proje-listesi/proje-fotograf-baglantisi.ts</c>.
/// </summary>
public static class ProjeFotografBaglantisiKurali
{
    /// <summary>Geçmiş kaydında ve iletilerde türün adı.</summary>
    public static string TurAdi(ProjeBaglantiTuru tur) => tur == ProjeBaglantiTuru.GerceklesenCeki
        ? "Gerçekleşen Çeki Bağlantısı"
        : "Fotoğraf Bağlantısı";

    public const int EnFazlaUzunluk = 1000;
    public const int AciklamaEnFazlaUzunluk = 100;
    public const int EnFazlaBaglanti = 20;

    /// <summary>Kırpılmış adres; şema yoksa https:// eklenir; boşsa null.</summary>
    public static string? Normalize(string? baglanti)
    {
        if (string.IsNullOrWhiteSpace(baglanti))
            return null;
        var deger = baglanti.Trim();
        return deger.Contains("://", StringComparison.Ordinal) ? deger : "https://" + deger;
    }

    /// <summary>Dolu ve geçerli bir http/https adresi mi (boş adres bağlantı değildir).</summary>
    public static bool Gecerli(string? baglanti)
    {
        var deger = Normalize(baglanti);
        return deger is not null && deger.Length <= EnFazlaUzunluk
            && !deger.Any(char.IsWhiteSpace)
            && Uri.TryCreate(deger, UriKind.Absolute, out var adres)
            && (adres.Scheme == Uri.UriSchemeHttps || adres.Scheme == Uri.UriSchemeHttp)
            && !string.IsNullOrWhiteSpace(adres.Host);
    }

    /// <summary>Kırpılmış açıklama; boşsa null.</summary>
    public static string? AciklamaNormalize(string? aciklama) =>
        string.IsNullOrWhiteSpace(aciklama) ? null : aciklama.Trim();

    /// <summary>Boş veya en fazla <see cref="AciklamaEnFazlaUzunluk"/> karakterlik açıklama mı.</summary>
    public static bool AciklamaGecerli(string? aciklama) =>
        (AciklamaNormalize(aciklama)?.Length ?? 0) <= AciklamaEnFazlaUzunluk;
}
