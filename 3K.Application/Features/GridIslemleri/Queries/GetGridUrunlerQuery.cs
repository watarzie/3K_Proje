using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.GridIslemleri.DTOs;

namespace _3K.Application.Features.GridIslemleri.Queries
{
    /// <summary>
    /// Grid personelinin göreceği ürün listesi — proje bazında.
    /// </summary>
    public class GetGridUrunlerQuery : IRequest<Result<List<GridUrunDto>>>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { "Admin", "PersonelGrid", "Personel3K", "Yonetici" };
        public int ProjeId { get; set; }
    }
}
