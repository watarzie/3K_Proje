namespace _3K.Application.Features.GridIslemleri.DTOs
{
    public class GridUrunDto
    {
        public int CekiSatiriId { get; set; }
        public int SiraNo { get; set; }
        public string BarkodNo { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public decimal IstenenAdet { get; set; }
        public string Birim { get; set; } = string.Empty;
        public string SandikNo { get; set; } = string.Empty;

        // Grid tarafı
        public int GridDurumuId { get; set; }
        public string GridDurumuMetni { get; set; } = string.Empty;
        public decimal GridGelenAdet { get; set; }
        public decimal TrafoSevkAdet { get; set; }
        public int GridSevkDurumuId { get; set; }
        public string GridSevkDurumuMetni { get; set; } = string.Empty;
        public decimal? GridSevkMiktari { get; set; }
        public decimal YenidenSevkGerekliAdet { get; set; }
        public DateTime? GridSevkTarihi { get; set; }
        public string? GridAciklama { get; set; }
        public decimal GridEksikMiktar { get; set; }

        // Parçalı karşılama (Madde 2)
        public decimal StokKarsilanan { get; set; }
        public decimal ProjeKarsilanan { get; set; }
        public decimal ProjeGonderilen { get; set; }
        public decimal TedarikciKarsilanan { get; set; }
        public decimal EksikMiktar { get; set; }
        public decimal KalanMiktar { get; set; }

        // 3K tarafı (read-only)
        public int UcKDurumuId { get; set; }
        public string UcKDurumuMetni { get; set; } = string.Empty;
        public decimal GelenMiktar { get; set; }
        public decimal GeriGonderilenMiktar { get; set; }
        public int? GeriGonderilmeSebebiId { get; set; }
        public string? GeriGonderilmeSebebiMetni { get; set; }
        public string? KaynakHedefProjeNo { get; set; }

        // Çapraz açıklama: 3K tarafının bu ürüne yazdığı açıklama (Grid tarafı görür)
        public string? UcKAciklama { get; set; }

        // Genel
        public int GenelDurumId { get; set; }
        public string GenelDurumMetni { get; set; } = string.Empty;

        // Kalite & Süreç
        public int? KaliteDurumId { get; set; }
        public string? KaliteDurumMetni { get; set; }
        public int? SurecDurumId { get; set; }
        public string? SurecDurumMetni { get; set; }
    }
}


