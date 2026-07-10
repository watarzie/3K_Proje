namespace _3K.Core.Entities
{
    public class KullaniciBildirimi : BaseEntity
    {
        public int BildirimId { get; set; }
        public int KullaniciId { get; set; }
        public bool OkunduMu { get; set; }
        public DateTime? OkunmaTarihi { get; set; }

        public virtual Bildirim Bildirim { get; set; } = null!;
        public virtual Kullanici Kullanici { get; set; } = null!;
    }
}
