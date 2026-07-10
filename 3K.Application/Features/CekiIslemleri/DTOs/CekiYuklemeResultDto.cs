namespace _3K.Application.Features.CekiIslemleri.DTOs
{
    public class CekiYuklemeResultDto
    {
        public int CekiId { get; set; }
        public int ProjeId { get; set; }
        public string ProjeNo { get; set; } = string.Empty;
        public int SatirSayisi { get; set; }
        public int SandikSayisi { get; set; }
        public string Mesaj { get; set; } = string.Empty;
    }
}
