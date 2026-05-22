using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.LookupIslemleri.DTOs;

namespace _3K.Application.Features.LookupIslemleri.Commands
{
    public class DepoLokasyonOlusturCommand : IRequest<Result<LookupItemDto>>, ISecuredRequest
    {
        public string Deger { get; set; } = string.Empty;
    }
}
