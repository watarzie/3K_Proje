using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries;

public sealed class FinansDuzenliIslerQuery : FinansQuery<FinansSayfaliSonuc<FinansDuzenliIsModel>>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.DuzenliIsYonet;
    public bool SadeceAktif { get; init; }
    public string? Arama { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public sealed class FinansGiderKategorileriQuery : FinansQuery<IReadOnlyList<FinansGiderKategoriModel>>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.GiderGoruntule;
    public bool SadeceAktif { get; init; }
}

public sealed class FinansGiderKalemleriQuery : FinansQuery<IReadOnlyList<FinansGiderKalemiModel>>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.GiderGoruntule;
    public int? KategoriId { get; init; }
    public bool SadeceAktif { get; init; }
}

public sealed class FinansGiderKutuphaneKategorileriQuery : FinansQuery<IReadOnlyList<FinansGiderKategoriModel>>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.GiderKutuphanesiYonet;
    public bool SadeceAktif { get; init; }
}

public sealed class FinansGiderKutuphaneKalemleriQuery : FinansQuery<IReadOnlyList<FinansGiderKalemiModel>>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.GiderKutuphanesiYonet;
    public int? KategoriId { get; init; }
    public bool SadeceAktif { get; init; }
}

public sealed class FinansUrunlerQuery : FinansQuery<FinansSayfaliSonuc<FinansUrunModel>>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.IsKutuphanesiYonet;
    public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
    [
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.IsKutuphanesiYonet),
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.BirimFiyatGoruntule)
    ];
    public bool SadeceAktif { get; init; }
    public DateTime? TarifeTarihi { get; init; }
    public string? Arama { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public sealed class FinansSablonlarQuery : FinansQuery<IReadOnlyList<FinansSablonModel>>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.KayitGoruntule;
}
