using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K_API.Controllers;

public sealed class FinansFilterRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public string? Arama { get; init; }
    public int? ProjeId { get; init; }
    public string? ProjeNo { get; init; }
    public FinansIsTuru? IsTuru { get; init; }
    public FinansIsDurumu? Durum { get; init; }
    public DateTime? Baslangic { get; init; }
    public DateTime? Bitis { get; init; }
    public string? ParaBirimi { get; init; }
    public bool IptalEdilenleriDahilEt { get; init; }
    public string? PoNumarasi { get; init; }
    public string? TalepEden { get; init; }
    public FinansSiparisDurumu? SiparisDurumu { get; init; }
    public FinansFaturaDurumu? FaturaDurumu { get; init; }
    public bool FaturaBekleyen { get; init; }
    public bool FaturalamaBekleyen { get; init; }
    public string? FaturaNumarasi { get; init; }
    public string? Firma { get; init; }
    public AmbalajSandikCinsi? SandikCinsi { get; init; }
    public int? GiderKategoriId { get; init; }
    public FinansKarsilanma? SiparisKarsilanma { get; init; }
    public FinansKarsilanma? FaturaKarsilanma { get; init; }
    /// <summary>Çoklu kategori seçimi: <c>kategoriler=1&amp;kategoriler=3</c>.</summary>
    public FinansIsKategorisi[]? Kategoriler { get; init; }
    /// <summary>Fatura ödeme/vade filtresi (D3).</summary>
    public FinansOdemeFiltresi? OdemeDurumu { get; init; }
    /// <summary>Sandık no içinde geçen metin (büyük/küçük harf duyarsız).</summary>
    public string? SandikNo { get; init; }

    public FinansListeFiltre ToModel() => new(
        PageNumber, PageSize, Arama, ProjeId, ProjeNo, IsTuru, Durum,
        Baslangic, Bitis, ParaBirimi, IptalEdilenleriDahilEt, PoNumarasi, TalepEden,
        SiparisDurumu, FaturaDurumu, FaturaBekleyen, FaturalamaBekleyen, FaturaNumarasi, Firma, SandikCinsi, GiderKategoriId,
        SiparisKarsilanma, FaturaKarsilanma, Kategoriler, OdemeDurumu, SandikNo);
}

/// <summary>PO açma ekranındaki grup seçicisinin sorgu parametreleri.</summary>
public sealed class FinansSiparisGrupRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public string? Arama { get; init; }
    public FinansIsKategorisi[]? Kategoriler { get; init; }
    public DateTime? Baslangic { get; init; }
    public DateTime? Bitis { get; init; }
    public string? ParaBirimi { get; init; }
    public int? ProjeId { get; init; }
    public int[]? IsKaydiIds { get; init; }
    public bool SadeceSiparisBekleyen { get; init; } = true;
    /// <summary>Yalnız bu düzenli işin dönemleri; düzenli işten PO açarken kullanılır.</summary>
    public int? DuzenliIsId { get; init; }

    public FinansSiparisGrupFiltre ToModel() => new(
        PageNumber, PageSize, Arama, Kategoriler, Baslangic, Bitis, ParaBirimi, ProjeId, IsKaydiIds, SadeceSiparisBekleyen, DuzenliIsId);
}

public sealed class FinansProjeSecenekRequest
{
    public string? Arama { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public sealed class FinansIsKayitlariSecimRequest
{
    public IReadOnlyList<int> Ids { get; init; } = Array.Empty<int>();
}

public sealed class FinansDuzenliIsDonemUretRequest
{
    public DateTime BitisTarihi { get; init; }
}
