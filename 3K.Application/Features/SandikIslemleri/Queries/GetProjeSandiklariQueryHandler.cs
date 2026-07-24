using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.SandikIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Queries
{
    public class GetProjeSandiklariQueryHandler : IRequestHandler<GetProjeSandiklariQuery, Result<IEnumerable<SandikDto>>>
    {
        private readonly ISandikService _sandikService;
        private readonly ILookupCacheService _lookupCache;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;
        private readonly ISahaAktarimSilmeKorumaService _sahaAktarimSilmeKorumaService;

        public GetProjeSandiklariQueryHandler(
            ISandikService sandikService,
            ILookupCacheService lookupCache,
            ISahaTamamlamaService sahaTamamlamaService,
            ISahaAktarimSilmeKorumaService sahaAktarimSilmeKorumaService)
        {
            _sandikService = sandikService;
            _lookupCache = lookupCache;
            _sahaTamamlamaService = sahaTamamlamaService;
            _sahaAktarimSilmeKorumaService = sahaAktarimSilmeKorumaService;
        }

        public async Task<Result<IEnumerable<SandikDto>>> Handle(GetProjeSandiklariQuery request, CancellationToken cancellationToken)
        {
            var sandiklar = (await _sandikService.GetProjeSandiklariAsync(request.ProjeId)).ToList();
            var kaynakSatirIdleri = sandiklar
                .SelectMany(s => s.SandikIcerikleri ?? new List<SandikIcerik>())
                .Select(i => i.CekiSatiri)
                .Where(cs => cs != null && !cs.KaynakCekiSatiriId.HasValue)
                .Select(cs => cs!.Id)
                .Distinct()
                .ToList();
            var sahaTamamlamaMap = await _sahaTamamlamaService.GetAktifTamamlamaMapAsync(kaynakSatirIdleri, cancellationToken);
            var sandikBazliAktarimSatirIds = await _sahaTamamlamaService.GetAktifSandikBazliAktarimSatirIdsAsync(kaynakSatirIdleri, cancellationToken);
            var aktifAktarimBagliSandikIds = await _sahaAktarimSilmeKorumaService
                .GetAktifAktarimBagliSandikIdsAsync(sandiklar.Select(s => s.Id), cancellationToken);

            var result = sandiklar.Select(s =>
            {
                var icerikler = s.SandikIcerikleri?.ToList() ?? new List<SandikIcerik>();
                var isManuelSandik = IsManuelSandik(icerikler);
                var kaynakSatirlar = icerikler
                    .Select(i => i.CekiSatiri)
                    .Where(cs => cs != null && !cs.KaynakCekiSatiriId.HasValue)
                    .Select(cs => cs!)
                    .GroupBy(cs => cs.Id)
                    .Select(g => g.First())
                    .ToList();
                var sahayaAktarilanMiktar = kaynakSatirlar.Sum(cs => sahaTamamlamaMap.GetValueOrDefault(cs.Id));
                var aktarilabilirKaynakSatirlar = kaynakSatirlar
                    .Where(cs => cs.KalanMiktar > 0)
                    .ToList();
                var sandikTamamenSahayaAktarildi = aktarilabilirKaynakSatirlar.Count > 0 &&
                    aktarilabilirKaynakSatirlar.All(cs => sandikBazliAktarimSatirIds.Contains(cs.Id));

                return new SandikDto
                {
                    Id = s.Id,
                    SandikNo = s.SandikNo,
                    Ad = s.Ad,
                    DurumId = s.DurumId,
                    DurumMetni = _lookupCache.GetDeger<LookupSandikDurum>(s.DurumId),
                    SevkiyatDuzeltmeAcikMi = s.SevkiyatDuzeltmeAcikMi,
                    DepoLokasyonId = s.DepoLokasyonId,
                    DepoLokasyonMetni = _lookupCache.GetDeger<LookupDepoLokasyon>(s.DepoLokasyonId),
                    UrunSayisi = icerikler.Count,
                    IsManuelSandik = isManuelSandik,
                    SilinebilirMi = !aktifAktarimBagliSandikIds.Contains(s.Id) &&
                        icerikler.All(i => i.Id > 0) &&
                        (icerikler.Count == 0 ||
                            (isManuelSandik && icerikler.All(i => !ManuelSatirIslemGormus(i.CekiSatiri!)))),
                    DepodaSayilacakMi = s.DepoLokasyonId != (int)DepoLokasyon.Belirsiz &&
                        SandikDepoKurali.DepoLokasyonuAtanabilir(s, icerikler),
                    SahayaAktarildiMi = sandikTamamenSahayaAktarildi,
                    SahayaAktarilanMiktar = sahayaAktarilanMiktar,
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
            return icerikler.Count > 0 &&
                icerikler.All(i =>
                    i.CekiSatiri?.IsManuelEklenen == true &&
                    !i.CekiSatiri.KaynakCekiSatiriId.HasValue);
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

    }
}
