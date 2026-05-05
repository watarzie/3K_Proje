namespace _3K.Application.Features.CekiIslemleri.DTOs
{
    public class CekiSatiriDto
    {
        public int Id { get; set; }
        public int SiraNo { get; set; }
        public string? OlcuResmiPozNo { get; set; }
        public string BarkodNo { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public decimal IstenenAdet { get; set; }
        public string Birim { get; set; } = string.Empty;
        public string CekideGecenSandikNo { get; set; } = string.Empty;
        public string? FiiliSandikNo { get; set; }
        public string? Remarks { get; set; }
        public string Durum { get; set; } = string.Empty;
        public string? PaketleyenBasHarf { get; set; }
        public string? KontrolEdenBasHarf { get; set; }
        public decimal KonulanAdet { get; set; }
        public decimal EksikAdet { get; set; }
    }
}


