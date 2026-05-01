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
        public int GridDurumuId { get; set; }
        public string GridDurumuMetni { get; set; } = string.Empty;
        public int GridGelenAdet { get; set; }
        public int TrafoSevkAdet { get; set; }
        public int GridSevkDurumuId { get; set; }
        public string GridSevkDurumuMetni { get; set; } = string.Empty;
        public int? GridSevkMiktari { get; set; }

        // 3K tarafı
        public int UcKKarsilamaTipiId { get; set; }
        public string UcKKarsilamaTipiMetni { get; set; } = string.Empty;
        public int GelenMiktar { get; set; }
        public int KarsilananMiktar { get; set; }
        public int HataliMiktar { get; set; }
        public string? KaynakHedefProjeNo { get; set; }
        public int? GeriGonderilmeSebebiId { get; set; }
        public string? GeriGonderilmeSebebiMetni { get; set; }
        public int GeriGonderilenMiktar { get; set; }
        public string? UcKAciklama { get; set; }

        // Çapraz açıklama: Grid tarafının bu ürüne yazdığı açıklama (3K tarafı görür)
        public string? GridAciklama { get; set; }

        // Parçalı karşılama (Madde 2)
        public int StokKarsilanan { get; set; }
        public int ProjeKarsilanan { get; set; }
        public int ProjeGonderilen { get; set; }
        public int TedarikciKarsilanan { get; set; }
        public int EksikMiktar { get; set; }

        // Hesaplanan
        public int Kalan { get; set; }
        public string KontrolUyari { get; set; } = string.Empty;
        public int GenelDurumId { get; set; }
        public string GenelDurumMetni { get; set; } = string.Empty;
        public bool IsManuelEklenen { get; set; }
    }
}
