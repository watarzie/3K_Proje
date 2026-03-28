using MediatR;
using _3K.Application.DTOs;

namespace _3K.Application.Features.SandikIslemleri.Queries
{
    public class GetSandikIcerikQuery : IRequest<SandikDetayDto>
    {
        public int SandikId { get; set; }
    }
}
