using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.DTOs;

namespace _3K.Application.Features.RolIslemleri.Commands
{
    /// <summary>Yeni ve izinsiz bir rol oluşturur; izinler rol güncelleme ile menü ağacından verilir.</summary>
    public class RolOlusturCommand : IRequest<Result<RolDto>>, ISecuredRequest
    {
        public string Ad { get; set; } = string.Empty;
    }
}
