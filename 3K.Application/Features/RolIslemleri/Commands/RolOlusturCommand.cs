using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.DTOs;
using _3K.Core.Enums;

namespace _3K.Application.Features.RolIslemleri.Commands
{
    /// <summary>Yeni rol oluşturur.</summary>
    public class RolOlusturCommand : IRequest<Result<RolDto>>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin };

        public string Ad { get; set; } = string.Empty;
    }
}
