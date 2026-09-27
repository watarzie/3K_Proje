namespace _3K.Core.Constants;

/// <summary>
/// MenuTanimlari.Kod değerlerinin tek kaynağı. Her kod ekran, alt ekran, işlem veya alan
/// izni olabilir; hepsi aynı N/R/W modeliyle değerlendirilir: R görüntüleme, W işlem yapma.
/// Üst düğüm alt düğümün tavanıdır, ancak üst W alt izni kendiliğinden vermez.
/// </summary>
public static class YetkiKodlari
{
    public const string Dashboard = "dashboard";
    public const string DepoDurumu = "depo-durumu";
    public const string HareketGecmisi = "hareket-gecmisi";
    public const string Kullanicilar = "kullanicilar";
    public const string RolYonetimi = "rol-yonetimi";

    public static class Proje
    {
        public const string Kok = "projeler";
        public const string AktifProjeler = "aktif-projeler";
        public const string SevkEdilen = "sevk-edilen";
        public const string SevkEt = "proje-sevk-et";
        public const string Sil = "proje-sil";
        public const string CekiYukle = "ceki-yukle";
        public const string PlanlananSevkTarihi = "planlanan-sevk-tarihi";
    }

    public static class Sandik
    {
        public const string Yonetimi = "sandik-yonetimi";
        public const string GridModulu = "grid-modulu";
        public const string UcKModulu = "3k-modulu";
        public const string KaliteModulu = "kalite-modulu";
        public const string SurecModulu = "surec-modulu";
        public const string EksikRaporu = "eksik-raporu";
        public const string GerceklesenCekiRaporu = "gerceklesen-ceki-raporu";
        public const string CekiVerisiDuzenle = "ceki-verisi-duzenle";
        public const string CekiVerisiSil = "ceki-verisi-sil";
        public const string UcKIsListesi = "3k-is-listesi";
        public const string GridIsListesi = "grid-is-listesi";
        public const string CekiRevizyonYukle = "ceki-revizyon-yukle";
        public const string UcKSandikDurumRaporu = "3k-sandik-durum-raporu";
        public const string LokasyonAtama = "sandik-lokasyon-atama";
    }

    public static class Stok
    {
        public const string Kok = "stok";
        public const string Sil = "stok-sil";
    }

    public static class Saha
    {
        public const string Yonetimi = "saha-yonetimi";
        public const string GridModulu = "saha-grid-modulu";
        public const string UcKModulu = "saha-3k-modulu";
        public const string Raporu = "saha-raporu";
        public const string Sandiklar = "saha-sandiklar";
        public const string SevkEt = "saha-sevk-et";
        public const string ProjeSil = "saha-proje-sil";
        public const string SevkSonrasiEksikRaporu = "saha-sevk-sonrasi-eksik-raporu";
        public const string UcKSandikDurumRaporu = "saha-3k-sandik-durum-raporu";
        public const string GerceklesenCekiRaporu = "saha-gerceklesen-ceki-raporu";
        public const string SahayaAktar = "sahaya-aktar";
        public const string AktarimGeriAl = "saha-aktarim-geri-al";
    }

    public static class Yedek
    {
        public const string Yonetimi = "yedek-yonetimi";
        public const string CekiYukle = "yedek-ceki-yukle";
        public const string GridModulu = "yedek-grid-modulu";
        public const string UcKModulu = "yedek-3k-modulu";
        public const string Sandiklar = "yedek-sandiklar";
        public const string Raporu = "yedek-raporu";
        public const string EksikRaporu = "yedek-eksik-raporu";
        public const string GerceklesenCekiRaporu = "yedek-gerceklesen-ceki-raporu";
        public const string UcKSandikDurumRaporu = "yedek-3k-sandik-durum-raporu";
        public const string SevkEt = "yedek-sevk-et";
        public const string ProjeSil = "yedek-proje-sil";
        public const string PlanlananSevkTarihi = "yedek-planlanan-sevk-tarihi";
    }

    public static class Onay
    {
        public const string Merkezi = "islem-onay-merkezi";
        public const string KurallariYonet = "onay-kurallari-yonet";
    }

