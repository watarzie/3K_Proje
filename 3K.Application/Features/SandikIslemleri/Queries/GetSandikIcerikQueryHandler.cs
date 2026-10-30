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
        private readonly ISahaAktarimSilmeKorumaService _sahaAktarimSilmeKorumaService;
        private readonly IUnitOfWork _unitOfWork;

        public GetSandikIcerikQueryHandler(
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
            var aktifAktarimBagliSatirIds = await _sahaAktarimSilmeKorumaService
                .GetAktifAktarimBagliCekiSatiriIdsAsync(cekiSatiriIdleri, cancellationToken);
            var aktifSahaProjeleri = await AktifSahaProjeleriniBulAsync(cekiSatiriIdleri);
            var proje = await _unitOfWork.GetRepository<Proje>().GetByIdAsync(sandik.ProjeId);
            var isSahaYedek = proje?.ProjeTipiId is (int)ProjeTipi.Saha or (int)ProjeTipi.Yedek;
            var sandikManuelIslemeAcik = !SandikSevkKilidiHelper.SandikKilitliMi(sandik);
            var projeTahsisleri = cekiSatiriIdleri.Count == 0
                ? new List<SandikIcerik>()
                : (await _unitOfWork.GetRepository<SandikIcerik>()
                    .FindAsync(i => i.CekiSatiriId.HasValue && cekiSatiriIdleri.Contains(i.CekiSatiriId.Value)))
                    .ToList();
            var satirTahsisMap = projeTahsisleri
                .Where(i => i.CekiSatiriId.HasValue)
                .GroupBy(i => i.CekiSatiriId!.Value)
                .ToDictionary(g => g.Key, g => (IReadOnlyCollection<SandikIcerik>)g.ToList());
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
            var kaynakSandikSahaDurumu = await _sahaTamamlamaService.GetKaynakSandikSahaAktarimDurumuAsync(
                new[] { sandik.Id },
                cancellationToken);
            var kaynakSatirlar = icerikler
                .Select(i => i.CekiSatiri)
                .Where(cs => cs != null && !cs.KaynakCekiSatiriId.HasValue)
                .Select(cs => cs!)
                .GroupBy(cs => cs.Id)
                .Select(g => g.First())
                .ToList();
            var sahayaAktarilanMiktar = kaynakSatirlar.Sum(cs => sahaTamamlamaMap.GetValueOrDefault(cs.Id));
            var fizikselSevkEdildi = sandik.DurumId == (int)SandikDurum.Sevkedildi;
            var sahaUzerindenSevkEdildi = !fizikselSevkEdildi &&
                kaynakSandikSahaDurumu.SahaUzerindenSevkEdilenSandikIds.Contains(sandik.Id);
            var sandikSahaAktariminda = !fizikselSevkEdildi &&
                !sahaUzerindenSevkEdildi &&
                kaynakSandikSahaDurumu.AktifAktarimaBagliSandikIds.Contains(sandik.Id);

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
                TipId = sandik.TipId,
                TipMetni = _lookupCache.GetDeger<LookupSandikTipi>(sandik.TipId),
                SahayaAktarildiMi = sandikSahaAktariminda,
                SahaUzerindenSevkEdildiMi = sahaUzerindenSevkEdildi,
                SahayaAktarilanMiktar = sahayaAktarilanMiktar,
                KapanmaTarihi = sandik.KapanmaTarihi,
                En = sandik.En,
                Boy = sandik.Boy,
                Yukseklik = sandik.Yukseklik,
                NetKg = sandik.NetKg,
                GrossKg = sandik.GrossKg,
                Icerikler = icerikler.Select(i =>
                {
                    var anaIstenen = i.CekiSatiri?.IstenenAdet ?? i.Miktar;
                    var satirTahsisleri = i.CekiSatiriId.HasValue
                        ? satirTahsisMap.GetValueOrDefault(i.CekiSatiriId.Value, [i])
                        : [i];
                    var (sandikMiktari, konulan, eksik) = SandikTahsisHelper.HesaplaIcerikMiktari(i, satirTahsisleri);
                    var ilgiliTransferler = i.CekiSatiriId.HasValue
                        ? sandikTransferleri.Where(t => t.CekiSatiriId == i.CekiSatiriId)
                        : sandikTransferleri.Where(t => t.KaynakSandikIcerikId == i.Id);
                    var transferOzeti = SandikTransferOzetiHelper.Hesapla(ilgiliTransferler, sandik.Id);

                    // Durum: konulana göre hesapla; eksiği kalmayan satır (Trafo Sevk vb.) tamamdır.
                    string durumMetni;
                    if (konulan >= sandikMiktari || eksik <= 0)
                        durumMetni = "Tamamlandı";
                    else if (konulan <= 0)
                        durumMetni = "Gelmedi";
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
                        ManuelSilinebilirMi = ManuelSilinebilirMi(
                            i,
                            isSahaYedek,
                            sandikManuelIslemeAcik,
                            aktifAktarimBagliSatirIds),
                        // Saha/Yedek + Birim
                        Isim = i.Isim,
                        Miktar = i.Miktar,
                        BirimId = i.BirimId,
                        // Çeki yüklemesinden gelen tahsiste birim tutulmaz; miktarın birimi çeki satırındadır.
                        BirimMetni = (i.BirimId ?? i.CekiSatiri?.BirimId) is int birimId
                            ? _lookupCache.GetDeger<LookupBirim>(birimId)
                            : null,
                        // Madde 2: Parçalı karşılama
                        StokKarsilanan = i.StokKarsilanan,
                        ProjeKarsilanan = i.ProjeKarsilanan,
                        TedarikciKarsilanan = i.TedarikciKarsilanan,
                        KaynakProjeNo = i.KaynakProjeNo,
                        UrunBilgisi = i.UrunBilgisi,
                        AktifSahaProjeleri = i.CekiSatiriId is int satirId
                            ? aktifSahaProjeleri.GetValueOrDefault(satirId, [])
                            : [],
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

        /// <summary>Kaynak satır → aktif (geri alınmamış, iptal edilmemiş) saha aktarımlarının hedef saha projeleri.</summary>
        private async Task<Dictionary<int, IReadOnlyList<SahaProjeBagDto>>> AktifSahaProjeleriniBulAsync(List<int> cekiSatiriIdleri)
        {
            if (cekiSatiriIdleri.Count == 0)
                return new Dictionary<int, IReadOnlyList<SahaProjeBagDto>>();

            var kalemler = (await _unitOfWork.GetRepository<SahaAktarimKalemi>().FindAsync(k =>
                    cekiSatiriIdleri.Contains(k.KaynakCekiSatiriId) &&
                    k.DurumId != (int)SahaAktarimDurum.GeriAlindi &&
                    k.DurumId != (int)SahaAktarimDurum.Iptal))
                .ToList();
            if (kalemler.Count == 0)
                return new Dictionary<int, IReadOnlyList<SahaProjeBagDto>>();

            var sahaProjeIds = kalemler.Select(k => k.SahaProjeId).Distinct().ToList();
            var projeNolari = (await _unitOfWork.GetRepository<Proje>().FindAsync(p => sahaProjeIds.Contains(p.Id)))
                .ToDictionary(p => p.Id, p => p.ProjeNo);
            return kalemler
                .GroupBy(k => k.KaynakCekiSatiriId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<SahaProjeBagDto>)g.Select(k => k.SahaProjeId).Distinct()
                        .Where(projeNolari.ContainsKey)
                        .Select(id => new SahaProjeBagDto(id, projeNolari[id]))
                        .OrderBy(x => x.ProjeNo, StringComparer.Ordinal)
                        .ToList());
        }

        private static bool ManuelSilinebilirMi(
            SandikIcerik icerik,
            bool isSahaYedek,
            bool sandikManuelIslemeAcik,
            IReadOnlySet<int> aktifAktarimBagliSatirIds)
        {
            if (!sandikManuelIslemeAcik || icerik.Id <= 0)
                return false;

            if (icerik.CekiSatiri == null)
                return isSahaYedek && !icerik.CekiSatiriId.HasValue;

            var satir = icerik.CekiSatiri;
            if (!satir.IsManuelEklenen ||
                satir.KaynakCekiSatiriId.HasValue ||
                aktifAktarimBagliSatirIds.Contains(satir.Id))
            {
                return false;
            }

            return !ManuelUrunSilmeKurali.IslemGormusMu(satir);
        }
    }
}
