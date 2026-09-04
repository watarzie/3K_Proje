using System.Globalization;
using System.Text;

namespace _3K.Core.Models
{
    public enum AmbalajAyakProfili
    {
        Standart = 0,
        GenlesmeKabi = 1
    }

    /// <summary>
    /// Ürün adındaki yazım/case/boşluk farklarını tek noktada normalize ederek
    /// yalnız özel ayak kuralının hangi kayıtlara uygulanacağını belirler.
    /// </summary>
    public static class AmbalajAyakProfiliBelirleyici
    {
        private const string GenlesmeKabiAnahtari = "genlesmekabi";

        public static AmbalajAyakProfili Belirle(params string?[] metinler) =>
            metinler.Any(metin => NormalizeEt(metin).Contains(GenlesmeKabiAnahtari, StringComparison.Ordinal))
                ? AmbalajAyakProfili.GenlesmeKabi
                : AmbalajAyakProfili.Standart;

        private static string NormalizeEt(string? metin)
        {
            if (string.IsNullOrWhiteSpace(metin))
                return string.Empty;

            var sonuc = new StringBuilder(metin.Length);
            foreach (var karakter in metin.Trim().Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(karakter) == UnicodeCategory.NonSpacingMark)
                    continue;
                if (!char.IsLetterOrDigit(karakter))
                    continue;

                sonuc.Append(karakter == 'ı' ? 'i' : char.ToLowerInvariant(karakter));
            }

