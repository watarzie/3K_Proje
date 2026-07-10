namespace _3K.Core.Entities
{
    public class BildirimAboneligi : BaseEntity
    {
        public int KullaniciId { get; set; }
        public int TipId { get; set; }

        public virtual Kullanici Kullanici { get; set; } = null!;
    }
}
