using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.DTOs;
using _3K.Core.Enums;

namespace _3K.Application.Features.RolIslemleri.Queries
{
    /// <summary>Tek rolün detayını (menü ağacı + yetkiler) getirir.</summary>
    public class GetRolDetayQuery : IRequest<Result<RolDetayDto>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "rol-yonetimi";
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
        public int RolId { get; set; }
    }
}
