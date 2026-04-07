using MediatR;
using _3K.Application.Common;
using _3K.Application.DTOs;

namespace _3K.Application.Features.SandikIslemleri.Queries
{
    /// <summary>
    /// Sandık içeriğini getirir — giriş yapmış herkes erişebilir.
    /// </summary>
    public class GetSandikIcerikQuery : IRequest<Result<SandikDetayDto>>, ISecuredRequest
    {
        public string[] RequiredRoles => Array.Empty<string>();
        public int SandikId { get; set; }
    }
}
