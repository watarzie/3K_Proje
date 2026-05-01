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
        public int GridDurumuId { get; set; }
        public string GridDurumuMetni { get; set; } = string.Empty;
        public int GridGelenAdet { get; set; }
        public int TrafoSevkAdet { get; set; }
        public int GridSevkDurumuId { get; set; }
        public string GridSevkDurumuMetni { get; set; } = string.Empty;
        public int? GridSevkMiktari { get; set; }
        public DateTime? GridSevkTarihi { get; set; }
        public string? GridAciklama { get; set; }
        public int GridEksikMiktar { get; set; }

        // Parçalı karşılama (Madde 2)
        public int StokKarsilanan { get; set; }
        public int ProjeKarsilanan { get; set; }
        public int ProjeGonderilen { get; set; }
        public int TedarikciKarsilanan { get; set; }
        public int EksikMiktar { get; set; }
        public int KalanMiktar { get; set; }

        // 3K tarafı (read-only)
        public int UcKDurumuId { get; set; }
        public string UcKDurumuMetni { get; set; } = string.Empty;
        public int GelenMiktar { get; set; }
        public int GeriGonderilenMiktar { get; set; }
        public string? KaynakHedefProjeNo { get; set; }

        // Çapraz açıklama: 3K tarafının bu ürüne yazdığı açıklama (Grid tarafı görür)
        public string? UcKAciklama { get; set; }

        // Genel
        public int GenelDurumId { get; set; }
        public string GenelDurumMetni { get; set; } = string.Empty;
    }
}
