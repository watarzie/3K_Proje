using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.AmbalajIslemleri.Queries;

public sealed class GetAmbalajManuelProjeSecenekleriQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAmbalajManuelProjeSecenekleriQuery, Result<AmbalajManuelProjeSecenekleriSayfasiDto>>
{
    public Task<Result<AmbalajManuelProjeSecenekleriSayfasiDto>> Handle(
        GetAmbalajManuelProjeSecenekleriQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable()
            .Where(x => !x.ProjeId.HasValue && x.ManuelProjeNo != null && x.ManuelProjeNo != "");
        if (!string.IsNullOrWhiteSpace(request.Arama))
        {
            var search = request.Arama.Trim().ToLower();
            query = query.Where(x => x.ManuelProjeNo!.ToLower().Contains(search) ||
                                     (x.ManuelProjeAdi != null && x.ManuelProjeAdi.ToLower().Contains(search)));
        }

        var grouped = query.GroupBy(x => new { No = x.ManuelProjeNo!, Ad = x.ManuelProjeAdi ?? "" });
        var totalCount = grouped.Count();
        var items = grouped
            .Select(group => new AmbalajManuelProjeSecenegiDto
            {
                No = group.Key.No,
                Ad = group.Key.Ad,
                KayitSayisi = group.Count(),
                AktifKayitSayisi = group.Count(x => !x.IptalMi),
                AmbalajaDahilKayitSayisi = group.Count(x => !x.IptalMi && x.AmbalajaDahil),
                UretimeAlinmisKayitSayisi = group.Count(x => !x.IptalMi && x.AmbalajaDahil && x.UretimeAlindi),
                ToplamSandikAdedi = group.Where(x => !x.IptalMi).Sum(x => x.Adet),
                NetM3 = group.Where(x => !x.IptalMi && x.AmbalajaDahil && x.UretimeAlindi)
                    .Sum(x => x.M3Override ?? x.HesaplananToplamM3),
                SarfM3 = group.Where(x => !x.IptalMi && x.AmbalajaDahil && x.UretimeAlindi).Sum(x => x.SarfM3),
                ToplamM3 = group.Where(x => !x.IptalMi && x.AmbalajaDahil && x.UretimeAlindi).Sum(x => x.ToplamM3)
            })
            .OrderBy(x => x.No)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(Result<AmbalajManuelProjeSecenekleriSayfasiDto>.Success(new AmbalajManuelProjeSecenekleriSayfasiDto
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        }));
    }
}
