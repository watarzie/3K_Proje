using MediatR;
using _3K.Application.DTOs;

namespace _3K.Application.Features.CekiIslemleri.Queries
{
    public class CekiSatirlariQuery : IRequest<IEnumerable<CekiSatiriDto>>
    {
        public int CekiId { get; set; }
    }
}
