namespace _3K.Application.Features.HareketGecmisiIslemleri.DTOs
{
    public class HareketGecmisiDto
    {
        public int Id { get; set; }
        public string Islem { get; set; } = string.Empty;
        public string IslemTipiMetni { get; set; } = string.Empty;
        public string ReferansTipi { get; set; } = string.Empty;
        public string? ReferansId { get; set; }
        public string? EskiDeger { get; set; }
        public string? YeniDeger { get; set; }
        public string? Aciklama { get; set; }
        public string KullaniciAdi { get; set; } = string.Empty;
        public DateTime Tarih { get; set; }
    }
}
