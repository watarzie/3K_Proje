using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.OnayIslemleri.Queries
{
    public class GetBekleyenSayisiQuery : IRequest<Result<int>>, ISecuredRequest
    {
    }
}
