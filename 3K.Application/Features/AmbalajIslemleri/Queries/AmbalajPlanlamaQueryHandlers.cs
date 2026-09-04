using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.AmbalajIslemleri.Queries;

public sealed class GetAmbalajPlanlamaProjeleriQueryHandler
    : IRequestHandler<GetAmbalajPlanlamaProjeleriQuery,
        Result<AmbalajPlanlamaProjeleriSayfasiDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReadQueryExecutor _readQueries;

    public GetAmbalajPlanlamaProjeleriQueryHandler(
        IUnitOfWork unitOfWork,
        IReadQueryExecutor? readQueries = null)
    {
        _unitOfWork = unitOfWork;
        _readQueries = readQueries ?? SynchronousReadQueryExecutor.Instance;
    }

    public async Task<Result<AmbalajPlanlamaProjeleriSayfasiDto>> Handle(
        GetAmbalajPlanlamaProjeleriQuery request,
        CancellationToken cancellationToken)
    {
        if (request.ProjeTipiId.HasValue && request.ProjeTipiId is not (1 or 2 or 3))
            return Result<AmbalajPlanlamaProjeleriSayfasiDto>.Failure("Proje tipi geçersiz.");
        if (request.Grup is not (1 or 2 or 3))
            return Result<AmbalajPlanlamaProjeleriSayfasiDto>.Failure("Üretim grubu geçersiz.");

        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var projeTipiId = request.ProjeTipiId ?? (int)ProjeTipi.Normal;
        var filtrelenmisProjeler = _readQueries
            .AsNoTracking(_unitOfWork.GetRepository<Proje>().Queryable())
            .Where(p => p.ProjeTipiId == projeTipiId);
        filtrelenmisProjeler = AmbalajProjeAramaFiltresi.Uygula(filtrelenmisProjeler, request.Arama);

        var totalCount = await _readQueries.CountAsync(filtrelenmisProjeler, cancellationToken);
        var sayfa = AmbalajSayfalamaYardimcisi.Olustur(request.PageNumber, pageSize, totalCount);
        var filteredSummary = request.IncludeSummary
            ? await FiltreOzetiniOlusturAsync(filtrelenmisProjeler, request.Grup, totalCount, cancellationToken)
            : null;
        var projeler = await _readQueries.ToListAsync(filtrelenmisProjeler
            .OrderByDescending(p => p.Id)
            .Skip(sayfa.Skip)
            .Take(pageSize), cancellationToken);
        var projeIds = projeler.Select(p => p.Id).ToHashSet();
        var sandikSatirlari = await _readQueries.ToListAsync(
            _readQueries.AsNoTracking(_unitOfWork.GetRepository<Sandik>().Queryable())
                .Where(s => projeIds.Contains(s.ProjeId)), cancellationToken);
        var sandiklar = sandikSatirlari
            .GroupBy(s => s.ProjeId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Sandik>)g.ToList());
        var kayitSatirlari = await _readQueries.ToListAsync(
            _readQueries.AsNoTracking(_unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable())
                .Where(k => k.ProjeId.HasValue && projeIds.Contains(k.ProjeId.Value) && !k.IptalMi),
            cancellationToken);
        var kayitlar = kayitSatirlari
            .GroupBy(k => k.ProjeId!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<AmbalajUretimKaydi>)g.ToList());
        var projeTipiIds = projeler.Select(p => p.ProjeTipiId).ToHashSet();
        var tipSatirlari = await _readQueries.ToListAsync(
            _readQueries.AsNoTracking(_unitOfWork.GetRepository<LookupProjeTipi>().Queryable())
                .Where(x => projeTipiIds.Contains(x.Id)), cancellationToken);
        var tipler = tipSatirlari
            .ToDictionary(x => x.Id, x => x.Deger);

        var sonuc = projeler.Select(proje => AmbalajPlanlamaYardimcisi.ProjeOzetDtoOlustur(
                proje,
                tipler.GetValueOrDefault(proje.ProjeTipiId, "-"),
                sandiklar.GetValueOrDefault(proje.Id, []),
                kayitlar.GetValueOrDefault(proje.Id, [])))
            .ToList();
        return Result<AmbalajPlanlamaProjeleriSayfasiDto>.Success(
            new AmbalajPlanlamaProjeleriSayfasiDto
            {
                Items = sonuc,
                PageNumber = sayfa.PageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = sayfa.TotalPages,
                FilteredSummary = filteredSummary
            });
    }

    private async Task<AmbalajPlanlamaProjeFiltreOzetiDto> FiltreOzetiniOlusturAsync(
        IQueryable<Proje> filtrelenmisProjeler,
        int grup,
        int projeSayisi,
        CancellationToken cancellationToken)
    {
        var filtrelenmisProjeIds = filtrelenmisProjeler.Select(p => p.Id);
        var aktifKayitlar = _readQueries
            .AsNoTracking(_unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable())
            .Where(k => k.ProjeId.HasValue && filtrelenmisProjeIds.Contains(k.ProjeId.Value) &&
                        !k.IptalMi && !k.BagimsizKayitMi);
        var kaynakKayitlari = await _readQueries.ToListAsync(aktifKayitlar
            .Where(k => k.KaynakKayitId.HasValue)
            .Select(k => new KaynakKayitOzetSatiri
            {
                Id = k.Id,
                ProjeId = k.ProjeId!.Value,
                KaynakSandikId = k.KaynakKayitId!.Value,
                Tur = k.Tur,
                AmbalajaDahil = k.AmbalajaDahil,
                UretimeAlindi = k.UretimeAlindi,
                CreatedDate = k.CreatedDate
            }), cancellationToken);
        var kaynakKayitMap = kaynakKayitlari
            .GroupBy(k => k.KaynakSandikId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(k => k.Id).First());
        var planBaslangiclari = kaynakKayitlari
            .GroupBy(k => k.ProjeId)
            .ToDictionary(g => g.Key, g => g.Min(k => k.CreatedDate));
        var kaynaklar = await _readQueries.ToListAsync(_readQueries
            .AsNoTracking(_unitOfWork.GetRepository<Sandik>().Queryable())
            .Where(s => filtrelenmisProjeIds.Contains(s.ProjeId))
            .Select(s => new KaynakSandikOzetSatiri
            {
                Id = s.Id,
                ProjeId = s.ProjeId,
                SandikNo = s.SandikNo,
                Ad = s.Ad,
                AdIngilizce = s.AdIngilizce,
                Boy = s.Boy,
                En = s.En,
                Yukseklik = s.Yukseklik,
                CreatedDate = s.CreatedDate
            }), cancellationToken);

        var kaynakSandikAdedi = 0;
        var kaynakHacmi = 0m;
        var eksikOlculuProjeler = new HashSet<int>();
        foreach (var kaynak in kaynaklar)
        {
            kaynakKayitMap.TryGetValue(kaynak.Id, out var kayit);
            if (kayit?.AmbalajaDahil == false)
                continue;

            var kaynakGrubu = kayit?.Tur == AmbalajSandikTuru.Ilave ||
                               (kayit == null && planBaslangiclari.TryGetValue(kaynak.ProjeId, out var baslangic) &&
                                kaynak.CreatedDate > baslangic)
                ? 2
                : 1;
            if (kaynakGrubu != grup)
                continue;

            kaynakSandikAdedi += grup == 1
                ? AmbalajPlanlamaYardimcisi.SandikAdediHesapla(kaynak.SandikNo)
                : 1;
            if (kayit?.UretimeAlindi != true)
                continue;

            kaynakHacmi += AmbalajPlanlamaYardimcisi.KaynakSandikToplamHacmiHesapla(
                kaynak.Ad,
                kaynak.AdIngilizce,
                kaynak.SandikNo,
                kaynak.Boy,
                kaynak.En,
                kaynak.Yukseklik);
            if (kaynak.Boy is not > 0 || kaynak.En is not > 0 || kaynak.Yukseklik is not > 0)
                eksikOlculuProjeler.Add(kaynak.ProjeId);
        }

        var manuelKayitlar = aktifKayitlar
            .Where(k => !k.KaynakKayitId.HasValue && k.UretimeAlindi);
        manuelKayitlar = grup switch
        {
            2 => manuelKayitlar.Where(k => k.Tur == AmbalajSandikTuru.Ilave),
            3 => manuelKayitlar.Where(k => k.Tur == AmbalajSandikTuru.Ic),
            _ => manuelKayitlar.Where(k =>
                k.Tur != AmbalajSandikTuru.Ilave && k.Tur != AmbalajSandikTuru.Ic)
        };
        var manuelOzet = (await _readQueries.ToListAsync(manuelKayitlar
            .GroupBy(_ => 1)
            .Select(g => new ManuelKayitOzeti
            {
                KayitSayisi = g.Count(),
                ToplamAdet = g.Sum(k => k.Adet),
                ToplamHacimM3 = g.Sum(k => k.M3Override ?? k.HesaplananToplamM3)
            })
            .Take(1), cancellationToken)).FirstOrDefault() ?? new ManuelKayitOzeti();

        return new AmbalajPlanlamaProjeFiltreOzetiDto
        {
            ProjeSayisi = projeSayisi,
            ToplamSandikAdedi = kaynakSandikAdedi + (grup == 1 ? manuelOzet.ToplamAdet : manuelOzet.KayitSayisi),
            ToplamHacimM3 = kaynakHacmi + manuelOzet.ToplamHacimM3,
            EksikOlculuProjeSayisi = eksikOlculuProjeler.Count
        };
    }

    private sealed class KaynakKayitOzetSatiri
    {
        public int Id { get; init; }
        public int ProjeId { get; init; }
        public int KaynakSandikId { get; init; }
        public AmbalajSandikTuru Tur { get; init; }
        public bool AmbalajaDahil { get; init; }
        public bool UretimeAlindi { get; init; }
        public DateTime CreatedDate { get; init; }
    }

    private sealed class KaynakSandikOzetSatiri
    {
        public int Id { get; init; }
        public int ProjeId { get; init; }
        public string SandikNo { get; init; } = string.Empty;
        public string? Ad { get; init; }
        public string? AdIngilizce { get; init; }
        public decimal? Boy { get; init; }
        public decimal? En { get; init; }
        public decimal? Yukseklik { get; init; }
        public DateTime CreatedDate { get; init; }
    }

    private sealed class ManuelKayitOzeti
    {
        public int KayitSayisi { get; init; }
        public int ToplamAdet { get; init; }
        public decimal ToplamHacimM3 { get; init; }
    }
}

