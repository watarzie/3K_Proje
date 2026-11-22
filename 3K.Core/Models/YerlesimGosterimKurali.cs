using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>
/// Saklanan yerleşimin gösterimi (plan ekranı, elle düzenleyici, saha ekranı ve PDF raporları tek eşlemeden geçer):
/// "Teyit edilmedi" hiçbir yerde gösterilmez (saha isteği, 20 Kasım 2026). Motor yeni hesapta teyit edilemeyen
/// kontrolleri zaten ayıklar; bu kural mevzuat kaldırılmadan önce hesaplanıp saklanmış yerleşimler içindir.
/// </summary>
public static class YerlesimGosterimKurali
{
    /// <summary>Kaldırılan kurallara ait, eski hesaplarda saklı kalmış genel uyarıların başlangıçları.</summary>
    private static readonly string[] KaldirilanUyarilar =
    [
        "Onaylı mevzuat kural seti yok",
        "Tanımlanmamış güvenlik payları"
    ];

    /// <summary>
    /// Kaldırılan genel uyarıların içerik parçaları (metin "Araç N: " ile başladığı için önekle eşleşmez). Farklı
    /// lokasyonların aynı araca atanması uyarı değildir: "tek yerden" kullanıcının bilinçli seçimidir, "lokasyona göre
    /// ayrı"da oluşmaz (saha isteği, 20 Kasım 2026).
    /// </summary>
    private static readonly string[] KaldirilanUyariIcerikleri =
    [
        "farklı lokasyonlardaki yükler aynı araca atandı"
    ];

    /// <summary>Kontrol ya da yük sorunu gösterilir mi: teyit edilemeyen sonuç gösterilmez.</summary>
    public static bool Gosterilir(YerlesimKontrolSonucu sonuc) => sonuc != YerlesimKontrolSonucu.TeyitEdilmedi;

    /// <summary>
    /// Araç ya da plan sonucu. Saklı "Teyit edilmedi", uygunsuz kontrol olmadığı anlamına gelir (sonuç en kötü kontrolden
    /// türer); teyitsiz kontroller sonuçtan düştüğünde yeni hesap da aynı aracı "Uygun" bulur.
    /// </summary>
    public static YerlesimKontrolSonucu Sonuc(YerlesimKontrolSonucu sonuc) =>
        sonuc == YerlesimKontrolSonucu.TeyitEdilmedi ? YerlesimKontrolSonucu.Uygun : sonuc;

    /// <summary>
    /// Genel uyarı gösterilir mi: mevzuat ve tanımsız güvenlik payı uyarıları (kaldırılan kurallar) ile farklı lokasyonların
    /// aynı araca atandığı uyarısı gösterilmez.
    /// </summary>
    public static bool UyariGosterilir(string uyari) =>
        !KaldirilanUyarilar.Any(o => uyari.StartsWith(o, StringComparison.Ordinal)) &&
        !KaldirilanUyariIcerikleri.Any(i => uyari.Contains(i, StringComparison.Ordinal));
}
