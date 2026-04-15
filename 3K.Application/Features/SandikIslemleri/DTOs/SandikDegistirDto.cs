namespace _3K.Application.Features.SandikIslemleri.DTOs
{
    public class SandikDegistirDto
    {
        public int CekiSatiriId { get; set; }
        public string YeniFiiliSandikNo { get; set; } = string.Empty;
        public int ProjeId { get; set; }
        public int KullaniciId { get; set; }
    }
}
