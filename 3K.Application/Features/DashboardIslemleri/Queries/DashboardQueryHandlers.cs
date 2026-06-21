using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.DashboardIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
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
                EksikSevkEdilenProje = stats.EksikSevkEdilenProje,
                ToplamSandik = stats.ToplamSandik,
                EksikUrunSayisi = stats.EksikUrunSayisi,
                ToplamDepoSandik = stats.ToplamDepoSandik,
                DepoUcKSandik = stats.DepoUcKSandik,
                DepoSeymenSandik = stats.DepoSeymenSandik,
                DepoGridSandik = stats.DepoGridSandik,
                DepoDigerSandik = stats.DepoDigerSandik,
                DepoDagilimlari = MapDepoDagilimlari(stats.DepoDagilimlari),
                NormalDepoDagilimlari = MapDepoDagilimlari(stats.NormalDepoDagilimlari),
                SahaDepoDagilimlari = MapDepoDagilimlari(stats.SahaDepoDagilimlari),
                YedekDepoDagilimlari = MapDepoDagilimlari(stats.YedekDepoDagilimlari),
                NormalSandik = stats.NormalSandik,
                SahaSandik = stats.SahaSandik,
                YedekSandik = stats.YedekSandik,
                SahaYuzde = stats.SahaYuzde,
                YedekYuzde = stats.YedekYuzde,
                ProjeTipiOzetleri = stats.ProjeTipiOzetleri
                    .Select(t => new DashboardProjeTipiOzetDto
                    {
                        ProjeTipiId = t.ProjeTipiId,
                        ProjeTipiMetni = t.ProjeTipiMetni,
                        ToplamProje = t.ToplamProje,
                        HazirlananProje = t.HazirlananProje,
                        SevkEdilenProje = t.SevkEdilenProje,
                        EksikSevkEdilenProje = t.EksikSevkEdilenProje,
                        TamamlananProje = t.TamamlananProje,
                        ToplamSandik = t.ToplamSandik,
                        EksikUrunSayisi = t.EksikUrunSayisi,
                        ToplamDepoSandik = t.ToplamDepoSandik,
                        TamamlanmaYuzdesi = t.TamamlanmaYuzdesi,
                        DepoDagilimlari = MapDepoDagilimlari(t.DepoDagilimlari)
                    })
                    .ToList()
            };

            return Result<DashboardOzetDto>.Success(ozet);
        }

        private static List<DashboardDepoDagilimDto> MapDepoDagilimlari(IEnumerable<DashboardDepoDagilimRawStats> dagilimlar)
        {
            return dagilimlar
                .Select(d => new DashboardDepoDagilimDto
                {
                    DepoLokasyonId = d.DepoLokasyonId,
                    DepoLokasyonMetni = d.DepoLokasyonMetni,
                    SandikSayisi = d.SandikSayisi
                })
                .ToList();
        }
    }

    public class DashboardProjelerQueryHandler : IRequestHandler<DashboardProjelerQuery, Result<DashboardPagedResultDto<DashboardProjeItemDto>>>
    {
        private readonly IProjeRepository _projeRepository;
        private readonly ILookupCacheService _lookupCache;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public DashboardProjelerQueryHandler(
            IProjeRepository projeRepository,
            ILookupCacheService lookupCache,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _projeRepository = projeRepository;
            _lookupCache = lookupCache;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public async Task<Result<DashboardPagedResultDto<DashboardProjeItemDto>>> Handle(DashboardProjelerQuery request, CancellationToken cancellationToken)
        {
            var page = Math.Max(request.Page, 1);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var (projeler, totalCount) = await _projeRepository.GetFilteredPagedAsync(
                request.ProjeTipiId,
                searchTerm: null,
                isSevkEdilen: null,
                page,
                pageSize,
                cancellationToken);
            var normalKaynakSatirIds = projeler
                .Where(p => p.ProjeTipiId == (int)ProjeTipi.Normal)
                .SelectMany(p => p.Cekiler?.SelectMany(c => c.CekiSatirlari) ?? Enumerable.Empty<CekiSatiri>())
                .Where(cs => !cs.KaynakCekiSatiriId.HasValue)
                .Select(cs => cs.Id)
                .ToList();
            var sahaTamamlamaMap = await _sahaTamamlamaService.GetSevkEdilenTamamlamaMapAsync(normalKaynakSatirIds, cancellationToken);
            var items = projeler.Select(p => DashboardProjection.ToProjeItem(p, _lookupCache, sahaTamamlamaMap)).ToList();

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
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public DashboardKritikEksiklerQueryHandler(
            IUnitOfWork unitOfWork,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public async Task<Result<DashboardPagedResultDto<DashboardKritikProjeDto>>> Handle(DashboardKritikEksiklerQuery request, CancellationToken cancellationToken)
        {
            var page = Math.Max(request.Page, 1);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var ranked = (await DashboardRankHelpers.BuildRankRowsAsync(_unitOfWork, _sahaTamamlamaService, cancellationToken))
                .Select(r => new DashboardKritikProjeDto
                {
                    ProjeNo = r.ProjeNo,
                    Eksik = r.Eksik,
                    Toplam = r.Toplam,
                    Sandik = r.Sandik
                })
                .ToList();

            return Result<DashboardPagedResultDto<DashboardKritikProjeDto>>.Success(DashboardRankHelpers.ToPaged(ranked, page, pageSize));
        }
    }

    public class DashboardEksikSiralamaQueryHandler : IRequestHandler<DashboardEksikSiralamaQuery, Result<DashboardPagedResultDto<DashboardEksikSiralamaDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public DashboardEksikSiralamaQueryHandler(
            IUnitOfWork unitOfWork,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public async Task<Result<DashboardPagedResultDto<DashboardEksikSiralamaDto>>> Handle(DashboardEksikSiralamaQuery request, CancellationToken cancellationToken)
        {
            var page = Math.Max(request.Page, 1);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var ranked = (await DashboardRankHelpers.BuildRankRowsAsync(_unitOfWork, _sahaTamamlamaService, cancellationToken))
                .Select(r => new DashboardEksikSiralamaDto
                {
                    ProjeNo = r.ProjeNo,
                    Lokasyon = r.Lokasyon,
                    EksikAdet = r.Eksik,
                    EksikYuzde = r.Toplam > 0 ? (int)Math.Round((decimal)r.Eksik / r.Toplam * 100) : 0
                })
                .ToList();

            return Result<DashboardPagedResultDto<DashboardEksikSiralamaDto>>.Success(DashboardRankHelpers.ToPaged(ranked, page, pageSize));
        }
    }

    internal static class DashboardRankHelpers
    {
        public static async Task<IReadOnlyList<DashboardRankRow>> BuildRankRowsAsync(
            IUnitOfWork unitOfWork,
            ISahaTamamlamaService sahaTamamlamaService,
            CancellationToken cancellationToken)
        {
            var projeRows = unitOfWork.GetRepository<Proje>()
                .Queryable()
                .Select(p => new DashboardRankSourceRow
                {
                    ProjeId = p.Id,
                    ProjeNo = p.ProjeNo,
                    Lokasyon = p.Lokasyon,
                    Sandik = p.Sandiklar.Count,
                    CreatedDate = p.CreatedDate,
                    ProjeTipiId = p.ProjeTipiId,
                    Toplam = p.ProjeTipiId == (int)ProjeTipi.Saha || p.ProjeTipiId == (int)ProjeTipi.Yedek
                        ? p.Sandiklar.SelectMany(s => s.SandikIcerikleri).Count()
                        : 0,
                    Tamamlanan = p.ProjeTipiId == (int)ProjeTipi.Saha || p.ProjeTipiId == (int)ProjeTipi.Yedek
                        ? p.Sandiklar.SelectMany(s => s.SandikIcerikleri).Count(si =>
                            (si.CekiSatiriId != null ? si.CekiSatiri!.IstenenAdet : si.Miktar) > 0 &&
                            si.KonulanAdet >= (si.CekiSatiriId != null ? si.CekiSatiri!.IstenenAdet : si.Miktar))
                        : 0
                })
                .ToList();

            var normalSatirRows = unitOfWork.GetRepository<CekiSatiri>()
                .Queryable()
                .Where(cs => cs.Ceki.Proje.ProjeTipiId == (int)ProjeTipi.Normal && !cs.KaynakCekiSatiriId.HasValue)
                .Select(cs => new DashboardRankCekiSatiriRow
                {
                    Id = cs.Id,
                    ProjeId = cs.Ceki.ProjeId,
                    IstenenAdet = cs.IstenenAdet,
                    GelenMiktar = cs.GelenMiktar,
                    StokKarsilanan = cs.StokKarsilanan,
                    ProjeKarsilanan = cs.ProjeKarsilanan,
                    TedarikciKarsilanan = cs.TedarikciKarsilanan,
                    ProjeGonderilen = cs.ProjeGonderilen,
                    TrafoSevkAdet = cs.TrafoSevkAdet,
                    HataliMiktar = cs.HataliMiktar,
                    DurumId = cs.DurumId,
                    GridDurumuId = cs.GridDurumuId
                })
                .ToList();

            var sahaTamamlamaMap = await sahaTamamlamaService.GetSevkEdilenTamamlamaMapAsync(
                normalSatirRows.Select(r => r.Id),
                cancellationToken);

            var normalOzet = normalSatirRows
                .GroupBy(r => r.ProjeId)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Toplam = g.Count(),
                        Tamamlanan = g.Count(r => HesaplaEtkinKalan(r, sahaTamamlamaMap) <= 0)
                    });

            var rows = projeRows
                .Select(r =>
                {
                    if (r.ProjeTipiId == (int)ProjeTipi.Normal && normalOzet.TryGetValue(r.ProjeId, out var ozet))
                    {
                        r.Toplam = ozet.Toplam;
                        r.Tamamlanan = ozet.Tamamlanan;
                    }

                    return r;
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

        private static decimal HesaplaEtkinKalan(
            DashboardRankCekiSatiriRow row,
            IReadOnlyDictionary<int, decimal> sahaTamamlamaMap)
        {
            var hamKalan = CekiSatiriKalanHelper.HesaplaHamKalan(
                row.IstenenAdet,
                row.GelenMiktar,
                row.StokKarsilanan,
                row.ProjeKarsilanan,
                row.TedarikciKarsilanan,
                row.ProjeGonderilen,
                row.TrafoSevkAdet,
                row.HataliMiktar,
                row.DurumId,
                row.GridDurumuId);

            var sahaTamamlanan = sahaTamamlamaMap.TryGetValue(row.Id, out var value) ? value : 0;
            return Math.Max(hamKalan - sahaTamamlanan, 0);
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
        public int ProjeId { get; set; }
        public string ProjeNo { get; set; } = string.Empty;
        public string? Lokasyon { get; set; }
        public int Sandik { get; set; }
        public int Toplam { get; set; }
        public int Tamamlanan { get; set; }
        public int ProjeTipiId { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    internal class DashboardRankCekiSatiriRow
    {
        public int Id { get; set; }
        public int ProjeId { get; set; }
        public decimal IstenenAdet { get; set; }
        public decimal GelenMiktar { get; set; }
        public decimal StokKarsilanan { get; set; }
        public decimal ProjeKarsilanan { get; set; }
        public decimal TedarikciKarsilanan { get; set; }
        public decimal ProjeGonderilen { get; set; }
        public decimal TrafoSevkAdet { get; set; }
        public decimal HataliMiktar { get; set; }
        public int DurumId { get; set; }
        public int GridDurumuId { get; set; }
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
