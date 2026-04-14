using MediatR;
using _3K.Application.Common;
using _3K.Application.DTOs;

namespace _3K.Application.Features.SandikIslemleri.Queries
{
    /// <summary>
    /// Grid sevk etti ama 3K tarafında eksik veya gelmemiş ürünler.
    /// </summary>
    public class GetEksikUrunlerQuery : IRequest<Result<List<EksikUrunDto>>>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { "Admin", "Personel3K", "Yonetici" };
        public int ProjeId { get; set; }
    }
}
