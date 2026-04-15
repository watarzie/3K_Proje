using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AuthIslemleri.DTOs;
using _3K.Core.Enums;

namespace _3K.Application.Features.AuthIslemleri.Commands
{
    /// <summary>
    /// Kullanıcı kayıt - sadece Admin.
    /// </summary>
    public class RegisterCommand : IRequest<Result<KullaniciDto>>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin };

        public string AdSoyad { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Sifre { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }
}
