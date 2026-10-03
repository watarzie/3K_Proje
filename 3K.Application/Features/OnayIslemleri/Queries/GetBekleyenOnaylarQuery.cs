using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.OnayIslemleri.DTOs;
using _3K.Core.Enums;

namespace _3K.Application.Features.OnayIslemleri.Queries
{
    public class GetBekleyenOnaylarQuery : IRequest<Result<List<OnayBekleyenIslemDto>>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "islem-onay-merkezi";
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
    }
}
