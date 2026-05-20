using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.OnayIslemleri.DTOs;

namespace _3K.Application.Features.OnayIslemleri.Queries
{
    public class GetBekleyenOnaylarQuery : IRequest<Result<List<OnayBekleyenIslemDto>>>, ISecuredRequest
    {
    }
}
