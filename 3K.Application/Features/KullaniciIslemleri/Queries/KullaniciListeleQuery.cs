using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AuthIslemleri.DTOs;

namespace _3K.Application.Features.KullaniciIslemleri.Queries
{
    /// <summary>
    /// Kullanıcı listesi — kullanıcı yönetimi okuma yetkisi gerektirir.
    /// </summary>
    public class KullaniciListeleQuery
        : IRequest<Result<IEnumerable<KullaniciDto>>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "kullanicilar";
    }
}
