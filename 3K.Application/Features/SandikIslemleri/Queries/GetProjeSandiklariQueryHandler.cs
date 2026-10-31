using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri;
using _3K.Application.Features.SandikIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.SandikIslemleri.Queries
{
    public class GetProjeSandiklariQueryHandler : IRequestHandler<GetProjeSandiklariQuery, Result<IEnumerable<SandikDto>>>
    {
        private readonly ISandikService _sandikService;
        private readonly ILookupCacheService _lookupCache;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;
        private readonly ISahaAktarimSilmeKorumaService _sahaAktarimSilmeKorumaService;
        private readonly IUnitOfWork _unitOfWork;

        public GetProjeSandiklariQueryHandler(
            ISandikService sandikService,
            ILookupCacheService lookupCache,
            ISahaTamamlamaService sahaTamamlamaService,
            ISahaAktarimSilmeKorumaService sahaAktarimSilmeKorumaService,
            IUnitOfWork unitOfWork)
        {
            _sandikService = sandikService;
            _lookupCache = lookupCache;
            _sahaTamamlamaService = sahaTamamlamaService;
            _sahaAktarimSilmeKorumaService = sahaAktarimSilmeKorumaService;
            _unitOfWork = unitOfWork;
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
            var kaynakSandikSahaDurumu = await _sahaTamamlamaService.GetKaynakSandikSahaAktarimDurumuAsync(
                sandiklar.Select(s => s.Id),
                cancellationToken);
            var aktifAktarimBagliSandikIds = await _sahaAktarimSilmeKorumaService
                .GetAktifAktarimBagliSandikIdsAsync(sandiklar.Select(s => s.Id), cancellationToken);
            var tumCekiSatiriIds = sandiklar
                .SelectMany(s => s.SandikIcerikleri ?? new List<SandikIcerik>())
                .Where(i => i.CekiSatiriId.HasValue)
                .Select(i => i.CekiSatiriId!.Value)
                .Distinct()
                .ToList();
            var satirTahsisleri = tumCekiSatiriIds.Count == 0
                ? new List<SandikIcerik>()
                : (await _unitOfWork.GetRepository<SandikIcerik>().FindAsync(i =>
                        i.CekiSatiriId.HasValue && tumCekiSatiriIds.Contains(i.CekiSatiriId.Value)))
                    .ToList();
            var birdenFazlaSandigaTahsisliSatirIds = satirTahsisleri
                .GroupBy(i => i.CekiSatiriId!.Value)
                .Where(g => g.Select(i => i.SandikId).Distinct().Count() > 1)
                .Select(g => g.Key)
                .ToHashSet();
            // Sandık detayıyla aynı tahsisler: satırın bütün sandıklardaki tahsis kayıtları.
            // Numara alanının kilidi Özellikleri Güncelle'deki sunucu denetimiyle aynı kuraldır.
            var cekiSandikNolari = AmbalajCekiSandiklari.ProjeIcinGetir(_unitOfWork, request.ProjeId);
            var sandikIdleri = sandiklar.Select(s => s.Id).ToList();
            var ambalajKayitlari = _unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable()
                .Where(k => k.KaynakKayitId.HasValue && sandikIdleri.Contains(k.KaynakKayitId.Value))
                .ToList()
                .ToLookup(k => k.KaynakKayitId!.Value);
            var satirTahsisMap = satirTahsisleri
                .GroupBy(i => i.CekiSatiriId!.Value)
                .ToDictionary(g => g.Key, g => (IReadOnlyCollection<SandikIcerik>)g.ToList());

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
                var fizikselSevkEdildi = s.DurumId == (int)SandikDurum.Sevkedildi;
                var sahaUzerindenSevkEdildi = !fizikselSevkEdildi &&
                    kaynakSandikSahaDurumu.SahaUzerindenSevkEdilenSandikIds.Contains(s.Id);
                var sandikSahaAktariminda = !fizikselSevkEdildi &&
                    !sahaUzerindenSevkEdildi &&
                    kaynakSandikSahaDurumu.AktifAktarimaBagliSandikIds.Contains(s.Id);
                var satirEksikleri = icerikler
                    .Select(i => SandikTahsisHelper.HesaplaIcerikMiktari(i, i.CekiSatiriId.HasValue
                        ? satirTahsisMap.GetValueOrDefault(i.CekiSatiriId.Value, [i])
                        : [i]).Eksik)
                    .Where(eksik => eksik > 0)
                    .ToList();

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
                    TipId = s.TipId,
                    TipMetni = _lookupCache.GetDeger<LookupSandikTipi>(s.TipId),
                    UrunSayisi = icerikler.Count,
                    IsManuelSandik = isManuelSandik,
                    SandikNoKilitNedeni = SandikNoDegisikligiKurali.KilitNedeni(s, cekiSandikNolari, ambalajKayitlari[s.Id]),
                    SilinebilirMi = s.DurumId != (int)SandikDurum.Sevkedildi &&
                        !aktifAktarimBagliSandikIds.Contains(s.Id) &&
                        icerikler.All(i => i.Id > 0) &&
                        (icerikler.Count == 0 ||
                            (isManuelSandik &&
                             icerikler.All(i =>
                                 !birdenFazlaSandigaTahsisliSatirIds.Contains(i.CekiSatiriId!.Value) &&
                                 !ManuelUrunSilmeKurali.IslemGormusMu(i.CekiSatiri!)))),
                    DepodaSayilacakMi = s.DepoLokasyonId != (int)DepoLokasyon.Belirsiz &&
                        SandikDepoKurali.DepoLokasyonuAtanabilir(s, icerikler),
                    SahayaAktarildiMi = sandikSahaAktariminda,
                    SahaUzerindenSevkEdildiMi = sahaUzerindenSevkEdildi,
                    SahayaAktarilanMiktar = sahayaAktarilanMiktar,
                    KapanmaTarihi = s.KapanmaTarihi,
                    EksikUrunSayisi = satirEksikleri.Count,
                    EksikAdet = satirEksikleri.Sum(),
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

    }
}
