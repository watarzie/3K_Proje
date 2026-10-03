using _3K.Core.Enums;

namespace _3K.Core.Models;

public sealed record FinansDuzenliIsKaydetModel(
    int? ProjeId,
    string? ManuelProjeNo,
    string? ManuelProjeAdi,
    string IsAdi,
    FinansIsTuru IsTuru,
    string Musteri,
    string? Aciklama,
    DateTime BaslangicTarihi,
    DateTime? BitisTarihi,
    int OlusturmaGunu,
    decimal Miktar,
    string Birim,
    int? FinansUrunId,
    decimal BirimFiyat,
    string ParaBirimi,
    decimal KdvOrani,
    bool Aktif,
    string? OzelIsTuru = null,
    FinansHesaplamaYontemi HesaplamaYontemi = FinansHesaplamaYontemi.DegiskenAdet,
    string RaporGrubu = "Özel İş");

public sealed class FinansDuzenliIsModel
{
    public int Id { get; init; }
    public int? ProjeId { get; init; }
    public string? ManuelProjeNo { get; init; }
    public string IsAdi { get; init; } = string.Empty;
    public string IsTuru { get; init; } = string.Empty;
    public FinansHesaplamaYontemi HesaplamaYontemi { get; init; }
    public string RaporGrubu { get; init; } = "Özel İş";
    public string Musteri { get; init; } = string.Empty;
    public string? Aciklama { get; init; }
    public string TekrarSikligi { get; init; } = "Aylık";
    public DateTime BaslangicTarihi { get; init; }
    public DateTime? BitisTarihi { get; init; }
    public int OlusturmaGunu { get; init; }
    public decimal Miktar { get; init; }
    public string Birim { get; init; } = string.Empty;
    public int? FinansUrunId { get; init; }
    public decimal BirimFiyat { get; init; }
    public string ParaBirimi { get; init; } = string.Empty;
    public decimal KdvOrani { get; init; }
    public bool Aktif { get; init; }
}

public sealed record FinansDonemOlusturSonucModel(int Taranan, int Olusturulan, DateTime ReferansTarihi)
{
    /// <summary>İş kuralına takıldığı için bu turda üretilemeyen şablonlar ("#id ad: neden").</summary>
    public IReadOnlyList<string> Hatalar { get; init; } = [];
}

public sealed record FinansUrunEslesmesiModel(
    int? Id,
    FinansIsTuru IsTuru,
    string? SandikAdi,
    string? SandikTipi,
    decimal? Boy,
    decimal? En,
    decimal? Yukseklik,
    int? IcSandikSablonId,
    bool Aktif = true);

public sealed class FinansUrunModel
{
    public int Id { get; init; }
    public string Kod { get; init; } = string.Empty;
    public string Ad { get; init; } = string.Empty;
    public FinansFiyatlandirmaBirimi FiyatlandirmaBirimi { get; init; }
    public bool Aktif { get; init; }
    public int Sira { get; init; }
    public decimal? GuncelBirimFiyat { get; init; }
    public string? GuncelParaBirimi { get; init; }
    public decimal? GuncelKdvOrani { get; init; }
    /// <summary>Bugün geçerli tarife sürümünün başlangıç ve bitiş tarihi.</summary>
    public DateTime? GuncelGecerlilikBaslangici { get; init; }
    public DateTime? GuncelGecerlilikBitisi { get; init; }
    /// <summary>Ürünün aktif tarife sürümleri (eski → yeni); fiyat geçmişi bu sürümlerde korunur.</summary>
    public IReadOnlyList<FinansTarifeSurumuModel> Tarifeler { get; init; } = Array.Empty<FinansTarifeSurumuModel>();
    public decimal BirimFiyat => GuncelBirimFiyat ?? 0m;
    public string ParaBirimi => GuncelParaBirimi ?? "EUR";
    public decimal KdvOrani => GuncelKdvOrani ?? 0m;
    public IReadOnlyList<FinansUrunEslesmesiModel> Eslesmeler { get; init; } = Array.Empty<FinansUrunEslesmesiModel>();
}

/// <summary>
/// Ürün/tarife kaydı. Fiyat verilirse <see cref="GecerlilikBaslangici"/> tarihinden (boşsa bugünden)
/// itibaren geçerli yeni tarife sürümü açılır; önceki sürüm kapatılır, geçmiş sürümler değişmez.
/// </summary>
public sealed record FinansUrunKaydetModel(
    string Kod,
    string Ad,
    FinansFiyatlandirmaBirimi FiyatlandirmaBirimi,
    bool Aktif,
    int Sira,
    IReadOnlyList<FinansUrunEslesmesiModel> Eslesmeler,
    decimal? BirimFiyat = null,
    string? ParaBirimi = null,
    decimal? KdvOrani = null,
    DateTime? GecerlilikBaslangici = null);

public sealed record FinansTarifeSurumuModel(
    int Id,
    DateTime GecerlilikBaslangici,
    DateTime GecerlilikBitisi,
    decimal BirimFiyat,
    string ParaBirimi,
    decimal KdvOrani);

/// <summary>Para birimi tanımı (F1); yeni para birimi eklendiğinde kartlar, grafikler, filtreler ve raporlar kendiliğinden gösterir.</summary>
public sealed record FinansParaBirimiModel(int Id, string Kod, string Ad, string? Sembol, bool Aktif, int Sira);

public sealed record FinansParaBirimiKaydetModel(string Kod, string Ad, string? Sembol, bool Aktif, int Sira);

/// <summary>Finans parametreleri (D3): faturanın varsayılan vade günü ve vade uyarısının kaç gün önceden verileceği.</summary>
public sealed record FinansParametrelerModel(int VarsayilanVadeGunu, int VadeUyariGunu);

/// <summary>D4 yeniden fiyatlama sonucu: kapsamdaki açık kalem sayısı ve fiyatı değişen kalem sayısı.</summary>
public sealed record FinansYenidenFiyatlamaSonucModel(int UrunId, int IncelenenKalem, int FiyatlananKalem);
