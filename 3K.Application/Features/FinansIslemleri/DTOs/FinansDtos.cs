using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.DTOs;

public sealed record FinansIptalDto(string Aciklama);

/// <summary>
/// Özel iş ekranının gönderdiği istek. Özel iş, iş türü <see cref="FinansIsTuru.OzelIs"/> olan manuel
/// iş kaydıdır: ekrandaki serbest iş türü <c>OzelIsTuru</c>'na, iş tarihi hem üretim tarihine hem
/// finans dönemine yazılır.
/// </summary>
public sealed record FinansOzelIsKaydetDto(
    string IsTuru,
    string Musteri,
    int? ProjeId,
    string IsAdi,
    string? Aciklama,
    decimal Miktar,
    string Birim,
    DateTime IsTarihi,
    FinansHesaplamaYontemi HesaplamaYontemi,
    string RaporGrubu,
    decimal BirimFiyat,
    string ParaBirimi,
    decimal KdvOrani,
    string? TalepEdenKisi = null,
    string? TalepEdenBolum = null,
    decimal? AhsapM3 = null,
    decimal? KontraM3 = null)
{
    /// <summary>
    /// m³ yönteminde ahşap ve kontra m³ ayrı girilir; birim m³ = bileşik toplam / miktar olur ve fiyat
    /// "m³ × birim fiyat" hesaplanır. Diğer yöntemlerde m³ yoktur.
    /// </summary>
    public FinansIsKaydiKaydetModel IsKaydiModeli()
    {
        var m3Yontemi = HesaplamaYontemi == FinansHesaplamaYontemi.Metrekup;
        var bilesik = m3Yontemi ? FinansHacimKurallari.Bilesik(AhsapM3, KontraM3) : 0m;
        return new(
            ProjeId, null, null, Musteri, FinansIsTuru.OzelIs,
            IsAdi, Aciklama, TalepEdenKisi, TalepEdenBolum, Miktar, Birim,
            m3Yontemi && Miktar > 0 ? decimal.Round(bilesik / Miktar, 6, MidpointRounding.AwayFromZero) : 0m,
            null, BirimFiyat, ParaBirimi, KdvOrani,
            IsTarihi.Date, IsTarihi.Date,
            OzelIsTuru: IsTuru,
            HesaplamaYontemi: HesaplamaYontemi,
            RaporGrubu: RaporGrubu,
            AhsapM3: m3Yontemi ? AhsapM3 ?? 0 : null,
            KontraM3: m3Yontemi ? KontraM3 ?? 0 : null);
    }
}

/// <summary>
/// Münferit İşler sekmesinin "Yeni Münferit İş" isteği. Fiyat alanı yoktur: kayıt seçilen ürünün iş tarihindeki
/// tarifesiyle fiyatlanır. Ürün m³ ile fiyatlanıyorsa ahşap/kontra m³ gönderilir.
/// </summary>
public sealed record FinansMunferitIsKaydetDto(
    int FinansUrunId,
    int? ProjeId,
    string? ProjeAdi,
    DateTime IsTarihi,
    decimal Adet = 1,
    decimal? AhsapM3 = null,
    decimal? KontraM3 = null)
{
    public FinansMunferitIsKaydetModel MunferitIsModeli() => new(
        FinansUrunId, ProjeId, string.IsNullOrWhiteSpace(ProjeAdi) ? null : ProjeAdi.Trim(), IsTarihi.Date, Adet, AhsapM3, KontraM3);
}

/// <summary>Kayıt detayındaki "Hacim" bölümünün isteği: ahşap ve kontra m³ ile gerekçe.</summary>
public sealed record FinansHacimGirDto(decimal AhsapM3, decimal KontraM3, string? Aciklama = null)
{
    public FinansHacimModel HacimModeli() => new(AhsapM3, KontraM3, Aciklama);
}

/// <summary>
/// Gider ekranının gönderdiği istek. Boş bırakılan alanlar gider varsayılanlarıyla tamamlanır: finans
/// dönemi gider tarihinin ayı, miktar 1, birim "Tutar", birim fiyat girilen tutar.
/// </summary>
public sealed record FinansGiderKaydetDto(
    DateTime Tarih,
    int KategoriId,
    string? AltKategori,
    string? FirmaVeyaKisi,
    string Aciklama,
    decimal Tutar,
    string ParaBirimi,
    bool KdvDahil,
    decimal KdvOrani,
    int? ProjeId,
    FinansIsTuru? IsTuru,
    decimal? Miktar = null,
    string? Birim = null,
    decimal? BirimFiyat = null,
    int? GiderKalemiId = null,
    DateTime? FinansDonemi = null,
    string? BelgeNo = null,
    bool AvansMi = false,
    int? MahsupEdilenAvansId = null,
    DateTime? FinansTarihi = null,
    int? FinansSiparisId = null)
{
    public FinansGiderKaydetModel GiderModeli() => new(
        Tarih, FinansDonemi ?? FinansTarihKurallari.AyBasi(Tarih),
        KategoriId, GiderKalemiId, AltKategori, FirmaVeyaKisi,
        Aciklama, Miktar ?? 1m, Birim ?? "Tutar", BirimFiyat ?? Tutar, ParaBirimi,
        KdvDahil, KdvOrani, ProjeId, null, IsTuru, BelgeNo, AvansMi, MahsupEdilenAvansId, FinansTarihi, FinansSiparisId);
}

/// <summary>
/// Düzenli iş ekranının gönderdiği istek. Düzenli işler özel iş türünde ve ürünsüz tanımlanır; ekrandaki
/// serbest iş türü <c>OzelIsTuru</c>'na yazılır. Tekrar sıklığı her zaman aylıktır; gönderilen değer
/// kullanılmaz.
/// </summary>
public sealed record FinansDuzenliIsKaydetDto(
    int? ProjeId,
    string IsAdi,
    string IsTuru,
    string Musteri,
    string? Aciklama,
    string TekrarSikligi,
    DateTime BaslangicTarihi,
    DateTime? BitisTarihi,
    int OlusturmaGunu,
    decimal Miktar,
    string Birim,
    decimal BirimFiyat,
    string ParaBirimi,
    decimal KdvOrani,
    bool Aktif,
    FinansHesaplamaYontemi HesaplamaYontemi = FinansHesaplamaYontemi.DegiskenAdet,
    string RaporGrubu = "Özel İş")
{
    public FinansDuzenliIsKaydetModel DuzenliIsModeli() => new(
        ProjeId, null, null, IsAdi, FinansIsTuru.OzelIs,
        Musteri, Aciklama, BaslangicTarihi, BitisTarihi,
        OlusturmaGunu, Miktar, Birim, null, BirimFiyat,
        ParaBirimi, KdvOrani, Aktif, IsTuru,
        HesaplamaYontemi, RaporGrubu);
}
