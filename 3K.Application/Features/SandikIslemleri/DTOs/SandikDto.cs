namespace _3K.Application.Features.SandikIslemleri.DTOs
{
    public class SandikDto
    {
        public int Id { get; set; }
        public string SandikNo { get; set; } = string.Empty;
        public string? Ad { get; set; }
        public int DurumId { get; set; }
        public string DurumMetni { get; set; } = string.Empty;
        public bool SevkiyatDuzeltmeAcikMi { get; set; }
        public int DepoLokasyonId { get; set; }
        public string DepoLokasyonMetni { get; set; } = string.Empty;
        public int UrunSayisi { get; set; }
        public bool IsManuelSandik { get; set; }
        public bool SilinebilirMi { get; set; }
        public bool DepodaSayilacakMi { get; set; }
        public bool SahayaAktarildiMi { get; set; }
        public decimal SahayaAktarilanMiktar { get; set; }
        
        // Fiziksel Özellikler
        public decimal? En { get; set; }
        public decimal? Boy { get; set; }
        public decimal? Yukseklik { get; set; }
        public decimal? NetKg { get; set; }
        public decimal? GrossKg { get; set; }
    }
}
