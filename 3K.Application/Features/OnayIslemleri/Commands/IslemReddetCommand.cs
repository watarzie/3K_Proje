using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.OnayIslemleri.Commands
{
    public class IslemReddetCommand : IRequest<Result>, ISecuredRequest
    {
        public int OnayBekleyenIslemId { get; set; }
        public string RedAciklamasi { get; set; } = string.Empty;
    }
}
