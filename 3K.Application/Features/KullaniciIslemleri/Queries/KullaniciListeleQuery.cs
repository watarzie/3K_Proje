using MediatR;
using _3K.Application.Common;
using _3K.Application.DTOs;

namespace _3K.Application.Features.KullaniciIslemleri.Queries
{
    /// <summary>
    /// Kullanıcı listesi — giriş yapmış herkes erişebilir.
    /// </summary>
    public class KullaniciListeleQuery : IRequest<Result<IEnumerable<KullaniciDto>>>, ISecuredRequest
    {
        public string[] RequiredRoles => Array.Empty<string>();
    }
}
