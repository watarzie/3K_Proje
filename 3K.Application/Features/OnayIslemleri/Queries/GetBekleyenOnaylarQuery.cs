using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.OnayIslemleri.DTOs;

namespace _3K.Application.Features.OnayIslemleri.Queries
{
    public class GetBekleyenOnaylarQuery : IRequest<Result<List<OnayBekleyenIslemDto>>>, ISecuredRequest
    {
        // Only Admin or managers with explicit role should fetch this
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.Yonetici };
    }
}
