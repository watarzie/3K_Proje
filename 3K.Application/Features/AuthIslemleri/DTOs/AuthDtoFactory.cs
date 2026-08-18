using _3K.Core.Entities;
using _3K.Core.Models;

namespace _3K.Application.Features.AuthIslemleri.DTOs
{
    internal static class AuthDtoFactory
    {
        public static LoginResultDto Authenticated(
            Kullanici kullanici,
            string token,
            IReadOnlyList<string>? kurtarmaKodlari = null,
            IkiFaktorAyarDurumu? ikiFaktorDurumu = null)
        {
            return new LoginResultDto
            {
                NextStep = LoginNextSteps.Authenticated,
                Token = token,
                Kullanici = Kullanici(kullanici, ikiFaktorDurumu),
                KurtarmaKodlari = kurtarmaKodlari
            };
        }

        public static KullaniciDto Kullanici(
            Kullanici kullanici,
            IkiFaktorAyarDurumu? ikiFaktorDurumu = null,
            string varsayilanRolAdi = "Unknown")
        {
            return new KullaniciDto
            {
                Id = kullanici.Id,
                AdSoyad = kullanici.AdSoyad,
                BasHarf = kullanici.BasHarf,
                RolId = kullanici.RolId,
                Rol = kullanici.Rol?.Ad ?? varsayilanRolAdi,
                Email = kullanici.Email,
                IkiFaktorZorunluMu = kullanici.IkiFaktorZorunluMu,
                IkiFaktorEtkinMi = ikiFaktorDurumu?.EtkinMi ?? false,
                IkiFaktorDogrulandiTarihiUtc = ikiFaktorDurumu?.DogrulandiTarihiUtc
            };
        }
    }
}
