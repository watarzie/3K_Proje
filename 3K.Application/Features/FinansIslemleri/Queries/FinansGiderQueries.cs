using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries;

public sealed class FinansGiderlerQuery : FinansQuery<FinansSayfaliSonuc<FinansGiderModel>>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.GiderGoruntule;
    public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
    [
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.SekmeGiderler),
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.GiderGoruntule)
    ];
    public FinansListeFiltre Filtre { get; init; } = new();
}
