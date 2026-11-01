using _3K.Core.Enums;

namespace _3K.Core.Models
{
    /// <summary>
    /// Sandık açılış/kapanış olayı kuralı. Hangi durum geçişinin olay sayılacağı ve kapanma tarihinin nasıl
    /// ilerleyeceği yalnız buradadır; tekil, toplu ve otomatik yolların hepsi kaydetme anında bu kuralla değerlendirilir.
    /// </summary>
    public static class SandikDurumOlayKurali
    {
        /// <summary>Geri açma notunun en fazla uzunluğu.</summary>
        public const int NotEnFazlaUzunluk = 500;

        /// <summary>
        /// Durum geçişinin olay türü. Kapanma: Kapandı'ya giriş (sevkli sandığın kilidi açılıp sevk öncesi Kapandı'ya
        /// dönmesi yeni kapanma değildir). Açılma: Kapandı'dan Hazırlanıyor'a veya Boş'a dönüş. Kapandı → Sevk edildi
        /// ayrı bir olaydır (sevkiyat), açılma sayılmaz.
        /// </summary>
        public static SandikDurumOlayTuru? OlayTuru(int? eskiDurumId, int yeniDurumId)
        {
            if (eskiDurumId == yeniDurumId)
                return null;
            if (yeniDurumId == (int)SandikDurum.Kapandi)
                return eskiDurumId == (int)SandikDurum.Sevkedildi ? null : SandikDurumOlayTuru.Kapandi;
            if (eskiDurumId == (int)SandikDurum.Kapandi &&
                yeniDurumId is (int)SandikDurum.Hazirlaniyor or (int)SandikDurum.Bos)
                return SandikDurumOlayTuru.Acildi;
            return null;
        }

        /// <summary>
        /// Geçişten sonraki kapanma tarihi: kapanmada şimdiki zaman; Hazırlanıyor/Boş'a dönüşte boş; diğer
        /// geçişlerde (Kapandı → Sevk edildi, sevk kilidinin açılması) mevcut değer korunur.
        /// </summary>
        public static DateTime? KapanmaTarihi(DateTime? mevcut, int? eskiDurumId, int yeniDurumId, DateTime simdi)
        {
            if (OlayTuru(eskiDurumId, yeniDurumId) == SandikDurumOlayTuru.Kapandi)
                return simdi;
            return yeniDurumId is (int)SandikDurum.Hazirlaniyor or (int)SandikDurum.Bos ? null : mevcut;
        }

        /// <summary>Ekran ve raporda olay türü metni.</summary>
        public static string TurEtiketi(SandikDurumOlayTuru tur) => tur switch
        {
            SandikDurumOlayTuru.Kapandi => "Kapandı",
            SandikDurumOlayTuru.Acildi => "Geri açıldı",
            _ => "Bilinmiyor"
        };

        /// <summary>Ekran ve raporda olay kaynağı metni.</summary>
        public static string KaynakEtiketi(SandikDurumOlayKaynagi kaynak) => kaynak switch
        {
            SandikDurumOlayKaynagi.Elle => "Elle",
            SandikDurumOlayKaynagi.Toplu => "Toplu işlem",
            SandikDurumOlayKaynagi.Otomatik => "Otomatik",
            SandikDurumOlayKaynagi.GecmistenAktarildi => "Geçmişten aktarıldı",
            _ => "Bilinmiyor"
        };

        /// <summary>Kullanıcının yazdığı not: boşluklar kırpılır, boşsa kaydedilmez.</summary>
        public static string? NotNormalize(string? not)
        {
            var temiz = not?.Trim();
            return string.IsNullOrEmpty(temiz) ? null : temiz;
        }

        private static readonly System.Globalization.CompareInfo TrKarsilastirma =
            System.Globalization.CultureInfo.GetCultureInfo("tr-TR").CompareInfo;

        /// <summary>İstatistikteki proje/müşteri araması: kırpılır, en fazla 100 karakter; boşsa süzgeç yoktur.</summary>
        public static string? AramaNormalize(string? arama)
        {
            var temiz = arama?.Trim();
            if (string.IsNullOrEmpty(temiz))
                return null;
            return temiz.Length > 100 ? temiz[..100] : temiz;
        }

        /// <summary>
        /// Metin aranan parçayı Türkçe büyük/küçük harf duyarsız içeriyor mu ("örnek" ↔ "ÖRNEK", "ı" ↔ "I").
        /// Veritabanı karşılaştırması (ctype C) Türkçe harfleri katlamadığı için eşleme bellekte yapılır.
        /// </summary>
        public static bool AramaEslesir(string? metin, string aranan) =>
            metin != null && TrKarsilastirma.IndexOf(metin, aranan, System.Globalization.CompareOptions.IgnoreCase) >= 0;

        /// <summary>İstatistik tarih aralığı: başlangıç bitişten sonra olamaz.</summary>
        public static bool TarihAraligiGecerli(DateTime? baslangic, DateTime? bitis) =>
            baslangic is null || bitis is null || baslangic.Value.Date <= bitis.Value.Date;

        /// <summary>Elle geri açmada sebep zorunludur; not isteğe bağlıdır ve sınırı aşmaz.</summary>
        public static string? GeriAcmaHatasi(int? sebepId, string? not)
        {
            if (sebepId is not > 0)
                return "Sandığı geri açmak için bir sebep seçin.";
            if (NotNormalize(not)?.Length > NotEnFazlaUzunluk)
                return $"Not en fazla {NotEnFazlaUzunluk} karakter olabilir.";
            return null;
        }
    }
}
