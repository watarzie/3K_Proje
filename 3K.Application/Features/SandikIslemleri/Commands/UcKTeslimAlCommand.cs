using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// 3K personeli ürün teslim alır — kümülatif (parça parça gelebilir).
    /// </summary>
    public class UcKTeslimAlCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { "Admin", "Personel3K" };

        public int CekiSatiriId { get; set; }
        public int ProjeId { get; set; }
        public int GelenMiktar { get; set; }
        public string? Not { get; set; }
    }
}
