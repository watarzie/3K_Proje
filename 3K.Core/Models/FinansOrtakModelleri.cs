using _3K.Core.Enums;

namespace _3K.Core.Models;

public sealed class FinansSayfaliSonuc<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    /// <summary>
    /// Sayfadaki satırlardan değil, uygulanan filtrenin tamamından hesaplanan
    /// para birimi bazlı toplamlar. Toplam gerektirmeyen listelerde boş döner.
    /// </summary>
    public IReadOnlyList<FinansParaToplamiModel> Toplamlar { get; init; } = Array.Empty<FinansParaToplamiModel>();
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

public sealed record FinansListeFiltre(
    int PageNumber = 1,
    int PageSize = 25,
    string? Arama = null,
    int? ProjeId = null,
    string? ProjeNo = null,
    FinansIsTuru? IsTuru = null,
    FinansIsDurumu? Durum = null,
    DateTime? Baslangic = null,
    DateTime? Bitis = null,
    string? ParaBirimi = null,
    bool IptalEdilenleriDahilEt = false,
    string? PoNumarasi = null,
    string? TalepEden = null,
    FinansSiparisDurumu? SiparisDurumu = null,
    FinansFaturaDurumu? FaturaDurumu = null,
    bool FaturaBekleyen = false,
    bool FaturalamaBekleyen = false,
    string? FaturaNumarasi = null,
    string? Firma = null,
    AmbalajSandikCinsi? SandikCinsi = null,
    int? GiderKategoriId = null,
    FinansKarsilanma? SiparisKarsilanma = null,
    FinansKarsilanma? FaturaKarsilanma = null,
    IReadOnlyList<FinansIsKategorisi>? Kategoriler = null,
    FinansOdemeFiltresi? OdemeDurumu = null);

/// <summary>PO açma ekranındaki proje × kategori / düzenli iş seçici filtresi.</summary>
public sealed record FinansSiparisGrupFiltre(
    int PageNumber = 1,
    int PageSize = 25,
    string? Arama = null,
    IReadOnlyList<FinansIsKategorisi>? Kategoriler = null,
    DateTime? Baslangic = null,
    DateTime? Bitis = null,
    string? ParaBirimi = null,
    int? ProjeId = null,
    IReadOnlyList<int>? IsKaydiIds = null,
    bool SadeceSiparisBekleyen = true,
    int? DuzenliIsId = null);

/// <summary>
/// PO'ya bağlanabilecek grup: üretim işleri proje × kategori × para birimi, düzenli işler iş × para birimi,
/// özel işler tek iş olarak gruplanır. Sandıklar satır değildir; <see cref="IsKaydiIds"/> dağıtım sırasıdır.
/// </summary>
public sealed record class FinansSiparisGrubuModel
{
    public string Anahtar { get; init; } = string.Empty;
    public int? ProjeId { get; init; }
    public string ProjeNo { get; init; } = string.Empty;
    public string Musteri { get; init; } = string.Empty;
    public FinansIsKategorisi Kategori { get; init; }
    public FinansIsTuru IsTuru { get; init; }
    public int? DuzenliIsId { get; init; }
    public string Ad { get; init; } = string.Empty;
    public string ParaBirimi { get; init; } = string.Empty;
    public DateTime IlkTarih { get; init; }
    public DateTime SonTarih { get; init; }
    public int KalemSayisi { get; init; }
    public IReadOnlyList<int> IsKaydiIds { get; init; } = Array.Empty<int>();
    public decimal NetTutar { get; init; }
    public decimal SiparisNetTutar { get; init; }
    public decimal KalanSiparisNetTutar { get; init; }
    public decimal FaturalananNetTutar { get; init; }
    public decimal AhsapM3 { get; init; }
    public decimal KontraM3 { get; init; }
    public FinansKarsilanma SiparisKarsilanma { get; init; }
    public FinansKarsilanma FaturaKarsilanma { get; init; }
    public int FiyatBekleyenKalem { get; init; }
}

public sealed record FinansParaToplamiModel(
    string ParaBirimi,
    decimal NetTutar,
    decimal KdvTutari,
    decimal ToplamTutar);
