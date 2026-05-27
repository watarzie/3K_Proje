using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;

namespace _3K.Application.Features.ProjeIslemleri.Queries
{
    public class GetProjeSevkiyatlariQuery : IRequest<Result<List<SevkiyatDto>>>, ISecuredRequest
    {
        public int ProjeId { get; set; }
    }
}
