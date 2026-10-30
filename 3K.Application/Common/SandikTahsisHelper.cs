using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Application.Common
{
    /// <summary>
    /// Çeki satırındaki proje toplamları ile sandık bazlı tahsis miktarlarını birbirinden ayırır.
    /// Bu helper yalnızca okuma modeli hesabı yapar; tahsis kayıtlarını değiştirmez.
    /// </summary>
    public static class SandikTahsisHelper
    {
        private const int MiktarOndalikBasamak = 4;
        private const decimal MiktarOlcek = 10_000m;

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

        /// <summary>
        /// Sandık detayındaki satır miktarları: sandık miktarı, konulan ve eksik. Sandık listesindeki eksik uyarısı
        /// da bu hesabı toplar; ikisi ayrışmaz. Satırın kalanını sıfırlayan durumlar eksik üretmez
        /// (<see cref="_3K.Core.Helpers.CekiSatiriKalanHelper.HesaplaHamKalan"/> ile aynı): Grid Kapandı satır tamam
        /// sayılır, Grid İptal satırda eksik yoktur, trafoyla giden adet sandığa konmaz ama teslim edilmiş sayılır.
        /// <paramref name="satirTahsisleri"/> çeki satırının bütün sandıklardaki tahsis kayıtlarıdır (bu kayıt dahil);
        /// trafo adedi 3K ürün listesindeki gibi tahsis miktarı oranında sandıklara paylaştırılır.
        /// </summary>
        public static SandikIcerikMiktari HesaplaIcerikMiktari(
            SandikIcerik icerik,
            IReadOnlyCollection<SandikIcerik> satirTahsisleri)
        {
            var satir = icerik.CekiSatiri;
            if (satir == null)
            {
                var miktar = icerik.TahsisMiktari > 0 ? icerik.TahsisMiktari : icerik.Miktar;
                return new SandikIcerikMiktari(miktar, icerik.KonulanAdet, Math.Max(miktar - icerik.KonulanAdet, 0));
            }

            var tahsisSayisi = Math.Max(satirTahsisleri.Count, 1);
            var sandikMiktari = HesaplaSandikMiktari(satir, icerik, tahsisSayisi);
            if (satir.GridDurumuId == (int)GridDurum.GridKapandi)
                return new SandikIcerikMiktari(sandikMiktari, sandikMiktari, 0);
            if (satir.GridDurumuId == (int)GridDurum.Iptal)
                return new SandikIcerikMiktari(sandikMiktari, icerik.KonulanAdet, 0);

            var toplamTahsisMiktari = satirTahsisleri.Count == 0
                ? sandikMiktari
                : satirTahsisleri.Sum(t => HesaplaSandikMiktari(satir, t, tahsisSayisi));
            var trafoPayi = ToplamdanTahsisPayi(satir.TrafoSevkAdet, sandikMiktari, toplamTahsisMiktari);
            var eksik = Math.Max(sandikMiktari - Math.Max(icerik.KonulanAdet, 0) - trafoPayi, 0);
            return new SandikIcerikMiktari(sandikMiktari, icerik.KonulanAdet, eksik);
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

        /// <summary>
        /// Grid listesinde tek satır, güncel çeki operasyon toplamlarını gösterir.
        /// Eski veya kısmi tahsis, gerçekleşmiş gelen/sevk miktarlarını kırpmamalıdır.
        /// Birden çok sandığa dağıtılmış satırlarda mevcut tahsis paylaştırması korunur.
        /// Fiziksel tahsis ve konulan miktarı değiştirmez.
        /// </summary>
        public static decimal ToplamdanSatirPayi(
            decimal toplam,
            decimal sandikMiktari,
            decimal toplamTahsisMiktari,
            int tahsisSayisi)
        {
            return tahsisSayisi == 1
                ? Math.Max(toplam, 0)
                : ToplamdanTahsisPayi(toplam, sandikMiktari, toplamTahsisMiktari);
        }

        /// <summary>
        /// Merkezi iş kurallarıyla hesaplanan toplam kalanı, fiziksel açığı bulunan
        /// sandık tahsislerine dağıtır. Yalnızca okuma modeli hesabıdır; entity veya
        /// tahsis kayıtlarını değiştirmez.
        /// </summary>
        public static IReadOnlyList<decimal> HesaplaKalanPaylari(
            decimal etkinToplamKalan,
            IReadOnlyList<decimal> tahsisMiktarlari,
            IReadOnlyList<decimal> tamamlananMiktarlari)
        {
            ArgumentNullException.ThrowIfNull(tahsisMiktarlari);
            ArgumentNullException.ThrowIfNull(tamamlananMiktarlari);

            if (tahsisMiktarlari.Count != tamamlananMiktarlari.Count)
                throw new ArgumentException("Tahsis ve tamamlanan miktar listeleri aynı uzunlukta olmalıdır.");

            if (tahsisMiktarlari.Count == 0)
                return Array.Empty<decimal>();

            var etkinKalan = Math.Round(
                Math.Max(etkinToplamKalan, 0),
                MiktarOndalikBasamak,
                MidpointRounding.AwayFromZero);

            // Tek tahsis, 13 Temmuz öncesindeki merkezi kalan davranışını aynen korur.
            if (tahsisMiktarlari.Count == 1)
                return new[] { etkinKalan };

            var hamKalanlar = new decimal[tahsisMiktarlari.Count];
            for (var index = 0; index < tahsisMiktarlari.Count; index++)
            {
                hamKalanlar[index] = Math.Max(
                    Math.Max(tahsisMiktarlari[index], 0) - Math.Max(tamamlananMiktarlari[index], 0),
                    0);
            }

            if (etkinKalan == 0)
                return new decimal[tahsisMiktarlari.Count];

            var toplamHamKalan = hamKalanlar.Sum();
            if (toplamHamKalan == 0)
            {
                // Hatalı/uyumsuz ürün kuralı gibi merkezi kalan üreten fakat fiziksel
                // açığı bulunmayan istisnalarda uyarıyı ilk tahsis satırında göster.
                var sonuc = new decimal[tahsisMiktarlari.Count];
                var hedefIndex = IlkPozitifTahsisIndexiniBul(tahsisMiktarlari);
                sonuc[hedefIndex] = etkinKalan;
                return sonuc;
            }

            return HamKalanOranindaDagit(etkinKalan, hamKalanlar, toplamHamKalan);
        }

        private static decimal[] HamKalanOranindaDagit(
            decimal dagitilacakToplam,
            IReadOnlyList<decimal> hamKalanlar,
            decimal toplamHamKalan)
        {
            var hedefBirim = decimal.ToInt64(dagitilacakToplam * MiktarOlcek);
            var payBirimleri = new long[hamKalanlar.Count];
            var kesirler = new decimal[hamKalanlar.Count];

            for (var index = 0; index < hamKalanlar.Count; index++)
            {
                var kesinBirim = hedefBirim * (hamKalanlar[index] / toplamHamKalan);
                var tamBirim = decimal.ToInt64(decimal.Floor(kesinBirim));
                payBirimleri[index] = tamBirim;
                kesirler[index] = kesinBirim - tamBirim;
            }

            var kalanBirim = hedefBirim - payBirimleri.Sum();
            if (kalanBirim > 0)
            {
                var oncelikliIndexler = Enumerable.Range(0, hamKalanlar.Count)
                    .OrderByDescending(index => kesirler[index])
                    .ThenBy(index => index)
                    .Take(checked((int)kalanBirim));

                foreach (var index in oncelikliIndexler)
                    payBirimleri[index]++;
            }
            else if (kalanBirim < 0)
            {
                var duzeltilecekIndexler = Enumerable.Range(0, hamKalanlar.Count)
                    .Where(index => payBirimleri[index] > 0)
                    .OrderBy(index => kesirler[index])
                    .ThenBy(index => index)
                    .Take(checked((int)-kalanBirim));

                foreach (var index in duzeltilecekIndexler)
                    payBirimleri[index]--;
            }

            return payBirimleri
                .Select(birim => birim / MiktarOlcek)
                .ToArray();
        }

        private static int IlkPozitifTahsisIndexiniBul(IReadOnlyList<decimal> tahsisMiktarlari)
        {
            for (var index = 0; index < tahsisMiktarlari.Count; index++)
            {
                if (tahsisMiktarlari[index] > 0)
                    return index;
            }

            return 0;
        }
    }

    /// <summary>Sandıktaki bir satırın sandık miktarı, konulan ve eksik miktarı (<see cref="SandikTahsisHelper.HesaplaIcerikMiktari"/>).</summary>
    public readonly record struct SandikIcerikMiktari(decimal SandikMiktari, decimal Konulan, decimal Eksik);
}
