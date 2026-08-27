using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Application.Features.GridIslemleri
{
    /// <summary>
    /// Önceki Grid sevkinin 3K teslimi beklenirken aynı satırın yeniden sevk
    /// edilmesini engelleyen ortak kurallar.
    /// </summary>
    public static class GridYenidenSevkKural
    {
        public static bool ProjeTransferTelafisiMi(CekiSatiri satir)
        {
            return ProjeTransferTelafisiMi(
                satir.GridSevkDurumuId,
                satir.GridSevkMiktari ?? 0,
                satir.ProjeGonderilen,
                satir.KalanMiktar,
                satir.UcKDurumuId);
        }

        public static bool ProjeTransferTelafisiMi(
            int gridSevkDurumuId,
            decimal gridSevkMiktari,
            decimal projeGonderilen,
            decimal kalanMiktar,
            int ucKDurumuId)
        {
            return ucKDurumuId != (int)UcKDurum.Bekliyor &&
                   gridSevkDurumuId == (int)GridSevkDurum.SevkEdildi &&
                   gridSevkMiktari > 0 &&
                   projeGonderilen > 0 &&
                   kalanMiktar > 0;
        }

        public static bool EskiParcaliPaketUzlastirmaGerektiriyorMu(CekiSatiri satir)
        {
            return satir.GridSevkDurumuId == (int)GridSevkDurum.YenidenSevkGerekli &&
                   satir.YenidenSevkGerekliAdet > 0 &&
                   (satir.GridSevkMiktari ?? 0) > 0 &&
                   satir.UcKDurumuId == (int)UcKDurum.Bekliyor &&
                   satir.UcKKarsilamaTipiId == (int)UcKDurum.Bekliyor;
        }

        public static bool YenidenSevkTamMiktarMi(
            decimal yenidenSevkGerekliAdet,
            decimal? sevkMiktari)
        {
            return yenidenSevkGerekliAdet > 0 &&
                   sevkMiktari.HasValue &&
                   sevkMiktari.Value == yenidenSevkGerekliAdet;
        }
    }
}
