namespace _3K.Application.Common
{
    /// <summary>
    /// Lookup tablolarındaki durum değerlerinin type-safe sabitleri.
    /// DB'deki string FK'lerle birebir eşleşir.
    /// Handler'larda magic string yerine bu sabitler kullanılır.
    /// </summary>
    public static class StatusConstants
    {
        public static class ProjeDurum
        {
            public const string Hazirlaniyor = "Hazirlaniyor";
            public const string Devam = "Devam";
            public const string Tamamlandi = "Tamamlandi";
            public const string Beklemede = "Beklemede";
            public const string SevkEdildi = "SevkEdildi";
            public const string EksikSevkEdildi = "EksikSevkEdildi";
        }

        public static class SandikDurum
        {
            public const string Bos = "Bos";
            public const string Hazirlaniyor = "Hazirlaniyor";
            public const string Hazir = "Hazir";
            public const string Sevkedildi = "Sevkedildi";
        }

        public static class UrunDurum
        {
            public const string Bekliyor = "Bekliyor";
            public const string KismiGeldi = "KismiGeldi";
            public const string Tamamlandi = "Tamamlandi";
            public const string Eksik = "Eksik";
            public const string StoktanKarsilandi = "StoktanKarsilandi";
            public const string FBdenKarsilandi = "FBdenKarsilandi";
            public const string SonraGidecek = "SonraGidecek";
            public const string SandikDegisti = "SandikDegisti";
            public const string IptalVeyaPasif = "IptalVeyaPasif";
            public const string TeslimAlindi = "TeslimAlindi";
            public const string GeriGonderildi = "GeriGonderildi";
            public const string KismiTamamlandi = "KismiTamamlandi";
            public const string Kayip = "Kayip";
            public const string GriddeHazir = "GriddeHazir";
            public const string GriddeEksik = "GriddeEksik";
            public const string Sipariste = "Sipariste";
            public const string Gelmedi = "Gelmedi";
            public const string TrafoSevk = "TrafoSevk";
            public const string BaskaProyeVerildi = "BaskaProyeVerildi";
            public const string HataliUrun = "HataliUrun";
        }

        public static class GridDurum
        {
            public const string Bekliyor = "Bekliyor";
            public const string Uretimde = "Uretimde";
            public const string StokHazir = "StokHazir";
            public const string SevkEdildi = "SevkEdildi";
            public const string KismiSevkEdildi = "KismiSevkEdildi";
            public const string Bekletiliyor = "Bekletiliyor";
            public const string IptalEdildi = "IptalEdildi";
            public const string TamGeldi = "TamGeldi";
            public const string EksikGeldi = "EksikGeldi";
            public const string Gelmedi = "Gelmedi";
            public const string TrafoSevk = "TrafoSevk";
            public const string Iptal = "Iptal";
            public const string Sipariste = "Sipariste";
        }

        public static class UcKDurum
        {
            public const string Bekliyor = "Bekliyor";
            public const string TamGeldi = "TamGeldi";
            public const string EksikGeldi = "EksikGeldi";
            public const string Gelmedi = "Gelmedi";
            public const string Paketlendi = "Paketlendi";
            public const string KontrolEdildi = "KontrolEdildi";
            public const string IadeEdildi = "IadeEdildi";
            public const string ProjedenKarsilandi = "ProjedenKarsilandi";
            public const string StoktanKarsilandi = "StoktanKarsilandi";
            public const string TedarikcidenGeldi = "TedarikcidenGeldi";
            public const string BaskaProyeVerildi = "BaskaProyeVerildi";
            public const string HataliUrun = "HataliUrun";
        }

        public static class DepoLokasyon
        {
            public const string Belirsiz = "Belirsiz";
            public const string Grid = "Grid";
            public const string UcK = "UcK";
            public const string Protest = "Protest";
        }

        public static class KullaniciRol
        {
            public const string Admin = "Admin";
            public const string Personel3K = "Personel3K";
            public const string PersonelGrid = "PersonelGrid";
            public const string Yonetici = "Yonetici";
        }

        public static class GridSevkDurum
        {
            public const string SevkEdildi = "SevkEdildi";
            public const string Bekliyor = "Bekliyor";
            public const string SevkEdilmedi = "SevkEdilmedi";
        }

        public static class IslemTipi
        {
            public const string CekiYuklendi = "CekiYuklendi";
            public const string SandikOlusturuldu = "SandikOlusturuldu";
            public const string SandikBolundu = "SandikBolundu";
            public const string SandikDegisti = "SandikDegisti";
            public const string UrunTasindi = "UrunTasindi";
            public const string FBTransferi = "FBTransferi";
            public const string StokKullanimi = "StokKullanimi";
            public const string EksikKapatildi = "EksikKapatildi";
            public const string PDFAlindi = "PDFAlindi";
            public const string MailGonderildi = "MailGonderildi";
            public const string UrunGuncellendi = "UrunGuncellendi";
            public const string KullaniciOlusturuldu = "KullaniciOlusturuldu";
            public const string ProjeOlusturuldu = "ProjeOlusturuldu";
        }
    }
}
