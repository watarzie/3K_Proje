namespace _3K.Core.Models;

/// <summary>
/// S-27: Sahaya aktarılıp sahada Grid İptal edilen ürün hiç gönderilmeyecektir. Ana (normal) projenin sevk durumu
/// hesabında bu ürünün aktarılan miktarı, ana projede iptal edilen ürün gibi kapanmış sayılır; böylece bütün
/// sandıkları sevk edilmiş ana proje yalnız bu ürün yüzünden Eksik Sevk Edildi'de kalmaz.
/// Kapanan miktar fiziksel teslim değildir: gerçekleşen saha sevki haritası, raporlar ve "saha sevki var mı"
/// kararı bu kuraldan etkilenmez. Sahadaki iptal geri alınırsa senkron durumu yeniden Eksik Sevk Edildi yapar.
/// Yalnız saha satırının Grid İptal durumu sayılır; sahada Grid Kapandı olan satır bu kuralla kapanmaz.
/// </summary>
public static class SahaIptalSevkKapanisKurali
{
    /// <summary>
    /// İptal edilen saha kaleminin, gerçekleşen sevkine ek olarak kapattığı miktar: aktarılan − gerçekleşen (en az 0).
    /// Gerçekleşen kısım zaten gerçekleşen haritasında sayıldığı için iki kez düşülmez.
    /// </summary>
    public static decimal KapananMiktar(decimal aktarilanMiktar, decimal gerceklesenMiktar) =>
        Math.Max(Math.Max(aktarilanMiktar, 0) - Math.Max(gerceklesenMiktar, 0), 0);

    /// <summary>
    /// Sevk durumu hesabında kök satırın kalanından düşülecek saha katkısı: gerçekleşen saha sevki + sahada iptalle
    /// kapanan miktar. Yalnız "bütün ürünler sevk kapsamında tamamlandı mı" kararında kullanılır.
    /// </summary>
    public static Dictionary<int, decimal> SevkKapanisHaritasi(
        IReadOnlyDictionary<int, decimal> gerceklesenSahaSevki,
        IReadOnlyDictionary<int, decimal> sahaIptalKapanan)
    {
        var sonuc = gerceklesenSahaSevki.ToDictionary(x => x.Key, x => x.Value);
        foreach (var (satirId, miktar) in sahaIptalKapanan)
            sonuc[satirId] = sonuc.GetValueOrDefault(satirId) + miktar;
        return sonuc;
    }
}