public sealed class GetAmbalajPlanlamaPlanQueryHandler
    : IRequestHandler<GetAmbalajPlanlamaPlanQuery, Result<AmbalajPlanlamaPlanDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAmbalajPlanlamaPlanQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<Result<AmbalajPlanlamaPlanDto>> Handle(
        GetAmbalajPlanlamaPlanQuery request,
        CancellationToken cancellationToken)
    {
        var proje = await _unitOfWork.GetRepository<Proje>().GetByIdAsync(request.ProjeId);
        if (proje == null) return Result<AmbalajPlanlamaPlanDto>.Failure("Proje bulunamadı.", 404);
        if (proje.ProjeTipiId != (int)ProjeTipi.Normal)
            return Result<AmbalajPlanlamaPlanDto>.Failure("Ambalaj üretim planı yalnız normal projeler için kullanılabilir.");
        if (request.KaynakProjeTipiId.HasValue && proje.ProjeTipiId != request.KaynakProjeTipiId.Value)
            return Result<AmbalajPlanlamaPlanDto>.Failure("Proje seçilen yönetim kaynağına ait değil.");
        if (request.Grup.HasValue && request.Grup is not (1 or 2 or 3))
            return Result<AmbalajPlanlamaPlanDto>.Failure("Üretim grubu geçersiz.");

        var sandiklar = _unitOfWork.GetRepository<Sandik>().Queryable()
            .Where(s => s.ProjeId == proje.Id).ToList();
        var kayitlar = _unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable()
            .Where(k => k.ProjeId == proje.Id && !k.IptalMi).ToList();
        var tipMetni = _unitOfWork.GetRepository<LookupProjeTipi>().Queryable()
            .Where(x => x.Id == proje.ProjeTipiId).Select(x => x.Deger).FirstOrDefault() ?? "-";
        return Result<AmbalajPlanlamaPlanDto>.Success(
            AmbalajPlanlamaYardimcisi.PlanDtoOlustur(proje, tipMetni, sandiklar, kayitlar, request.Grup));
    }
}

