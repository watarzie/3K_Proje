using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.GridIslemleri.Commands
{
    /// <summary>
    /// Grid toplu durum güncelleme: Tam Geldi, Grid Kapandı veya İptal.
    /// </summary>
    public class GridTopluDurumGuncelleCommand : IRequest<Result>, ISecuredRequest
    {

        public int ProjeId { get; set; }
        public List<int> CekiSatiriIdler { get; set; } = new();

        /// <summary>
        /// Hedef Grid durumu ID'si (GridDurum enum): TamGeldi, GridKapandi, Iptal
        /// </summary>
        public int HedefDurumId { get; set; }
        public string? Aciklama { get; set; }
    }
}
