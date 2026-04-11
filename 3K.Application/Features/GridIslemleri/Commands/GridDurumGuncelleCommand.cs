using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.GridIslemleri.Commands
{
    /// <summary>
    /// Grid personeli ürün durumunu günceller.
    /// </summary>
    public class GridDurumGuncelleCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { "Admin", "PersonelGrid" };

        public int CekiSatiriId { get; set; }
        public int ProjeId { get; set; }
        public string YeniDurum { get; set; } = string.Empty;
        public int? SevkMiktari { get; set; }
        public string? Not { get; set; }
    }
}
