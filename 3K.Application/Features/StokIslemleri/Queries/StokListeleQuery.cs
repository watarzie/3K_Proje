using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.StokIslemleri.DTOs;

namespace _3K.Application.Features.StokIslemleri.Queries
{
    /// <summary>
    /// Stok listesi — giriş yapmış herkes erişebilir.
    /// </summary>
    public class StokListeleQuery : IRequest<Result<IEnumerable<StokKaydiDto>>>, ISecuredRequest
    {
        public string[] RequiredRoles => Array.Empty<string>();
        public string? MalzemeKodu { get; set; }
    }
}
