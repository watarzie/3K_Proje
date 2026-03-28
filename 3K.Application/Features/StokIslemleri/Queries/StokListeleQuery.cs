using MediatR;
using _3K.Application.DTOs;

namespace _3K.Application.Features.StokIslemleri.Queries
{
    public class StokListeleQuery : IRequest<IEnumerable<StokKaydiDto>>
    {
        public string? MalzemeKodu { get; set; }
    }
}
