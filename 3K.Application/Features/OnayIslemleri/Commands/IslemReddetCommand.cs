using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.OnayIslemleri.Commands
{
    public class IslemReddetCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.Yonetici };
        public int OnayBekleyenIslemId { get; set; }
        public string RedAciklamasi { get; set; } = string.Empty;
    }
}
