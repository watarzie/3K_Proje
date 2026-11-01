using _3K.Core.Helpers;

namespace _3K.Core.Entities
{
    public class SahaAktarim : BaseEntity
    {
        public int SahaProjeId { get; set; }
        public int? KaynakProjeId { get; set; }
        public int? KullaniciId { get; set; }
        public DateTime Tarih { get; set; } = TurkeyTime.Now;
        public string? Aciklama { get; set; }

        /// <summary>Sahadan sahaya taşımada satırların geldiği saha projesi; normalden sahaya aktarımda boştur.</summary>
        public int? KaynakSahaProjeId { get; set; }

        /// <summary>İstemcinin tekrar anahtarı: aynı anahtarla gelen ikinci istek yeni kayıt üretmez.</summary>
        public Guid? IslemAnahtari { get; set; }

        public virtual Proje SahaProje { get; set; } = null!;
        public virtual Proje? KaynakProje { get; set; }
        public virtual Proje? KaynakSahaProje { get; set; }
        public virtual Kullanici? Kullanici { get; set; }
        public virtual ICollection<SahaAktarimKalemi> Kalemler { get; set; } = new List<SahaAktarimKalemi>();
        public virtual ICollection<SahaTasimaKalemi> TasimaKalemleri { get; set; } = new List<SahaTasimaKalemi>();
    }
}
