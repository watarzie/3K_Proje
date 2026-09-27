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

    public FinansListeFiltre ToModel() => new(
        PageNumber, PageSize, Arama, ProjeId, ProjeNo, IsTuru, Durum,
        Baslangic, Bitis, ParaBirimi, IptalEdilenleriDahilEt, PoNumarasi, TalepEden,
        SiparisDurumu, FaturaDurumu, FaturaBekleyen, FaturalamaBekleyen, FaturaNumarasi, Firma, SandikCinsi, GiderKategoriId);
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
