namespace _3K.Core.Entities
{
    /// <summary>
    /// Proje arası ürün transfer kaydı.
    /// PROJEDEN KARŞILANDI / BAŞKA PROJEYE VERİLDİ işlemlerinde oluşturulur.
    /// </summary>
    public class ProjeTransfer : BaseEntity
    {
        public int KaynakProjeId { get; set; }
        public int HedefProjeId { get; set; }
        public int KaynakCekiSatiriId { get; set; }
        public int? HedefCekiSatiriId { get; set; }
        public string BarkodNo { get; set; } = string.Empty;
        public int Miktar { get; set; }
        public int KullaniciId { get; set; }
        public string? Aciklama { get; set; }
        public DateTime Tarih { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Proje KaynakProje { get; set; } = null!;
        public virtual Proje HedefProje { get; set; } = null!;
        public virtual CekiSatiri KaynakCekiSatiri { get; set; } = null!;
        public virtual CekiSatiri? HedefCekiSatiri { get; set; }
        public virtual Kullanici Kullanici { get; set; } = null!;
    }
}
