using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.GridIslemleri.Commands
{
    /// <summary>
    /// Kalite durumunu günceller. Tekli veya toplu.
    /// </summary>
    public class KaliteDurumGuncelleCommand : IRequest<Result>, ISecuredRequest
    {

        public int ProjeId { get; set; }
        public List<int> CekiSatiriIdler { get; set; } = new();
        public int KaliteDurumId { get; set; }
    }
}
