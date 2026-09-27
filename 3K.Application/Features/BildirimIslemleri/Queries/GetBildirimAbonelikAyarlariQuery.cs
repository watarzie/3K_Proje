using _3K.Application.Common;
using _3K.Application.Features.BildirimIslemleri.DTOs;
using MediatR;
using _3K.Core.Enums;

namespace _3K.Application.Features.BildirimIslemleri.Queries
{
    public class GetBildirimAbonelikAyarlariQuery
        : IRequest<Result<List<BildirimAbonelikAyariDto>>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "kullanicilar";
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
    }
}
