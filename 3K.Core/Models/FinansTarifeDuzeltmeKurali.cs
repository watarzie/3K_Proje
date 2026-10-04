using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;

namespace _3K.Core.Models;

/// <summary>
/// Hatalı tarife düzeltme: yanlış girilmiş bir tarife sürümünün birim fiyatı/KDV'si gerekçeyle düzeltilir ve
/// yalnız o sürümün tarife değeriyle fiyatlanmış açık kalemler yeni değere çekilir. Sürümün tarih aralığı,
/// diğer sürümler ve diğer tarihlerdeki kalemler değişmez; böylece "her kalem kendi tarihindeki tarifeyle
/// fiyatlanır, eski fiyat korunur" kuralı bozulmaz. Fiyatı PO'da sabitlenmiş ya da elle verilmiş kalem korunur.
/// </summary>
public static class FinansTarifeDuzeltmeKurali
{
    public const string PoyaBagli = "İptal edilmemiş bir PO'ya bağlı; fiyatı PO'da sabitlendi.";
    public const string IptalVeyaPasif = "İptal edilmiş ya da kaynağı pasif.";
    public const string ManuelNet = "Net tutarı elle girilmiş.";
    public const string Bilesenli = "Bileşenli hesapla fiyatlanmış.";
    public const string ElleFiyat = "Birim fiyatı, KDV'si ya da para birimi elle değiştirilmiş.";

    /// <summary>
    /// Kalem düzeltilecekse null, korunacaksa nedenini döndürür. Karşılaştırma sürümün düzeltilmeden önceki
    /// değerleriyle yapılır; PO satırları <see cref="FinansSiparisKalemi.FinansSiparis"/> ile yüklenmiş olmalıdır.
    /// </summary>
    public static string? KorunmaNedeni(FinansIsKaydi kalem, FinansFiyatTarifesi surum)
    {
        ArgumentNullException.ThrowIfNull(kalem);
        ArgumentNullException.ThrowIfNull(surum);
        if (kalem.IptalEdildi || !kalem.KaynakAktif) return IptalVeyaPasif;
        if (kalem.SiparisKalemleri.Any(x => !x.FinansSiparis.IptalEdildi)) return PoyaBagli;
        if (kalem.ManuelNetTutar.HasValue) return ManuelNet;
        if (kalem.HesaplamaYontemi == FinansHesaplamaYontemi.Bilesenli) return Bilesenli;
        if (kalem.BirimFiyatSnapshot != surum.BirimFiyat || kalem.KdvOraniSnapshot != surum.KdvOrani ||
            !string.Equals(kalem.ParaBirimiSnapshot, surum.ParaBirimi, StringComparison.OrdinalIgnoreCase))
            return ElleFiyat;
        return null;
    }

    /// <summary>Kalemin yeni birim fiyatla hesaplanan net bedeli; <see cref="FinansTutarKurallari.IsNet"/> ile aynı yuvarlama.</summary>
    public static decimal YeniNet(FinansIsKaydi kalem, decimal yeniBirimFiyat) => FinansTutarKurallari.Para(yeniBirimFiyat *
        FinansTutarKurallari.FiyatlandirmaMiktari(kalem.FiyatlandirmaBirimiSnapshot, kalem.Adet, kalem.ToplamM3));

    /// <summary>Düzeltme değerlerini doğrular; sürümle aynı değer bir düzeltme değildir.</summary>
    public static void Dogrula(FinansFiyatTarifesi surum, decimal birimFiyat, decimal kdvOrani)
    {
        ArgumentNullException.ThrowIfNull(surum);
        if (birimFiyat < 0)
            throw IsKuraliException.Cakisma("Birim fiyat sıfırdan küçük olamaz.");
        if (kdvOrani is < 0 or > 100)
            throw IsKuraliException.Cakisma("KDV oranı 0-100 aralığında olmalıdır.");
        if (surum.BirimFiyat == birimFiyat && surum.KdvOrani == kdvOrani)
            throw IsKuraliException.Cakisma("Yeni birim fiyat ve KDV, sürümün mevcut değerleriyle aynı.");
    }
}
