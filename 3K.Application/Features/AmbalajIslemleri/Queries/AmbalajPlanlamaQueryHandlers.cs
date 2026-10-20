using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.AmbalajIslemleri.Queries;

public sealed class GetAmbalajPlanlamaProjeleriQueryHandler
    : IRequestHandler<GetAmbalajPlanlamaProjeleriQuery,
        Result<AmbalajPlanlamaProjeleriSayfasiDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRolService _roller;
    private readonly ICurrentUserService _kullanici;
    private readonly IReadQueryExecutor _readQueries;

    public GetAmbalajPlanlamaProjeleriQueryHandler(
        IUnitOfWork unitOfWork,
        IRolService roller,
        ICurrentUserService kullanici,
        IReadQueryExecutor? readQueries = null)
    {
        _unitOfWork = unitOfWork;
        _roller = roller;
        _kullanici = kullanici;
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

        var izinliDurumlar = await AmbalajYetkilendirmeYardimcisi.IzinliDurumlariGetirAsync(_roller, _kullanici, cancellationToken);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var projeTipiId = request.ProjeTipiId ?? (int)ProjeTipi.Normal;
        var filtrelenmisProjeler = _readQueries
            .AsNoTracking(_unitOfWork.GetRepository<Proje>().Queryable())
            .Where(p => p.ProjeTipiId == projeTipiId);
        filtrelenmisProjeler = AmbalajProjeAramaFiltresi.Uygula(filtrelenmisProjeler, request.Arama);
        filtrelenmisProjeler = AmbalajDurumErisimi.ProjeleriFiltrele(filtrelenmisProjeler,
            _unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable(), izinliDurumlar);
        filtrelenmisProjeler = AmbalajProjeTamamlanmaFiltresi.Uygula(filtrelenmisProjeler,
            _unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable(), request.Tamamlanan);
        filtrelenmisProjeler = AmbalajProjeTamamlanmaFiltresi.DurumaGore(filtrelenmisProjeler,
            _unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable(), (AmbalajUretimDurumu?)request.UretimDurumu);

        var totalCount = await _readQueries.CountAsync(filtrelenmisProjeler, cancellationToken);
        var sayfa = AmbalajSayfalamaYardimcisi.Olustur(request.PageNumber, pageSize, totalCount);
        var filteredSummary = request.IncludeSummary
            ? await FiltreOzetiniOlusturAsync(filtrelenmisProjeler, request.Grup, totalCount, izinliDurumlar, cancellationToken)
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
                .Where(k => k.ProjeId.HasValue && projeIds.Contains(k.ProjeId.Value)),
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
                kayitlar.GetValueOrDefault(proje.Id, []), izinliDurumlar))
            .ToList();
        var izin = await AmbalajYetkilendirmeYardimcisi.GorunumYetkileriniGetirAsync(_roller, _kullanici, cancellationToken);
        return Result<AmbalajPlanlamaProjeleriSayfasiDto>.Success(AmbalajAlanMaskesi.Maskele(
            new AmbalajPlanlamaProjeleriSayfasiDto
            {
                Items = sonuc,
                PageNumber = sayfa.PageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = sayfa.TotalPages,
                FilteredSummary = filteredSummary
            }, izin));
    }

    private async Task<AmbalajPlanlamaProjeFiltreOzetiDto> FiltreOzetiniOlusturAsync(
        IQueryable<Proje> filtrelenmisProjeler,
        int grup,
        int projeSayisi,
        int[]? izinliDurumlar,
        CancellationToken cancellationToken)
    {
        var filtrelenmisProjeIds = filtrelenmisProjeler.Select(p => p.Id);
        var tumProjeKayitlari = _readQueries
            .AsNoTracking(_unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable())
            .Where(k => k.ProjeId.HasValue && filtrelenmisProjeIds.Contains(k.ProjeId.Value));
        var projeKayitlari = tumProjeKayitlari.Where(k => !k.IptalMi);
        // Tamamlanmış projeye sonradan eklenen kaynaklar, açık dahil kararı olmadan
        // stok öngörüsüne de alınmaz; plan DTO'suyla aynı yaşam döngüsü sınırı korunur.
        var tamamlananProjeIds = (await _readQueries.ToListAsync(projeKayitlari
            .Where(k => k.AmbalajaDahil)
            .GroupBy(k => k.ProjeId!.Value)
            .Where(g => g.All(k => k.UretimDurumu == AmbalajUretimDurumu.Tamamlandi))
            .Select(g => g.Key), cancellationToken)).ToHashSet();
        var aktifKayitlar = projeKayitlari.Where(k => !k.BagimsizKayitMi);
        var kaynakKayitlari = await _readQueries.ToListAsync(tumProjeKayitlari
            .Where(k => !k.BagimsizKayitMi && k.KaynakKayitId.HasValue)
            .Select(k => new KaynakKayitOzetSatiri
            {
                Id = k.Id,
                ProjeId = k.ProjeId!.Value,
                KaynakSandikId = k.KaynakKayitId!.Value,
                Tur = k.Tur,
                IptalMi = k.IptalMi,
                AmbalajaDahil = k.AmbalajaDahil,
                SandikCinsi = k.SandikCinsi,
                NetM3 = k.M3Override ?? k.HesaplananToplamM3,
                Adet = k.Adet,
                Boy = k.Boy,
                En = k.En,
                Yukseklik = k.Yukseklik,
                CreatedDate = k.CreatedDate
            }), cancellationToken);
        var kaynakKayitMap = kaynakKayitlari
            .GroupBy(k => k.KaynakSandikId)
            .ToDictionary(g => g.Key, g => g.OrderBy(k => k.IptalMi).ThenByDescending(k => k.Id).First());
        var planBaslangiclari = kaynakKayitlari
            .Where(k => !k.IptalMi)
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
                TipId = s.TipId,
                Boy = s.Boy,
                En = s.En,
                Yukseklik = s.Yukseklik,
                CreatedDate = s.CreatedDate
            }), cancellationToken);

        var izinliKaynakIds = (await _readQueries.ToListAsync(AmbalajDurumErisimi.Filtrele(aktifKayitlar, izinliDurumlar)
            .Where(k => k.KaynakKayitId.HasValue).Select(k => k.KaynakKayitId!.Value), cancellationToken)).ToHashSet();

        var kaynakSandikAdedi = 0;
        var kaynakHacmi = 0m;
        var eksikOlculuProjeler = new HashSet<int>();
        foreach (var kaynak in kaynaklar)
        {
            kaynakKayitMap.TryGetValue(kaynak.Id, out var kayit);
            if (kayit?.IptalMi == true)
                continue;
            if (izinliDurumlar != null && (kayit == null ? !izinliDurumlar.Contains(1) : !izinliKaynakIds.Contains(kaynak.Id)))
                continue;
            if (kayit?.AmbalajaDahil == false || (kayit == null && AmbalajUretimPolitikasi.VarsayilanYapilmazMi(kaynak.Ad, kaynak.AdIngilizce)))
                continue;
            if (kayit == null && tamamlananProjeIds.Contains(kaynak.ProjeId))
                continue;

            var kaynakGrubu = AmbalajUretimPolitikasi.KaynakGrubu(AmbalajSandikTuru.Normal, kayit?.Tur,
                    planBaslangiclari.TryGetValue(kaynak.ProjeId, out var baslangic) ? baslangic : null, kaynak.CreatedDate)
                == AmbalajSandikTuru.Ilave
                ? 2
                : 1;
            if (kaynakGrubu != grup)
                continue;

            kaynakSandikAdedi += grup == 1
                ? AmbalajPlanlamaYardimcisi.SandikAdediHesapla(kaynak.SandikNo)
                : 1;
            // Satın alma öngörüsü seçim/üretim başlangıcını beklemez. Kaydedilmiş
            // hesap veya override korunur; henüz senkronize edilmemiş kaynak salt okunur hesaplanır.
            var cins = kayit?.SandikCinsi ?? AmbalajUretimPolitikasi.KaynakCinsi(kaynak.TipId);
            if (AmbalajUretimPolitikasi.M3HesaplanabilirMi(cins))
                kaynakHacmi += kayit?.NetM3 ?? AmbalajPlanlamaYardimcisi.KaynakSandikToplamHacmiHesapla(
                    kaynak.Ad, kaynak.AdIngilizce, kaynak.SandikNo, kaynak.Boy, kaynak.En, kaynak.Yukseklik);
            // Plan satırındaki "ölçü hazır" ile aynı kural: form ve tamamlama kaydın ölçüsünü kullanır.
            var olcuHazir = kayit != null
                ? AmbalajUretimYardimcilari.OlculerGecerli(true, kayit.SandikCinsi, kayit.Adet, kayit.Boy, kayit.En, kayit.Yukseklik)
                : AmbalajUretimYardimcilari.OlculerGecerli(true, cins, AmbalajPlanlamaYardimcisi.SandikAdediHesapla(kaynak.SandikNo),
                    kaynak.Boy, kaynak.En, kaynak.Yukseklik);
            if (!olcuHazir)
                eksikOlculuProjeler.Add(kaynak.ProjeId);
        }

        var manuelKayitlar = AmbalajDurumErisimi.Filtrele(aktifKayitlar, izinliDurumlar)
            .Where(k => !k.KaynakKayitId.HasValue && k.AmbalajaDahil);
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
                ToplamHacimM3 = g.Sum(k => ((k.SandikCinsi == _3K.Core.Enums.AmbalajSandikCinsi.AhsapKapali || k.SandikCinsi == _3K.Core.Enums.AmbalajSandikCinsi.Kafes) ? k.M3Override ?? k.HesaplananToplamM3 : 0))
            })
            .Take(1), cancellationToken)).FirstOrDefault() ?? new ManuelKayitOzeti();
        var eksikManuelProjeIds = await _readQueries.ToListAsync(manuelKayitlar
            .Where(k => k.Adet <= 0 || k.Boy <= 0 || k.En <= 0 || k.Yukseklik <= 0)
            .Select(k => k.ProjeId!.Value).Distinct(), cancellationToken);
        eksikOlculuProjeler.UnionWith(eksikManuelProjeIds);

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
        public bool IptalMi { get; init; }
        public bool AmbalajaDahil { get; init; }
        public AmbalajSandikCinsi SandikCinsi { get; init; }
        public decimal NetM3 { get; init; }
        public int Adet { get; init; }
        public decimal Boy { get; init; }
        public decimal En { get; init; }
        public decimal Yukseklik { get; init; }
        public DateTime CreatedDate { get; init; }
    }

    private sealed class KaynakSandikOzetSatiri
    {
        public int Id { get; init; }
        public int ProjeId { get; init; }
        public string SandikNo { get; init; } = string.Empty;
        public string? Ad { get; init; }
        public string? AdIngilizce { get; init; }
        public int TipId { get; init; }
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
    private readonly IRolService _roller;
    private readonly ICurrentUserService _kullanici;

    public GetAmbalajPlanlamaPlanQueryHandler(IUnitOfWork unitOfWork, IRolService roller, ICurrentUserService kullanici)
    {
        _unitOfWork = unitOfWork;
        _roller = roller;
        _kullanici = kullanici;
    }

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
            .Where(k => k.ProjeId == proje.Id).ToList();
        var tipMetni = _unitOfWork.GetRepository<LookupProjeTipi>().Queryable()
            .Where(x => x.Id == proje.ProjeTipiId).Select(x => x.Deger).FirstOrDefault() ?? "-";
        var izinliDurumlar = await AmbalajYetkilendirmeYardimcisi.IzinliDurumlariGetirAsync(_roller, _kullanici, cancellationToken);
        var plan = AmbalajPlanlamaYardimcisi.PlanDtoOlustur(proje, tipMetni, sandiklar, kayitlar, request.Grup, izinliDurumlar);
        var izin = await AmbalajYetkilendirmeYardimcisi.GorunumYetkileriniGetirAsync(_roller, _kullanici, cancellationToken);
        return Result<AmbalajPlanlamaPlanDto>.Success(AmbalajAlanMaskesi.Maskele(plan, izin));
    }
}

