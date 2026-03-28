namespace _3K.Core.Entities
{
    public class Ceki : BaseEntity
    {
        public int ProjeId { get; set; }
        public string OrijinalDosyaYolu { get; set; } = string.Empty;
        public DateTime YuklemeTarihi { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Proje Proje { get; set; } = null!;
        public virtual ICollection<CekiSatiri> CekiSatirlari { get; set; } = new List<CekiSatiri>();
    }
}
