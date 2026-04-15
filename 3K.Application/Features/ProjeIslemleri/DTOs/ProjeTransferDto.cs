namespace _3K.Application.Features.ProjeIslemleri.DTOs
{
    public class ProjeTransferDto
    {
        public int Id { get; set; }
        public string KaynakProjeNo { get; set; } = string.Empty;
        public string HedefProjeNo { get; set; } = string.Empty;
        public string BarkodNo { get; set; } = string.Empty;
        public int Miktar { get; set; }
        public string KullaniciAdi { get; set; } = string.Empty;
        public string? Aciklama { get; set; }
        public DateTime Tarih { get; set; }
    }
}
