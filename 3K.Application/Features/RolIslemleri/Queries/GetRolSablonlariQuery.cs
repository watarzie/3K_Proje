using MediatR;
using _3K.Application.Common;
using _3K.Core.Models;

namespace _3K.Application.Features.RolIslemleri.Queries;

public sealed class GetRolSablonlariQuery : IRequest<Result<IReadOnlyList<RolSablonu>>>, ISecuredRequest, IRequiresMenuPermission
{
    public string RequiredMenuKod => "rol-yonetimi";
}

public sealed class GetRolSablonlariQueryHandler : IRequestHandler<GetRolSablonlariQuery, Result<IReadOnlyList<RolSablonu>>>
{
    public Task<Result<IReadOnlyList<RolSablonu>>> Handle(GetRolSablonlariQuery request, CancellationToken cancellationToken)
        => Task.FromResult(Result<IReadOnlyList<RolSablonu>>.Success(RolSablonlari.Tum));
}
