using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.LookupIslemleri.Commands
{
    public class DepoLokasyonSilCommand : IRequest<Result>, ISecuredRequest
    {
        public int Id { get; set; }
    }
}
