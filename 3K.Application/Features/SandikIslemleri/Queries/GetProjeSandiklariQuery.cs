using System.Collections.Generic;
using MediatR;
using 3K.Core.Entities;

namespace 3K.Application.Features.SandikIslemleri.Queries
{
    public class GetProjeSandiklariQuery : IRequest<IEnumerable<Sandik>>
{
    public int ProjeId { get; set; }
}
}