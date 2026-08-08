using _3K.Core.Enums;

namespace _3K.Core.Helpers
{
    public static class NormalProjeSevkDurumHelper
    {
        public static int? Hesapla(
            int toplamSandik,
            int sevkEdilenSandik,
            bool sahaSevkiyleTamamlamaVar,
            bool tumUrunlerSevkKapsamindaTamamlandi,
            bool sahaSandiklariylaTumSandiklarEtkinSevkEdildi = false)
        {
            var normalSandikSevkiVar = sevkEdilenSandik > 0;
            var sevkKaydiVar = normalSandikSevkiVar ||
                sahaSevkiyleTamamlamaVar ||
                sahaSandiklariylaTumSandiklarEtkinSevkEdildi;

            if (!sevkKaydiVar)
                return null;

            // Yalnizca sandik-bazli saha aktarimlari kalan fiziksel sandiklarin tamamini
            // kapatiyorsa etkin sevk tam sayilir. Normal ve urun-bazli saha akislarinin
            // mevcut tamamlama kurallari bu ozel durum disinda aynen korunur.
            if (sahaSandiklariylaTumSandiklarEtkinSevkEdildi)
                return (int)ProjeDurum.SevkEdildi;

            var kismiNormalSandikSevkiVar =
                toplamSandik > 0 &&
                sevkEdilenSandik > 0 &&
                sevkEdilenSandik < toplamSandik;

            if (kismiNormalSandikSevkiVar)
                return (int)ProjeDurum.EksikSevkEdildi;

            return tumUrunlerSevkKapsamindaTamamlandi
                ? (int)ProjeDurum.SevkEdildi
                : (int)ProjeDurum.EksikSevkEdildi;
        }
    }
}