public sealed class GetAmbalajIcSandikSablonlariQueryHandler
    : IRequestHandler<GetAmbalajIcSandikSablonlariQuery,
        Result<IReadOnlyList<AmbalajIcSandikSablonDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    public GetAmbalajIcSandikSablonlariQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public Task<Result<IReadOnlyList<AmbalajIcSandikSablonDto>>> Handle(
        GetAmbalajIcSandikSablonlariQuery request,
        CancellationToken cancellationToken)
    {
        var sonuc = _unitOfWork.GetRepository<AmbalajIcSandikSablonu>().Queryable()
            .OrderBy(x => x.Ad)
            .ToList()
            .Select(x => new AmbalajIcSandikSablonDto(
                x.Id, x.Ad,
                AmbalajPlanlamaYardimcisi.SandikTipiMetni(x.SandikCinsi, x.DigerSandikCinsi),
                x.Boy, x.En, x.Yukseklik))
            .ToList();
        return Task.FromResult(Result<IReadOnlyList<AmbalajIcSandikSablonDto>>.Success(sonuc));
    }
}

public sealed class GetAmbalajTalepEdenlerQueryHandler
    : IRequestHandler<GetAmbalajTalepEdenlerQuery, Result<IReadOnlyList<AmbalajTalepEdenDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    public GetAmbalajTalepEdenlerQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public Task<Result<IReadOnlyList<AmbalajTalepEdenDto>>> Handle(
        GetAmbalajTalepEdenlerQuery request,
        CancellationToken cancellationToken)
    {
        var sonuc = _unitOfWork.GetRepository<AmbalajTalepEden>().Queryable()
            .OrderBy(x => x.Ad)
            .Select(x => new AmbalajTalepEdenDto(x.Id, x.Ad))
            .ToList();
        return Task.FromResult(Result<IReadOnlyList<AmbalajTalepEdenDto>>.Success(sonuc));
    }
}

