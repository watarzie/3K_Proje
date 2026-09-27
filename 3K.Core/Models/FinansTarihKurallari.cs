namespace _3K.Core.Models;

/// <summary>
/// Finans kayıt tarihi, dönem ve tarih aralığı kuralları için tek kaynak. Doğrulayıcılar,
/// servis korumaları ve veritabanı kontrolü (tarife yılı) aynı sınırları kullanır.
/// </summary>
public static class FinansTarihKurallari
{
    public const int EnKucukYil = 2000;
    public const int EnBuyukYil = 2200;

    /// <summary>Panel ve raporlarda seçilebilecek en uzun tarih aralığı (yaklaşık on yıl).</summary>
    public const int EnUzunAralikGun = 3660;

    public static bool GecerliYil(int yil) => yil is >= EnKucukYil and <= EnBuyukYil;

    public static bool GecerliTarih(DateTime tarih) => GecerliYil(tarih.Year);

    public static bool GecerliDonem(int yil, int ay) => GecerliYil(yil) && ay is >= 1 and <= 12;

    public static bool GecerliAralik(DateTime baslangic, DateTime bitis) =>
        GecerliTarih(baslangic) &&
        GecerliTarih(bitis) &&
        bitis.Date >= baslangic.Date &&
        (bitis.Date - baslangic.Date).TotalDays <= EnUzunAralikGun;
}
