namespace _3K.Core.Entities
{
    public class Revizyon : BaseEntity
    {
        public int ProjeId { get; set; }
        public int KullaniciId { get; set; }
        public string Tip { get; set; } = string.Empty;
        public string? EskiDeger { get; set; }
        public string? YeniDeger { get; set; }
        public string? Aciklama { get; set; }
        public DateTime Tarih { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Proje Proje { get; set; } = null!;
        public virtual Kullanici Kullanici { get; set; } = null!;
    }
}
