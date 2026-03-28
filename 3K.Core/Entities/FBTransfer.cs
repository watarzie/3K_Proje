namespace _3K.Core.Entities
{
    public class FBTransfer : BaseEntity
    {
        public int CekiSatiriId { get; set; }
        public int KullaniciId { get; set; }
        public string AsilFB { get; set; } = string.Empty;
        public string AlinanFB { get; set; } = string.Empty;
        public int Miktar { get; set; }
        public string? Neden { get; set; }
        public string? IadeDurumu { get; set; }
        public string? Aciklama { get; set; }
        public DateTime Tarih { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual CekiSatiri CekiSatiri { get; set; } = null!;
        public virtual Kullanici Kullanici { get; set; } = null!;
    }
}
