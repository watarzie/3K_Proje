namespace _3K.Application.Features.NotIslemleri.DTOs
{
    public class NotDto
    {
        public int Id { get; set; }
        public string YazanTaraf { get; set; } = string.Empty;
        public string Icerik { get; set; } = string.Empty;
        public DateTime Tarih { get; set; }
        public string KullaniciAdi { get; set; } = string.Empty;
        public string? KullaniciBasHarf { get; set; }
        public string BagliReferansTipi { get; set; } = string.Empty;
        public int BagliReferansId { get; set; }
    }
}
