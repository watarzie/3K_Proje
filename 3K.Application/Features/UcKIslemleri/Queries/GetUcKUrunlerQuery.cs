using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.UcKIslemleri.DTOs;

namespace _3K.Application.Features.UcKIslemleri.Queries
{
    public class GetUcKUrunlerQuery : IRequest<Result<List<UcKUrunDto>>>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { "Admin", "Personel3K", "PersonelGrid", "Yonetici" };
        public int ProjeId { get; set; }
    }
}
