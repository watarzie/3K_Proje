using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.SandikIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Queries
{
    public class GetProjeSandiklariQueryHandler : IRequestHandler<GetProjeSandiklariQuery, Result<IEnumerable<SandikDto>>>
    {
        private readonly ISandikService _sandikService;
        private readonly ILookupCacheService _lookupCache;

        public GetProjeSandiklariQueryHandler(ISandikService sandikService, ILookupCacheService lookupCache)
        {
            _sandikService = sandikService;
            _lookupCache = lookupCache;
        }

        public async Task<Result<IEnumerable<SandikDto>>> Handle(GetProjeSandiklariQuery request, CancellationToken cancellationToken)
        {
            var sandiklar = await _sandikService.GetProjeSandiklariAsync(request.ProjeId);

            var result = sandiklar.Select(s => new SandikDto
            {
                Id = s.Id,
                SandikNo = s.SandikNo,
                DurumId = s.DurumId,
                DurumMetni = _lookupCache.GetDeger<LookupSandikDurum>(s.DurumId),
                DepoLokasyonId = s.DepoLokasyonId,
                DepoLokasyonMetni = _lookupCache.GetDeger<LookupDepoLokasyon>(s.DepoLokasyonId),
                UrunSayisi = s.SandikIcerikleri?.Count ?? 0
            });

            return Result<IEnumerable<SandikDto>>.Success(result);
        }
    }
}