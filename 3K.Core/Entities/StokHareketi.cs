namespace _3K.Core.Entities
{
    public class StokHareketi : BaseEntity
    {
        public int StokKaydiId { get; set; }
        public int CekiSatiriId { get; set; }
        public int ProjeId { get; set; }
        public int KullaniciId { get; set; }
        public int Miktar { get; set; }
        public string IslemTipi { get; set; } = string.Empty;
        public string? Aciklama { get; set; }
        public DateTime Tarih { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual StokKaydi StokKaydi { get; set; } = null!;
        public virtual CekiSatiri CekiSatiri { get; set; } = null!;
        public virtual Proje Proje { get; set; } = null!;
        public virtual Kullanici Kullanici { get; set; } = null!;
    }
}
