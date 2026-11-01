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
    public const string Katlanir = "Katlanır sandık; fiyatı elle verilir, tarife düzeltmesi uygulanmaz.";

    /// <summary>
    /// Kalem düzeltilecekse null, korunacaksa nedenini döndürür. Karşılaştırma sürümün düzeltilmeden önceki
    /// değerleriyle yapılır; PO satırları <see cref="FinansSiparisKalemi.FinansSiparis"/> ile yüklenmiş olmalıdır.
    /// </summary>
    public static string? KorunmaNedeni(FinansIsKaydi kalem, FinansFiyatTarifesi surum)
    {
        ArgumentNullException.ThrowIfNull(kalem);
        ArgumentNullException.ThrowIfNull(surum);
        return KorunmaNedeni(kalem, surum, AhsapTarafi(kalem, surum));
    }

    /// <summary>Malzemeli fiyatta sürüm kalemin ahşap tarafına mı ait (ahşap tarifesi sürümü).</summary>
    public static bool AhsapTarafi(FinansIsKaydi kalem, FinansFiyatTarifesi surum) =>
        kalem.AhsapBirimFiyatSnapshot.HasValue && kalem.AhsapTarifeIdSnapshot == surum.Id && kalem.TarifeIdSnapshot != surum.Id;

    private static string? KorunmaNedeni(FinansIsKaydi kalem, FinansFiyatTarifesi surum, bool ahsapTarafi)
    {
        if (kalem.IptalEdildi || !kalem.KaynakAktif) return IptalVeyaPasif;
        if (kalem.SiparisKalemleri.Any(x => !x.FinansSiparis.IptalEdildi)) return PoyaBagli;
        if (kalem.ManuelNetTutar.HasValue) return ManuelNet;
        if (FinansKatlanirKurali.Katlanir(kalem)) return Katlanir;
        if (kalem.HesaplamaYontemi == FinansHesaplamaYontemi.Bilesenli) return Bilesenli;
        var fiyat = ahsapTarafi ? kalem.AhsapBirimFiyatSnapshot!.Value : kalem.BirimFiyatSnapshot;
        if (fiyat != surum.BirimFiyat || (!ahsapTarafi && kalem.KdvOraniSnapshot != surum.KdvOrani) ||
            !string.Equals(kalem.ParaBirimiSnapshot, surum.ParaBirimi, StringComparison.OrdinalIgnoreCase))
            return ElleFiyat;
        return null;
    }

    /// <summary>Kalemin yeni birim fiyatla hesaplanan net bedeli; <see cref="FinansTutarKurallari.IsNet"/> ile aynı yuvarlama.</summary>
    /// <remarks>Malzemeli fiyatta yalnız sürümün ait olduğu taraf (kontra ve/veya ahşap) yeni fiyatı alır.</remarks>
    public static decimal YeniNet(FinansIsKaydi kalem, decimal yeniBirimFiyat, int? surumId = null)
    {
        if (!kalem.AhsapBirimFiyatSnapshot.HasValue)
            return FinansTutarKurallari.Para(yeniBirimFiyat *
                FinansTutarKurallari.FiyatlandirmaMiktari(kalem.FiyatlandirmaBirimiSnapshot, kalem.Adet, kalem.ToplamM3));
        var kontra = surumId is null || kalem.TarifeIdSnapshot == surumId ? yeniBirimFiyat : kalem.BirimFiyatSnapshot;
        var ahsap = surumId is not null && kalem.AhsapTarifeIdSnapshot == surumId ? yeniBirimFiyat : kalem.AhsapBirimFiyatSnapshot.Value;
        return FinansTutarKurallari.MalzemeliNet(kalem, kontra, ahsap);
    }

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
