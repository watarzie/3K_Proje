namespace _3K.Application.Features.UcKIslemleri.DTOs
{
    public class UcKUrunDto
    {
        public int CekiSatiriId { get; set; }
        public int SiraNo { get; set; }
        public string BarkodNo { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public string SandikNo { get; set; } = string.Empty;
        public decimal IstenenAdet { get; set; }
        public string Birim { get; set; } = string.Empty;

        // Grid tarafı (read-only)
        public int GridDurumuId { get; set; }
        public string GridDurumuMetni { get; set; } = string.Empty;
        public decimal GridGelenAdet { get; set; }
        public decimal TrafoSevkAdet { get; set; }
        public int GridSevkDurumuId { get; set; }
        public string GridSevkDurumuMetni { get; set; } = string.Empty;
        public decimal? GridSevkMiktari { get; set; }

        // 3K tarafı
        public int UcKKarsilamaTipiId { get; set; }
        public string UcKKarsilamaTipiMetni { get; set; } = string.Empty;
        public decimal GelenMiktar { get; set; }
        public decimal KarsilananMiktar { get; set; }
        public decimal HataliMiktar { get; set; }
        public string? KaynakHedefProjeNo { get; set; }
        public int? GeriGonderilmeSebebiId { get; set; }
        public string? GeriGonderilmeSebebiMetni { get; set; }
        public decimal GeriGonderilenMiktar { get; set; }
        public string? UcKAciklama { get; set; }

        // Çapraz açıklama: Grid tarafının bu ürüne yazdığı açıklama (3K tarafı görür)
        public string? GridAciklama { get; set; }

        // Parçalı karşılama (Madde 2)
        public decimal StokKarsilanan { get; set; }
        public decimal ProjeKarsilanan { get; set; }
        public decimal ProjeGonderilen { get; set; }
        public decimal NetKullanilabilir { get; set; }
        public bool TransferZinciriVar { get; set; }
        public List<ProjeTransferZincirDto> TransferZinciri { get; set; } = new();
        public decimal TedarikciKarsilanan { get; set; }
        public decimal EksikMiktar { get; set; }

        // Hesaplanan
        public decimal Kalan { get; set; }
        public string KontrolUyari { get; set; } = string.Empty;
        public int GenelDurumId { get; set; }
        public string GenelDurumMetni { get; set; } = string.Empty;

        // Kalite & Süreç (read-only)
        public int? KaliteDurumId { get; set; }
        public string? KaliteDurumMetni { get; set; }
        public int? SurecDurumId { get; set; }
        public string? SurecDurumMetni { get; set; }

        public bool IsManuelEklenen { get; set; }
    }

    public class ProjeTransferZincirDto
    {
        public int Id { get; set; }
        public string Yon { get; set; } = string.Empty;
        public string KaynakProjeNo { get; set; } = string.Empty;
        public string HedefProjeNo { get; set; } = string.Empty;
        public string BarkodNo { get; set; } = string.Empty;
        public string UrunAdi { get; set; } = string.Empty;
        public decimal Miktar { get; set; }
        public string TransferTipi { get; set; } = string.Empty;
        public string Durum { get; set; } = string.Empty;
        public int? ParentTransferId { get; set; }
        public int? RootTransferId { get; set; }
        public int ZincirSeviyesi { get; set; }
        public string? Aciklama { get; set; }
        public DateTime Tarih { get; set; }
    }
}


