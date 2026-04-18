namespace _3K.Application.Features.UcKIslemleri.DTOs
{
    public class UcKUrunDto
    {
        public int CekiSatiriId { get; set; }
        public int SiraNo { get; set; }
        public string BarkodNo { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public string SandikNo { get; set; } = string.Empty;
        public int IstenenAdet { get; set; }
        public string Birim { get; set; } = string.Empty;

        // Grid tarafı (read-only)
        public string GridDurumu { get; set; } = string.Empty;
        public int GridGelenAdet { get; set; }
        public int TrafoSevkAdet { get; set; }

        // 3K tarafı
        public string UcKKarsilamaTipi { get; set; } = string.Empty;
        public int GelenMiktar { get; set; }
        public int KarsilananMiktar { get; set; }
        public int HataliMiktar { get; set; }
        public string? KaynakHedefProjeNo { get; set; }
        public string? GeriGonderilmeSebebi { get; set; }
        public string? UcKAciklama { get; set; }
        public string? UcKNotu { get; set; }

        // Hesaplanan
        public int Kalan { get; set; }
        public string KontrolUyari { get; set; } = string.Empty;
        public string GenelDurum { get; set; } = string.Empty;
    }
}
