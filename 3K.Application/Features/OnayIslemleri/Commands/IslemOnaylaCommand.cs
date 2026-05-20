using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.OnayIslemleri.Commands
{
    public class IslemOnaylaCommand : IRequest<Result>, ISecuredRequest
    {
        public int OnayBekleyenIslemId { get; set; }
    }
}