public sealed class GetAmbalajIcSandikSablonlariQueryHandler
    : IRequestHandler<GetAmbalajIcSandikSablonlariQuery,
        Result<IReadOnlyList<AmbalajIcSandikSablonDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRolService _roller;
    private readonly ICurrentUserService _kullanici;

    public GetAmbalajIcSandikSablonlariQueryHandler(IUnitOfWork unitOfWork, IRolService roller, ICurrentUserService kullanici)
    {
        _unitOfWork = unitOfWork;
        _roller = roller;
        _kullanici = kullanici;
    }

    public async Task<Result<IReadOnlyList<AmbalajIcSandikSablonDto>>> Handle(
        GetAmbalajIcSandikSablonlariQuery request,
        CancellationToken cancellationToken)
    {
        var izin = await AmbalajYetkilendirmeYardimcisi.GorunumYetkileriniGetirAsync(_roller, _kullanici, cancellationToken);
        var sonuc = _unitOfWork.GetRepository<AmbalajIcSandikSablonu>().Queryable()
            .OrderBy(x => x.Ad)
            .ToList()
            .Select(x => AmbalajAlanMaskesi.Maskele(new AmbalajIcSandikSablonDto(
                x.Id, x.Ad,
                AmbalajPlanlamaYardimcisi.SandikTipiMetni(x.SandikCinsi, x.DigerSandikCinsi),
                x.Boy, x.En, x.Yukseklik), izin))
            .ToList();
        return Result<IReadOnlyList<AmbalajIcSandikSablonDto>>.Success(sonuc);
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
    private readonly IRolService _roller;
    private readonly ICurrentUserService _kullanici;
    private readonly IReadQueryExecutor _readQueries;

    public GetAmbalajBagimsizSandiklarQueryHandler(
        IUnitOfWork unitOfWork,
        IRolService roller,
        ICurrentUserService kullanici,
        IReadQueryExecutor? readQueries = null)
    {
        _unitOfWork = unitOfWork;
        _roller = roller;
        _kullanici = kullanici;
        _readQueries = readQueries ?? SynchronousReadQueryExecutor.Instance;
    }

    public async Task<Result<AmbalajBagimsizSandiklarSayfasiDto>> Handle(
        GetAmbalajBagimsizSandiklarQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Tur.HasValue && request.Tur is not (2 or 3 or 4 or 5))
            return Result<AmbalajBagimsizSandiklarSayfasiDto>.Failure("Özel sandık türü geçersiz.");
        var finansDurumu = request.FinansDurumu?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(finansDurumu) && finansDurumu is not ("bekleyen" or "aktarilan"))
            return Result<AmbalajBagimsizSandiklarSayfasiDto>.Failure("Finans durumu \"bekleyen\" veya \"aktarilan\" olmalıdır.");

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
        aktifBagimsizQuery = AmbalajDurumErisimi.Filtrele(aktifBagimsizQuery,
            await AmbalajYetkilendirmeYardimcisi.IzinliDurumlariGetirAsync(_roller, _kullanici, cancellationToken));

        var query = AmbalajBagimsizSandikAramaFiltresi.Uygula(
            aktifBagimsizQuery, projelerQuery, kaynakSandiklarQuery, tumKayitlar, request.Arama);
        if (request.Tur.HasValue)
        {
            var tur = AmbalajPlanlamaYardimcisi.OzelTurCoz(request.Tur.Value);
            query = query.Where(k => k.Tur == tur);
        }
        // Finans durumu sekmeleri: sayılar arama ve tür filtresine göredir, finans durumu süzgecinden önce alınır.
        var finansSayilari = request.IncludeSummary
            ? await _readQueries.ToListAsync(query.GroupBy(k => k.UretimeAlindi).Select(g => new { Aktarildi = g.Key, Adet = g.Count() }), cancellationToken)
            : [];
        if (finansDurumu == "bekleyen") query = query.Where(k => !k.UretimeAlindi);
        else if (finansDurumu == "aktarilan") query = query.Where(k => k.UretimeAlindi);

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
                    ToplamHacimM3 = g.Sum(k => k.AmbalajaDahil && k.SandikCinsi != AmbalajSandikCinsi.Kontrplak
                        ? ((k.SandikCinsi == _3K.Core.Enums.AmbalajSandikCinsi.AhsapKapali || k.SandikCinsi == _3K.Core.Enums.AmbalajSandikCinsi.Kafes) ? k.M3Override ?? k.HesaplananToplamM3 : 0)
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
                    ToplamHacimM3 = g.Sum(k => k.AmbalajaDahil && k.SandikCinsi != AmbalajSandikCinsi.Kontrplak
                        ? ((k.SandikCinsi == _3K.Core.Enums.AmbalajSandikCinsi.AhsapKapali || k.SandikCinsi == _3K.Core.Enums.AmbalajSandikCinsi.Kafes) ? k.M3Override ?? k.HesaplananToplamM3 : 0)
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
                TurOzetleri = turOzetleri,
                FinansaAktarilmayanKayitSayisi = finansSayilari.Where(x => !x.Aktarildi).Sum(x => x.Adet),
                FinansaAktarilanKayitSayisi = finansSayilari.Where(x => x.Aktarildi).Sum(x => x.Adet)
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
        sonuc = await DenetimBilgileriniEkleAsync(kayitlar, sonuc, cancellationToken);
        var izin = await AmbalajYetkilendirmeYardimcisi.GorunumYetkileriniGetirAsync(_roller, _kullanici, cancellationToken);
        return Result<AmbalajBagimsizSandiklarSayfasiDto>.Success(AmbalajAlanMaskesi.Maskele(
            new AmbalajBagimsizSandiklarSayfasiDto
            {
                Items = sonuc,
                PageNumber = sayfa.PageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = sayfa.TotalPages,
                FilteredSummary = filteredSummary
            }, izin));
    }

    /// <summary>
    /// Satırın denetim bilgisi: oluşturan ve oluşturma tarihi; finansa aktaran ve aktarım tarihi (son "Finansa aktar"
    /// hareketi; eski yolla üretime alınmış kayıtta aktaran bilinmez, tarih üretim tarihidir).
    /// </summary>
    private async Task<List<AmbalajBagimsizSandikDto>> DenetimBilgileriniEkleAsync(
        IReadOnlyList<AmbalajUretimKaydi> kayitlar,
        List<AmbalajBagimsizSandikDto> sonuc,
        CancellationToken cancellationToken)
    {
        var ids = kayitlar.Select(k => k.Id).ToList();
        var aktarimlar = (await _readQueries.ToListAsync(
                _readQueries.AsNoTracking(_unitOfWork.GetRepository<AmbalajUretimHareketi>().Queryable())
                    .Where(h => ids.Contains(h.AmbalajUretimKaydiId) && h.Islem == Commands.AmbalajOzelSandikFinansCommandHandler.AktarimIslemi),
                cancellationToken))
            .GroupBy(h => h.AmbalajUretimKaydiId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(h => h.Tarih).ThenByDescending(h => h.Id).First());
        var kullaniciIds = kayitlar
            .Select(k => int.TryParse(k.CreatedBy, out var id) ? id : 0)
            .Concat(aktarimlar.Values.Select(h => h.KullaniciId))
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        var adlar = (await _readQueries.ToListAsync(
                _readQueries.AsNoTracking(_unitOfWork.GetRepository<Kullanici>().Queryable()).Where(k => kullaniciIds.Contains(k.Id)),
                cancellationToken))
            .ToDictionary(k => k.Id, k => k.AdSoyad);
        var kayitById = kayitlar.ToDictionary(k => k.Id);
        return sonuc.Select(dto =>
        {
            var kayit = kayitById[dto.Id];
            var aktarim = kayit.UretimeAlindi ? aktarimlar.GetValueOrDefault(kayit.Id) : null;
            return dto with
            {
                OlusturmaTarihi = kayit.CreatedDate,
                OlusturanAdi = int.TryParse(kayit.CreatedBy, out var olusturan) ? adlar.GetValueOrDefault(olusturan) : null,
                FinansaAktarimTarihi = kayit.UretimeAlindi ? aktarim?.Tarih ?? kayit.UretimTarihi : null,
                FinansaAktaranAdi = aktarim is null ? null : adlar.GetValueOrDefault(aktarim.KullaniciId)
            };
        }).ToList();
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
    private readonly IRolService _roller;
    private readonly ICurrentUserService _kullanici;

    public GetAmbalajIlaveSandikAdaylariQueryHandler(IUnitOfWork unitOfWork, IRolService roller, ICurrentUserService kullanici)
    {
        _unitOfWork = unitOfWork;
        _roller = roller;
        _kullanici = kullanici;
    }

    public async Task<Result<IReadOnlyList<AmbalajIlaveSandikAdayDto>>> Handle(
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
            .Select(s => new { s.Id, s.SandikNo, s.Ad, s.Boy, s.En, s.Yukseklik, s.TipId })
            .ToList()
            .Select(s => new AmbalajIlaveSandikAdayDto(s.Id, s.SandikNo, s.Ad, s.Boy, s.En, s.Yukseklik,
                AmbalajUretimYardimcilari.CinsMetni(AmbalajUretimPolitikasi.KaynakCinsi(s.TipId), null)))
            .ToList();
        var izin = await AmbalajYetkilendirmeYardimcisi.GorunumYetkileriniGetirAsync(_roller, _kullanici, cancellationToken);
        return Result<IReadOnlyList<AmbalajIlaveSandikAdayDto>>.Success(
            sonuc.Select(aday => AmbalajAlanMaskesi.Maskele(aday, izin)).ToList());
    }
}

public sealed class GetAmbalajProjeSandikSecenekleriQueryHandler
    : IRequestHandler<GetAmbalajProjeSandikSecenekleriQuery,
        Result<IReadOnlyList<AmbalajSandikSecenegiDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRolService _roller;
    private readonly ICurrentUserService _kullanici;
    private readonly IReadQueryExecutor _readQueries;

    public GetAmbalajProjeSandikSecenekleriQueryHandler(
        IUnitOfWork unitOfWork,
        IRolService roller,
        ICurrentUserService kullanici,
        IReadQueryExecutor? readQueries = null)
    {
        _unitOfWork = unitOfWork;
        _roller = roller;
        _kullanici = kullanici;
        _readQueries = readQueries ?? SynchronousReadQueryExecutor.Instance;
    }

    public async Task<Result<IReadOnlyList<AmbalajSandikSecenegiDto>>> Handle(
        GetAmbalajProjeSandikSecenekleriQuery request,
        CancellationToken cancellationToken)
    {
        var query = _readQueries.AsNoTracking(
                _unitOfWork.GetRepository<Sandik>().Queryable())
            .Where(sandik => sandik.ProjeId == request.ProjeId)
            .Select(sandik => new SandikSecenekSatiri(
                sandik.Id,
                sandik.SandikNo,
                sandik.Ad,
                sandik.Boy,
                sandik.En,
                sandik.Yukseklik,
                sandik.TipId));

        var izin = await AmbalajYetkilendirmeYardimcisi.GorunumYetkileriniGetirAsync(_roller, _kullanici, cancellationToken);
        var sonuc = (await _readQueries.ToListAsync(query, cancellationToken))
            .OrderBy(sandik => AmbalajPlanlamaYardimcisi.SandikSiraAnahtari(sandik.SandikNo))
            .ThenBy(sandik => sandik.SandikNo, StringComparer.OrdinalIgnoreCase)
            .Select(sandik => AmbalajAlanMaskesi.Maskele(new AmbalajSandikSecenegiDto(
                sandik.Id, sandik.SandikNo, sandik.Ad, sandik.Boy, sandik.En, sandik.Yukseklik,
                AmbalajUretimYardimcilari.CinsMetni(AmbalajUretimPolitikasi.KaynakCinsi(sandik.TipId), null)), izin))
            .ToList();
        return Result<IReadOnlyList<AmbalajSandikSecenegiDto>>.Success(sonuc);
    }

    private sealed record SandikSecenekSatiri(
        int Id, string SandikNo, string? Ad, decimal? Boy, decimal? En, decimal? Yukseklik, int TipId);
}
