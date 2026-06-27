using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Application.Common.DTOs;
using _3K.Application.Features.GridIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.GridIslemleri.Queries
{
    public class GetGridUrunlerQueryHandler : IRequestHandler<GetGridUrunlerQuery, Result<List<GridUrunDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILookupCacheService _lookupCache;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public GetGridUrunlerQueryHandler(
            IUnitOfWork unitOfWork,
            ILookupCacheService lookupCache,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _lookupCache = lookupCache;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public async Task<Result<List<GridUrunDto>>> Handle(GetGridUrunlerQuery request, CancellationToken cancellationToken)
        {
            var proje = await _unitOfWork.GetRepository<Proje>().GetByIdAsync(request.ProjeId);
            if (proje == null)
                return Result<List<GridUrunDto>>.Failure("Proje bulunamadı.", 404);

            var projeSandiklari = (await _unitOfWork.GetRepository<Sandik>()
                    .FindAsync(s => s.ProjeId == request.ProjeId))
                .ToList();
            var sandikMap = projeSandiklari.ToDictionary(s => s.Id);
            var sandikIdler = projeSandiklari.Select(s => s.Id).ToList();
            // Tek sorgu: CekiSatirlari → Ceki.ProjeId filtresi ile direkt erişim (AsNoTracking GenericRepository'de)
            var satirlar = (await _unitOfWork.GetRepository<CekiSatiri>()
                .FindAsync(cs => cs.Ceki.ProjeId == request.ProjeId))
                .OrderBy(cs => cs.SiraNo)
                .ToList();

            var satirIdler = satirlar.Select(s => s.Id).ToList();
            var sandikIcerikleri = (await _unitOfWork.GetRepository<SandikIcerik>()
                    .FindAsync(i => i.CekiSatiriId.HasValue && satirIdler.Contains(i.CekiSatiriId.Value)))
                .ToList();
            var manuelSahaIcerikleri = proje.ProjeTipiId == (int)ProjeTipi.Saha && sandikIdler.Any()
                ? (await _unitOfWork.GetRepository<SandikIcerik>()
                    .FindAsync(i => !i.CekiSatiriId.HasValue && sandikIdler.Contains(i.SandikId)))
                    .OrderBy(i => sandikMap.GetValueOrDefault(i.SandikId)?.SandikNo)
                    .ThenBy(i => i.Id)
                    .ToList()
                : new List<SandikIcerik>();

            if (!satirlar.Any() && !manuelSahaIcerikleri.Any())
                return Result<List<GridUrunDto>>.Failure("Bu projeye ait ürün bulunamadı.", 404);

            var sahaTamamlamaMap = proje.ProjeTipiId == (int)ProjeTipi.Normal
                ? await _sahaTamamlamaService.GetAktifGerceklesenTamamlamaMapAsync(
                    satirlar.Where(s => !s.KaynakCekiSatiriId.HasValue).Select(s => s.Id),
                    cancellationToken)
                : new Dictionary<int, decimal>();
            var sahaTamamlamaIzMap = proje.ProjeTipiId == (int)ProjeTipi.Normal
                ? GetSahaTamamlamaIzMap(satirlar.Where(s => !s.KaynakCekiSatiriId.HasValue).Select(s => s.Id).ToList())
                : new Dictionary<int, List<SahaTamamlamaIzDto>>();
            var sahaKaynakIzMap = proje.ProjeTipiId == (int)ProjeTipi.Saha
                ? GetSahaKaynakIzMap(satirlar.Where(s => s.KaynakCekiSatiriId.HasValue).Select(s => s.KaynakCekiSatiriId!.Value).ToList())
                : new Dictionary<int, SahaKaynakSatirIzDto>();

            var sandikNoMap = projeSandiklari
                .Where(s => !string.IsNullOrWhiteSpace(s.SandikNo))
                .GroupBy(s => s.SandikNo.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var sandikIcerikleriBySatirId = sandikIcerikleri
                .Where(i => i.CekiSatiriId.HasValue)
                .GroupBy(i => i.CekiSatiriId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = satirlar
                .Select(cs =>
                {
                    var etkinKalan = CekiSatiriKalanHelper.HesaplaEtkinKalan(cs, sahaTamamlamaMap);
                    var gorunenUcKGelen = Math.Max(cs.GelenMiktar - cs.ProjeGonderilen, 0);
                    var netKullanilabilir = Math.Max(cs.GelenMiktar + cs.ProjeKarsilanan - cs.ProjeGonderilen, 0);
                    var sandikNo = cs.FiiliSandikNo ?? cs.CekideGecenSandikNo ?? string.Empty;
                    var satirIcerikleri = sandikIcerikleriBySatirId.GetValueOrDefault(cs.Id) ?? new List<SandikIcerik>();
                    var sandik = satirIcerikleri
                        .Select(i => sandikMap.GetValueOrDefault(i.SandikId))
                        .Where(s => s != null)
                        .Select(s => s!)
                        .FirstOrDefault(s => string.Equals(s.SandikNo, sandikNo, StringComparison.OrdinalIgnoreCase))
                        ?? satirIcerikleri
                            .Select(i => sandikMap.GetValueOrDefault(i.SandikId))
                            .FirstOrDefault(s => s != null)
                        ?? (sandikNoMap.TryGetValue(sandikNo.Trim(), out var sandikByNo) ? sandikByNo : null);
                    var gorunenSandikNo = sandik?.SandikNo ?? sandikNo;
                    var kaynakIz = cs.KaynakCekiSatiriId.HasValue
                        ? sahaKaynakIzMap.GetValueOrDefault(cs.KaynakCekiSatiriId.Value)
                        : null;
                    var sahaTamamlamalari = sahaTamamlamaIzMap.GetValueOrDefault(cs.Id) ?? new List<SahaTamamlamaIzDto>();

                    return new GridUrunDto
                {
                    CekiSatiriId = cs.Id,
                    KaynakCekiSatiriId = cs.KaynakCekiSatiriId,
                    KaynakProjeNo = kaynakIz?.KaynakProjeNo,
                    KaynakSandikNo = kaynakIz?.KaynakSandikNo,
                    KaynakSiraNo = kaynakIz?.KaynakSiraNo,
                    SahaTamamlamalari = sahaTamamlamalari,
                    SahaAktarildiMi = sahaTamamlamalari.Any(),
                    SahaAktarilanMiktar = sahaTamamlamalari.Sum(i => i.Miktar),
                    SiraNo = cs.SiraNo,
                    BarkodNo = cs.BarkodNo,
                    OlcuResmiPozNo = cs.OlcuResmiPozNo,
                    Aciklama = cs.Aciklama,
                    IstenenAdet = cs.IstenenAdet,
                    BirimId = cs.BirimId,
                    Birim = ((Birim)cs.BirimId).ToString(),
                    SandikNo = gorunenSandikNo,
                    SandikDurumId = sandik?.DurumId,
                    SandikDurumMetni = sandik != null ? _lookupCache.GetDeger<LookupSandikDurum>(sandik.DurumId) : null,
                    SandikSevkEdildiMi = sandik != null && SandikSevkKilidiHelper.SandikKilitliMi(sandik),
                    GridDurumuId = cs.GridDurumuId,
                    GridDurumuMetni = _lookupCache.GetDeger<LookupGridDurum>(cs.GridDurumuId),
                    GridGelenAdet = cs.GridGelenAdet,
                    TrafoSevkAdet = cs.TrafoSevkAdet,
                    GridSevkDurumuId = cs.GridSevkDurumuId,
                    GridSevkDurumuMetni = _lookupCache.GetDeger<LookupGridSevkDurum>(cs.GridSevkDurumuId),
                    GridSevkMiktari = cs.GridSevkMiktari,
                    YenidenSevkGerekliAdet = cs.YenidenSevkGerekliAdet,
                    GridSevkTarihi = cs.GridSevkTarihi,
                    GridAciklama = cs.GridAciklama,
                    GridEksikMiktar = cs.GridEksikMiktar,
                    UcKDurumuId = cs.UcKDurumuId,
                    UcKDurumuMetni = _lookupCache.GetDeger<LookupUcKDurum>(cs.UcKDurumuId),
                    GelenMiktar = gorunenUcKGelen,
                    GeriGonderilenMiktar = cs.GeriGonderilenMiktar,
                    GeriGonderilmeSebebiId = cs.GeriGonderilmeSebebiId,
                    GeriGonderilmeSebebiMetni = cs.GeriGonderilmeSebebiId.HasValue
                        ? _lookupCache.GetDeger<LookupGeriGonderilmeSebebi>(cs.GeriGonderilmeSebebiId.Value)
                        : null,
                    KaynakHedefProjeNo = cs.KaynakHedefProjeNo,
                    UcKAciklama = cs.UcKAciklama,
                    GenelDurumId = cs.DurumId,
                    GenelDurumMetni = _lookupCache.GetDeger<LookupUrunDurum>(cs.DurumId),
                    // Madde 2: Parçalı karşılama
                    StokKarsilanan = cs.StokKarsilanan,
                    ProjeKarsilanan = cs.ProjeKarsilanan,
                    ProjeGonderilen = cs.ProjeGonderilen,
                    NetKullanilabilir = netKullanilabilir,
                    TedarikciKarsilanan = cs.TedarikciKarsilanan,
                    EksikMiktar = etkinKalan,
                    KalanMiktar = etkinKalan,
                    // Kalite & Süreç
                    KaliteDurumId = cs.KaliteDurumId,
                    KaliteDurumMetni = cs.KaliteDurumId.HasValue ? _lookupCache.GetDeger<LookupKaliteDurum>(cs.KaliteDurumId.Value) : null,
                    SurecDurumId = cs.SurecDurumId,
                    SurecDurumMetni = cs.SurecDurumId.HasValue ? _lookupCache.GetDeger<LookupSurecDurum>(cs.SurecDurumId.Value) : null
                };
                })
                .ToList();

            var manuelSiraNo = satirlar.Any() ? satirlar.Max(s => s.SiraNo) : 0;
            result.AddRange(manuelSahaIcerikleri.Select((icerik, index) =>
                MapSahaManuelIcerik(icerik, sandikMap.GetValueOrDefault(icerik.SandikId), manuelSiraNo + index + 1)));

            return Result<List<GridUrunDto>>.Success(result);
        }

        private GridUrunDto MapSahaManuelIcerik(SandikIcerik icerik, Sandik? sandik, int siraNo)
        {
            var birimId = icerik.BirimId ?? (int)Birim.Adet;
            var miktar = icerik.Miktar > 0 ? icerik.Miktar : icerik.KonulanAdet;
            var gorunenAd = !string.IsNullOrWhiteSpace(icerik.Isim)
                ? icerik.Isim!
                : icerik.Aciklama ?? string.Empty;

            return new GridUrunDto
            {
                CekiSatiriId = -icerik.Id,
                SandikIcerikId = icerik.Id,
                IsSahaManuelSandikIcerigi = true,
                SiraNo = siraNo,
                BarkodNo = icerik.BarkodNo ?? string.Empty,
                OlcuResmiPozNo = null,
                Aciklama = gorunenAd,
                IstenenAdet = miktar,
                BirimId = birimId,
                Birim = ((Birim)birimId).ToString(),
                SandikNo = sandik?.SandikNo ?? string.Empty,
                SandikDurumId = sandik?.DurumId,
                SandikDurumMetni = sandik != null ? _lookupCache.GetDeger<LookupSandikDurum>(sandik.DurumId) : null,
                SandikSevkEdildiMi = sandik != null && SandikSevkKilidiHelper.SandikKilitliMi(sandik),
                GridDurumuId = (int)GridDurum.TamGeldi,
                GridDurumuMetni = _lookupCache.GetDeger<LookupGridDurum>((int)GridDurum.TamGeldi),
                GridGelenAdet = miktar,
                TrafoSevkAdet = 0,
                GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi,
                GridSevkDurumuMetni = _lookupCache.GetDeger<LookupGridSevkDurum>((int)GridSevkDurum.SevkEdildi),
                GridSevkMiktari = miktar,
                YenidenSevkGerekliAdet = 0,
                GridSevkTarihi = null,
                GridAciklama = null,
                GridEksikMiktar = 0,
                UcKDurumuId = (int)UcKDurum.TamGeldi,
                UcKDurumuMetni = _lookupCache.GetDeger<LookupUcKDurum>((int)UcKDurum.TamGeldi),
                GelenMiktar = miktar,
                GeriGonderilenMiktar = 0,
                KaynakHedefProjeNo = icerik.KaynakProjeNo,
                UcKAciklama = icerik.Aciklama,
                GenelDurumId = (int)UrunDurum.Tamamlandi,
                GenelDurumMetni = _lookupCache.GetDeger<LookupUrunDurum>((int)UrunDurum.Tamamlandi),
                StokKarsilanan = 0,
                ProjeKarsilanan = 0,
                ProjeGonderilen = 0,
                NetKullanilabilir = miktar,
                TedarikciKarsilanan = 0,
                EksikMiktar = 0,
                KalanMiktar = 0
            };
        }

        private Dictionary<int, List<SahaTamamlamaIzDto>> GetSahaTamamlamaIzMap(List<int> kaynakSatirIds)
        {
            if (!kaynakSatirIds.Any())
                return new Dictionary<int, List<SahaTamamlamaIzDto>>();

            var izler = _unitOfWork.GetRepository<SandikIcerik>().Queryable()
                .Where(i =>
                    i.CekiSatiriId.HasValue &&
                    i.CekiSatiri!.KaynakCekiSatiriId.HasValue &&
                    kaynakSatirIds.Contains(i.CekiSatiri.KaynakCekiSatiriId.Value) &&
                    i.Sandik.Proje.ProjeTipiId == (int)ProjeTipi.Saha)
                .Select(i => new SahaTamamlamaIzDto
                {
                    KaynakCekiSatiriId = i.CekiSatiri!.KaynakCekiSatiriId!.Value,
                    KaynakProjeId = i.CekiSatiri.KaynakCekiSatiri!.Ceki.ProjeId,
                    KaynakProjeNo = i.CekiSatiri.KaynakCekiSatiri.Ceki.Proje.ProjeNo,
                    KaynakSandikNo = i.CekiSatiri.KaynakCekiSatiri.FiiliSandikNo ?? i.CekiSatiri.KaynakCekiSatiri.CekideGecenSandikNo,
                    KaynakSiraNo = i.CekiSatiri.KaynakCekiSatiri.SiraNo,
                    KaynakUrunAdi = i.CekiSatiri.KaynakCekiSatiri.Aciklama,
                    SahaProjeId = i.Sandik.ProjeId,
                    SahaProjeNo = i.Sandik.Proje.ProjeNo,
                    SahaSandikId = i.SandikId,
                    SahaSandikNo = i.Sandik.SandikNo,
                    SahaCekiSatiriId = i.CekiSatiriId.GetValueOrDefault(),
                    Miktar = i.KonulanAdet > 0 ? i.KonulanAdet : i.CekiSatiri.IstenenAdet,
                    BirimId = i.CekiSatiri.BirimId,
                    DurumId = i.Sandik.DurumId,
                    SevkEdildiMi = i.Sandik.DurumId == (int)SandikDurum.Sevkedildi,
                    SevkTarihi = i.Sandik.Proje.GerceklesenSevkTarihi
                })
                .ToList();

            foreach (var iz in izler)
            {
                iz.Birim = ((Birim)iz.BirimId).ToString();
                iz.DurumMetni = _lookupCache.GetDeger<LookupSandikDurum>(iz.DurumId);
            }

            return izler
                .OrderBy(i => i.SahaProjeNo)
                .ThenBy(i => i.SahaSandikNo)
                .GroupBy(i => i.KaynakCekiSatiriId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        private Dictionary<int, SahaKaynakSatirIzDto> GetSahaKaynakIzMap(List<int> kaynakSatirIds)
        {
            if (!kaynakSatirIds.Any())
                return new Dictionary<int, SahaKaynakSatirIzDto>();

            return _unitOfWork.GetRepository<CekiSatiri>().Queryable()
                .Where(cs => kaynakSatirIds.Contains(cs.Id))
                .Select(cs => new SahaKaynakSatirIzDto
                {
                    CekiSatiriId = cs.Id,
                    KaynakProjeId = cs.Ceki.ProjeId,
                    KaynakProjeNo = cs.Ceki.Proje.ProjeNo,
                    KaynakSandikNo = cs.FiiliSandikNo ?? cs.CekideGecenSandikNo,
                    KaynakSiraNo = cs.SiraNo
                })
                .ToList()
                .ToDictionary(x => x.CekiSatiriId);
        }
    }
}

