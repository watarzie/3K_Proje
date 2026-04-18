using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.DTOs;
using _3K.Core.Enums;

namespace _3K.Application.Features.RolIslemleri.Commands
{
    /// <summary>Rol adı ve yetkilerini günceller.</summary>
    public class RolGuncelleCommand : IRequest<Result<RolDetayDto>>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin };

        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
        public List<RolYetkiItemDto> Yetkiler { get; set; } = new();
    }
}
