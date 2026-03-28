using System.Collections.Generic;
using MediatR;
using _3K.Application.DTOs;

namespace _3K.Application.Features.SandikIslemleri.Queries
{
    public class GetProjeSandiklariQuery : IRequest<IEnumerable<SandikDto>>
    {
        public int ProjeId { get; set; }
    }
}