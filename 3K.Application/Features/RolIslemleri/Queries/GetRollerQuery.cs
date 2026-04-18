using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.DTOs;

namespace _3K.Application.Features.RolIslemleri.Queries
{
    /// <summary>Tüm rolleri listeler.</summary>
    public class GetRollerQuery : IRequest<Result<List<RolDto>>> { }
}
