namespace _3K.Application.Common
{
    /// <summary>
    /// Lookup tablolarındaki durum değerlerinin type-safe sabitleri.
    /// DB'deki string FK'lerle birebir eşleşir.
    /// Handler'larda magic string yerine bu sabitler kullanılır.
    /// </summary>
    public static class StatusConstants
    {
        public const int ActionQueuedForApproval = 202;

        public static class ProjeDurum
        {
            public const string Hazirlaniyor = "Hazırlanıyor";
            public const string Devam = "Devam";
            public const string Tamamlandi = "Tamamlandı";
            public const string Beklemede = "Beklemede";
            public const string SevkEdildi = "Sevk Edildi";
            public const string EksikSevkEdildi = "Eksik Sevk Edildi";
        }

        public static class SandikDurum
        {
            public const string Bos = "Boş";
            public const string Hazirlaniyor = "Hazırlanıyor";
            public const string Hazir = "Hazır";
            public const string Sevkedildi = "Sevk Edildi";
        }

        public static class UrunDurum
        {
            public const string Bekliyor = "Bekliyor";
            public const string KismiGeldi = "Kısmi Geldi";
            public const string Tamamlandi = "Tamamlandı";
            public const string Eksik = "Eksik";
            public const string StoktanKarsilandi = "Stoktan Karşılandı";
            public const string FBdenKarsilandi = "FB'den Karşılandı";
            public const string SonraGidecek = "Sonra Gidecek";
            public const string SandikDegisti = "Sandık Değişti";
            public const string IptalVeyaPasif = "İptal/Pasif";
            public const string TeslimAlindi = "Teslim Alındı";
            public const string GeriGonderildi = "Geri Gönderildi";
            public const string KismiTamamlandi = "Kısmi Tamamlandı";
            public const string Kayip = "Kayıp";
            public const string GriddeHazir = "Grid'de Hazır";
            public const string GriddeEksik = "Grid'de Eksik";
            public const string Sipariste = "Siparişte";
            public const string Gelmedi = "Gelmedi";
            public const string TrafoSevk = "Trafo Sevk";
            public const string BaskaProyeVerildi = "Başka Projeye Verildi";
            public const string HataliUrun = "Hatalı Ürün";
        }

        public static class GridDurum
        {
            public const string Bekliyor = "Bekliyor";
            public const string Uretimde = "Üretimde";
            public const string StokHazir = "Stok Hazır";
            public const string SevkEdildi = "Sevk Edildi";
            public const string KismiSevkEdildi = "Kısmi Sevk Edildi";
            public const string Bekletiliyor = "Bekletiliyor";
            public const string IptalEdildi = "İptal Edildi";
            public const string TamGeldi = "Tam Geldi";
            public const string EksikGeldi = "Eksik Geldi";
            public const string Gelmedi = "Gelmedi";
            public const string TrafoSevk = "Trafo Sevk";
            public const string Iptal = "İptal";
            public const string Sipariste = "Siparişte";
        }

        public static class UcKDurum
        {
            public const string Bekliyor = "Bekliyor";
            public const string TamGeldi = "Tam Geldi";
            public const string EksikGeldi = "Eksik Geldi";
            public const string Gelmedi = "Gelmedi";
            public const string Paketlendi = "Paketlendi";
            public const string KontrolEdildi = "Kontrol Edildi";
            public const string IadeEdildi = "İade Edildi";
            public const string ProjedenKarsilandi = "Projeden Karşılandı";
            public const string StoktanKarsilandi = "Stoktan Karşılandı";
            public const string TedarikcidenGeldi = "Tedarikçiden Geldi";
            public const string BaskaProyeVerildi = "Başka Projeye Verildi";
            public const string GeriGonderildi = "Geri Gönderildi";
            public const string HataliUrun = "Hatalı Ürün";
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
            public const string SevkEdildi = "Sevk Edildi";
            public const string Bekliyor = "Bekliyor";
            public const string SevkEdilmedi = "Sevk Edilmedi";
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
