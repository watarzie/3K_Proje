using MediatR;
using _3K.Application.Common;
using _3K.Application.DTOs;

namespace _3K.Application.Features.SandikIslemleri.Queries
{
    /// <summary>
    /// Projeye ait sandıkları listeler — giriş yapmış herkes erişebilir.
    /// </summary>
    public class GetProjeSandiklariQuery : IRequest<Result<IEnumerable<SandikDto>>>, ISecuredRequest
    {
        public string[] RequiredRoles => Array.Empty<string>(); // Authenticate yeterli, rol gerekmez
        public int ProjeId { get; set; }
    }
}