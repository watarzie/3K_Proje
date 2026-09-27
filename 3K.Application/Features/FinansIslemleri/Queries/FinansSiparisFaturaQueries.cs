using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries;

public sealed class FinansSiparislerQuery : FinansQuery<FinansSayfaliSonuc<FinansSiparisModel>>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.KayitGoruntule;
    public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
    [
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.KayitGoruntule)
    ];
    public FinansListeFiltre Filtre { get; init; } = new();
}

public sealed class FinansSiparisGetirQuery : FinansQuery<FinansSiparisModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.KayitGoruntule;
    public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
    [
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.KayitGoruntule)
    ];
    public int Id { get; init; }
}

public sealed class FinansFaturalarQuery : FinansQuery<FinansSayfaliSonuc<FinansFaturaModel>>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.KayitGoruntule;
    public FinansListeFiltre Filtre { get; init; } = new();
}

public sealed class FinansFaturaGetirQuery : FinansQuery<FinansFaturaModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.KayitGoruntule;
    public int Id { get; init; }
}
