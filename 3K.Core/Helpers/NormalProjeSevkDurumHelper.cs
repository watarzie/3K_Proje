using _3K.Core.Enums;

namespace _3K.Core.Helpers
{
    public static class NormalProjeSevkDurumHelper
    {
        public static int? Hesapla(
            int toplamSandik,
            int sevkEdilenSandik,
            bool sahaSevkiyleTamamlamaVar,
            bool tumUrunlerSevkKapsamindaTamamlandi)
        {
            var normalSandikSevkiVar = sevkEdilenSandik > 0;
            var sevkKaydiVar = normalSandikSevkiVar || sahaSevkiyleTamamlamaVar;

            if (!sevkKaydiVar)
                return null;

            var kismiNormalSandikSevkiVar =
                toplamSandik > 0 &&
                sevkEdilenSandik > 0 &&
                sevkEdilenSandik < toplamSandik;

            // Ürünler tamamlanmış görünse bile sevk edilmemiş normal sandık varsa
            // fiziksel sandık sevki tamamlanmış sayılamaz.
            if (kismiNormalSandikSevkiVar)
                return (int)ProjeDurum.EksikSevkEdildi;

            return tumUrunlerSevkKapsamindaTamamlandi
                ? (int)ProjeDurum.SevkEdildi
                : (int)ProjeDurum.EksikSevkEdildi;
        }
    }
}
