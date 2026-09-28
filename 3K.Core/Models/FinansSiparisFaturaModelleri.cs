using _3K.Core.Enums;

namespace _3K.Core.Models;

public sealed record FinansSiparisDagitimModel(
    int IsKaydiId,
    decimal Adet,
    decimal M3,
    int? FinansUrunId = null,
    decimal? BirimFiyat = null,
    string? ParaBirimi = null,
    decimal? KdvOrani = null,
    decimal? NetTutar = null);

public sealed record FinansSiparisOlusturModel(
    string PoNumarasi,
    DateTime SiparisTarihi,
    string? Aciklama,
    IReadOnlyList<FinansSiparisDagitimModel> Kalemler,
    string? ParaBirimi = null,
    decimal? BelgeNetTutar = null);

public sealed record FinansSiparisGuncelleModel(
    string PoNumarasi,
    DateTime SiparisTarihi,
    string? Aciklama,
    IReadOnlyList<FinansSiparisDagitimModel>? Kalemler = null,
    string? Gerekce = null);

public sealed record class FinansSiparisModel
{
    public int Id { get; init; }
    public string KayitNo { get; init; } = string.Empty;
    public string PoNumarasi { get; init; } = string.Empty;
    public string ProjeNo { get; init; } = string.Empty;
    public string Musteri { get; init; } = string.Empty;
    public IReadOnlyList<string> IsTurleri { get; init; } = Array.Empty<string>();
    public DateTime SiparisTarihi { get; init; }
    public decimal SandikAdedi { get; init; }
    public decimal ToplamM3 { get; init; }
    public decimal FaturalananAdet { get; init; }
    public decimal FaturalananM3 { get; init; }
    public decimal KalanAdet { get; init; }
    public decimal KalanM3 { get; init; }
    public FinansSiparisDurumu Durum { get; init; }
    public string? Aciklama { get; init; }
    public IReadOnlyList<FinansParaToplamiModel> Tutarlar { get; init; } = Array.Empty<FinansParaToplamiModel>();
    public IReadOnlyList<FinansSiparisKalemiModel> Kalemler { get; init; } = Array.Empty<FinansSiparisKalemiModel>();
    public bool IptalEdildi { get; init; }
    public DateTime CreatedDate { get; init; }
    public string? CreatedBy { get; init; }
}

public sealed record class FinansSiparisKalemiModel
{
    public int Id { get; init; }
    public int IsKaydiId { get; init; }
    public string SandikNo { get; init; } = string.Empty;
    public string SandikAdi { get; init; } = string.Empty;
    public FinansIsTuru IsTuru { get; init; }
    public decimal Adet { get; init; }
    public decimal M3 { get; init; }
    public decimal FaturalananAdet { get; init; }
    public decimal FaturalananM3 { get; init; }
    public decimal KalanAdet { get; init; }
    public decimal KalanM3 { get; init; }
    public int? FinansUrunId { get; init; }
    public string UrunKodu { get; init; } = string.Empty;
    public string UrunAdi { get; init; } = string.Empty;
    public FinansFiyatlandirmaBirimi FiyatlandirmaBirimi { get; init; }
    public decimal FiyatlandirmaMiktari { get; init; }
    public decimal BirimFiyat { get; init; }
    public string ParaBirimi { get; init; } = string.Empty;
    public decimal KdvOrani { get; init; }
    public decimal NetTutar { get; init; }
    public decimal KdvTutari { get; init; }
    public decimal ToplamTutar { get; init; }
    public decimal FaturalananNetTutar { get; init; }
    public decimal KalanFaturaNetTutar { get; init; }
    public bool TutarBazli { get; init; }
    public bool FiyatManuelDegistirildi { get; init; }
}

public sealed record FinansFaturaKalemiOlusturModel(int SiparisKalemiId, decimal Adet, decimal M3, decimal? NetTutar = null);

public sealed record FinansFaturaOlusturModel(
    int SiparisId,
    string FaturaNumarasi,
    DateTime FaturaTarihi,
    string? Aciklama,
    IReadOnlyList<FinansFaturaKalemiOlusturModel> Kalemler,
    string? BelgeParaBirimi = null,
    decimal? BelgeNetTutar = null,
    decimal? BelgeKdvTutari = null,
    decimal? BelgeToplamTutar = null,
    string? MutabakatAciklamasi = null);

public sealed record FinansFaturaGuncelleModel(
    string FaturaNumarasi,
    DateTime FaturaTarihi,
    string? Aciklama,
    string? BelgeParaBirimi = null,
    decimal? BelgeNetTutar = null,
    decimal? BelgeKdvTutari = null,
    decimal? BelgeToplamTutar = null,
    string? MutabakatAciklamasi = null,
    bool BelgeMutabakatiniKoru = false,
    IReadOnlyList<FinansFaturaKalemiOlusturModel>? Kalemler = null,
    string? Gerekce = null);

public sealed record FinansFaturaKalemiModel(int Id, int SiparisKalemiId, int IsKaydiId,
    decimal NetTutar, decimal KdvTutari, decimal ToplamTutar, string ParaBirimi, bool TutarBazli);

public sealed record class FinansFaturaModel
{
    public IReadOnlyList<FinansFaturaKalemiModel> Kalemler { get; init; } = Array.Empty<FinansFaturaKalemiModel>();
    public int Id { get; init; }
    public string KayitNo { get; init; } = string.Empty;
    public string FaturaNumarasi { get; init; } = string.Empty;
    public DateTime FaturaTarihi { get; init; }
    public int SiparisId { get; init; }
    public string PoNumarasi { get; init; } = string.Empty;
    public string ProjeNo { get; init; } = string.Empty;
    public IReadOnlyList<string> IsTurleri { get; init; } = Array.Empty<string>();
    public decimal SandikAdedi { get; init; }
    public decimal ToplamM3 { get; init; }
    public FinansFaturaDurumu Durum { get; init; }
    public string? Aciklama { get; init; }
    public IReadOnlyList<FinansParaToplamiModel> Tutarlar { get; init; } = Array.Empty<FinansParaToplamiModel>();
    public string? BelgeParaBirimi { get; init; }
    public decimal? BelgeNetTutar { get; init; }
    public decimal? BelgeKdvTutari { get; init; }
    public decimal? BelgeToplamTutar { get; init; }
    public decimal MutabakatFarki { get; init; }
    public string? MutabakatAciklamasi { get; init; }
    public bool IptalEdildi { get; init; }
    public DateTime CreatedDate { get; init; }
    public string? CreatedBy { get; init; }
}