public sealed class GetAmbalajTalepEdenKullanicilarQueryHandler
    : IRequestHandler<GetAmbalajTalepEdenKullanicilarQuery,
        Result<IReadOnlyList<AmbalajKullaniciSecenegiDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReadQueryExecutor _readQueries;

    public GetAmbalajTalepEdenKullanicilarQueryHandler(
        IUnitOfWork unitOfWork,
        IReadQueryExecutor? readQueries = null)
    {
        _unitOfWork = unitOfWork;
        _readQueries = readQueries ?? SynchronousReadQueryExecutor.Instance;
    }

    public async Task<Result<IReadOnlyList<AmbalajKullaniciSecenegiDto>>> Handle(
        GetAmbalajTalepEdenKullanicilarQuery request,
        CancellationToken cancellationToken)
    {
        var query = _readQueries.AsNoTracking(
                _unitOfWork.GetRepository<Kullanici>().Queryable())
            .Where(kullanici => kullanici.AdSoyad != string.Empty)
            .Select(kullanici => new AmbalajKullaniciSecenegiDto(
                kullanici.Id,
                kullanici.AdSoyad));

        var kullanicilar = await _readQueries.ToListAsync(query, cancellationToken);
        var sonuc = kullanicilar
            .Where(kullanici => !string.IsNullOrWhiteSpace(kullanici.AdSoyad))
            .Select(kullanici => kullanici with { AdSoyad = kullanici.AdSoyad.Trim() })
            .GroupBy(kullanici => kullanici.AdSoyad, StringComparer.OrdinalIgnoreCase)
            .Select(grup => grup.OrderBy(kullanici => kullanici.Id).First())
            .OrderBy(kullanici => kullanici.AdSoyad, StringComparer.Create(
                System.Globalization.CultureInfo.GetCultureInfo("tr-TR"),
                ignoreCase: true))
            .ToList();

        return Result<IReadOnlyList<AmbalajKullaniciSecenegiDto>>.Success(sonuc);
    }
}

