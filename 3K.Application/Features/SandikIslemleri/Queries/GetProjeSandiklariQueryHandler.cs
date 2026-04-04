using MediatR;
using _3K.Application.Common;
using _3K.Application.DTOs;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Queries
{
    public class GetProjeSandiklariQueryHandler : IRequestHandler<GetProjeSandiklariQuery, Result<IEnumerable<SandikDto>>>
    {
        private readonly ISandikService _sandikService;

        public GetProjeSandiklariQueryHandler(ISandikService sandikService)
        {
            _sandikService = sandikService;
        }

        public async Task<Result<IEnumerable<SandikDto>>> Handle(GetProjeSandiklariQuery request, CancellationToken cancellationToken)
        {
            var sandiklar = await _sandikService.GetProjeSandiklariAsync(request.ProjeId);

            var result = sandiklar.Select(s => new SandikDto
            {
                Id = s.Id,
                SandikNo = s.SandikNo,
                Durum = s.Durum.ToString(),
                DepoLokasyonu = s.DepoLokasyonu.ToString(),
                UrunSayisi = s.SandikIcerikleri?.Count ?? 0
            });

            return Result<IEnumerable<SandikDto>>.Success(result);
        }
    }
}