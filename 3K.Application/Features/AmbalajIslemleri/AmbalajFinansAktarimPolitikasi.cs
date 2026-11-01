using _3K.Core.Entities;

namespace _3K.Application.Features.AmbalajIslemleri;

/// <summary>
/// Ambalaj kaydının Finans modülünde aktif bir üretim işi sayılıp
/// sayılmayacağını tek noktadan belirler.
/// </summary>
internal static class AmbalajFinansAktarimPolitikasi
{
    public static bool AktarimaHazirMi(AmbalajUretimKaydi kayit) =>
        !kayit.IptalMi &&
        kayit.AmbalajaDahil &&
        kayit.UretimeAlindi &&
        kayit.UretimTarihi.HasValue &&
        AmbalajUretimYardimcilari.UretimMiktariGecerli(kayit);

    /// <summary>
    /// Finans iş kaydının talep eden kişisi. Ambalaj formu özel sandıkta (İlave, Saha, Yedek, İç) işi isteyeni
    /// "Talimat veren" alanında zorunlu tutar; eski talep eden alanı formdan kalktığı için boşsa talimat veren yazılır.
    /// </summary>
    public static string? TalepEdenKisi(AmbalajUretimKaydi kayit) =>
        AmbalajUretimYardimcilari.Temizle(kayit.TalepEdenKisi) ?? AmbalajUretimYardimcilari.Temizle(kayit.TalimatVeren);
}
