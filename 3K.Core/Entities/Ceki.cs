using _3K.Core.Enums;
using _3K.Core.Helpers;
namespace _3K.Core.Entities
{
    public class Ceki : BaseEntity
    {
        public int ProjeId { get; set; }
        public string OrijinalDosyaYolu { get; set; } = string.Empty;
        public DateTime YuklemeTarihi { get; set; } = TurkeyTime.Now;
        public int CekiTipiId { get; set; } = (int)CekiTipi.Normal;
        public int? KaynakCekiId { get; set; }
        public int? TamamlamaNo { get; set; }
        public string? Aciklama { get; set; }

        // Navigation Properties
        public virtual Proje Proje { get; set; } = null!;
        public virtual Ceki? KaynakCeki { get; set; }
        public virtual ICollection<Ceki> TamamlamaCekileri { get; set; } = new List<Ceki>();
        public virtual ICollection<CekiSatiri> CekiSatirlari { get; set; } = new List<CekiSatiri>();
    }
}
