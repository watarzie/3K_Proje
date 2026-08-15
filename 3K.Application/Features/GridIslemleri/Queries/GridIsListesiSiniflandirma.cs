using _3K.Core.Enums;

namespace _3K.Application.Features.GridIslemleri.Queries
{
    /// <summary>
    /// Grid iş listesinde gösterilecek aksiyonların, mevcut Grid yeniden sevk
    /// akışlarıyla aynı kuralları kullanmasını sağlar.
    /// </summary>
    public static class GridIsListesiSiniflandirma
    {
        public const string TipYeniden = "yeniden";
        public const string TipEksik = "eksik";

        public static GridIsListesiSiniflandirmaSonucu? Belirle(
            int gridDurumuId,
            int gridSevkDurumuId,
            decimal gridSevkMiktari,
            decimal yenidenSevkGerekliAdet,
            decimal projeGonderilen,
            decimal gridEksikMiktar,
            decimal kalanMiktar)
        {
            var explicitYenidenSevk =
                gridSevkDurumuId == (int)GridSevkDurum.YenidenSevkGerekli &&
                yenidenSevkGerekliAdet > 0;
            var projeTransferTelafisi =
                gridSevkDurumuId == (int)GridSevkDurum.SevkEdildi &&
                gridSevkMiktari > 0 &&
                projeGonderilen > 0 &&
                kalanMiktar > 0;

            if (explicitYenidenSevk || projeTransferTelafisi)
            {
                return new GridIsListesiSiniflandirmaSonucu(
                    TipYeniden,
                    "Yeniden sevk gerekli",
                    1);
            }

            if (gridDurumuId == (int)GridDurum.EksikGeldi &&
                gridEksikMiktar > 0 &&
                kalanMiktar > 0)
            {
                return new GridIsListesiSiniflandirmaSonucu(
                    TipEksik,
                    "Eksik geldi",
                    2);
            }

            return null;
        }
    }

    public sealed record GridIsListesiSiniflandirmaSonucu(
        string IsTipi,
        string IsTipiMetni,
        int Oncelik);
}
