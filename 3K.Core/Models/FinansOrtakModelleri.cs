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
    int? GiderKategoriId = null);

public sealed record FinansParaToplamiModel(
    string ParaBirimi,
    decimal NetTutar,
    decimal KdvTutari,
    decimal ToplamTutar);
