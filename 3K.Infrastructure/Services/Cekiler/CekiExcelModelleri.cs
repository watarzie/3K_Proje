
namespace _3K.Infrastructure.Services.Cekiler;

internal sealed class CiktiImportBilgisi
{
    public string FbNo { get; set; } = string.Empty;
    public string? Musteri { get; set; }
    public string? Lokasyon { get; set; }
    public List<CiktiSatirImportBilgisi> Satirlar { get; set; } = new();
    public Dictionary<string, SandikImportBilgisi> SandikBilgileri { get; set; } = new();
}

internal sealed class CiktiBaslikBilgisi
{
    public string FbNo { get; set; } = string.Empty;
    public string? Musteri { get; set; }
    public string? Lokasyon { get; set; }
}

internal sealed class CiktiSatirImportBilgisi
{
    public int ExcelSatirNo { get; set; }
    public int SiraNo { get; set; }
    public string? OlcuResmiPozNo { get; set; }
    public string BarkodNo { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;
    public string KoliNo { get; set; } = string.Empty;
    public decimal IstenenAdet { get; set; }
    public int BirimId { get; set; }
    public string? Remarks { get; set; }
    public string CheckKodu { get; set; } = string.Empty;
    public bool VeriSatiriMi { get; set; }
}

internal sealed class SandikImportBilgisi
{
    public string SandikNo { get; set; } = string.Empty;
    public string? SandikIsmi { get; set; }
    public string? SandikIsmiIngilizce { get; set; }
    public string? AmbalajCinsi { get; set; }
    public decimal? En { get; set; }
    public decimal? Boy { get; set; }
    public decimal? Yukseklik { get; set; }
    public decimal? NetKg { get; set; }
    public decimal? GrossKg { get; set; }
}
