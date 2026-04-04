namespace _3K.Core.Entities
{
    public class Sandik : BaseEntity
    {
        public int ProjeId { get; set; }
        public string SandikNo { get; set; } = string.Empty;
        public string Tip { get; set; } = "Proje";
        public string Durum { get; set; } = "Bos";
        public string DepoLokasyonu { get; set; } = "Belirsiz";

        // Navigation Properties
        public virtual Proje Proje { get; set; } = null!;
        public virtual ICollection<SandikIcerik> SandikIcerikleri { get; set; } = new List<SandikIcerik>();
    }
}