public sealed class GetAmbalajBagimsizSandiklarQueryHandler
    : IRequestHandler<GetAmbalajBagimsizSandiklarQuery,
        Result<AmbalajBagimsizSandiklarSayfasiDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReadQueryExecutor _readQueries;

    public GetAmbalajBagimsizSandiklarQueryHandler(
        IUnitOfWork unitOfWork,
        IReadQueryExecutor? readQueries = null)
    {
        _unitOfWork = unitOfWork;
        _readQueries = readQueries ?? SynchronousReadQueryExecutor.Instance;
    }

    public async Task<Result<AmbalajBagimsizSandiklarSayfasiDto>> Handle(
        GetAmbalajBagimsizSandiklarQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Tur.HasValue && request.Tur is not (2 or 3 or 4 or 5))
            return Result<AmbalajBagimsizSandiklarSayfasiDto>.Failure("Özel sandık türü geçersiz.");

        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var tumKayitlar = _readQueries
            .AsNoTracking(_unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable());
        var projelerQuery = _readQueries
            .AsNoTracking(_unitOfWork.GetRepository<Proje>().Queryable());
        var kaynakSandiklarQuery = _readQueries
            .AsNoTracking(_unitOfWork.GetRepository<Sandik>().Queryable());
        var aktifBagimsizQuery = tumKayitlar
            .Where(k => k.BagimsizKayitMi && !k.IptalMi &&
                        (k.Tur == AmbalajSandikTuru.Ilave ||
                         k.Tur == AmbalajSandikTuru.Ic ||
                         k.Tur == AmbalajSandikTuru.Saha ||
                         k.Tur == AmbalajSandikTuru.Yedek));

        var query = AmbalajBagimsizSandikAramaFiltresi.Uygula(
            aktifBagimsizQuery, projelerQuery, kaynakSandiklarQuery, tumKayitlar, request.Arama);
        if (request.Tur.HasValue)
        {
            var tur = AmbalajPlanlamaYardimcisi.OzelTurCoz(request.Tur.Value);
            query = query.Where(k => k.Tur == tur);
        }

        AmbalajBagimsizSandikFiltreOzetiDto? filteredSummary = null;
        int totalCount;
        if (request.IncludeSummary)
        {
            // Kartlar arama/tür filtresinden bağımsız global aktif toplamları taşır.
            var globalTurToplamlari = await _readQueries.ToListAsync(aktifBagimsizQuery
                .GroupBy(k => k.Tur)
                .Select(g => new BagimsizTurToplami
                {
                    Tur = g.Key,
                    KayitSayisi = g.Count(),
                    ToplamSandikAdedi = g.Sum(k => k.Adet),
                    UretimeAlinanSandikAdedi = g.Sum(k => k.UretimeAlindi ? k.Adet : 0),
                    ToplamHacimM3 = g.Sum(k => k.SandikCinsi != AmbalajSandikCinsi.Kontrplak
                        ? k.M3Override ?? k.HesaplananToplamM3
                        : 0)
                }), cancellationToken);
            var turOzetleri = new[] { 4, 5, 2, 3 }
                .Select(turId =>
                {
                    var toplam = globalTurToplamlari.FirstOrDefault(
                        x => x.Tur == AmbalajPlanlamaYardimcisi.OzelTurCoz(turId));
                    return new AmbalajBagimsizSandikTurOzetiDto
                    {
                        Tur = turId,
                        KayitSayisi = toplam?.KayitSayisi ?? 0,
                        ToplamSandikAdedi = toplam?.ToplamSandikAdedi ?? 0,
                        ToplamHacimM3 = toplam?.ToplamHacimM3 ?? 0
                    };
                })
                .ToList();
            var filtreToplami = (await _readQueries.ToListAsync(query
                .GroupBy(_ => 1)
                .Select(g => new BagimsizFiltreToplami
                {
                    KayitSayisi = g.Count(),
                    ToplamSandikAdedi = g.Sum(k => k.Adet),
                    UretimeAlinanSandikAdedi = g.Sum(k => k.UretimeAlindi ? k.Adet : 0),
                    ToplamHacimM3 = g.Sum(k => k.SandikCinsi != AmbalajSandikCinsi.Kontrplak
                        ? k.M3Override ?? k.HesaplananToplamM3
                        : 0)
                })
                .Take(1), cancellationToken)).FirstOrDefault() ?? new BagimsizFiltreToplami();
            totalCount = filtreToplami.KayitSayisi;
            filteredSummary = new AmbalajBagimsizSandikFiltreOzetiDto
            {
                KayitSayisi = totalCount,
                ToplamSandikAdedi = filtreToplami.ToplamSandikAdedi,
                UretimeAlinanSandikAdedi = filtreToplami.UretimeAlinanSandikAdedi,
                ToplamHacimM3 = filtreToplami.ToplamHacimM3,
                TurOzetleri = turOzetleri
            };
        }
        else
        {
            totalCount = await _readQueries.CountAsync(query, cancellationToken);
        }

        var sayfa = AmbalajSayfalamaYardimcisi.Olustur(request.PageNumber, pageSize, totalCount);
        var kayitlar = await _readQueries.ToListAsync(query
            .OrderByDescending(k => k.Id)
            .Skip(sayfa.Skip)
            .Take(pageSize), cancellationToken);
        var projeIds = kayitlar.Where(k => k.ProjeId.HasValue).Select(k => k.ProjeId!.Value).ToHashSet();
        var projeSatirlari = await _readQueries.ToListAsync(
            projelerQuery.Where(p => projeIds.Contains(p.Id)), cancellationToken);
        var projeler = projeSatirlari.ToDictionary(p => p.Id);
        var ustIds = kayitlar.Where(k => k.UstKayitId.HasValue).Select(k => k.UstKayitId!.Value).ToHashSet();
        var ustSatirlari = await _readQueries.ToListAsync(
            tumKayitlar.Where(k => ustIds.Contains(k.Id)), cancellationToken);
        var ustler = ustSatirlari.ToDictionary(k => k.Id);
        var kaynakIds = kayitlar.Where(k => k.KaynakKayitId.HasValue).Select(k => k.KaynakKayitId!.Value).ToHashSet();
        var kaynakSatirlari = await _readQueries.ToListAsync(
            kaynakSandiklarQuery.Where(k => kaynakIds.Contains(k.Id)), cancellationToken);
        var kaynaklar = kaynakSatirlari.ToDictionary(k => k.Id);
        var sonuc = kayitlar.Select(k => AmbalajPlanlamaYardimcisi.BagimsizDtoOlustur(
                k,
                k.ProjeId.HasValue ? projeler.GetValueOrDefault(k.ProjeId.Value) : null,
                k.UstKayitId.HasValue ? ustler.GetValueOrDefault(k.UstKayitId.Value) : null,
                k.KaynakKayitId.HasValue ? kaynaklar.GetValueOrDefault(k.KaynakKayitId.Value) : null))
            .ToList();
        return Result<AmbalajBagimsizSandiklarSayfasiDto>.Success(
            new AmbalajBagimsizSandiklarSayfasiDto
            {
                Items = sonuc,
                PageNumber = sayfa.PageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = sayfa.TotalPages,
                FilteredSummary = filteredSummary
            });
    }

    private sealed class BagimsizTurToplami
    {
        public AmbalajSandikTuru Tur { get; init; }
        public int KayitSayisi { get; init; }
        public int ToplamSandikAdedi { get; init; }
        public int UretimeAlinanSandikAdedi { get; init; }
        public decimal ToplamHacimM3 { get; init; }
    }

    private sealed class BagimsizFiltreToplami
    {
        public int KayitSayisi { get; init; }
        public int ToplamSandikAdedi { get; init; }
        public int UretimeAlinanSandikAdedi { get; init; }
        public decimal ToplamHacimM3 { get; init; }
    }
}

