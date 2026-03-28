using MediatR;
using _3K.Application.DTOs;

namespace _3K.Application.Features.HareketGecmisiIslemleri.Queries
{
    public class GetProjeHareketleriQuery : IRequest<IEnumerable<HareketGecmisiDto>>
    {
        public int ProjeId { get; set; }
    }
}
