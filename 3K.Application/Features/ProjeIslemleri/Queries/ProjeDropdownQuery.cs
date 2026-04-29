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
        public string[] RequiredRoles => Array.Empty<string>();
    }
}
