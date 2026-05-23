using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.HareketGecmisiIslemleri.DTOs;

namespace _3K.Application.Features.HareketGecmisiIslemleri.Queries
{
    /// <summary>
    /// Proje hareket geçmişi — server-side pagination ile.
    /// </summary>
    public class GetProjeHareketleriQuery : PaginatedQuery<Result<PaginatedList<HareketGecmisiDto>>>, ISecuredRequest
    {
        public int ProjeId { get; set; }
        public string? SearchTerm { get; set; }
        public int? IslemTipiId { get; set; }
    }
}
