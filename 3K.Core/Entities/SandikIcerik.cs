namespace _3K.Core.Entities
{
    public class SandikIcerik : BaseEntity
    {
        public int SandikId { get; set; }
        public int CekiSatiriId { get; set; }
        public int KonulanAdet { get; set; }
        public int EksikAdet { get; set; }

        // Navigation Properties
        public virtual Sandik Sandik { get; set; } = null!;
        public virtual CekiSatiri CekiSatiri { get; set; } = null!;
    }
}
