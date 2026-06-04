using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.UcKIslemleri.DTOs;

namespace _3K.Application.Features.UcKIslemleri.Queries
{
    public class GetUcKIsListesiQuery : IRequest<Result<UcKIsListesiDto>>, ISecuredRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? IsTipi { get; set; }
        public int? ProjeId { get; set; }
    }
}
