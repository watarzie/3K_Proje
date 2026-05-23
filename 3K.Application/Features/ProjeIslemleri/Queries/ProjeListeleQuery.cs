using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;

namespace _3K.Application.Features.ProjeIslemleri.Queries
{
    /// <summary>
    /// Projeleri listeler — server-side pagination ile.
    /// </summary>
    public class ProjeListeleQuery : PaginatedQuery<Result<PaginatedList<ProjeDto>>>, ISecuredRequest
    {
        public int? ProjeTipiId { get; set; }
        public string? SearchTerm { get; set; }
        public bool? IsSevkEdilen { get; set; }
    }
}
