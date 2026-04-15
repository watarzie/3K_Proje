namespace _3K.Application.Features.SandikIslemleri.DTOs
{
    public class UrunGuncelleDto
    {
        public int CekiSatiriId { get; set; }
        public int? KonulanAdet { get; set; }
        public int? EksikAdet { get; set; }
        public int? PaketleyenId { get; set; }
        public int? KontrolEdenId { get; set; }
        public string? Aciklama { get; set; }
        public string? YeniFiiliSandikNo { get; set; }
        public int KullaniciId { get; set; }
        public int ProjeId { get; set; }
    }
}
