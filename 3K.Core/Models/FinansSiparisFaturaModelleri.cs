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

/// <summary>
/// Proje × kategori (veya düzenli iş) grubuna girilen PO tutarı. Tutar sunucuda
/// <see cref="FinansDagitimKurallari"/> ile grubun kalemlerine dağıtılır.
/// </summary>
public sealed record FinansSiparisGrupDagitimModel(IReadOnlyList<int> IsKaydiIds, decimal NetTutar);

/// <summary>
/// Doğrudan girilen PO. PO bir belge yüklemesine bağlı değildir; PDF isteğe bağlı olarak sonradan
/// eklenebilir. Kalemler tek tek (<see cref="Kalemler"/>) veya grup düzeyinde (<see cref="Gruplar"/>) dağıtılır.
/// </summary>
public sealed record FinansSiparisOlusturModel(
    string PoNumarasi,
    DateTime SiparisTarihi,
    string? Aciklama,
    IReadOnlyList<FinansSiparisDagitimModel> Kalemler,
    string? ParaBirimi = null,
    decimal? BelgeNetTutar = null,
    IReadOnlyList<FinansSiparisGrupDagitimModel>? Gruplar = null);

/// <summary>Mevcut aktif PO'ya yeni iş kalemi veya aynı işe ek tutar bağlama isteği.</summary>
public sealed record FinansSiparisKalemEkleModel(
    IReadOnlyList<FinansSiparisDagitimModel>? Kalemler = null,
    IReadOnlyList<FinansSiparisGrupDagitimModel>? Gruplar = null);

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
    public string? ParaBirimi { get; init; }
    public IReadOnlyList<FinansParaToplamiModel> Tutarlar { get; init; } = Array.Empty<FinansParaToplamiModel>();
    /// <summary>Aktif faturalardaki faturalanan net/KDV/brüt, para birimi bazında.</summary>
    public IReadOnlyList<FinansParaToplamiModel> FaturalananTutarlar { get; init; } = Array.Empty<FinansParaToplamiModel>();
    /// <summary>PO ile ilişkilendirilmiş, iptal edilmemiş ve avans olmayan giderler; para birimi bazında.</summary>
    public IReadOnlyList<FinansParaToplamiModel> Giderler { get; init; } = Array.Empty<FinansParaToplamiModel>();
    /// <summary>Para birimi bazında PO net sonucu: faturalanan net − ilişkili net gider (tutarlar çevrilmez).</summary>
    public IReadOnlyList<FinansPoNetModel> Netler { get; init; } = Array.Empty<FinansPoNetModel>();
    public IReadOnlyList<string> FaturaNumaralari { get; init; } = Array.Empty<string>();
    public IReadOnlyList<FinansSiparisKalemiModel> Kalemler { get; init; } = Array.Empty<FinansSiparisKalemiModel>();
    public bool IptalEdildi { get; init; }
    public string? IptalAciklamasi { get; init; }
    public DateTime CreatedDate { get; init; }
    public string? CreatedBy { get; init; }
}

/// <summary>PO'nun tek para birimindeki sipariş, faturalanan, gider ve net sonucu.</summary>
public sealed record FinansPoNetModel(
    string ParaBirimi,
    decimal SiparisNetTutar,
    decimal FaturalananNetTutar,
    decimal Gider,
    decimal Net,
    decimal TahminiKar);

public sealed record class FinansSiparisKalemiModel
{
    public int Id { get; init; }
    public int IsKaydiId { get; init; }
    public int? ProjeId { get; init; }
    public string ProjeNo { get; init; } = string.Empty;
    public string Musteri { get; init; } = string.Empty;
    public string IsAdi { get; init; } = string.Empty;
    public FinansIsKategorisi Kategori { get; init; }
    public int? DuzenliIsId { get; init; }
    public DateTime FinansTarihi { get; init; }
    /// <summary>İşin toplam net bedeli (bütün PO'lar için).</summary>
    public decimal IsBedeli { get; init; }
    /// <summary>İşin henüz hiçbir PO'ya bağlanmamış net tutarı.</summary>
    public decimal KalanSiparisNetTutar { get; init; }
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

/// <summary>
/// PO'nun proje × kategori grubuna girilen fatura net tutarı; grubun PO kalemlerine
/// <see cref="FinansDagitimKurallari"/> ile dağıtılır.
/// </summary>
public sealed record FinansFaturaGrupKalemiModel(IReadOnlyList<int> SiparisKalemiIds, decimal NetTutar);

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
    string? MutabakatAciklamasi = null,
    IReadOnlyList<FinansFaturaGrupKalemiModel>? Gruplar = null,
    DateTime? VadeTarihi = null);

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
    string? Gerekce = null,
    DateTime? VadeTarihi = null);

/// <summary>Faturanın ödendi/ödenmedi işareti (D3); ödeme tarihi işaretlenirken zorunludur.</summary>
public sealed record FinansOdemeKaydetModel(bool Odendi, DateTime? OdemeTarihi, string? Aciklama);

/// <summary>Vadesi yaklaşan veya geçen ödenmemiş fatura (uyarı listesi satırı).</summary>
public sealed record FinansVadeUyarisiModel(
    int FaturaId,
    string FaturaNumarasi,
    string PoNumarasi,
    string ProjeNo,
    DateTime FaturaTarihi,
    DateTime VadeTarihi,
    int KalanGun,
    FinansOdemeDurumu OdemeDurumu,
    string ParaBirimi,
    decimal ToplamTutar);

/// <summary>Vade uyarıları: uyarı penceresi, vadesi geçen ve yaklaşan fatura sayıları ve en acil kalemler.</summary>
public sealed record FinansVadeUyarilariModel(
    int VadeUyariGunu,
    int VadesiGecenAdet,
    int YaklasanAdet,
    IReadOnlyList<FinansVadeUyarisiModel> Kalemler);

public sealed record FinansFaturaKalemiModel(int Id, int SiparisKalemiId, int IsKaydiId,
    decimal NetTutar, decimal KdvTutari, decimal ToplamTutar, string ParaBirimi, bool TutarBazli,
    string ProjeNo = "", string IsAdi = "", FinansIsKategorisi Kategori = FinansIsKategorisi.AnaAmbalaj);

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
    public DateTime VadeTarihi { get; init; }
    public bool OdendiMi { get; init; }
    public DateTime? OdemeTarihi { get; init; }
    public string? OdemeAciklamasi { get; init; }
    public FinansOdemeDurumu OdemeDurumu { get; init; }
    /// <summary>Ödenmemişte vadeye kalan gün (negatif: gecikme); ödenende vadeden kaç gün sonra ödendiği.</summary>
    public int VadeFarkiGun { get; init; }
    public bool IptalEdildi { get; init; }
    public DateTime CreatedDate { get; init; }
    public string? CreatedBy { get; init; }
}
