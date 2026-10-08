namespace _3K.Core.Entities
{
    public class Sevkiyat : BaseEntity
    {
        public int ProjeId { get; set; }
        public int SevkiyatNo { get; set; }
        public DateTime SevkTarihi { get; set; }
        public string? Aciklama { get; set; }
        public string? AracPlaka { get; set; }
        public int KullaniciId { get; set; }

        /// <summary>Sevkiyatın ait olduğu tır seferi; eski kayıtlarda boştur (istatistikte plaka + gün ile gruplanır).</summary>
        public int? SevkSeferiId { get; set; }

        public virtual Proje Proje { get; set; } = null!;
        public virtual SevkSeferi? SevkSeferi { get; set; }
        public virtual Kullanici Kullanici { get; set; } = null!;
        public virtual ICollection<SevkiyatSandik> Sandiklar { get; set; } = new List<SevkiyatSandik>();
    }
}
