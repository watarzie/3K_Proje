using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AuthIslemleri.DTOs;
using _3K.Core.Enums;

namespace _3K.Application.Features.AuthIslemleri.Commands
{
    /// <summary>
    /// Kullanıcı kaydı; kullanıcı yönetimi yazma izni ve atanacak role ilişkin ek kontroller gerekir.
    /// </summary>
    public class RegisterCommand
        : IRequest<Result<KullaniciDto>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "kullanicilar";

        public string AdSoyad { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Sifre { get; set; } = string.Empty;
        public int RolId { get; set; }
    }
}
