namespace _3K.Application.Features.AmbalajIslemleri
{
    using _3K.Application.Common;
    using _3K.Core.Enums;

    public static class AmbalajMenuKodlari
    {
        public const string Listele = "ambalaj-uretim-listesi";
        public const string KayitDuzenle = Listele;
        public const string RaporGoruntule = Listele;

        // İşlem alias'ları rol ekranındaki tek kök modül yetkisine konsolide edilir.
        public const string M3Goruntule = Listele;
        public const string SarfGoruntule = Listele;
        public const string KaynakGoruntule = Listele;
        public const string M3Duzenle = KayitDuzenle;
        public const string SarfDuzenle = KayitDuzenle;
        public const string KaynakSenkronizeEt = KayitDuzenle;
        public const string UretimeAl = KayitDuzenle;
        public const string UretimdenCikar = KayitDuzenle;
        public const string DahilEt = KayitDuzenle;
        public const string HaricTut = KayitDuzenle;
        public const string IlaveOlustur = KayitDuzenle;
        public const string SahaOlustur = KayitDuzenle;
        public const string YedekOlustur = KayitDuzenle;
        public const string IcOlustur = KayitDuzenle;
        public const string DigerOlustur = KayitDuzenle;
        public const string ManuelProje = KayitDuzenle;
        public const string TurDuzenle = KayitDuzenle;
        public const string CinsDuzenle = KayitDuzenle;
        public const string ProjeDuzenle = KayitDuzenle;
        public const string OlcuDuzenle = KayitDuzenle;
        public const string TalepBilgileriDuzenle = KayitDuzenle;
        public const string DurumDuzenle = KayitDuzenle;
        public const string Iptal = KayitDuzenle;
        public const string GeriYukle = KayitDuzenle;
        public const string KaynakMudahalesi = KayitDuzenle;
        public const string FormGoruntule = RaporGoruntule;
        public const string FormIndir = RaporGoruntule;
        public const string ExcelIndir = RaporGoruntule;
        public const string PdfIndir = RaporGoruntule;

        // Önceki isimleri kullanan istemci/test kodları için kaynak uyumluluğu.
        public const string Duzenle = KayitDuzenle;
        public const string Rapor = RaporGoruntule;

        public static MenuPermissionRequirement Read(string kod) => new(kod, YetkiTipi.R);
        public static MenuPermissionRequirement Write(string kod) => new(kod, YetkiTipi.W);

        public static string? TurOlusturmaKodu(AmbalajSandikTuru tur) => tur switch
        {
            AmbalajSandikTuru.Ilave => IlaveOlustur,
            AmbalajSandikTuru.Saha => SahaOlustur,
            AmbalajSandikTuru.Yedek => YedekOlustur,
            AmbalajSandikTuru.Ic => IcOlustur,
            AmbalajSandikTuru.Diger => DigerOlustur,
            _ => null
        };
    }
}
