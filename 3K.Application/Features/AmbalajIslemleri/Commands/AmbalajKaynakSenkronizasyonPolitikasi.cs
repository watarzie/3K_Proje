using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Application.Features.AmbalajIslemleri.Commands;

internal static class AmbalajKaynakSenkronizasyonPolitikasi
{
    public const string SistemKaynakEksigiNedeni = "system-source-missing";

    public static bool KaynakEksigindeSistemIptalineUygunMu(AmbalajUretimKaydi kayit) =>
        kayit.KaynakKayitId.HasValue &&
        !kayit.IptalMi &&
        !kayit.KaynakSenkronizasyonuKilitliMi &&
        kayit.UretimDurumu == AmbalajUretimDurumu.Planlandi;

    public static bool SistemIptalindenOtomatikAktiflesebilirMi(AmbalajUretimKaydi kayit) =>
        kayit.KaynakKayitId.HasValue &&
        kayit.IptalMi &&
        kayit.IptalEdenKullaniciId == null &&
        string.Equals(
            kayit.IptalNedeni,
            SistemKaynakEksigiNedeni,
            StringComparison.Ordinal);
}
