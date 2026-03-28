using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using _3K.Application.DTOs;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Queries
{
    public class GetProjeSandiklariQueryHandler : IRequestHandler<GetProjeSandiklariQuery, IEnumerable<SandikDto>>
    {
        private readonly ISandikService _sandikService;

        public GetProjeSandiklariQueryHandler(ISandikService sandikService)
        {
            _sandikService = sandikService;
        }

        public async Task<IEnumerable<SandikDto>> Handle(GetProjeSandiklariQuery request, CancellationToken cancellationToken)
        {
            var sandiklar = await _sandikService.GetProjeSandiklariAsync(request.ProjeId);

            return sandiklar.Select(s => new SandikDto
            {
                Id = s.Id,
                SandikNo = s.SandikNo,
                Durum = s.Durum.ToString(),
                DepoLokasyonu = s.DepoLokasyonu,
                UrunSayisi = s.SandikIcerikleri?.Count ?? 0
            });
        }
    }
}