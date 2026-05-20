using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.SandikIslemleri.DTOs;

namespace _3K.Application.Features.SandikIslemleri.Queries
{
    /// <summary>
    /// Sandık içeriğini getirir — giriş yapmış herkes erişebilir.
    /// </summary>
    public class GetSandikIcerikQuery : IRequest<Result<SandikDetayDto>>, ISecuredRequest
    {
        public int SandikId { get; set; }
    }
}
