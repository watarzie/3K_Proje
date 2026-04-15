namespace _3K.Application.Features.SandikIslemleri.DTOs
{
    public class SandikDetayDto
    {
        public int Id { get; set; }
        public string SandikNo { get; set; } = string.Empty;
        public string Durum { get; set; } = string.Empty;
        public string? DepoLokasyonu { get; set; }
        public List<SandikIcerikDto> Icerikler { get; set; } = new();
    }
}
