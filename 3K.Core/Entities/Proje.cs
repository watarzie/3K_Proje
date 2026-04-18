namespace _3K.Core.Entities
{
    public class Proje : BaseEntity
    {
        public string ProjeNo { get; set; } = string.Empty;
        public string Musteri { get; set; } = string.Empty;
        public string Durum { get; set; } = "Hazırlanıyor";
        public DateTime? PlanlananSevkTarihi { get; set; }
        public string SorumluKisi { get; set; } = string.Empty;

        // Excel header'dan gelen teknik bilgiler
        public string? FBNo { get; set; }
        public string? Guc { get; set; }
        public string? Gerilim { get; set; }
        public string? Lokasyon { get; set; }
        public string? OlcuResmiNo { get; set; }
        public string? NakilOlcuResmiNo { get; set; }
        public string? SonMontajResmiNo { get; set; }
        public string? ProjeMuduru { get; set; }

        // Navigation Properties
        public virtual ICollection<Ceki> Cekiler { get; set; } = new List<Ceki>();
        public virtual ICollection<Sandik> Sandiklar { get; set; } = new List<Sandik>();
        public virtual ICollection<Revizyon> Revizyonlar { get; set; } = new List<Revizyon>();
        public virtual ICollection<HareketGecmisi> HareketGecmisleri { get; set; } = new List<HareketGecmisi>();
        public virtual ICollection<StokHareketi> StokHareketleri { get; set; } = new List<StokHareketi>();
    }
}