    public static class Ambalaj
    {
        public const string Listele = "ambalaj-uretim-listesi";
        public const string KayitDuzenle = "ambalaj-kayit-duzenle";
        public const string RaporGoruntule = "ambalaj-rapor-goruntule";
        public const string M3Goruntule = "ambalaj-m3-goruntule";
        public const string SarfGoruntule = "ambalaj-sarf-goruntule";
        public const string KaynakGoruntule = "ambalaj-kaynak-goruntule";
        public const string DahilEt = "ambalaj-dahil-et";
        public const string HaricTut = "ambalaj-haric-tut";
        public const string DurumDuzenle = "ambalaj-durum-duzenle";
        public const string Iptal = "ambalaj-iptal";
        public const string KaynakMudahalesi = "ambalaj-kaynak-mudahalesi";
        public const string FormGoruntule = "ambalaj-form-goruntule";
        public const string FormIndir = "ambalaj-form-indir";
        public const string ExcelIndir = "ambalaj-excel-indir";
        public const string PdfIndir = "ambalaj-pdf-indir";
        public const string FormOlustur = "ambalaj-form-olustur";
        public const string FormYenidenOlustur = "ambalaj-form-yeniden-olustur";
        public const string UretimiTamamla = "ambalaj-uretimi-tamamla";
        public const string UretimiYenidenAc = "ambalaj-uretimi-yeniden-ac";
        public const string DurumuGeriAl = "ambalaj-durumu-geri-al";
        public const string KritikVeriDuzenle = "ambalaj-kritik-veri-duzenle";
        public const string FormSonrasiSecimDegistir = "ambalaj-form-sonrasi-secim-degistir";
        public const string TamamlananProjeyeEkle = "ambalaj-tamamlanan-projeye-ekle";
        public const string OlcuGoruntule = "ambalaj-olcu-goruntule";
        public const string PlanGoruntule = "ambalaj-plan-goruntule";
        public const string PlanOlustur = "ambalaj-plan-olustur";
        public const string BekleyenGoruntule = "ambalaj-bekleyen-goruntule";
        public const string UretimdeGoruntule = "ambalaj-uretimde-goruntule";
        public const string TamamlananGoruntule = "ambalaj-tamamlanan-goruntule";
        public const string GerceklesmeDuzelt = "ambalaj-gerceklesme-duzelt";
        public const string ProjeDetayGoruntule = "ambalaj-proje-detay-goruntule";
        public const string SandikListesiGoruntule = "ambalaj-sandik-listesi-goruntule";
        public const string M3RaporGoruntule = "ambalaj-m3-rapor-goruntule";
        public const string KayitEkle = "ambalaj-kayit-ekle";
        public const string KayitSil = "ambalaj-kayit-sil";
    }

    public static class Finans
    {
        public const string Modul = "finans-yonetimi";
        public const string GelirGoruntule = "finans-gelir-goruntule";
        public const string GiderGoruntule = "finans-gider-goruntule";
        public const string GiderYonet = "finans-gider-yonet";
        public const string SiparisOperasyonGoruntule = "finans-siparis-goruntule";
        public const string FaturaYonet = "finans-fatura-yonet";
        public const string TarifeYonet = "finans-tarife-yonet";
        public const string RaporGoruntule = "finans-rapor-goruntule";
        public const string ManuelIsEkle = "finans-is-ekle";
        public const string ManuelIsDuzenle = "finans-is-duzenle";
        public const string IsIptal = "finans-is-iptal";
        public const string TarihDegistir = "finans-tarih-degistir";
        public const string PoGir = "finans-po-gir";
        public const string PoDegistir = "finans-po-degistir";
        public const string BirimFiyatGoruntule = "finans-birim-fiyat-goruntule";
        public const string BirimFiyatDegistir = "finans-birim-fiyat-degistir";
        public const string KarlilikGoruntule = "finans-karlilik-goruntule";
        public const string ExcelAktar = "finans-excel-aktar";
        public const string PdfAktar = "finans-pdf-aktar";
        public const string GiderEkle = "finans-gider-ekle";
        public const string GiderDuzenle = "finans-gider-duzenle";
        public const string GiderKutuphanesiYonet = "finans-gider-kutuphanesi-yonet";
        public const string IsKutuphanesiYonet = "finans-is-kutuphanesi-yonet";
        public const string DuzenliIsYonet = "finans-duzenli-is-yonet";
        public const string ParasalVeriGoruntule = "finans-parasal-veri-goruntule";
        public const string TutarGoruntule = "finans-tutar-goruntule";
        public const string KayitGoruntule = "finans-kayit-goruntule";
        public const string FaturaGir = "finans-fatura-gir";
        public const string FaturaDegistir = "finans-fatura-degistir";
        public const string FaturaIptal = "finans-fatura-iptal";
        public const string PoIptal = "finans-po-iptal";
        public const string GiderIptal = "finans-gider-iptal";
        public const string KaliciSil = "finans-kalici-sil";
        public const string BelgeYukle = "finans-belge-yukle";
        public const string BelgeIndir = "finans-belge-indir";
        public const string SablonYonet = "finans-sablon-yonet";
        public const string DenetimGor = "finans-denetim-gor";
        public const string FiyatlandirmaDegistir = "finans-fiyatlandirma-degistir";
    }
}
