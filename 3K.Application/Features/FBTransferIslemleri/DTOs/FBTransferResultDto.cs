namespace _3K.Application.Features.FBTransferIslemleri.DTOs
{
    public class FBTransferResultDto
    {
        public int Id { get; set; }
        public string AsilFB { get; set; } = string.Empty;
        public string AlinanFB { get; set; } = string.Empty;
        public int Miktar { get; set; }
        public string? Neden { get; set; }
        public string? IadeDurumu { get; set; }
        public string? Aciklama { get; set; }
        public DateTime Tarih { get; set; }
    }
}
