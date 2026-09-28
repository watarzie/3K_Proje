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
    public decimal BirimFiyat => GuncelBirimFiyat ?? 0m;
    public string ParaBirimi => GuncelParaBirimi ?? "EUR";
    public decimal KdvOrani => GuncelKdvOrani ?? 0m;
    public IReadOnlyList<FinansUrunEslesmesiModel> Eslesmeler { get; init; } = Array.Empty<FinansUrunEslesmesiModel>();
}

public sealed record FinansUrunKaydetModel(
    string Kod,
    string Ad,
    FinansFiyatlandirmaBirimi FiyatlandirmaBirimi,
    bool Aktif,
    int Sira,
    IReadOnlyList<FinansUrunEslesmesiModel> Eslesmeler,
    decimal? BirimFiyat = null,
    string? ParaBirimi = null,
    decimal? KdvOrani = null);

public sealed record FinansSablonAlanModel(string Kod, string Ad, string VeriTuru, bool Zorunlu);

public sealed record FinansSablonKaydetModel(string Kod, string Ad, bool Aktif, IReadOnlyList<FinansSablonAlanModel> Alanlar);

public sealed record FinansSablonModel(int Id, string Kod, string Ad, bool Aktif, int SurumId, int Surum, IReadOnlyList<FinansSablonAlanModel> Alanlar);
