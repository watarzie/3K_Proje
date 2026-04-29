namespace _3K.Application.Features.SandikIslemleri.DTOs
{
    public class SandikIcerikDto
    {
        public int Id { get; set; }
        public int? CekiSatiriId { get; set; }
        public string? OlcuResmiPozNo { get; set; }
        public string BarkodNo { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public int IstenenAdet { get; set; }
        public int KonulanAdet { get; set; }
        public int EksikAdet { get; set; }
        public int DurumId { get; set; }
        public string DurumMetni { get; set; } = string.Empty;
        public string? PaketleyenBasHarf { get; set; }
        public string? KontrolEdenBasHarf { get; set; }
        public string? Remarks { get; set; }

        // Saha/Yedek manuel malzeme alanları
        public string? Isim { get; set; }
        public decimal Miktar { get; set; }
        public int? BirimId { get; set; }
        public string? BirimMetni { get; set; }

        // Parçalı karşılama (Madde 2)
        public int StokKarsilanan { get; set; }
        public int ProjeKarsilanan { get; set; }
        public int TedarikciKarsilanan { get; set; }

        // Backend-hesaplanan alanlar (Dumb UI — KURAL 3)
        public int KalanMiktar { get; set; }
        public int GenelDurumId { get; set; }
        public string GenelDurumMetni { get; set; } = string.Empty;
    }
}

