using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SandikLokasyonGuncelleCommand : IRequest<Result<bool>>, ISecuredRequest
    {
        public List<int> SandikIds { get; set; } = new();
        public int DepoLokasyonId { get; set; }
        
        public string[] RequiredRoles => Array.Empty<string>();
    }
}