            return sonuc.ToString();
        }
    }

    public sealed record AmbalajOlculeri(decimal Boy, decimal En, decimal Yukseklik);

    public sealed record AmbalajParcasi(
        string Kod,
        string Grup,
        string Aciklama,
        decimal KesitEn,
        decimal KesitYukseklik,
        decimal Uzunluk,
        decimal Adet,
        string Malzeme = "ÇAM")
    {
        public decimal HacimM3 => Uzunluk * KesitEn * KesitYukseklik * Adet / 1_000_000_000m;
        public int UretimAdedi => (int)Math.Ceiling(Adet);
    }

    public sealed record AmbalajHesapSonucu(
        AmbalajOlculeri IcOlculer,
        AmbalajOlculeri DisOlculer,
        int UstKizakAdedi,
        int AyakAdedi,
        int YanKusakAdedi,
        decimal OnDuvarYuksekligi,
        IReadOnlyList<AmbalajParcasi> Parcalar)
    {
        public decimal AltPaletHacmiM3 => GrupHacmi("AP");
        public decimal OnDuvarHacmiM3 => GrupHacmi("OD");
        public decimal UstTavanHacmiM3 => GrupHacmi("UT");
        public decimal YanDuvarHacmiM3 => GrupHacmi("YD");
        public decimal ToplamHacimM3 => Parcalar.Sum(p => p.HacimM3);

        private decimal GrupHacmi(string grup) => Parcalar.Where(p => p.Grup == grup).Sum(p => p.HacimM3);
    }

    /// <summary>
    /// Üretim formunda kullanılan kereste sarfiyat hesabı. Girdiler milimetre,
    /// sonuçlar m³'tür. Formül sürümü AmbalajUretimKaydi üzerinde snapshot olarak saklanır.
    /// </summary>
    public static class AmbalajHesaplayici
    {
        public const string FormulVersiyonu = "KER-2026-03";
        public const decimal VarsayilanSarfOrani = 0.11m;

        private const decimal TahtaEn = 93m;
        private const decimal TahtaYukseklik = 23m;
        private const decimal TakozKesit = 93m;
        private const decimal AyakMesafesi = 300m;

        public static AmbalajHesapSonucu Hesapla(
            decimal boy,
            decimal en,
            decimal yukseklik,
            AmbalajAyakProfili ayakProfili = AmbalajAyakProfili.Standart,
            decimal? ayakHesapBoyu = null)
        {
            if (boy <= 0 || en <= 0 || yukseklik <= 0)
                throw new ArgumentOutOfRangeException(nameof(boy), "İç boy, en ve yükseklik sıfırdan büyük olmalıdır.");

            var ustKizakAdedi = 4;
            var ayakAdedi = AyakAdediHesapla(boy, ayakProfili, ayakHesapBoyu);
            var yanKusakAdedi = 2;

            var disBoy = boy + 4 * TahtaYukseklik;
            var disEn = en + 4 * TahtaYukseklik;
            var disYukseklik = yukseklik + 2 * TakozKesit + 3 * TahtaYukseklik;

            var onDuvarYuksekligi = yukseklik + TakozKesit + TahtaYukseklik;
            var onDuvarKusakBoyu = yukseklik + 2 * TakozKesit + 2 * TahtaYukseklik - 15;
            var onDuvarBolmeSayisi = ayakAdedi - 1;
            var onDuvarCaprazYatay =
                (disBoy - 2 * (AyakMesafesi + 2 * TahtaYukseklik) - (onDuvarBolmeSayisi + 1) * TahtaEn) /
                onDuvarBolmeSayisi;
            var onDuvarCaprazBoyu = Hipotenus(onDuvarCaprazYatay, onDuvarYuksekligi);

            var yanDuvarKusakBoyu = yukseklik + TakozKesit + 2 * TahtaYukseklik;
            var yanDuvarUstTahtaBoyu = en - yanKusakAdedi * TahtaEn;
            var yanDuvarCaprazBoyu = Hipotenus(yanDuvarUstTahtaBoyu, yanDuvarKusakBoyu - TahtaEn);

            var ustTavanYatayBoyu = boy + 2 * TahtaYukseklik;
            var ustTavanYatayAdedi = (en + 2 * TahtaYukseklik) / TahtaEn;
            var ustTavanCaprazBoyu = Hipotenus(onDuvarCaprazYatay, en + 2 * TahtaYukseklik);

            var parcalar = new List<AmbalajParcasi>
            {
                new("AP_3", "AP", "ALT PALET ÜST TAHTALARI", TahtaEn, TahtaYukseklik, en, boy / TahtaEn),
                new("AP_2", "AP", "ALT PALET ÜST TAKOZLARI", TakozKesit, TakozKesit, boy, ustKizakAdedi),
                new("AP_1", "AP", "ALT PALET ALT TAKOZLARI", TakozKesit, TakozKesit, en + 2 * TahtaYukseklik, ayakAdedi),
                new("OD_4", "OD", "ÖN DUVAR YATAY TAHTALARI", TahtaEn, TahtaYukseklik, disBoy, 2 * onDuvarYuksekligi / TahtaEn),
                new("OD_5", "OD", "ÖN DUVAR KUŞAK TAHTALARI", TahtaEn, TahtaYukseklik, onDuvarKusakBoyu, ayakAdedi * 2),
                new("OD_10", "OD", "ÖN DUVAR ÇAPRAZ TAHTALARI", TahtaEn, TahtaYukseklik, onDuvarCaprazBoyu, onDuvarBolmeSayisi * 2),
                new("UT_7", "UT", "ÜST TAVAN YATAY TAHTALARI", TahtaEn, TahtaYukseklik, ustTavanYatayBoyu, ustTavanYatayAdedi),
                new("UT_6", "UT", "ÜST TAVAN KUŞAK TAHTALARI", TahtaEn, TahtaYukseklik, disEn, ayakAdedi),
                new("UT_11", "UT", "ÜST TAVAN ÇAPRAZ TAHTALARI", TahtaEn, TahtaYukseklik, ustTavanCaprazBoyu, onDuvarBolmeSayisi),
                new("YD_9", "YD", "YAN DUVAR YATAY TAHTALARI", TahtaEn, TahtaYukseklik, en, 2 * onDuvarYuksekligi / TahtaEn),
                new("YD_8", "YD", "YAN DUVAR KUŞAK TAHTALARI", TahtaEn, TahtaYukseklik, yanDuvarKusakBoyu, yanKusakAdedi * 2),
                new("YD_12", "YD", "YAN DUVAR ÜST TAHTALARI", TahtaEn, TahtaYukseklik, yanDuvarUstTahtaBoyu, 2),
                new("YD_13", "YD", "YAN DUVAR ÇAPRAZ TAHTALARI", TahtaEn, TahtaYukseklik, yanDuvarCaprazBoyu, (yanKusakAdedi - 1) * 2)
            };

            return new AmbalajHesapSonucu(
                new AmbalajOlculeri(boy, en, yukseklik),
                new AmbalajOlculeri(disBoy, disEn, disYukseklik),
                ustKizakAdedi,
                ayakAdedi,
                yanKusakAdedi,
                onDuvarYuksekligi,
                parcalar);
        }

        public static AmbalajM3Ozeti M3OzetiHesapla(
            decimal boy,
            decimal en,
            decimal yukseklik,
            int adet,
            decimal sarfOrani = VarsayilanSarfOrani,
            decimal? m3Override = null,
            AmbalajAyakProfili ayakProfili = AmbalajAyakProfili.Standart,
            decimal? ayakHesapBoyu = null)
        {
            if (adet <= 0)
                throw new ArgumentOutOfRangeException(nameof(adet), "Adet sıfırdan büyük olmalıdır.");
            if (sarfOrani < 0 || sarfOrani > 1)
                throw new ArgumentOutOfRangeException(nameof(sarfOrani), "Sarf oranı 0 ile 1 arasında olmalıdır.");
            if (m3Override < 0)
                throw new ArgumentOutOfRangeException(nameof(m3Override), "M³ override negatif olamaz.");

            var birim = Hesapla(boy, en, yukseklik, ayakProfili, ayakHesapBoyu).ToplamHacimM3;
            var hesaplanan = birim * adet;
            var net = m3Override ?? hesaplanan;
            var sarf = net * sarfOrani;
            var yuvarlanmisNet = Yuvarla(net);
            var yuvarlanmisSarf = Yuvarla(sarf);

            return new AmbalajM3Ozeti(
                Yuvarla(birim),
                Yuvarla(hesaplanan),
                yuvarlanmisNet,
                yuvarlanmisSarf,
                yuvarlanmisNet + yuvarlanmisSarf);
        }

        private static int AyakAdediHesapla(
            decimal icBoy,
            AmbalajAyakProfili ayakProfili,
            decimal? disBoy = null)
        {
            // 599 cm ve üzerindeki yeni uzun sandık kuralı tüm sandık tipleri
            // için dış boya göre uygulanır. Dış boy verilmeyen manuel kayıtlarda
            // girilen boy aynı zamanda kural boyudur.
            var uzunSandikKuralBoyu = disBoy ?? icBoy;
            if (uzunSandikKuralBoyu >= 7000m)
                return 7;
            if (uzunSandikKuralBoyu >= 5990m)
                return 6;

            if (ayakProfili == AmbalajAyakProfili.GenlesmeKabi)
                return uzunSandikKuralBoyu <= 4000m ? 4 : 5;

            return icBoy switch
            {
                < 2500m => 2,
                < 4000m => 3,
                < 5000m => 4,
                _ => 5
            };
        }

        private static decimal Hipotenus(decimal yatay, decimal dikey) =>
            (decimal)Math.Sqrt((double)(yatay * yatay + dikey * dikey));

        private static decimal Yuvarla(decimal deger) => Math.Round(deger, 6, MidpointRounding.AwayFromZero);
    }

    public sealed record AmbalajM3Ozeti(
        decimal HesaplananBirimM3,
        decimal HesaplananToplamM3,
        decimal NetM3,
        decimal SarfM3,
        decimal ToplamM3);
}
