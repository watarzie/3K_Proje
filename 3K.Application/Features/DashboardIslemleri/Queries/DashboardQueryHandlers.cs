using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.DashboardIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

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
                SandikDurumOzetleri = MapSandikDurumOzetleri(stats.SandikDurumOzetleri),
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
                        DepoDagilimlari = MapDepoDagilimlari(t.DepoDagilimlari),
                        SandikDurumOzetleri = MapSandikDurumOzetleri(t.SandikDurumOzetleri)
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

        private static List<DashboardSandikDurumDto> MapSandikDurumOzetleri(
            IEnumerable<DashboardSandikDurumRawStats> durumlar)
        {
            return durumlar
                .Select(d => new DashboardSandikDurumDto
                {
                    DurumId = d.DurumId,
                    DurumMetni = d.DurumMetni,
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
            var sahaTamamlamaMap = await _sahaTamamlamaService.GetAktifGerceklesenTamamlamaMapAsync(normalKaynakSatirIds, cancellationToken);
            var sevkEdilenSahaTamamlamaMap = await _sahaTamamlamaService.GetSevkEdilenGerceklesenTamamlamaMapAsync(normalKaynakSatirIds, cancellationToken);
            var normalKaynakSandikIds = projeler
                .Where(p => p.ProjeTipiId == (int)ProjeTipi.Normal)
                .SelectMany(p => p.Sandiklar ?? Enumerable.Empty<Sandik>())
                .Select(s => s.Id)
                .ToList();
            var kaynakSandikSahaDurumu = await _sahaTamamlamaService.GetKaynakSandikSahaAktarimDurumuAsync(
                normalKaynakSandikIds,
                cancellationToken);
            var items = projeler
                .Select(p => DashboardProjection.ToProjeItem(
                    p,
                    _lookupCache,
                    sahaTamamlamaMap,
                    sevkEdilenSahaTamamlamaMap,
                    kaynakSandikSahaDurumu.SahaUzerindenSevkEdilenSandikIds))
                .ToList();

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

    public class DashboardSahayaAktarilanSandiklarQueryHandler : IRequestHandler<DashboardSahayaAktarilanSandiklarQuery, Result<DashboardPagedResultDto<DashboardSahayaAktarilanSandikDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILookupCacheService _lookupCache;

        public DashboardSahayaAktarilanSandiklarQueryHandler(
            IUnitOfWork unitOfWork,
            ILookupCacheService lookupCache)
        {
            _unitOfWork = unitOfWork;
            _lookupCache = lookupCache;
        }

        public Task<Result<DashboardPagedResultDto<DashboardSahayaAktarilanSandikDto>>> Handle(
            DashboardSahayaAktarilanSandiklarQuery request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = Math.Max(request.Page, 1);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var query = _unitOfWork.GetRepository<SahaAktarimKalemi>()
                .Queryable()
                .Where(k =>
                    k.AktarimTipiId == (int)SahaAktarimTipi.SandikBazli &&
                    k.KaynakSandikId.HasValue &&
                    k.SahaSandikId.HasValue &&
                    k.DurumId != (int)SahaAktarimDurum.GeriAlindi &&
                    k.DurumId != (int)SahaAktarimDurum.Iptal);

            if (request.ProjeId.HasValue)
            {
                query = query.Where(k =>
                    k.KaynakProjeId == request.ProjeId.Value ||
                    k.SahaProjeId == request.ProjeId.Value);
            }

            var ozetQuery = query
                .GroupBy(k => new
                {
                    k.SahaAktarimId,
                    k.KaynakProjeId,
                    KaynakProjeNo = k.KaynakProje.ProjeNo,
                    KaynakSandikId = k.KaynakSandikId!.Value,
                    KaynakSandikNo = k.KaynakSandik!.SandikNo,
                    k.SahaProjeId,
                    SahaProjeNo = k.SahaProje.ProjeNo,
                    SahaSandikId = k.SahaSandikId!.Value,
                    SahaSandikNo = k.SahaSandik!.SandikNo,
                    SandikDurumId = k.SahaSandik.DurumId,
                    AktarimTarihi = k.SahaAktarim.Tarih
                })
                .Select(g => new DashboardSahaSandikOzetRow
                {
                    SahaAktarimId = g.Key.SahaAktarimId,
                    KaynakProjeId = g.Key.KaynakProjeId,
                    KaynakProjeNo = g.Key.KaynakProjeNo,
                    KaynakSandikId = g.Key.KaynakSandikId,
                    KaynakSandikNo = g.Key.KaynakSandikNo,
                    SahaProjeId = g.Key.SahaProjeId,
                    SahaProjeNo = g.Key.SahaProjeNo,
                    SahaSandikId = g.Key.SahaSandikId,
                    SahaSandikNo = g.Key.SahaSandikNo,
                    SandikDurumId = g.Key.SandikDurumId,
                    AktarimTarihi = g.Key.AktarimTarihi,
                    SevkTarihi = g.Max(x => x.SevkTarihi),
                    ToplamUrunSayisi = g.Select(x => x.KaynakCekiSatiriId).Distinct().Count(),
                    ToplamMiktar = g.Sum(x => x.Miktar)
                });

            var totalCount = ozetQuery.Count();
            var sayfaOzetleri = ozetQuery
                .OrderByDescending(s => s.AktarimTarihi)
                .ThenByDescending(s => s.SahaAktarimId)
                .ThenBy(s => s.KaynakSandikNo)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            cancellationToken.ThrowIfCancellationRequested();

            var sayfaSahaSandikIds = sayfaOzetleri.Select(s => s.SahaSandikId).ToList();
            var durumSatirlari = sayfaSahaSandikIds.Count == 0
                ? new List<DashboardSahaSandikDurumRow>()
                : query
                    .Where(k => k.SahaSandikId.HasValue && sayfaSahaSandikIds.Contains(k.SahaSandikId.Value))
                    .Select(k => new DashboardSahaSandikDurumRow
                    {
                        SahaSandikId = k.SahaSandikId!.Value,
                        KaynakCekiSatiriId = k.KaynakCekiSatiriId,
                        DurumId = k.DurumId
                    })
                    .ToList();

            var durumMap = durumSatirlari
                .GroupBy(s => s.SahaSandikId)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(s => s.DurumId)
                        .OrderBy(d => d.Key)
                        .Select(d => new DashboardSahaAktarimDurumDto
                        {
                            DurumId = d.Key,
                            DurumMetni = GetSahaAktarimDurumMetni(d.Key),
                            UrunSayisi = d.Select(x => x.KaynakCekiSatiriId).Distinct().Count()
                        })
                        .ToList());

            var items = sayfaOzetleri
                .Select(s => new DashboardSahayaAktarilanSandikDto
                {
                    SahaAktarimId = s.SahaAktarimId,
                    KaynakProjeId = s.KaynakProjeId,
                    KaynakProjeNo = s.KaynakProjeNo,
                    KaynakSandikId = s.KaynakSandikId,
                    KaynakSandikNo = s.KaynakSandikNo,
                    SahaProjeId = s.SahaProjeId,
                    SahaProjeNo = s.SahaProjeNo,
                    SahaSandikId = s.SahaSandikId,
                    SahaSandikNo = s.SahaSandikNo,
                    SandikDurumId = s.SandikDurumId,
                    SandikDurumMetni = _lookupCache.GetDeger<LookupSandikDurum>(s.SandikDurumId),
                    ToplamUrunSayisi = s.ToplamUrunSayisi,
                    ToplamMiktar = s.ToplamMiktar,
                    AktarimTarihi = s.AktarimTarihi,
                    SevkTarihi = s.SevkTarihi,
                    AktarimDurumlari = durumMap.GetValueOrDefault(s.SahaSandikId) ?? new List<DashboardSahaAktarimDurumDto>()
                })
                .ToList();

            return Task.FromResult(Result<DashboardPagedResultDto<DashboardSahayaAktarilanSandikDto>>.Success(
                new DashboardPagedResultDto<DashboardSahayaAktarilanSandikDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    HasMore = page * pageSize < totalCount
                }));
        }

        private static string GetSahaAktarimDurumMetni(int durumId)
        {
            return (SahaAktarimDurum)durumId switch
            {
                SahaAktarimDurum.Planlandi => "Planlandı",
                SahaAktarimDurum.Hazirlaniyor => "Hazırlanıyor",
                SahaAktarimDurum.Tamamlandi => "Tamamlandı",
                SahaAktarimDurum.SevkiyatDuzeltmede => "Sevkiyat Düzeltmede",
                SahaAktarimDurum.SevkEdildi => "Sevk Edildi",
                SahaAktarimDurum.GeriAlindi => "Geri Alındı",
                SahaAktarimDurum.Iptal => "İptal",
                _ => $"Durum {durumId}"
            };
        }
    }

    public class DashboardProjeFilterOptionsQueryHandler : IRequestHandler<DashboardProjeFilterOptionsQuery, Result<List<DashboardProjeFilterOptionDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DashboardProjeFilterOptionsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task<Result<List<DashboardProjeFilterOptionDto>>> Handle(
            DashboardProjeFilterOptionsQuery request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var take = Math.Clamp(request.Take, 1, 50);
            var searchTerm = request.SearchTerm?.Trim().ToLower();
            IQueryable<DashboardProjeFilterOptionRow> query;

            if (request.SadeceSandikAktarimli)
            {
                var aktarimlar = _unitOfWork.GetRepository<SahaAktarimKalemi>()
                    .Queryable()
                    .Where(k =>
                        k.AktarimTipiId == (int)SahaAktarimTipi.SandikBazli &&
                        k.DurumId != (int)SahaAktarimDurum.GeriAlindi &&
                        k.DurumId != (int)SahaAktarimDurum.Iptal);

                var kaynakProjeler = aktarimlar.Select(k => new DashboardProjeFilterOptionRow
                {
                    Id = k.KaynakProjeId,
                    ProjeNo = k.KaynakProje.ProjeNo,
                    Musteri = k.KaynakProje.Musteri,
                    ProjeTipiId = k.KaynakProje.ProjeTipiId
                });
                var sahaProjeleri = aktarimlar.Select(k => new DashboardProjeFilterOptionRow
                {
                    Id = k.SahaProjeId,
                    ProjeNo = k.SahaProje.ProjeNo,
                    Musteri = k.SahaProje.Musteri,
                    ProjeTipiId = k.SahaProje.ProjeTipiId
                });

                query = kaynakProjeler.Concat(sahaProjeleri).Distinct();
            }
            else
            {
                query = _unitOfWork.GetRepository<Proje>()
                    .Queryable()
                    .Select(p => new DashboardProjeFilterOptionRow
                    {
                        Id = p.Id,
                        ProjeNo = p.ProjeNo,
                        Musteri = p.Musteri,
                        ProjeTipiId = p.ProjeTipiId
                    });
            }

            if (request.ProjeTipiId.HasValue)
                query = query.Where(p => p.ProjeTipiId == request.ProjeTipiId.Value);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p =>
                    p.ProjeNo.ToLower().Contains(searchTerm) ||
                    p.Musteri.ToLower().Contains(searchTerm));
            }

            var options = query
                .OrderByDescending(p => p.Id)
                .Take(take)
                .Select(p => new DashboardProjeFilterOptionDto
                {
                    Id = p.Id,
                    ProjeNo = p.ProjeNo,
                    Musteri = p.Musteri,
                    ProjeTipiId = p.ProjeTipiId
                })
                .ToList();

            return Task.FromResult(Result<List<DashboardProjeFilterOptionDto>>.Success(options));
        }
    }

    public class DashboardProjeSandikDurumQueryHandler : IRequestHandler<DashboardProjeSandikDurumQuery, Result<DashboardProjeSandikDurumDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILookupCacheService _lookupCache;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public DashboardProjeSandikDurumQueryHandler(
            IUnitOfWork unitOfWork,
            ILookupCacheService lookupCache,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _lookupCache = lookupCache;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public async Task<Result<DashboardProjeSandikDurumDto>> Handle(
            DashboardProjeSandikDurumQuery request,
            CancellationToken cancellationToken)
        {
            if (request.ProjeId <= 0)
                return Result<DashboardProjeSandikDurumDto>.Failure("Geçerli bir proje seçilmelidir.", 400);

            var proje = await _unitOfWork.GetRepository<Proje>().GetByIdAsync(request.ProjeId);
            if (proje == null)
                return Result<DashboardProjeSandikDurumDto>.Failure("Proje bulunamadı.", 404);

            cancellationToken.ThrowIfCancellationRequested();

            var sandiklar = _unitOfWork.GetRepository<Sandik>()
                .Queryable()
                .Where(s => s.ProjeId == request.ProjeId)
                .Select(s => new { s.Id, s.DurumId })
                .ToList();

            IReadOnlySet<int> sahaUzerindenSevkEdilenSandikIds = new HashSet<int>();
            if (proje.ProjeTipiId == (int)ProjeTipi.Normal && sandiklar.Count > 0)
            {
                var sahaDurumu = await _sahaTamamlamaService.GetKaynakSandikSahaAktarimDurumuAsync(
                    sandiklar.Select(s => s.Id),
                    cancellationToken);
                sahaUzerindenSevkEdilenSandikIds = sahaDurumu.SahaUzerindenSevkEdilenSandikIds;
            }

            var durumCounts = sandiklar
                .GroupBy(s => sahaUzerindenSevkEdilenSandikIds.Contains(s.Id)
                    ? (int)SandikDurum.Sevkedildi
                    : s.DurumId)
                .ToDictionary(g => g.Key, g => g.Count());

            var durumlar = Enum.GetValues<SandikDurum>()
                .Select(durum => new DashboardSandikDurumDto
                {
                    DurumId = (int)durum,
                    DurumMetni = _lookupCache.GetDeger<LookupSandikDurum>((int)durum),
                    SandikSayisi = durumCounts.GetValueOrDefault((int)durum)
                })
                .ToList();

            return Result<DashboardProjeSandikDurumDto>.Success(new DashboardProjeSandikDurumDto
            {
                ProjeId = proje.Id,
                ProjeNo = proje.ProjeNo,
                Musteri = proje.Musteri,
                ProjeTipiId = proje.ProjeTipiId,
                ToplamSandik = durumlar.Sum(d => d.SandikSayisi),
                SandikDurumOzetleri = durumlar
            });
        }
    }

    public class DashboardProjeSandiklariDrillDownQueryHandler
        : IRequestHandler<DashboardProjeSandiklariDrillDownQuery, Result<DashboardPagedResultDto<DashboardSandikDrillDownDto>>>
    {
        private readonly IDashboardSandikQueryRepository _sandikQueryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public DashboardProjeSandiklariDrillDownQueryHandler(
            IDashboardSandikQueryRepository sandikQueryRepository,
            IUnitOfWork unitOfWork,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _sandikQueryRepository = sandikQueryRepository;
            _unitOfWork = unitOfWork;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public async Task<Result<DashboardPagedResultDto<DashboardSandikDrillDownDto>>> Handle(
            DashboardProjeSandiklariDrillDownQuery request,
            CancellationToken cancellationToken)
        {
            var page = Math.Max(request.Page, 1);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var proje = await _unitOfWork.GetRepository<Proje>().GetByIdAsync(request.ProjeId);
            if (proje == null)
            {
                return Result<DashboardPagedResultDto<DashboardSandikDrillDownDto>>
                    .Failure("Proje bulunamadı.", 404);
            }

            IReadOnlySet<int> sahaUzerindenSevkEdilenSandikIds = new HashSet<int>();
            if (proje.ProjeTipiId == (int)ProjeTipi.Normal)
            {
                var kaynakSandikIds = _unitOfWork.GetRepository<Sandik>()
                    .Queryable()
                    .Where(s => s.ProjeId == request.ProjeId)
                    .Select(s => s.Id)
                    .ToList();
                var sahaDurumu = await _sahaTamamlamaService.GetKaynakSandikSahaAktarimDurumuAsync(
                    kaynakSandikIds,
                    cancellationToken);
                sahaUzerindenSevkEdilenSandikIds = sahaDurumu.SahaUzerindenSevkEdilenSandikIds;
            }

            var sonuc = await _sandikQueryRepository.GetProjeSandiklariAsync(
                new DashboardSandikDrillDownFiltresi
                {
                    ProjeId = request.ProjeId,
                    DurumId = request.DurumId,
                    SearchTerm = request.SearchTerm?.Trim(),
                    Page = page,
                    PageSize = pageSize,
                    SahaUzerindenSevkEdilenSandikIds = sahaUzerindenSevkEdilenSandikIds
                },
                cancellationToken);

            if (!sonuc.ProjeBulundu)
            {
                return Result<DashboardPagedResultDto<DashboardSandikDrillDownDto>>
                    .Failure("Proje bulunamadı.", 404);
            }

            return Result<DashboardPagedResultDto<DashboardSandikDrillDownDto>>.Success(
                new DashboardPagedResultDto<DashboardSandikDrillDownDto>
                {
                    Items = sonuc.Items.Select(sandik => new DashboardSandikDrillDownDto
                    {
                        SandikId = sandik.SandikId,
                        SandikNo = sandik.SandikNo,
                        SandikAdi = sandik.SandikAdi,
                        DurumId = sandik.DurumId,
                        DurumMetni = sandik.DurumMetni,
                        DepoLokasyonId = sandik.DepoLokasyonId,
                        DepoLokasyonMetni = sandik.DepoLokasyonMetni
                    }).ToList(),
                    TotalCount = sonuc.TotalCount,
                    Page = page,
                    PageSize = pageSize,
                    HasMore = page * pageSize < sonuc.TotalCount
                });
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

            var sahaTamamlamaMap = await sahaTamamlamaService.GetAktifGerceklesenTamamlamaMapAsync(
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

    internal class DashboardSahaSandikOzetRow
    {
        public int SahaAktarimId { get; set; }
        public int KaynakProjeId { get; set; }
        public string KaynakProjeNo { get; set; } = string.Empty;
        public int KaynakSandikId { get; set; }
        public string KaynakSandikNo { get; set; } = string.Empty;
        public int SahaProjeId { get; set; }
        public string SahaProjeNo { get; set; } = string.Empty;
        public int SahaSandikId { get; set; }
        public string SahaSandikNo { get; set; } = string.Empty;
        public int SandikDurumId { get; set; }
        public DateTime AktarimTarihi { get; set; }
        public DateTime? SevkTarihi { get; set; }
        public int ToplamUrunSayisi { get; set; }
        public decimal ToplamMiktar { get; set; }
    }

    internal class DashboardSahaSandikDurumRow
    {
        public int SahaSandikId { get; set; }
        public int KaynakCekiSatiriId { get; set; }
        public int DurumId { get; set; }
    }

    internal class DashboardProjeFilterOptionRow
    {
        public int Id { get; set; }
        public string ProjeNo { get; set; } = string.Empty;
        public string Musteri { get; set; } = string.Empty;
        public int ProjeTipiId { get; set; }
    }
}
