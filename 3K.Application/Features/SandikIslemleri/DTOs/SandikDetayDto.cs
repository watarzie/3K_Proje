namespace _3K.Application.Features.SandikIslemleri.DTOs
{
    public class SandikDetayDto
    {
        public int Id { get; set; }
        public string SandikNo { get; set; } = string.Empty;
        public int DurumId { get; set; }
        public string DurumMetni { get; set; } = string.Empty;
        public int DepoLokasyonId { get; set; }
        public string DepoLokasyonMetni { get; set; } = string.Empty;
        public List<SandikIcerikDto> Icerikler { get; set; } = new();
    }
}
