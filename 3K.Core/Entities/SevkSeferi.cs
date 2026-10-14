namespace _3K.Core.Entities
{
    /// <summary>
    /// Tır seferi: aynı araçla aynı anda sevk edilen bir veya birden çok projenin sevkiyatlarını bağlayan başlık.
    /// Proje bazlı <see cref="Sevkiyat"/> kayıtları ve proje içi sevkiyat numaraları değişmez; sefer yalnız onları
    /// gruplar ve istatistiklerin kaynağıdır (hareket geçmişi arşivlendiği için kullanılmaz).
    /// </summary>
    public class SevkSeferi : BaseEntity
    {
        /// <summary>Global benzersiz sefer numarası: oluşturma yılı + kimlik (ör. 2026-000123).</summary>
        public string SeferNo { get; set; } = string.Empty;

        /// <summary>Normalize plaka (boşluk ve tire yok, büyük harf); gösterimde biçimlenir. Tekil sevkte boş olabilir.</summary>
        public string? AracPlaka { get; set; }

        public string? Sofor { get; set; }

        /// <summary>Normalize dorse plakası.</summary>
        public string? DorsePlaka { get; set; }

        public string? Aciklama { get; set; }
        public DateTime SevkTarihi { get; set; }
        public int KullaniciId { get; set; }

        /// <summary>Toplu sevkin tekrar anahtarı ve istek özeti; tekil sevkte boştur.</summary>
        public Guid? IslemAnahtari { get; set; }
        public string? IstekHash { get; set; }

        public virtual Kullanici Kullanici { get; set; } = null!;
        public virtual ICollection<Sevkiyat> Sevkiyatlar { get; set; } = new List<Sevkiyat>();
    }
}
