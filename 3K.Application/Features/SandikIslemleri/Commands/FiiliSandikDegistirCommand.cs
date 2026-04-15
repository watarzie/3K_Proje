using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// İş akışı 4: Fiili sandık değiştir.
    /// Roller: Admin, Personel3K
    /// </summary>
    public class FiiliSandikDegistirCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.Personel3K };

        public int CekiSatiriId { get; set; }
        public string YeniFiiliSandikNo { get; set; } = string.Empty;
        public int ProjeId { get; set; }
        public int KullaniciId { get; set; }
    }
}