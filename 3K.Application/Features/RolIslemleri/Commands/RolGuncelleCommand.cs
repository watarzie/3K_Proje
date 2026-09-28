using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.DTOs;

namespace _3K.Application.Features.RolIslemleri.Commands
{
    /// <summary>Rol adı ve yetkilerini günceller.</summary>
    public class RolGuncelleCommand : IRequest<Result<RolDetayDto>>, ISecuredRequest
    {

        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
        public List<RolYetkiItemDto> Yetkiler { get; set; } = new();
    }
}
