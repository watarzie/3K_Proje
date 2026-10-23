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
        /// <summary>Sevkiyatlar sayfası: tır seferleri ve sevkiyat istatistikleri (R görüntüleme).</summary>
        public const string Sevkiyatlar = "sevkiyatlar";
        /// <summary>
        /// Proje fotoğraf sayfası bağlantısı; Normal, Saha ve Yedek proje listelerinin hepsinde aynı koddur (proje tipinden
        /// bağımsız): R bağlantıyı görür ve açar, W ekler, değiştirir, kaldırır. Projeler altında rotasız düğümdür
        /// (Projeler tavanıdır).
        /// </summary>
        public const string FotografBaglantisi = "proje-fotograf-baglantisi";
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
        /// <summary>
        /// Grid Sahası ekranı (salt görüntüleme, R): Grid'e gelmiş ürünler ve Grid personelinin mal kabul kayıtları,
        /// proje tipinden bağımsız tek listede. Satırdan Grid ekranına geçiş tipin Grid modülü iznini ister.
        /// </summary>
        public const string GridSahaGorunumu = "grid-saha-gorunumu";
        public const string CekiRevizyonYukle = "ceki-revizyon-yukle";
        public const string UcKSandikDurumRaporu = "3k-sandik-durum-raporu";
        public const string LokasyonAtama = "sandik-lokasyon-atama";
        /// <summary>
        /// Sevk sonrası düzeltme (W): sevkli sandığı sevkiyat kaydı korunarak düzeltmeye açma. Proje tipinin
        /// sandık/proje izniyle birlikte istenir; düzeltmedeki işlemler mevcut izinlerle yapılır.
        /// </summary>
        public const string SevkSonrasiDuzeltme = "sevk-sonrasi-duzeltme";
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
        // Finans yetkisi sekme ve alt sekme düzeyindedir (finans-yonetimi altında, menüde görünmez): R verileri gösterir,
        // W işlemleri yapar; alt işlem ve alan izni yoktur (20261013_02 ve 20261013_03 betikleri). İş Takibi, Tanımlar ve
        // PO/Fatura sekmelerinin işlemleri alt sekmeye bağlıdır; sekme alt sekmenin tavanıdır, alt sekme izni ayrıca verilir.
        // "Tüm dönemlerde ara" alt sekmesi yalnız ekranı açar; genel arama ucu kayıt sekmeleri kümesiyle (FinansSekmeleri.Kayit) çalışır.
        // Birden çok sekmede açılan ortak işlemler FinansSekmeleri kümeleriyle yetkilendirilir.
        public const string SekmePanel = "finans-sekme-panel";
        public const string SekmeIsTakibi = "finans-sekme-is-takibi";
        public const string SekmePoFatura = "finans-sekme-po-fatura";
        public const string SekmeGiderler = "finans-sekme-giderler";
        public const string SekmeGiderKategorileri = "finans-sekme-gider-kategorileri";
        public const string SekmeTanimlar = "finans-sekme-tanimlar";
        public const string SekmeRaporlar = "finans-sekme-raporlar";
        public const string SekmeMunferitIsler = "finans-sekme-munferit-isler";
        // Alt sekmeler (sekme düğümünün altında; sekme alt sekmenin tavanıdır). R alt sekmeyi gösterir, W işlemlerini yapar.
        public const string SekmeIsTakibiAylik = "finans-sekme-is-takibi-aylik";
        public const string SekmeIsTakibiArama = "finans-sekme-is-takibi-arama";
        public const string SekmeIsTakibiBekleyen = "finans-sekme-is-takibi-bekleyen";
        public const string SekmeTanimlarDuzenli = "finans-sekme-tanimlar-duzenli";
        public const string SekmeTanimlarTarifeler = "finans-sekme-tanimlar-tarifeler";
        public const string SekmeTanimlarParaBirimi = "finans-sekme-tanimlar-para-birimi";
        public const string SekmePoFaturaPo = "finans-sekme-po-fatura-po";
        public const string SekmePoFaturaFatura = "finans-sekme-po-fatura-fatura";
        /// <summary>İş Takibi'ndeki vade uyarısı (vadesi geçen/yaklaşan faturalar, tek tek kalan günüyle); yalnız R.</summary>
        public const string VadeUyarisi = "finans-vade-uyarisi";
    }
}