internal static class AmbalajSayfalamaYardimcisi
{
    internal static AmbalajSayfaBilgisi Olustur(int istenenSayfa, int sayfaBoyutu, int toplamKayit)
    {
        var toplamSayfa = toplamKayit == 0
            ? 0
            : (int)(((long)toplamKayit + sayfaBoyutu - 1) / sayfaBoyutu);
        var etkinSayfa = toplamSayfa == 0
            ? 1
            : Math.Min(Math.Max(1, istenenSayfa), toplamSayfa);
        var atlanacak = (int)Math.Min((long)(etkinSayfa - 1) * sayfaBoyutu, int.MaxValue);
        return new AmbalajSayfaBilgisi(etkinSayfa, toplamSayfa, atlanacak);
    }
}

internal readonly record struct AmbalajSayfaBilgisi(int PageNumber, int TotalPages, int Skip);

public sealed class GetAmbalajIlaveSandikAdaylariQueryHandler
    : IRequestHandler<GetAmbalajIlaveSandikAdaylariQuery,
        Result<IReadOnlyList<AmbalajIlaveSandikAdayDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    public GetAmbalajIlaveSandikAdaylariQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public Task<Result<IReadOnlyList<AmbalajIlaveSandikAdayDto>>> Handle(
        GetAmbalajIlaveSandikAdaylariQuery request,
        CancellationToken cancellationToken)
    {
        var kullanilan = _unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable()
            .Where(k => k.BagimsizKayitMi && k.Tur == AmbalajSandikTuru.Ilave &&
                        k.KaynakKayitId.HasValue && k.Id != request.MevcutKayitId && !k.IptalMi)
            .Select(k => k.KaynakKayitId!.Value)
            .ToHashSet();
        var haricTutulan = _unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable()
            .Where(k => k.ProjeId == request.ProjeId && k.KaynakKayitId.HasValue &&
                        !k.AmbalajaDahil && !k.IptalMi)
            .Select(k => k.KaynakKayitId!.Value)
            .ToHashSet();
        var sonuc = _unitOfWork.GetRepository<Sandik>().Queryable()
            .Where(s => s.ProjeId == request.ProjeId && haricTutulan.Contains(s.Id) && !kullanilan.Contains(s.Id))
            .OrderBy(s => s.SandikNo)
            .Select(s => new AmbalajIlaveSandikAdayDto(s.Id, s.SandikNo, s.Ad, s.Boy, s.En, s.Yukseklik))
            .ToList();
        return Task.FromResult(Result<IReadOnlyList<AmbalajIlaveSandikAdayDto>>.Success(sonuc));
    }
}

