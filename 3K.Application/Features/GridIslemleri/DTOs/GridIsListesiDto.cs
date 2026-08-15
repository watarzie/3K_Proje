namespace _3K.Application.Features.GridIslemleri.DTOs
{
    public class GridIsListesiDto
    {
        public int Toplam { get; set; }
        public int EksikGelen { get; set; }
        public int YenidenSevkGerekli { get; set; }
        public int BugunGridIslemi { get; set; }
        public GridPagedResultDto<GridIsListesiItemDto> Liste { get; set; } = new();
    }

    public class GridIsListesiItemDto
    {
        public int CekiSatiriId { get; set; }
        public int ProjeId { get; set; }
        public int ProjeTipiId { get; set; }
        public string ProjeNo { get; set; } = string.Empty;
        public string Musteri { get; set; } = string.Empty;
        public string? SandikNo { get; set; }
        public int SiraNo { get; set; }
        public string BarkodNo { get; set; } = string.Empty;
        public string? OlcuResmiPozNo { get; set; }
        public string Aciklama { get; set; } = string.Empty;
        public string Birim { get; set; } = string.Empty;
        public decimal IstenenAdet { get; set; }
        public decimal GridGelenAdet { get; set; }
        public decimal GridEksikMiktar { get; set; }
        public decimal GridSevkMiktari { get; set; }
        public decimal TrafoSevkAdet { get; set; }
        public decimal YenidenSevkGerekliAdet { get; set; }
        public decimal UcKGelenMiktar { get; set; }
        public decimal KalanMiktar { get; set; }
        public int GridDurumuId { get; set; }
        public string GridDurumuMetni { get; set; } = string.Empty;
        public int GridSevkDurumuId { get; set; }
        public string GridSevkDurumuMetni { get; set; } = string.Empty;
        public int UcKDurumuId { get; set; }
        public string UcKDurumuMetni { get; set; } = string.Empty;
        public string? GridAciklama { get; set; }
        public DateTime? GridSevkTarihi { get; set; }
        public DateTime? SonIslemTarihi { get; set; }
        public string IsTipi { get; set; } = string.Empty;
        public string IsTipiMetni { get; set; } = string.Empty;
        public int Oncelik { get; set; }
    }

    public class GridPagedResultDto<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool HasMore { get; set; }
    }
}
