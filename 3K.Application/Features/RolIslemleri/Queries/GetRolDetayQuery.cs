using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.DTOs;

namespace _3K.Application.Features.RolIslemleri.Queries
{
    /// <summary>Tek rolün detayını (menü ağacı + yetkiler) getirir.</summary>
    public class GetRolDetayQuery : IRequest<Result<RolDetayDto>>
    {
        public int RolId { get; set; }
    }
}
