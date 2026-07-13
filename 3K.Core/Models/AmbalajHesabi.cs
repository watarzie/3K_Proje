namespace _3K.Core.Models
{
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

    public static class AmbalajHesaplayici
    {
        private const decimal TahtaEn = 93m;
        private const decimal TahtaYukseklik = 23m;
        private const decimal TakozKesit = 93m;
        private const decimal AyakMesafesi = 300m;

        public static AmbalajHesapSonucu Hesapla(decimal boy, decimal en, decimal yukseklik)
        {
            if (boy <= 0 || en <= 0 || yukseklik <= 0)
                throw new ArgumentOutOfRangeException(nameof(boy), "İç boy, en ve yükseklik sıfırdan büyük olmalıdır.");

            var ustKizakAdedi = 4;
            var ayakAdedi = AyakAdediHesapla(boy);
            var yanKusakAdedi = 2;

            var disBoy = boy + 4 * TahtaYukseklik;
            var disEn = en + 4 * TahtaYukseklik;
            var disYukseklik = yukseklik + 2 * TakozKesit + 3 * TahtaYukseklik;

            var onDuvarYuksekligi = yukseklik + TakozKesit + TahtaYukseklik;
            var onDuvarKusakBoyu = yukseklik + 2 * TakozKesit + 2 * TahtaYukseklik - 15;
            var onDuvarBolmeSayisi = ayakAdedi - 1;
            var onDuvarCaprazYatay = (disBoy - 2 * (AyakMesafesi + 2 * TahtaYukseklik) - (onDuvarBolmeSayisi + 1) * TahtaEn) / onDuvarBolmeSayisi;
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

        private static int AyakAdediHesapla(decimal boy) => boy switch
        {
            <= 2500m => 2,
            <= 4000m => 3,
            _ => 4
        };

        private static decimal Hipotenus(decimal yatay, decimal dikey) =>
            (decimal)Math.Sqrt((double)(yatay * yatay + dikey * dikey));
    }
}