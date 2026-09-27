using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Commands;

public abstract class FinansCommand<T> : IRequest<Result<T>>, ISecuredRequest, IRequiresMenuPermission, IRequiresMenuPermissions
{
    public abstract string RequiredMenuKod { get; }
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
    public virtual IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions => Array.Empty<MenuPermissionRequirement>();
}

public abstract class FinansCommand : IRequest<Result>, ISecuredRequest, IRequiresMenuPermission, IRequiresMenuPermissions
{
    public abstract string RequiredMenuKod { get; }
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
    public virtual IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions => Array.Empty<MenuPermissionRequirement>();
}

internal static class FinansCommandPermissionRules
{
    public static bool RequiresPriceOverride(FinansIsKaydiKaydetModel? model)
        => model is not null &&
           (model.ManuelBirimFiyat.HasValue || !string.IsNullOrWhiteSpace(model.ParaBirimi) || model.KdvOrani.HasValue);
}
