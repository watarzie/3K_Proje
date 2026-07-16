using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Application.Common
{
    /// <summary>
    /// Saha aktarımının geri alınabilmesi için satır ve sandık içeriğinde
    /// herhangi bir Grid, 3K veya karşılama işlemi başlamamış olmalıdır.
    /// </summary>
    public static class SahaAktarimGeriAlmaPolicy
    {
        public static bool IslemGormusMu(
            CekiSatiri sahaSatiri,
            IEnumerable<SandikIcerik> sahaIcerikleri)
        {
            ArgumentNullException.ThrowIfNull(sahaSatiri);
            ArgumentNullException.ThrowIfNull(sahaIcerikleri);

            return sahaSatiri.GridDurumuId != (int)GridDurum.Gelmedi ||
                sahaSatiri.GridGelenAdet > 0 ||
                sahaSatiri.TrafoSevkAdet > 0 ||
                sahaSatiri.GridSevkDurumuId != (int)GridSevkDurum.SevkEdilmedi ||
                (sahaSatiri.GridSevkMiktari ?? 0) > 0 ||
                sahaSatiri.YenidenSevkGerekliAdet > 0 ||
                sahaSatiri.GelenMiktar > 0 ||
                sahaSatiri.KarsilananMiktar > 0 ||
                sahaSatiri.StokKarsilanan > 0 ||
                sahaSatiri.ProjeKarsilanan > 0 ||
                sahaSatiri.ProjeGonderilen > 0 ||
                sahaSatiri.TedarikciKarsilanan > 0 ||
                sahaSatiri.HataliMiktar > 0 ||
                sahaSatiri.GeriGonderilenMiktar > 0 ||
                sahaSatiri.UcKDurumuId != (int)UcKDurum.Bekliyor ||
                sahaSatiri.UcKKarsilamaTipiId != (int)UcKDurum.Bekliyor ||
                sahaIcerikleri.Any(IslemGormusMu);
        }

        private static bool IslemGormusMu(SandikIcerik sahaIcerigi)
        {
            return sahaIcerigi.KonulanAdet > 0 ||
                sahaIcerigi.EksikAdet > 0 ||
                sahaIcerigi.StokKarsilanan > 0 ||
                sahaIcerigi.ProjeKarsilanan > 0 ||
                sahaIcerigi.TedarikciKarsilanan > 0;
        }
    }
}
