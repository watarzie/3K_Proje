using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;

namespace _3K.Application.Features.ProjeIslemleri.Queries
{
    /// <summary>
    /// Dropdown'lar için hafif proje listesi — Include yok, sadece Id/ProjeNo/Musteri.
    /// </summary>
    public class ProjeDropdownQuery : IRequest<Result<IEnumerable<ProjeDropdownDto>>>, ISecuredRequest
    {
        public int? ProjeTipiId { get; set; }
        public string? SearchTerm { get; set; }
        public bool? IsSevkEdilen { get; set; }
        public int Take { get; set; } = 50;
        public List<int> IncludeIds { get; set; } = [];
    }
}
