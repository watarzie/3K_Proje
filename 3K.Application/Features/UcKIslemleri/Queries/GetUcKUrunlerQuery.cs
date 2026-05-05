using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.UcKIslemleri.DTOs;

namespace _3K.Application.Features.UcKIslemleri.Queries
{
    public class GetUcKUrunlerQuery : IRequest<Result<List<UcKUrunDto>>>, ISecuredRequest
    {
        public string[] RequiredRoles => Array.Empty<string>();
        public string? RequiredMenuKod => "3k-modulu";
        public int ProjeId { get; set; }
    }
}
