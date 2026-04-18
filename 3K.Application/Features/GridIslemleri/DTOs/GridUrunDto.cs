namespace _3K.Application.Features.GridIslemleri.DTOs
{
    public class GridUrunDto
    {
        public int CekiSatiriId { get; set; }
        public int SiraNo { get; set; }
        public string BarkodNo { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public int IstenenAdet { get; set; }
        public string Birim { get; set; } = string.Empty;
        public string SandikNo { get; set; } = string.Empty;

        // Grid tarafı
        public string GridDurumu { get; set; } = string.Empty;
        public int GridGelenAdet { get; set; }
        public int TrafoSevkAdet { get; set; }
        public string GridSevkDurumu { get; set; } = string.Empty;
        public int? GridSevkMiktari { get; set; }
        public DateTime? GridSevkTarihi { get; set; }
        public string? GridNotu { get; set; }
        public int GridEksikMiktar { get; set; }

        // 3K tarafı (read-only)
        public string UcKDurumu { get; set; } = string.Empty;
        public int GelenMiktar { get; set; }
        public string? KaynakHedefProjeNo { get; set; }

        // Genel
        public string GenelDurum { get; set; } = string.Empty;
    }
}
