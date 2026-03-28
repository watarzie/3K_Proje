using MediatR;
using _3K.Application.DTOs;

namespace _3K.Application.Features.KullaniciIslemleri.Queries
{
    public class KullaniciListeleQuery : IRequest<IEnumerable<KullaniciDto>>
    {
    }
}
