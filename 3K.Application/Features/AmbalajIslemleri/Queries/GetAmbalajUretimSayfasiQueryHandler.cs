using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.AmbalajIslemleri.Queries;

public sealed class GetAmbalajUretimSayfasiQueryHandler(
    IUnitOfWork unitOfWork,
    IRolService rolService,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetAmbalajUretimSayfasiQuery, Result<AmbalajUretimSayfasiDto>>
{
    public async Task<Result<AmbalajUretimSayfasiDto>> Handle(
        GetAmbalajUretimSayfasiQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var yetkiler = await AmbalajYetkilendirmeYardimcisi.GorunumYetkileriniGetirAsync(
            rolService, currentUserService, cancellationToken);
        if (!yetkiler.KaynakGorunur)
            request.KaynakModul = null;

        var query = AmbalajSorguYardimcisi.Filtrele(
            unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable(), request);

        // Count ve tüm filtre toplamları sayfalamadan önce DB tarafında hesaplanır.
        var summary = query.GroupBy(_ => 1)
            .Select(group => new AmbalajFiltreOzetiDto
            {
                KayitSayisi = group.Count(),
                ProjeSandikKayitSayisi = group.Count(x => x.Tur == _3K.Core.Enums.AmbalajSandikTuru.Normal),
                OzelSandikKayitSayisi = group.Count(x => x.Tur != _3K.Core.Enums.AmbalajSandikTuru.Normal),
                ToplamSandikAdedi = group.Sum(x => x.Adet),
                NetM3 = group.Where(x => x.AmbalajaDahil && x.UretimeAlindi)
                    .Sum(x => x.M3Override ?? x.HesaplananToplamM3),
                SarfM3 = group.Where(x => x.AmbalajaDahil && x.UretimeAlindi).Sum(x => x.SarfM3),
                ToplamM3 = group.Where(x => x.AmbalajaDahil && x.UretimeAlindi).Sum(x => x.ToplamM3)
            })
            .FirstOrDefault() ?? new AmbalajFiltreOzetiDto();

        if (!yetkiler.M3Gorunur)
        {
            summary.NetM3 = 0;
            summary.ToplamM3 = 0;
        }
        if (!yetkiler.SarfGorunur)
        {
            summary.SarfM3 = 0;
            summary.ToplamM3 = 0;
        }

        var records = query
            .OrderByDescending(x => x.UretimTarihi ?? x.CreatedDate)
            .ThenByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        var items = AmbalajSorguYardimcisi.DtolariOlustur(unitOfWork, records).ToList();
        items.ForEach(dto => AmbalajYetkilendirmeYardimcisi.DtoyuMaskele(dto, yetkiler));

        return Result<AmbalajUretimSayfasiDto>.Success(new AmbalajUretimSayfasiDto
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = summary.KayitSayisi,
            TotalPages = (int)Math.Ceiling(summary.KayitSayisi / (double)pageSize),
            FilteredSummary = summary
        });
    }
}
