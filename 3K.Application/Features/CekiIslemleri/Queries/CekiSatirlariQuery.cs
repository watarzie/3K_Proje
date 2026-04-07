using MediatR;
using _3K.Application.Common;
using _3K.Application.DTOs;

namespace _3K.Application.Features.CekiIslemleri.Queries
{
    /// <summary>
    /// Çeki satırlarını listeler — giriş yapmış herkes erişebilir.
    /// </summary>
    public class CekiSatirlariQuery : IRequest<Result<IEnumerable<CekiSatiriDto>>>, ISecuredRequest
    {
        public string[] RequiredRoles => Array.Empty<string>();
        public int CekiId { get; set; }
    }
}
