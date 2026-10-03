using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.FinansIslemleri.Queries;

public abstract class FinansQuery<T> : IRequest<Result<T>>, ISecuredRequest, IRequiresMenuPermission, IRequiresMenuPermissions
{
    public abstract string RequiredMenuKod { get; }
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
    public virtual IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions => Array.Empty<MenuPermissionRequirement>();
}
