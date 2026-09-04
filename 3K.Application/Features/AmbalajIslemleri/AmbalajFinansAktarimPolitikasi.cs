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
}
