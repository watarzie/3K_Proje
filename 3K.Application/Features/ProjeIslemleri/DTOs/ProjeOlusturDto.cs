namespace _3K.Application.Features.ProjeIslemleri.DTOs
{
    public class ProjeOlusturDto
    {
        public string ProjeNo { get; set; } = string.Empty;
        public string Musteri { get; set; } = string.Empty;
        public DateTime? PlanlananSevkTarihi { get; set; }
        public string SorumluKisi { get; set; } = string.Empty;
    }
}
