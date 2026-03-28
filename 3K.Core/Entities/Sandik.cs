using _3K.Core.Enums;

namespace _3K.Core.Entities
{
    public class Sandik : BaseEntity
    {
        public int ProjeId { get; set; }
        public string SandikNo { get; set; } = string.Empty;
        public SandikDurum Durum { get; set; } = SandikDurum.Bos;
        public string? DepoLokasyonu { get; set; }

        // Navigation Properties
        public virtual Proje Proje { get; set; } = null!;
        public virtual ICollection<SandikIcerik> SandikIcerikleri { get; set; } = new List<SandikIcerik>();
    }
}
