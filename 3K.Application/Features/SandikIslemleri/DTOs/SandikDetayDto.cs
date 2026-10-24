namespace _3K.Application.Features.SandikIslemleri.DTOs
{
    public class SandikDetayDto
    {
        public int Id { get; set; }
        public string SandikNo { get; set; } = string.Empty;
        public string? Ad { get; set; }
        public int DurumId { get; set; }
        public string DurumMetni { get; set; } = string.Empty;
        public bool SevkiyatDuzeltmeAcikMi { get; set; }
        public int DepoLokasyonId { get; set; }
        public string DepoLokasyonMetni { get; set; } = string.Empty;
        /// <summary>Sandık tipi; özellik güncellemesinde zorunlu olduğu için ön yüz mevcut değeri buradan alır (G2).</summary>
        public int TipId { get; set; }
        public string TipMetni { get; set; } = string.Empty;
        public bool SahayaAktarildiMi { get; set; }
        public bool SahaUzerindenSevkEdildiMi { get; set; }
        public decimal SahayaAktarilanMiktar { get; set; }
        /// <summary>Son kapanma zamanı; yalnız kapalı ve sevkli sandıkta doludur.</summary>
        public DateTime? KapanmaTarihi { get; set; }
        
        // Fiziksel Özellikler
        public decimal? En { get; set; }
        public decimal? Boy { get; set; }
        public decimal? Yukseklik { get; set; }
        public decimal? NetKg { get; set; }
        public decimal? GrossKg { get; set; }

        public List<SandikIcerikDto> Icerikler { get; set; } = new();
    }
}
