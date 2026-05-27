namespace _3K.Core.Entities
{
    public class Sevkiyat : BaseEntity
    {
        public int ProjeId { get; set; }
        public int SevkiyatNo { get; set; }
        public DateTime SevkTarihi { get; set; }
        public string? Aciklama { get; set; }
        public int KullaniciId { get; set; }

        public virtual Proje Proje { get; set; } = null!;
        public virtual Kullanici Kullanici { get; set; } = null!;
        public virtual ICollection<SevkiyatSandik> Sandiklar { get; set; } = new List<SevkiyatSandik>();
    }
}
