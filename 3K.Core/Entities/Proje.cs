using _3K.Core.Enums;

namespace _3K.Core.Entities
{
    public class Proje : BaseEntity
    {
        public string ProjeNo { get; set; } = string.Empty;
        public string Musteri { get; set; } = string.Empty;
        public ProjeDurum Durum { get; set; } = ProjeDurum.Hazirlaniyor;
        public DateTime? PlanlananSevkTarihi { get; set; }
        public string SorumluKisi { get; set; } = string.Empty;

        // Navigation Properties
        public virtual ICollection<Ceki> Cekiler { get; set; } = new List<Ceki>();
        public virtual ICollection<Sandik> Sandiklar { get; set; } = new List<Sandik>();
        public virtual ICollection<Revizyon> Revizyonlar { get; set; } = new List<Revizyon>();
        public virtual ICollection<HareketGecmisi> HareketGecmisleri { get; set; } = new List<HareketGecmisi>();
        public virtual ICollection<StokHareketi> StokHareketleri { get; set; } = new List<StokHareketi>();
    }
}
