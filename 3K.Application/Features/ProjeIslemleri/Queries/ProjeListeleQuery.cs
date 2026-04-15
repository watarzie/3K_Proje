using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;

namespace _3K.Application.Features.ProjeIslemleri.Queries
{
    /// <summary>
    /// Tüm projeleri listeler — giriş yapmış herkes erişebilir.
    /// </summary>
    public class ProjeListeleQuery : IRequest<Result<IEnumerable<ProjeDto>>>, ISecuredRequest
    {
        public string[] RequiredRoles => Array.Empty<string>();
    }
}
