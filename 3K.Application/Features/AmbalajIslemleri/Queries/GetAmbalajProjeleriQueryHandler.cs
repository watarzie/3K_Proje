using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.AmbalajIslemleri.Queries
{
    public sealed class GetAmbalajProjeleriQueryHandler
        : IRequestHandler<GetAmbalajProjeleriQuery, Result<PaginatedList<AmbalajProjeOzetDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRolService _rolService;
        private readonly ICurrentUserService _currentUserService;

        public GetAmbalajProjeleriQueryHandler(
            IUnitOfWork unitOfWork,
            IRolService rolService,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _rolService = rolService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<PaginatedList<AmbalajProjeOzetDto>>> Handle(
            GetAmbalajProjeleriQuery request,
            CancellationToken cancellationToken)
        {
            var yetkiler = await AmbalajYetkilendirmeYardimcisi.GorunumYetkileriniGetirAsync(
                _rolService, _currentUserService, cancellationToken);
            var query = _unitOfWork.GetRepository<Proje>().Queryable();
            if (request.ProjeTipiId.HasValue)
                query = query.Where(p => p.ProjeTipiId == request.ProjeTipiId.Value);
            query = AmbalajProjeAramaFiltresi.Uygula(query, request.Arama);

            var toplam = query.Count();
            var projeler = query
                .OrderByDescending(p => p.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();
            var projeIds = projeler.Select(p => p.Id).ToList();
            var kaynakOzetleri = yetkiler.KaynakGorunur
                ? _unitOfWork.GetRepository<Sandik>().Queryable()
                    .Where(s => projeIds.Contains(s.ProjeId))
                    .GroupBy(s => s.ProjeId)
                    .Select(g => new
                    {
                        ProjeId = g.Key,
                        KaynakSandikSayisi = g.Count(),
                        EksikOlculuKaynakSayisi = g.Count(s =>
                            !s.Boy.HasValue || s.Boy <= 0 ||
                            !s.En.HasValue || s.En <= 0 ||
                            !s.Yukseklik.HasValue || s.Yukseklik <= 0)
                    })
                    .ToDictionary(x => x.ProjeId)
                : [];
            var kayitOzetleri = _unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable()
                .Where(k => k.ProjeId.HasValue && projeIds.Contains(k.ProjeId.Value) && !k.IptalMi)
                .GroupBy(k => k.ProjeId!.Value)
                .Select(g => new
                {
                    ProjeId = g.Key,
                    AmbalajaDahilSandikAdedi = g.Sum(k => k.AmbalajaDahil ? k.Adet : 0),
                    UretimeAlinanSandikAdedi = g.Sum(k =>
                        k.AmbalajaDahil && k.UretimeAlindi ? k.Adet : 0),
                    TamamlananSandikAdedi = g.Sum(k =>
                        k.AmbalajaDahil &&
                        k.UretimeAlindi &&
                        k.UretimDurumu == AmbalajUretimDurumu.Tamamlandi
                            ? k.Adet
                            : 0),
                    NetM3 = yetkiler.M3Gorunur
                        ? g.Sum(k => k.AmbalajaDahil && k.UretimeAlindi
                            ? k.M3Override ?? k.HesaplananToplamM3
                            : 0)
                        : 0,
                    SarfM3 = yetkiler.SarfGorunur
                        ? g.Sum(k => k.AmbalajaDahil && k.UretimeAlindi ? k.SarfM3 : 0)
                        : 0,
                    ToplamM3 = yetkiler.M3Gorunur && yetkiler.SarfGorunur
                        ? g.Sum(k => k.AmbalajaDahil && k.UretimeAlindi ? k.ToplamM3 : 0)
                        : 0,
                    SonUretimTarihi = g
                        .Where(k => k.AmbalajaDahil && k.UretimeAlindi)
                        .Max(k => k.UretimTarihi)
                })
                .ToDictionary(x => x.ProjeId);

            var dtolar = projeler.Select(proje =>
            {
                kaynakOzetleri.TryGetValue(proje.Id, out var kaynakOzeti);
                kayitOzetleri.TryGetValue(proje.Id, out var kayitOzeti);
                return new AmbalajProjeOzetDto
                {
                    M3BilgisiGorunurMu = yetkiler.M3Gorunur,
                    SarfBilgisiGorunurMu = yetkiler.SarfGorunur,
                    KaynakBilgisiGorunurMu = yetkiler.KaynakGorunur,
                    ProjeId = proje.Id,
                    ProjeNo = proje.ProjeNo,
                    FbNo = proje.FBNo,
                    Musteri = proje.Musteri,
                    ProjeTipiId = proje.ProjeTipiId,
                    KaynakSandikSayisi = kaynakOzeti?.KaynakSandikSayisi ?? 0,
                    EksikOlculuKaynakSayisi = kaynakOzeti?.EksikOlculuKaynakSayisi ?? 0,
                    AmbalajaDahilSandikAdedi = kayitOzeti?.AmbalajaDahilSandikAdedi ?? 0,
                    UretimeAlinanSandikAdedi = kayitOzeti?.UretimeAlinanSandikAdedi ?? 0,
                    TamamlananSandikAdedi = kayitOzeti?.TamamlananSandikAdedi ?? 0,
                    NetM3 = kayitOzeti?.NetM3 ?? 0,
                    SarfM3 = kayitOzeti?.SarfM3 ?? 0,
                    ToplamM3 = kayitOzeti?.ToplamM3 ?? 0,
                    SonUretimTarihi = kayitOzeti?.SonUretimTarihi
                };
            }).ToList();

            return Result<PaginatedList<AmbalajProjeOzetDto>>.Success(
                new PaginatedList<AmbalajProjeOzetDto>(dtolar, toplam, request.PageNumber, request.PageSize));
        }
    }
}
