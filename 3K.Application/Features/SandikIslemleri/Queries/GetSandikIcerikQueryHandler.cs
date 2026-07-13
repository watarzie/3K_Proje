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
        private readonly ISahaTamamlamaService _sahaTamamlamaService;
        private readonly IUnitOfWork _unitOfWork;

        public GetSandikIcerikQueryHandler(
            ISandikService sandikService,
            ILookupCacheService lookupCache,
            ISahaTamamlamaService sahaTamamlamaService,
            IUnitOfWork unitOfWork)
        {
            _sandikService = sandikService;
            _lookupCache = lookupCache;
            _sahaTamamlamaService = sahaTamamlamaService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<SandikDetayDto>> Handle(GetSandikIcerikQuery request, CancellationToken cancellationToken)
        {
            var sandik = await _sandikService.GetSandikDetayAsync(request.SandikId);
            if (sandik == null)
                return Result<SandikDetayDto>.Failure($"Sandık bulunamadı: {request.SandikId}", 404);

            var icerikler = (await _sandikService.GetSandikIcerikAsync(request.SandikId)).ToList();
            var cekiSatiriIdleri = icerikler
                .Where(i => i.CekiSatiriId.HasValue)
                .Select(i => i.CekiSatiriId!.Value)
                .Distinct()
                .ToList();
            var projeTahsisleri = cekiSatiriIdleri.Count == 0
                ? new List<SandikIcerik>()
                : (await _unitOfWork.GetRepository<SandikIcerik>()
                    .FindAsync(i => i.CekiSatiriId.HasValue && cekiSatiriIdleri.Contains(i.CekiSatiriId.Value)))
                    .ToList();
            var tahsisSayilari = projeTahsisleri
                .Where(i => i.CekiSatiriId.HasValue)
                .GroupBy(i => i.CekiSatiriId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());
            var sandikTransferleri = (await _unitOfWork.GetRepository<SandikUrunTransferi>()
                .FindAsync(t => t.ProjeId == sandik.ProjeId &&
                    (t.KaynakSandikId == sandik.Id || t.HedefSandikId == sandik.Id)))
                .ToList();

            var kaynakSatirIdleri = icerikler
                .Select(i => i.CekiSatiri)
                .Where(cs => cs != null && !cs.KaynakCekiSatiriId.HasValue)
                .Select(cs => cs!.Id)
                .Distinct()
                .ToList();
            var sahaTamamlamaMap = await _sahaTamamlamaService.GetAktifTamamlamaMapAsync(kaynakSatirIdleri, cancellationToken);
            var sandikBazliAktarimSatirIds = await _sahaTamamlamaService.GetAktifSandikBazliAktarimSatirIdsAsync(kaynakSatirIdleri, cancellationToken);
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

            var dto = new SandikDetayDto
            {
                Id = sandik.Id,
                SandikNo = sandik.SandikNo,
                Ad = sandik.Ad,
                DurumId = sandik.DurumId,
                DurumMetni = _lookupCache.GetDeger<LookupSandikDurum>(sandik.DurumId),
                SevkiyatDuzeltmeAcikMi = sandik.SevkiyatDuzeltmeAcikMi,
                DepoLokasyonId = sandik.DepoLokasyonId,
                DepoLokasyonMetni = _lookupCache.GetDeger<LookupDepoLokasyon>(sandik.DepoLokasyonId),
                SahayaAktarildiMi = sandikTamamenSahayaAktarildi,
                SahayaAktarilanMiktar = sahayaAktarilanMiktar,
                En = sandik.En,
                Boy = sandik.Boy,
                Yukseklik = sandik.Yukseklik,
                NetKg = sandik.NetKg,
                GrossKg = sandik.GrossKg,
                Icerikler = icerikler.Select(i =>
                {
                    var anaIstenen = i.CekiSatiri?.IstenenAdet ?? i.Miktar;
                    var tahsisSayisi = i.CekiSatiriId.HasValue
                        ? tahsisSayilari.GetValueOrDefault(i.CekiSatiriId.Value, 1)
                        : 1;
                    var sandikMiktari = i.CekiSatiri != null
                        ? SandikTahsisHelper.HesaplaSandikMiktari(i.CekiSatiri, i, tahsisSayisi)
                        : (i.TahsisMiktari > 0 ? i.TahsisMiktari : i.Miktar);
                    var gridKapandi = i.CekiSatiri?.GridDurumuId == (int)GridDurum.GridKapandi;
                    var konulan = gridKapandi ? sandikMiktari : i.KonulanAdet;
                    var eksik = gridKapandi ? 0 : Math.Max(sandikMiktari - konulan, 0);
                    var ilgiliTransferler = i.CekiSatiriId.HasValue
                        ? sandikTransferleri.Where(t => t.CekiSatiriId == i.CekiSatiriId)
                        : sandikTransferleri.Where(t => t.KaynakSandikIcerikId == i.Id);
                    var transferOzeti = SandikTransferOzetiHelper.Hesapla(ilgiliTransferler, sandik.Id);

                    // Durum: konulana göre hesapla
                    string durumMetni;
                    if (konulan <= 0)
                        durumMetni = "Gelmedi";
                    else if (konulan >= sandikMiktari)
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
                        AnaIstenenAdet = anaIstenen,
                        SandikMiktari = sandikMiktari,
                        IstenenAdet = sandikMiktari,
                        KonulanAdet = konulan,
                        EksikAdet = eksik,
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
                        SandikAktarilanGiris = transferOzeti.Giris,
                        SandikAktarilanCikis = transferOzeti.Cikis,
                        SandikTransferOzeti = string.IsNullOrWhiteSpace(transferOzeti.Metin) ? null : transferOzeti.Metin,
                        // KURAL 3: Backend-hesaplanan alanlar (Dumb UI)
                        KalanMiktar = eksik,
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
