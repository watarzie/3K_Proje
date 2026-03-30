using MediatR;
using _3K.Application.DTOs;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Queries
{
    public class GetSandikIcerikQueryHandler : IRequestHandler<GetSandikIcerikQuery, SandikDetayDto>
    {
        private readonly ISandikService _sandikService;

        public GetSandikIcerikQueryHandler(ISandikService sandikService)
        {
            _sandikService = sandikService;
        }

        public async Task<SandikDetayDto> Handle(GetSandikIcerikQuery request, CancellationToken cancellationToken)
        {
            var sandik = await _sandikService.GetSandikDetayAsync(request.SandikId);
            if (sandik == null)
                throw new KeyNotFoundException($"Sandık bulunamadı: {request.SandikId}");

            var icerikler = await _sandikService.GetSandikIcerikAsync(request.SandikId);

            return new SandikDetayDto
            {
                Id = sandik.Id,
                SandikNo = sandik.SandikNo,
                Durum = sandik.Durum.ToString(),
                DepoLokasyonu = sandik.DepoLokasyonu.ToString(),
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
                    Durum = i.CekiSatiri?.Durum.ToString() ?? "",
                    PaketleyenBasHarf = i.CekiSatiri?.Paketleyen?.BasHarf,
                    KontrolEdenBasHarf = i.CekiSatiri?.KontrolEden?.BasHarf,
                    Remarks = i.CekiSatiri?.Remarks
                }).ToList()
            };
        }
    }
}
