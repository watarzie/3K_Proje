using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Application.Features.GridIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.GridIslemleri.Queries
{
    public class GetGridUrunlerQueryHandler : IRequestHandler<GetGridUrunlerQuery, Result<List<GridUrunDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILookupCacheService _lookupCache;

        public GetGridUrunlerQueryHandler(IUnitOfWork unitOfWork, ILookupCacheService lookupCache)
        {
            _unitOfWork = unitOfWork;
            _lookupCache = lookupCache;
        }

        public async Task<Result<List<GridUrunDto>>> Handle(GetGridUrunlerQuery request, CancellationToken cancellationToken)
        {
            // Tek sorgu: CekiSatirlari → Ceki.ProjeId filtresi ile direkt erişim (AsNoTracking GenericRepository'de)
            var satirlar = (await _unitOfWork.GetRepository<CekiSatiri>()
                .FindAsync(cs => cs.Ceki.ProjeId == request.ProjeId))
                .OrderBy(cs => cs.SiraNo)
                .ToList();

            if (!satirlar.Any())
                return Result<List<GridUrunDto>>.Failure("Bu projeye ait çeki bulunamadı.", 404);

            var satirIdler = satirlar.Select(s => s.Id).ToList();
            var sandikIcerikleri = (await _unitOfWork.GetRepository<SandikIcerik>()
                    .FindAsync(i => i.CekiSatiriId.HasValue && satirIdler.Contains(i.CekiSatiriId.Value)))
                .ToList();
            var projeSandiklari = (await _unitOfWork.GetRepository<Sandik>()
                    .FindAsync(s => s.ProjeId == request.ProjeId))
                .ToList();
            var sandikMap = projeSandiklari.ToDictionary(s => s.Id);
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

                    return new GridUrunDto
                {
                    CekiSatiriId = cs.Id,
                    SiraNo = cs.SiraNo,
                    BarkodNo = cs.BarkodNo,
                    OlcuResmiPozNo = cs.OlcuResmiPozNo,
                    Aciklama = cs.Aciklama,
                    IstenenAdet = cs.IstenenAdet,
                    BirimId = cs.BirimId,
                    Birim = ((Birim)cs.BirimId).ToString(),
                    SandikNo = sandikNo,
                    SandikDurumId = sandik?.DurumId,
                    SandikDurumMetni = sandik != null ? _lookupCache.GetDeger<LookupSandikDurum>(sandik.DurumId) : null,
                    SandikSevkEdildiMi = sandik?.DurumId == (int)SandikDurum.Sevkedildi,
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
                    EksikMiktar = cs.EksikMiktar,
                    KalanMiktar = cs.KalanMiktar,
                    // Kalite & Süreç
                    KaliteDurumId = cs.KaliteDurumId,
                    KaliteDurumMetni = cs.KaliteDurumId.HasValue ? _lookupCache.GetDeger<LookupKaliteDurum>(cs.KaliteDurumId.Value) : null,
                    SurecDurumId = cs.SurecDurumId,
                    SurecDurumMetni = cs.SurecDurumId.HasValue ? _lookupCache.GetDeger<LookupSurecDurum>(cs.SurecDurumId.Value) : null
                };
                })
                .ToList();

            return Result<List<GridUrunDto>>.Success(result);
        }
    }
}

