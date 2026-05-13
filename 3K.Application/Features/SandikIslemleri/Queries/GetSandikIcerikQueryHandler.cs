using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.SandikIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
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
            var etkinDepoLokasyonId = icerikler.Any(i => i.CekiSatiri?.GridDurumuId == (int)GridDurum.GridKapandi)
                ? (int)DepoLokasyon.Grid
                : sandik.DepoLokasyonId;

            var dto = new SandikDetayDto
            {
                Id = sandik.Id,
                SandikNo = sandik.SandikNo,
                Ad = sandik.Ad,
                DurumId = sandik.DurumId,
                DurumMetni = _lookupCache.GetDeger<LookupSandikDurum>(sandik.DurumId),
                DepoLokasyonId = etkinDepoLokasyonId,
                DepoLokasyonMetni = _lookupCache.GetDeger<LookupDepoLokasyon>(etkinDepoLokasyonId),
                En = sandik.En,
                Boy = sandik.Boy,
                Yukseklik = sandik.Yukseklik,
                NetKg = sandik.NetKg,
                GrossKg = sandik.GrossKg,
                Icerikler = icerikler.Select(i =>
                {
                    var istenen = i.CekiSatiri?.IstenenAdet ?? (int)i.Miktar;
                    var konulan = i.KonulanAdet;

                    // Durum: konulana göre hesapla
                    string durumMetni;
                    if (konulan <= 0)
                        durumMetni = "Gelmedi";
                    else if (konulan >= istenen)
                        durumMetni = "Tamamlandı";
                    else
                        durumMetni = "Kısmi Geldi";

                    return new SandikIcerikDto
                    {
                        Id = i.Id,
                        CekiSatiriId = i.CekiSatiriId,
                        OlcuResmiPozNo = i.CekiSatiri?.OlcuResmiPozNo,
                        BarkodNo = i.CekiSatiri?.BarkodNo ?? i.BarkodNo ?? "",
                        Aciklama = i.CekiSatiri?.Aciklama ?? i.Isim ?? "",
                        IstenenAdet = istenen,
                        KonulanAdet = konulan,
                        EksikAdet = i.EksikAdet,
                        DurumId = i.CekiSatiri?.DurumId ?? 0,
                        DurumMetni = durumMetni,
                        PaketleyenBasHarf = i.CekiSatiri?.Paketleyen?.BasHarf,
                        KontrolEdenBasHarf = i.CekiSatiri?.KontrolEden?.BasHarf,
                        Remarks = i.CekiSatiri?.Remarks,
                        IsManuelEklenen = i.CekiSatiri == null || (i.CekiSatiri?.IsManuelEklenen ?? false),
                        // Saha/Yedek + Birim
                        Isim = i.Isim,
                        Miktar = i.Miktar,
                        BirimId = i.BirimId,
                        BirimMetni = i.BirimId.HasValue ? _lookupCache.GetDeger<LookupBirim>(i.BirimId.Value) : null,
                        // Madde 2: Parçalı karşılama
                        StokKarsilanan = i.StokKarsilanan,
                        ProjeKarsilanan = i.ProjeKarsilanan,
                        TedarikciKarsilanan = i.TedarikciKarsilanan,
                        KaynakProjeNo = i.KaynakProjeNo,
                        // KURAL 3: Backend-hesaplanan alanlar (Dumb UI)
                        KalanMiktar = i.CekiSatiri?.KalanMiktar ?? 0,
                        GenelDurumId = i.CekiSatiri?.DurumId ?? 0,
                        GenelDurumMetni = i.CekiSatiri != null
                            ? _lookupCache.GetDeger<LookupUrunDurum>(i.CekiSatiri.DurumId)
                            : ""
                    };
                }).ToList()
            };

            return Result<SandikDetayDto>.Success(dto);
        }
    }
}
