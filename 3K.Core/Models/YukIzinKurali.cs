using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>Plan yükünün döndürme ve istif izinleri ile üst yük sınırı.</summary>
public sealed record PlanYukIzinleri(bool DondurulebilirMi, bool UsteKonabilirMi, bool AltaKalabilirMi, decimal? UstYukSiniriKg);

/// <summary>
/// Yük izinlerinin varsayılanı ve korunması (Simülasyon isterleri 6–7). Yan yatırma ve ters çevirme her zaman
/// yasaktır; bu yüzden izin olarak tutulmaz.
/// </summary>
public static class YukIzinKurali
{
    public const string UstYukSiniriMesaji = "Üst yük sınırı sıfır veya pozitif olmalıdır.";

    /// <summary>
    /// Başlangıç izinleri. Sandıkta tabanda 90° döndürme pasif, üste konabilir ve altta kalabilir aktif, üst yük sınırı
    /// kendi brüt ağırlığıdır (işletmenin başlangıç kabulü; doğrulanmış üretici dayanımı değildir). Brüt bilinmiyorsa
    /// sınır da bilinmez. Özel yükte sandık varsayılanları uygulanmaz: yük profili tanımlanana kadar izinler kapalı
    /// ve sınır boştur; kullanıcı elle açabilir.
    /// </summary>
    public static PlanYukIzinleri Varsayilan(int yukTuruId, decimal? brutKg) =>
        YukTuruKurali.OzelYukMu(yukTuruId)
            ? new PlanYukIzinleri(false, false, false, null)
            : new PlanYukIzinleri(false, true, true, brutKg > 0 ? brutKg : null);

    /// <summary>
    /// Kaynaktan yenileme ve yeniden hesaplama: kullanıcının elle değiştirdiği izin korunur, öbürleri yeni kaynak
    /// değerlerinden varsayılana göre yeniden kurulur.
    /// </summary>
    public static PlanYukIzinleri Yenile(PlanYukIzinleri mevcut, PlanYukIzni elleDegisen, int yukTuruId, decimal? brutKg)
    {
        var varsayilan = Varsayilan(yukTuruId, brutKg);
        return new PlanYukIzinleri(
            elleDegisen.HasFlag(PlanYukIzni.Dondurme) ? mevcut.DondurulebilirMi : varsayilan.DondurulebilirMi,
            elleDegisen.HasFlag(PlanYukIzni.UsteKonma) ? mevcut.UsteKonabilirMi : varsayilan.UsteKonabilirMi,
            elleDegisen.HasFlag(PlanYukIzni.AltaKalma) ? mevcut.AltaKalabilirMi : varsayilan.AltaKalabilirMi,
            elleDegisen.HasFlag(PlanYukIzni.UstYukSiniri) ? mevcut.UstYukSiniriKg : varsayilan.UstYukSiniriKg);
    }

    public const string ElleDegisenMesaji = "Elle değişen izin bilgisi geçersiz; sayfayı yenileyip tekrar deneyin.";

    /// <summary>Tanımlı bütün izin bayrakları; elle değişen bilgisi bunların dışında bit taşıyamaz.</summary>
    public const PlanYukIzni TumIzinler = PlanYukIzni.Dondurme | PlanYukIzni.UsteKonma | PlanYukIzni.AltaKalma | PlanYukIzni.UstYukSiniri;

    /// <summary>İstemcinin gönderdiği "elle değişti" bayrakları yalnız tanımlı izinlerden oluşmalıdır.</summary>
    public static bool ElleDegisenGecerli(PlanYukIzni elleDegisen) => (elleDegisen & ~TumIzinler) == 0;

    public static bool UstYukSiniriGecerli(decimal? ustYukSiniriKg) =>
        ustYukSiniriKg is null || (ustYukSiniriKg >= 0 && ustYukSiniriKg <= YukVeriKurali.EnFazlaAgirlikKg);
}
