namespace _3K.Core.Entities
{
    public class HareketGecmisi : BaseEntity
    {
        public int ProjeId { get; set; }
        public string ReferansTipi { get; set; } = string.Empty;
        public string? ReferansId { get; set; }
        public string Islem { get; set; } = string.Empty;
        public int KullaniciId { get; set; }
        public DateTime Tarih { get; set; } = DateTime.UtcNow;

        // İş akışı 11: eski değer, yeni değer, açıklama
        public string? EskiDeger { get; set; }
        public string? YeniDeger { get; set; }
        public string? Aciklama { get; set; }

        // Navigation Properties
        public virtual Proje Proje { get; set; } = null!;
        public virtual Kullanici Kullanici { get; set; } = null!;
    }
}
