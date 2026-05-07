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

            var result = sandiklar.Select(s =>
            {
                var icerikler = s.SandikIcerikleri?.ToList() ?? new List<SandikIcerik>();
                var isManuelSandik = IsManuelSandik(icerikler);

                return new SandikDto
                {
                    Id = s.Id,
                    SandikNo = s.SandikNo,
                    Ad = s.Ad,
                    DurumId = s.DurumId,
                    DurumMetni = _lookupCache.GetDeger<LookupSandikDurum>(s.DurumId),
                    DepoLokasyonId = s.DepoLokasyonId,
                    DepoLokasyonMetni = _lookupCache.GetDeger<LookupDepoLokasyon>(s.DepoLokasyonId),
                    UrunSayisi = icerikler.Count,
                    IsManuelSandik = isManuelSandik,
                    SilinebilirMi = icerikler.Count == 0 || (isManuelSandik && icerikler.All(i => !ManuelSatirIslemGormus(i.CekiSatiri!))),
                    DepodaSayilacakMi = DepodaSayilacakSandik(s, icerikler),
                    En = s.En,
                    Boy = s.Boy,
                    Yukseklik = s.Yukseklik,
                    NetKg = s.NetKg,
                    GrossKg = s.GrossKg
                };
            });

            return Result<IEnumerable<SandikDto>>.Success(result);
        }

        private static bool IsManuelSandik(IReadOnlyCollection<SandikIcerik> icerikler)
        {
            return icerikler.Count > 0 && icerikler.All(i => i.CekiSatiri?.IsManuelEklenen == true);
        }

        private static bool ManuelSatirIslemGormus(CekiSatiri satir)
        {
            return satir.GelenMiktar > 0
                || satir.KarsilananMiktar > 0
                || satir.HataliMiktar > 0
                || satir.StokKarsilanan > 0
                || satir.ProjeKarsilanan > 0
                || satir.ProjeGonderilen > 0
                || satir.TedarikciKarsilanan > 0
                || satir.GeriGonderilenMiktar > 0;
        }

        private static bool DepodaSayilacakSandik(Sandik sandik, IReadOnlyCollection<SandikIcerik> icerikler)
        {
            if (sandik.DurumId == (int)_3K.Core.Enums.SandikDurum.Sevkedildi)
                return false;

            return icerikler.Any(i =>
            {
                var satir = i.CekiSatiri;
                if (satir == null)
                    return i.Miktar > 0 || i.KonulanAdet > 0 || i.StokKarsilanan > 0 || i.ProjeKarsilanan > 0 || i.TedarikciKarsilanan > 0;

                return satir.GelenMiktar > 0
                    || satir.ProjeKarsilanan > 0
                    || satir.StokKarsilanan > 0
                    || satir.TedarikciKarsilanan > 0;
            });
        }
    }
}
