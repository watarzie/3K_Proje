using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.SandikIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Queries
{
    public class GetSandikIcerikQueryHandler : IRequestHandler<GetSandikIcerikQuery, Result<SandikDetayDto>>
    {
        private readonly ISandikService _sandikService;
        private readonly ILookupCacheService _lookupCache;

        public GetSandikIcerikQueryHandler(ISandikService sandikService, ILookupCacheService lookupCache)
        {
            _sandikService = sandikService;
            _lookupCache = lookupCache;
        }

        public async Task<Result<SandikDetayDto>> Handle(GetSandikIcerikQuery request, CancellationToken cancellationToken)
        {
            var sandik = await _sandikService.GetSandikDetayAsync(request.SandikId);
            if (sandik == null)
                return Result<SandikDetayDto>.Failure($"Sandık bulunamadı: {request.SandikId}", 404);

            var icerikler = await _sandikService.GetSandikIcerikAsync(request.SandikId);

            var dto = new SandikDetayDto
            {
                Id = sandik.Id,
                SandikNo = sandik.SandikNo,
                DurumId = sandik.DurumId,
                DurumMetni = _lookupCache.GetDeger<LookupSandikDurum>(sandik.DurumId),
                DepoLokasyonId = sandik.DepoLokasyonId,
                DepoLokasyonMetni = _lookupCache.GetDeger<LookupDepoLokasyon>(sandik.DepoLokasyonId),
                Icerikler = icerikler.Select(i => new SandikIcerikDto
                {
                    Id = i.Id,
                    CekiSatiriId = i.CekiSatiriId,
                    OlcuResmiPozNo = i.CekiSatiri?.OlcuResmiPozNo,
                    BarkodNo = i.CekiSatiri?.BarkodNo ?? "",
                    Aciklama = i.CekiSatiri?.Aciklama ?? "",
                    IstenenAdet = i.CekiSatiri?.IstenenAdet ?? 0,
                    KonulanAdet = i.KonulanAdet,
                    EksikAdet = i.EksikAdet,
                    DurumId = i.CekiSatiri?.DurumId ?? 0,
                    DurumMetni = i.CekiSatiri != null ? _lookupCache.GetDeger<LookupUrunDurum>(i.CekiSatiri.DurumId) : "",
                    PaketleyenBasHarf = i.CekiSatiri?.Paketleyen?.BasHarf,
                    KontrolEdenBasHarf = i.CekiSatiri?.KontrolEden?.BasHarf,
                    Remarks = i.CekiSatiri?.Remarks
                }).ToList()
            };

            return Result<SandikDetayDto>.Success(dto);
        }
    }
}
