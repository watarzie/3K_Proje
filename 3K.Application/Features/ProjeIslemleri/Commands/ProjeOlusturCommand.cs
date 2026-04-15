using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Core.Enums;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    /// <summary>
    /// Proje oluştur.
    /// Roller: Admin
    /// </summary>
    public class ProjeOlusturCommand : IRequest<Result<ProjeDto>>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin };

        public string ProjeNo { get; set; } = string.Empty;
        public string Musteri { get; set; } = string.Empty;
        public DateTime? PlanlananSevkTarihi { get; set; }
        public string SorumluKisi { get; set; } = string.Empty;
        public int KullaniciId { get; set; }
    }
}
