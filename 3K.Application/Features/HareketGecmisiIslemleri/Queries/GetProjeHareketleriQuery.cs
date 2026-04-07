using MediatR;
using _3K.Application.Common;
using _3K.Application.DTOs;

namespace _3K.Application.Features.HareketGecmisiIslemleri.Queries
{
    /// <summary>
    /// Proje hareket geçmişi — giriş yapmış herkes erişebilir.
    /// </summary>
    public class GetProjeHareketleriQuery : IRequest<Result<IEnumerable<HareketGecmisiDto>>>, ISecuredRequest
    {
        public string[] RequiredRoles => Array.Empty<string>();
        public int ProjeId { get; set; }
    }
}
