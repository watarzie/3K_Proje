using _3K.Core.Enums;

namespace _3K.Core.Entities
{
    public class Sandik : BaseEntity
    {
        public int ProjeId { get; set; }
        public string SandikNo { get; set; } = string.Empty;
        public int TipId { get; set; } = (int)SandikTipi.Proje;
        public int DurumId { get; set; } = (int)SandikDurum.Bos;
        public int DepoLokasyonId { get; set; } = (int)DepoLokasyon.Belirsiz;

        // Navigation Properties
        public virtual Proje Proje { get; set; } = null!;
        public virtual LookupSandikDurum? DurumLookup { get; set; }
        public virtual LookupSandikTipi? TipLookup { get; set; }
        public virtual LookupDepoLokasyon? DepoLokasyonLookup { get; set; }
        public virtual ICollection<SandikIcerik> SandikIcerikleri { get; set; } = new List<SandikIcerik>();
    }
}
