using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.OnayIslemleri.Queries
{
    public class GetBekleyenSayisiQuery : IRequest<Result<int>>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.Yonetici };
    }
}
