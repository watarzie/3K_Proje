using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.GridIslemleri.Commands
{
    /// <summary>
    /// Süreç durumunu günceller. Tekli veya toplu.
    /// Roller: Admin, Surec
    /// </summary>
    public class SurecDurumGuncelleCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.Surec };
        public string? RequiredMenuKod => "surec-modulu";

        public int ProjeId { get; set; }
        public List<int> CekiSatiriIdler { get; set; } = new();
        public int SurecDurumId { get; set; }
    }
}
