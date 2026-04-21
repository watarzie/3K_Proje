using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;

namespace _3K.Application.Features.GridIslemleri.Commands
{
    /// <summary>
    /// Birden fazla ürünü tek seferde (int)GridDurum.SevkEdildi yapar.
    /// </summary>
    public class GridTopluSevkCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.PersonelGrid };

        public int ProjeId { get; set; }
        public List<int> CekiSatiriIdler { get; set; } = new();
        public string? Not { get; set; }
    }
}
