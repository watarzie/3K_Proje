using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.GridIslemleri.Commands
{
    /// <summary>
    /// Kalite durumunu günceller. Tekli veya toplu.
    /// Roller: Admin, Kalite
    /// </summary>
    public class KaliteDurumGuncelleCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.Kalite };
        public string? RequiredMenuKod => "kalite-modulu";

        public int ProjeId { get; set; }
        public List<int> CekiSatiriIdler { get; set; } = new();
        public int KaliteDurumId { get; set; }
    }
}
