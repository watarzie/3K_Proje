using _3K.Core.Entities;

namespace _3K.Application.Common
{
    /// <summary>
    /// Çeki satırındaki proje toplamları ile sandık bazlı tahsis miktarlarını birbirinden ayırır.
    /// Bu helper yalnızca okuma modeli hesabı yapar; tahsis kayıtlarını değiştirmez.
    /// </summary>
    public static class SandikTahsisHelper
    {
        public static decimal HesaplaSandikMiktari(
            CekiSatiri satir,
            SandikIcerik? icerik,
            int tahsisSayisi)
        {
            if (icerik == null)
                return Math.Max(satir.IstenenAdet, 0);

            if (icerik.TahsisMiktari > 0)
                return icerik.TahsisMiktari;

            // Eski verilerde tahsis kolonu bulunmadığından tek kayıt ana miktarı temsil eder.
            // Çoklu kayıt ise parçalı taşıma sonucudur ve eldeki fiziksel miktar güvenli fallback'tir.
            return tahsisSayisi <= 1
                ? Math.Max(satir.IstenenAdet, 0)
                : Math.Max(icerik.KonulanAdet, 0);
        }

        public static decimal ToplamdanTahsisPayi(
            decimal toplam,
            decimal sandikMiktari,
            decimal toplamTahsisMiktari)
        {
            if (toplam <= 0 || sandikMiktari <= 0 || toplamTahsisMiktari <= 0)
                return 0;

            var dagitilabilirToplam = Math.Min(toplam, toplamTahsisMiktari);
            return Math.Min(sandikMiktari, dagitilabilirToplam * sandikMiktari / toplamTahsisMiktari);
        }
    }
}
