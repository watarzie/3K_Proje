using _3K.Core.Helpers;

namespace _3K.Core.Entities
{
    public class HareketGecmisiArsiv : BaseEntity
    {
        public int ProjeId { get; set; }
        public string ReferansTipi { get; set; } = string.Empty;
        public string? ReferansId { get; set; }
        public string Islem { get; set; } = string.Empty;
        public int? IslemTipiId { get; set; }
        public int KullaniciId { get; set; }
        public DateTime Tarih { get; set; } = TurkeyTime.Now;
        public string? EskiDeger { get; set; }
        public string? YeniDeger { get; set; }
        public string? Aciklama { get; set; }
        public string? ReferansMetni { get; set; }
        public string? KullaniciAdi { get; set; }
        public string? IslemTipiMetni { get; set; }
        public string? ProjeNo { get; set; }
    }
}
