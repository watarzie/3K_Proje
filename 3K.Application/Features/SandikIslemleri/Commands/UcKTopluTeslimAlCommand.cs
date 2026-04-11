using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// Birden fazla ürünü tek seferde teslim alır — toplu teslim.
    /// </summary>
    public class UcKTopluTeslimAlCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { "Admin", "Personel3K" };

        public int ProjeId { get; set; }
        public List<TopluTeslimItem> Urunler { get; set; } = new();
        public string? Not { get; set; }
    }

    public class TopluTeslimItem
    {
        public int CekiSatiriId { get; set; }
        public int GelenMiktar { get; set; }
    }
}
