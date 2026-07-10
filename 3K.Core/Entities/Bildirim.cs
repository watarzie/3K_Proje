namespace _3K.Core.Entities
{
    public class Bildirim : BaseEntity
    {
        public int TipId { get; set; }
        public string Baslik { get; set; } = string.Empty;
        public string Mesaj { get; set; } = string.Empty;
        public string? HedefUrl { get; set; }
        public string? ReferansTipi { get; set; }
        public int? ReferansId { get; set; }
        public int? OlusturanKullaniciId { get; set; }

        public virtual Kullanici? OlusturanKullanici { get; set; }
        public virtual ICollection<KullaniciBildirimi> Alicilar { get; set; } = new List<KullaniciBildirimi>();
    }
}
