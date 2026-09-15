using _3K.Application.Common;
using _3K.Core.Entities;
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

        /// <summary>
        /// Canlı iş listesi sınıflandırması. Grid komutlarıyla aynı merkezi devam
        /// sevki kararını kullanır. 3K işlemi başlamamış sevkin Grid'deki eksik
        /// miktarı listede kalır; kullanıcı mevcut sevk toplamını güncelleyebilir.
        /// </summary>
        public static GridIsListesiSiniflandirmaSonucu? Belirle(
            CekiSatiri satir,
            decimal gridEksikMiktar,
            decimal kalanMiktar)
        {
            ArgumentNullException.ThrowIfNull(satir);

            var devamKarari = GridUcKSevkPartisiKurali.DevamSevkiniDegerlendir(satir);
            if (devamKarari.YeniPartiMi)
            {
                return new GridIsListesiSiniflandirmaSonucu(
                    TipYeniden,
                    "Yeniden sevk gerekli",
                    1);
            }

            if (GridUcKSevkPartisiKurali.AktifPartiTeslimEdilebilirMi(satir) &&
                GridUcKSevkPartisiKurali.UcKTarafindaIslemVar(satir))
            {
                return null;
            }

            if (satir.GridDurumuId == (int)GridDurum.EksikGeldi &&
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

        // Eski saf imza birim testleri ve mevcut tüketiciler için korunur.
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
