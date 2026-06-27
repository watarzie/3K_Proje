using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.OnayIslemleri.Commands
{
    public class IslemOnaylaCommand : IRequest<Result>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "islem-onay-merkezi";
        public int OnayBekleyenIslemId { get; set; }
    }
}