public sealed class GetAmbalajProjeSandikSecenekleriQueryHandler
    : IRequestHandler<GetAmbalajProjeSandikSecenekleriQuery,
        Result<IReadOnlyList<AmbalajSandikSecenegiDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReadQueryExecutor _readQueries;

    public GetAmbalajProjeSandikSecenekleriQueryHandler(
        IUnitOfWork unitOfWork,
        IReadQueryExecutor? readQueries = null)
    {
        _unitOfWork = unitOfWork;
        _readQueries = readQueries ?? SynchronousReadQueryExecutor.Instance;
    }

    public async Task<Result<IReadOnlyList<AmbalajSandikSecenegiDto>>> Handle(
        GetAmbalajProjeSandikSecenekleriQuery request,
        CancellationToken cancellationToken)
    {
        var query = _readQueries.AsNoTracking(
                _unitOfWork.GetRepository<Sandik>().Queryable())
            .Where(sandik => sandik.ProjeId == request.ProjeId)
            .Select(sandik => new AmbalajSandikSecenegiDto(
                sandik.Id,
                sandik.SandikNo,
                sandik.Ad,
                sandik.Boy,
                sandik.En,
                sandik.Yukseklik));

        var sonuc = (await _readQueries.ToListAsync(query, cancellationToken))
            .OrderBy(sandik => AmbalajPlanlamaYardimcisi.SandikSiraAnahtari(sandik.SandikNo))
            .ThenBy(sandik => sandik.SandikNo, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return Result<IReadOnlyList<AmbalajSandikSecenegiDto>>.Success(sonuc);
    }
}
