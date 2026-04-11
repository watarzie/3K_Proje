using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.GridIslemleri.Commands
{
    /// <summary>
    /// Birden fazla ürünü tek seferde "SevkEdildi" yapar.
    /// </summary>
    public class GridTopluSevkCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { "Admin", "PersonelGrid" };

        public int ProjeId { get; set; }
        public List<int> CekiSatiriIdler { get; set; } = new();
        public string? Not { get; set; }
    }
}
