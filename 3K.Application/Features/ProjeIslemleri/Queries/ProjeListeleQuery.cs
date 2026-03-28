using MediatR;
using _3K.Application.DTOs;

namespace _3K.Application.Features.ProjeIslemleri.Queries
{
    public class ProjeListeleQuery : IRequest<IEnumerable<ProjeDto>>
    {
    }
}
