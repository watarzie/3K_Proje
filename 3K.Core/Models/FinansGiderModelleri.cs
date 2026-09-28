using _3K.Core.Enums;

namespace _3K.Core.Models;

public sealed record FinansGiderKaydetModel(
    DateTime Tarih,
    DateTime FinansDonemi,
    int KategoriId,
    int? GiderKalemiId,
    string? AltKategori,
    string? FirmaVeyaKisi,
    string Aciklama,
    decimal Miktar,
    string Birim,
    decimal BirimFiyat,
    string ParaBirimi,
    bool KdvDahil,
    decimal KdvOrani,
    int? ProjeId,
    string? ManuelProjeNo,
    FinansIsTuru? IsTuru,
    string? BelgeNo = null,
    bool AvansMi = false,
    int? MahsupEdilenAvansId = null,
    DateTime? FinansTarihi = null);

public sealed class FinansGiderModel
{
    public int Id { get; init; }
    public DateTime Tarih { get; init; }
    public DateTime FinansTarihi { get; init; }
    public string? BelgeNo { get; init; }
    public bool AvansMi { get; init; }
    public int? MahsupEdilenAvansId { get; init; }
    public DateTime FinansDonemi { get; init; }
    public int KategoriId { get; init; }
    public string Kategori { get; init; } = string.Empty;
    public int? GiderKalemiId { get; init; }
    public string? GiderKalemi { get; init; }
    public string? AltKategori { get; init; }
    public string? FirmaVeyaKisi { get; init; }
    public string Aciklama { get; init; } = string.Empty;
    public decimal Miktar { get; init; }
    public string Birim { get; init; } = string.Empty;
    public decimal BirimFiyat { get; init; }
    public decimal Tutar { get; init; }
    public string ParaBirimi { get; init; } = string.Empty;
    public bool KdvDahil { get; init; }
    public decimal KdvOrani { get; init; }
    public decimal Matrah { get; init; }
    public decimal KdvTutari { get; init; }
    public decimal ToplamTutar { get; init; }
    public int? ProjeId { get; init; }
    public string ProjeNo { get; init; } = string.Empty;
    public FinansIsTuru? IsTuru { get; init; }
    public bool IptalEdildi { get; init; }
    public string? IptalAciklamasi { get; init; }
}

public sealed record FinansGiderKategoriModel(int Id, string Ad, bool Aktif);

public sealed record FinansGiderKategoriKaydetModel(string Ad, bool Aktif);

public sealed record FinansGiderKalemiModel(
    int Id,
    int KategoriId,
    string Kod,
    string Ad,
    bool Aktif,
    string? VarsayilanFirmaVeyaKisi = null,
    decimal? VarsayilanMiktar = null,
    string? VarsayilanBirim = null,
    decimal? VarsayilanBirimFiyat = null,
    string? VarsayilanParaBirimi = null,
    bool VarsayilanKdvDahil = false,
    decimal? VarsayilanKdvOrani = null);

public sealed record FinansGiderKalemiKaydetModel(
    int KategoriId,
    string Kod,
    string Ad,
    bool Aktif,
    string? VarsayilanFirmaVeyaKisi = null,
    decimal? VarsayilanMiktar = null,
    string? VarsayilanBirim = null,
    decimal? VarsayilanBirimFiyat = null,
    string? VarsayilanParaBirimi = null,
    bool VarsayilanKdvDahil = false,
    decimal? VarsayilanKdvOrani = null);
