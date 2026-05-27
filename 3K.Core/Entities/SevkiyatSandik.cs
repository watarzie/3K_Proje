namespace _3K.Core.Entities
{
    public class SevkiyatSandik : BaseEntity
    {
        public int SevkiyatId { get; set; }
        public int SandikId { get; set; }

        public virtual Sevkiyat Sevkiyat { get; set; } = null!;
        public virtual Sandik Sandik { get; set; } = null!;
    }
}
