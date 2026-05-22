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
        private readonly IProjeRepository _projeRepository;
        private readonly ILookupCacheService _lookupCache;

        public DashboardOzetQueryHandler(IProjeRepository projeRepository, ILookupCacheService lookupCache)
        {
            _projeRepository = projeRepository;
            _lookupCache = lookupCache;
        }

        public async Task<Result<DashboardOzetDto>> Handle(DashboardOzetQuery request, CancellationToken cancellationToken)
        {
            var projeler = (await _projeRepository.GetAllWithDetailsAsync(cancellationToken)).ToList();
            var projeItems = projeler.Select(p => DashboardProjection.ToProjeItem(p, _lookupCache)).ToList();
            var depoSandiklar = new List<(Sandik Sandik, int LokasyonId)>();

            foreach (var proje in projeler)
            {
                var cekiSatirlari = proje.Cekiler?.SelectMany(c => c.CekiSatirlari).ToList() ?? new List<CekiSatiri>();
                var gridKapandiSandikNolari = cekiSatirlari
                    .Where(cs => cs.GridDurumuId == (int)GridDurum.GridKapandi)
                    .Select(cs => cs.FiiliSandikNo ?? cs.CekideGecenSandikNo)
                    .Where(sandikNo => !string.IsNullOrWhiteSpace(sandikNo))
                    .Select(sandikNo => sandikNo!.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var sandik in proje.Sandiklar.Where(s => DashboardProjection.DepodaSayilacakSandik(s, gridKapandiSandikNolari)))
                {
                    var lokasyonId = DashboardProjection.EtkinDepoLokasyonId(sandik, gridKapandiSandikNolari);
                    if (lokasyonId != (int)DepoLokasyon.Belirsiz)
                        depoSandiklar.Add((sandik, lokasyonId));
                }
            }

            var sahaProjeleri = projeItems.Where(p => p.ProjeTipiId == (int)ProjeTipi.Saha).ToList();
            var yedekProjeleri = projeItems.Where(p => p.ProjeTipiId == (int)ProjeTipi.Yedek).ToList();
            var sahaToplam = sahaProjeleri.Sum(p => p.ToplamUrunSayisi);
            var sahaTamamlanan = sahaProjeleri.Sum(p => p.TamamlananUrunSayisi);
            var yedekToplam = yedekProjeleri.Sum(p => p.ToplamUrunSayisi);
            var yedekTamamlanan = yedekProjeleri.Sum(p => p.TamamlananUrunSayisi);

            var ozet = new DashboardOzetDto
            {
                ToplamProje = projeItems.Count,
                HazirlananProje = projeItems.Count(p => p.DurumId == (int)ProjeDurum.Hazirlaniyor),
                BeklemedeProje = projeItems.Count(p => p.DurumId == (int)ProjeDurum.Beklemede),
                TamamlananProje = projeItems.Count(p => p.DurumId == (int)ProjeDurum.Tamamlandi),
                SevkEdilenProje = projeItems.Count(p => p.DurumId == (int)ProjeDurum.SevkEdildi || p.DurumId == (int)ProjeDurum.EksikSevkEdildi),
                ToplamSandik = projeItems.Sum(p => p.SandikSayisi),
                EksikUrunSayisi = projeItems.Sum(p => Math.Max(p.ToplamUrunSayisi - p.TamamlananUrunSayisi, 0)),
                ToplamDepoSandik = depoSandiklar.Count,
                DepoUcKSandik = depoSandiklar.Count(s => s.LokasyonId == (int)DepoLokasyon.UcK),
                DepoSeymenSandik = depoSandiklar.Count(s => s.LokasyonId == (int)DepoLokasyon.Seymen),
                DepoGridSandik = depoSandiklar.Count(s => s.LokasyonId == (int)DepoLokasyon.Grid),
                NormalSandik = projeItems.Where(p => p.ProjeTipiId == (int)ProjeTipi.Normal).Sum(p => p.SandikSayisi),
                SahaSandik = sahaProjeleri.Sum(p => p.SandikSayisi),
                YedekSandik = yedekProjeleri.Sum(p => p.SandikSayisi),
                SahaYuzde = sahaToplam > 0 ? (int)Math.Floor((decimal)sahaTamamlanan / sahaToplam * 100) : 0,
                YedekYuzde = yedekToplam > 0 ? (int)Math.Floor((decimal)yedekTamamlanan / yedekToplam * 100) : 0
            };
            ozet.DepoDigerSandik = Math.Max(ozet.ToplamDepoSandik - ozet.DepoUcKSandik - ozet.DepoSeymenSandik - ozet.DepoGridSandik, 0);

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
