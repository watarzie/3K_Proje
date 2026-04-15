namespace _3K.Application.Features.SandikIslemleri.DTOs
{
    public class EksikUrunDto
    {
        public int CekiSatiriId { get; set; }
        public int SiraNo { get; set; }
        public string BarkodNo { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public int IstenenAdet { get; set; }
        public int GelenMiktar { get; set; }
        public int EksikMiktar { get; set; }
        public string GridDurumu { get; set; } = string.Empty;
        public string UcKDurumu { get; set; } = string.Empty;
        public string SandikNo { get; set; } = string.Empty;
    }
}
