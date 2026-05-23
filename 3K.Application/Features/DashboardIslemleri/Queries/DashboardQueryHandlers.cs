using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.DashboardIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.DashboardIslemleri.Queries
{
    public class DashboardOzetQueryHandler : IRequestHandler<DashboardOzetQuery, Result<DashboardOzetDto>>
    {
        private readonly IDashboardStatsProvider _statsProvider;

        public DashboardOzetQueryHandler(IDashboardStatsProvider statsProvider)
        {
            _statsProvider = statsProvider;
        }

        public async Task<Result<DashboardOzetDto>> Handle(DashboardOzetQuery request, CancellationToken cancellationToken)
        {
            var stats = await _statsProvider.GetOzetStatsAsync(cancellationToken);

            var ozet = new DashboardOzetDto
            {
                ToplamProje = stats.ToplamProje,
                HazirlananProje = stats.HazirlananProje,
                BeklemedeProje = stats.BeklemedeProje,
                TamamlananProje = stats.TamamlananProje,
                SevkEdilenProje = stats.SevkEdilenProje,
                ToplamSandik = stats.ToplamSandik,
                EksikUrunSayisi = stats.EksikUrunSayisi,
                ToplamDepoSandik = stats.ToplamDepoSandik,
                DepoUcKSandik = stats.DepoUcKSandik,
                DepoSeymenSandik = stats.DepoSeymenSandik,
                DepoGridSandik = stats.DepoGridSandik,
                DepoDigerSandik = stats.DepoDigerSandik,
                DepoDagilimlari = stats.DepoDagilimlari
                    .Select(d => new DashboardDepoDagilimDto
                    {
                        DepoLokasyonId = d.DepoLokasyonId,
                        DepoLokasyonMetni = d.DepoLokasyonMetni,
                        SandikSayisi = d.SandikSayisi
                    })
                    .ToList(),
                NormalSandik = stats.NormalSandik,
                SahaSandik = stats.SahaSandik,
                YedekSandik = stats.YedekSandik,
                SahaYuzde = stats.SahaYuzde,
                YedekYuzde = stats.YedekYuzde
            };

            return Result<DashboardOzetDto>.Success(ozet);
        }
    }

    public class DashboardProjelerQueryHandler : IRequestHandler<DashboardProjelerQuery, Result<DashboardPagedResultDto<DashboardProjeItemDto>>>
    {
        private readonly IProjeRepository _projeRepository;
        private readonly ILookupCacheService _lookupCache;

        public DashboardProjelerQueryHandler(IProjeRepository projeRepository, ILookupCacheService lookupCache)
        {
            _projeRepository = projeRepository;
            _lookupCache = lookupCache;
        }

        public async Task<Result<DashboardPagedResultDto<DashboardProjeItemDto>>> Handle(DashboardProjelerQuery request, CancellationToken cancellationToken)
        {
            var page = Math.Max(request.Page, 1);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var totalCount = await _projeRepository.CountAsync(cancellationToken);
            var projeler = await _projeRepository.GetPagedWithDetailsAsync(page, pageSize, cancellationToken);
            var items = projeler.Select(p => DashboardProjection.ToProjeItem(p, _lookupCache)).ToList();

            return Result<DashboardPagedResultDto<DashboardProjeItemDto>>.Success(new DashboardPagedResultDto<DashboardProjeItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                HasMore = page * pageSize < totalCount
            });
        }
    }

    public class DashboardKritikEksiklerQueryHandler : IRequestHandler<DashboardKritikEksiklerQuery, Result<DashboardPagedResultDto<DashboardKritikProjeDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DashboardKritikEksiklerQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task<Result<DashboardPagedResultDto<DashboardKritikProjeDto>>> Handle(DashboardKritikEksiklerQuery request, CancellationToken cancellationToken)
        {
            var page = Math.Max(request.Page, 1);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var ranked = DashboardRankHelpers.BuildRankRows(_unitOfWork)
                .Select(r => new DashboardKritikProjeDto
                {
                    ProjeNo = r.ProjeNo,
                    Eksik = r.Eksik,
                    Toplam = r.Toplam,
                    Sandik = r.Sandik
                })
                .ToList();

            return Task.FromResult(Result<DashboardPagedResultDto<DashboardKritikProjeDto>>.Success(DashboardRankHelpers.ToPaged(ranked, page, pageSize)));
        }
    }

    public class DashboardEksikSiralamaQueryHandler : IRequestHandler<DashboardEksikSiralamaQuery, Result<DashboardPagedResultDto<DashboardEksikSiralamaDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DashboardEksikSiralamaQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task<Result<DashboardPagedResultDto<DashboardEksikSiralamaDto>>> Handle(DashboardEksikSiralamaQuery request, CancellationToken cancellationToken)
        {
            var page = Math.Max(request.Page, 1);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var ranked = DashboardRankHelpers.BuildRankRows(_unitOfWork)
                .Select(r => new DashboardEksikSiralamaDto
                {
                    ProjeNo = r.ProjeNo,
                    Lokasyon = r.Lokasyon,
                    EksikAdet = r.Eksik,
                    EksikYuzde = r.Toplam > 0 ? (int)Math.Round((decimal)r.Eksik / r.Toplam * 100) : 0
                })
                .ToList();

            return Task.FromResult(Result<DashboardPagedResultDto<DashboardEksikSiralamaDto>>.Success(DashboardRankHelpers.ToPaged(ranked, page, pageSize)));
        }
    }

    internal static class DashboardRankHelpers
    {
        public static IReadOnlyList<DashboardRankRow> BuildRankRows(IUnitOfWork unitOfWork)
        {
            var rows = unitOfWork.GetRepository<Proje>()
                .Queryable()
                .Select(p => new DashboardRankSourceRow
                {
                    ProjeNo = p.ProjeNo,
                    Lokasyon = p.Lokasyon,
                    Sandik = p.Sandiklar.Count,
                    CreatedDate = p.CreatedDate,
                    Toplam = p.ProjeTipiId == (int)ProjeTipi.Saha || p.ProjeTipiId == (int)ProjeTipi.Yedek
                        ? p.Sandiklar.SelectMany(s => s.SandikIcerikleri).Count()
                        : p.Cekiler.SelectMany(c => c.CekiSatirlari).Count(),
                    Tamamlanan = p.ProjeTipiId == (int)ProjeTipi.Saha || p.ProjeTipiId == (int)ProjeTipi.Yedek
                        ? p.Sandiklar.SelectMany(s => s.SandikIcerikleri).Count(si =>
                            (si.CekiSatiriId != null ? si.CekiSatiri!.IstenenAdet : si.Miktar) > 0 &&
                            si.KonulanAdet >= (si.CekiSatiriId != null ? si.CekiSatiri!.IstenenAdet : si.Miktar))
                        : p.Cekiler.SelectMany(c => c.CekiSatirlari).Count(cs =>
                            cs.GridDurumuId == (int)GridDurum.GridKapandi ||
                            cs.GridDurumuId == (int)GridDurum.Iptal ||
                            (cs.HataliMiktar <= 0 &&
                             cs.DurumId != (int)UrunDurum.HataliUyumsuzGonderim &&
                             cs.IstenenAdet - cs.GelenMiktar - cs.StokKarsilanan - cs.ProjeKarsilanan - cs.TedarikciKarsilanan + cs.ProjeGonderilen - cs.TrafoSevkAdet <= 0))
                })
                .ToList();

            return rows
                .Select(r => new DashboardRankRow
                {
                    ProjeNo = r.ProjeNo,
                    Lokasyon = r.Lokasyon,
                    Sandik = r.Sandik,
                    Toplam = r.Toplam,
                    Eksik = Math.Max(r.Toplam - r.Tamamlanan, 0),
                    CreatedDate = r.CreatedDate
                })
                .Where(r => r.Eksik > 0)
                .OrderByDescending(r => r.Eksik)
                .ThenByDescending(r => r.CreatedDate)
                .ThenBy(r => r.ProjeNo)
                .ToList();
        }

        public static DashboardPagedResultDto<T> ToPaged<T>(IReadOnlyList<T> source, int page, int pageSize)
        {
            return new DashboardPagedResultDto<T>
            {
                Items = source.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                TotalCount = source.Count,
                Page = page,
                PageSize = pageSize,
                HasMore = page * pageSize < source.Count
            };
        }
    }

    internal class DashboardRankSourceRow
    {
        public string ProjeNo { get; set; } = string.Empty;
        public string? Lokasyon { get; set; }
        public int Sandik { get; set; }
        public int Toplam { get; set; }
        public int Tamamlanan { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    internal class DashboardRankRow
    {
        public string ProjeNo { get; set; } = string.Empty;
        public string? Lokasyon { get; set; }
        public int Sandik { get; set; }
        public int Toplam { get; set; }
        public int Eksik { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
