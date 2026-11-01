using _3K.Core.Exceptions;
using _3K.Core.Helpers;

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

    /// <summary>Servis koruması: aralık geçersizse iş kuralı hatası fırlatır.</summary>
    public static void AraligiDogrula(DateTime baslangic, DateTime bitis)
    {
        if (!GecerliAralik(baslangic, bitis))
            throw IsKuraliException.Cakisma("Geçerli ve en fazla on yıllık tarih aralığı seçin.");
    }

    /// <summary>
    /// Finansal durum raporunun dönemi: filtrenin finans tarihi aralığı; bitiş boşsa bugün, başlangıç boşsa bitişin
    /// yılbaşı. Rapor servisi ve isteğin doğrulayıcısı aynı dönemi kullanır.
    /// </summary>
    public static (DateTime Baslangic, DateTime Bitis) FinansalDurumDonemi(DateTime? baslangic, DateTime? bitis)
    {
        var son = (bitis ?? TurkeyTime.Now).Date;
        return ((baslangic ?? new DateTime(son.Year, 1, 1)).Date, son);
    }

    /// <summary>Tarihin ait olduğu finans dönemi: ayın ilk günü.</summary>
    public static DateTime AyBasi(DateTime tarih) => new(tarih.Year, tarih.Month, 1);
}
