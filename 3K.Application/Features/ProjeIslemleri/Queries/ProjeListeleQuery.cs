using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;

namespace _3K.Application.Features.ProjeIslemleri.Queries
{
    /// <summary>
    /// Projeleri listeler — opsiyonel ProjeTipiId filtresi ile.
    /// </summary>
    public class ProjeListeleQuery : IRequest<Result<IEnumerable<ProjeDto>>>, ISecuredRequest
    {
        public string[] RequiredRoles => Array.Empty<string>();
        /// <summary>
        /// Null ise tüm projeler döner, doluysa sadece o tipteki projeler (Normal=1, Saha=2, Yedek=3).
        /// </summary>
        public int? ProjeTipiId { get; set; }
    }
}
