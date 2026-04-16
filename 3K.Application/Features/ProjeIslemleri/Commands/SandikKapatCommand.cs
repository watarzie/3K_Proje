using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    /// <summary>
    /// Sandık Kapat/Aç — Sadece Admin tarafından yapılabilir.
    /// </summary>
    public class SandikKapatCommand : IRequest<Result<bool>>, ISecuredRequest
    {
        public int SandikId { get; set; }
        public bool Kapali { get; set; }
        public string[] RequiredRoles => new[] { "Admin" };
    }
}
