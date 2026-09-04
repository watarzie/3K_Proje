using System.Linq.Expressions;
using _3K.Application.Features.AmbalajIslemleri;
using _3K.Application.Features.AmbalajIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace _3K.Application.Tests;

public sealed class AmbalajPlanlamaSayfalamaTests
{
    [Fact]
    public async Task Projeler_SayfalanirVeFiltreOzetiTumEslesenProjelerdenHesaplanir()
    {
        var projeler = new[]
        {
            new Proje { Id = 1, ProjeNo = "PA699-01", Musteri = "ACME", ProjeTipiId = 1 },
            new Proje { Id = 2, ProjeNo = "PA699-02", Musteri = "ACME", ProjeTipiId = 1 },
            new Proje { Id = 3, ProjeNo = "PA700-01", Musteri = "ACME", ProjeTipiId = 1 },
            new Proje { Id = 4, ProjeNo = "SA699-01", Musteri = "ACME", ProjeTipiId = 2 }
        };
        var sandiklar = new[]
        {
            new Sandik { Id = 11, ProjeId = 1, SandikNo = "1-2", Boy = 1000, En = 800, Yukseklik = 700 },
            new Sandik { Id = 12, ProjeId = 2, SandikNo = "3", Boy = null, En = 800, Yukseklik = 700 },
            new Sandik { Id = 13, ProjeId = 3, SandikNo = "1", Boy = 1000, En = 800, Yukseklik = 700 }
        };
        var kayitlar = new[]
        {
            new AmbalajUretimKaydi
            {
                Id = 101, ProjeId = 1, KaynakKayitId = 11, Tur = AmbalajSandikTuru.Normal,
                AmbalajaDahil = true, UretimeAlindi = true, HesaplananToplamM3 = 1m
            },
            new AmbalajUretimKaydi
            {
                Id = 102, ProjeId = 2, KaynakKayitId = 12, Tur = AmbalajSandikTuru.Normal,
                AmbalajaDahil = true, UretimeAlindi = true, HesaplananToplamM3 = 2m
            },
            new AmbalajUretimKaydi
            {
                Id = 103, ProjeId = 3, KaynakKayitId = 13, Tur = AmbalajSandikTuru.Normal,
                AmbalajaDahil = true, UretimeAlindi = true, HesaplananToplamM3 = 10m
            }
        };
        var unitOfWork = new FakeUnitOfWork()
            .AddRepository(projeler)
            .AddRepository(sandiklar)
            .AddRepository(kayitlar)
            .AddRepository(new LookupProjeTipi { Id = 1, Anahtar = 1, Deger = "Normal" });
        var handler = new GetAmbalajPlanlamaProjeleriQueryHandler(unitOfWork);

        var sonuc = await handler.Handle(new GetAmbalajPlanlamaProjeleriQuery
        {
            Arama = "699",
            ProjeTipiId = 1,
            Grup = 1,
            PageNumber = 2,
            PageSize = 1
        }, CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(2, sonuc.Value!.TotalCount);
        Assert.Equal(2, sonuc.Value.TotalPages);
        Assert.True(sonuc.Value.HasPreviousPage);
        Assert.False(sonuc.Value.HasNextPage);
        Assert.Equal(1, Assert.Single(sonuc.Value.Items).ProjeId);
        Assert.Equal(2, sonuc.Value.FilteredSummary!.ProjeSayisi);
        Assert.Equal(3, sonuc.Value.FilteredSummary.ToplamSandikAdedi);
        Assert.Equal(
            AmbalajPlanlamaYardimcisi.KaynakSandikToplamHacmiHesapla(
                null, null, "1-2", 1000, 800, 700),
            sonuc.Value.FilteredSummary.ToplamHacimM3);
        Assert.Equal(1, sonuc.Value.FilteredSummary.EksikOlculuProjeSayisi);
    }

    [Fact]
    public async Task Projeler_TasmaYapanSayfaNumarasiniSonSayfayaSinirlarVeGuncelSandikHacminiKullanir()
    {
        var proje = new Proje { Id = 1, ProjeNo = "PA800-01", Musteri = "ACME", ProjeTipiId = 1 };
        var sandik = new Sandik
        {
            Id = 11, ProjeId = 1, SandikNo = "1-2", Ad = "Genleşme Kabı",
            Boy = 7300, En = 2450, Yukseklik = 2955
        };
        var kayit = new AmbalajUretimKaydi
        {
            Id = 101, ProjeId = 1, KaynakKayitId = 11, Tur = AmbalajSandikTuru.Normal,
            AmbalajaDahil = true, UretimeAlindi = true,
            HesaplananToplamM3 = 999m, M3Override = 777m
        };
        var handler = new GetAmbalajPlanlamaProjeleriQueryHandler(new FakeUnitOfWork()
            .AddRepository(proje)
            .AddRepository(sandik)
            .AddRepository(kayit)
            .AddRepository(new LookupProjeTipi { Id = 1, Anahtar = 1, Deger = "Normal" }));

        var sonuc = await handler.Handle(new GetAmbalajPlanlamaProjeleriQuery
        {
            ProjeTipiId = 1,
            Grup = 1,
            PageNumber = int.MaxValue,
            PageSize = 1
        }, CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(1, sonuc.Value!.PageNumber);
        Assert.Equal(1, sonuc.Value.TotalPages);
        var gorunenHacim = Assert.Single(sonuc.Value.Items).ProjeSandiklariHacimM3;
        Assert.Equal(gorunenHacim, sonuc.Value.FilteredSummary!.ToplamHacimM3);
        Assert.NotEqual(999m, gorunenHacim);
        Assert.NotEqual(777m, gorunenHacim);
    }

    [Fact]
    public async Task BagimsizSandiklar_FiltreliListeyiSayfalarVeDortGlobalTurOzetiniBirlikteDoner()
    {
        var projeler = new[]
        {
            new Proje { Id = 1, ProjeNo = "PA699-01", Musteri = "ACME" },
            new Proje { Id = 2, ProjeNo = "PA700-01", Musteri = "BASKA" }
        };
        var kayitlar = new[]
        {
            BagimsizKayit(201, 1, AmbalajSandikTuru.Ilave, 2, 1.5m, true),
            BagimsizKayit(202, 1, AmbalajSandikTuru.Saha, 3, 2m, false, AmbalajSandikCinsi.Kontrplak),
            BagimsizKayit(203, 1, AmbalajSandikTuru.Yedek, 4, 4m, false),
            BagimsizKayit(204, 1, AmbalajSandikTuru.Ic, 5, 5m, false),
            BagimsizKayit(205, 2, AmbalajSandikTuru.Ilave, 7, 7m, false),
            BagimsizKayit(206, 1, AmbalajSandikTuru.Yedek, 100, 100m, false, iptalMi: true),
            new AmbalajUretimKaydi
            {
                Id = 207, ProjeId = 1, BagimsizKayitMi = false, Tur = AmbalajSandikTuru.Ilave,
                SandikNo = "KAYNAK", Ad = "Kaynak", Adet = 50, HesaplananToplamM3 = 50m
            }
        };
        var handler = new GetAmbalajBagimsizSandiklarQueryHandler(new FakeUnitOfWork()
            .AddRepository(projeler)
            .AddRepository(Array.Empty<Sandik>())
            .AddRepository(kayitlar));

        var sonuc = await handler.Handle(new GetAmbalajBagimsizSandiklarQuery
        {
            Arama = "acme",
            Tur = 2,
            PageNumber = 1,
            PageSize = 1
        }, CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(1, sonuc.Value!.TotalCount);
        Assert.Equal(201, Assert.Single(sonuc.Value.Items).Id);
        Assert.Equal(1, sonuc.Value.FilteredSummary!.KayitSayisi);
        Assert.Equal(2, sonuc.Value.FilteredSummary.ToplamSandikAdedi);
        Assert.Equal(2, sonuc.Value.FilteredSummary.UretimeAlinanSandikAdedi);
        Assert.Equal(1.5m, sonuc.Value.FilteredSummary.ToplamHacimM3);

        Assert.Equal(4, sonuc.Value.FilteredSummary.TurOzetleri.Count);
        var ilave = Assert.Single(sonuc.Value.FilteredSummary.TurOzetleri, x => x.Tur == 2);
        Assert.Equal(2, ilave.KayitSayisi);
        Assert.Equal(9, ilave.ToplamSandikAdedi);
        Assert.Equal(8.5m, ilave.ToplamHacimM3);
        var saha = Assert.Single(sonuc.Value.FilteredSummary.TurOzetleri, x => x.Tur == 4);
        Assert.Equal(3, saha.ToplamSandikAdedi);
        Assert.Equal(0m, saha.ToplamHacimM3);
        Assert.Equal(1, Assert.Single(sonuc.Value.FilteredSummary.TurOzetleri, x => x.Tur == 5).KayitSayisi);
        Assert.Equal(1, Assert.Single(sonuc.Value.FilteredSummary.TurOzetleri, x => x.Tur == 3).KayitSayisi);
    }

    [Fact]
    public void BagimsizSandikAramasiVeSayfalama_PostgreSqlSorgusunaCevrilebilir()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=translation_only;Username=test;Password=test")
            .Options;
        using var context = new AppDbContext(options);
        var tumKayitlar = context.AmbalajUretimKayitlari.AsNoTracking();

        var query = AmbalajBagimsizSandikAramaFiltresi.Uygula(
                tumKayitlar.Where(k => k.BagimsizKayitMi && !k.IptalMi),
                context.Projeler.AsNoTracking(),
                context.Sandiklar.AsNoTracking(),
                tumKayitlar,
                "PA 699")
            .OrderByDescending(k => k.Id)
            .Skip(25)
            .Take(25);

        var sql = query.ToQueryString();

        Assert.Contains("EXISTS", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("replace", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LIMIT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("OFFSET", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BagimsizSandiklar_TasmaYapanSayfaNumarasiniSonSayfayaSinirlar()
    {
        var kayitlar = new[]
        {
            BagimsizKayit(201, 1, AmbalajSandikTuru.Saha, 1, 1m, false),
            BagimsizKayit(202, 1, AmbalajSandikTuru.Saha, 1, 1m, false)
        };
        var handler = new GetAmbalajBagimsizSandiklarQueryHandler(new FakeUnitOfWork()
            .AddRepository(new Proje { Id = 1, ProjeNo = "PA800-01" })
            .AddRepository(Array.Empty<Sandik>())
            .AddRepository(kayitlar));

        var sonuc = await handler.Handle(new GetAmbalajBagimsizSandiklarQuery
        {
            PageNumber = int.MaxValue,
            PageSize = 1
        }, CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(2, sonuc.Value!.PageNumber);
        Assert.Equal(2, sonuc.Value.TotalPages);
        Assert.Equal(201, Assert.Single(sonuc.Value.Items).Id);
    }

    [Fact]
    public async Task BosSonuc_SayfaSozlesmesiniVeSifirOzetleriniKorur()
    {
        var unitOfWork = new FakeUnitOfWork()
            .AddRepository(Array.Empty<Proje>())
            .AddRepository(Array.Empty<Sandik>())
            .AddRepository(Array.Empty<AmbalajUretimKaydi>())
            .AddRepository(Array.Empty<LookupProjeTipi>());

        var projeSonucu = await new GetAmbalajPlanlamaProjeleriQueryHandler(unitOfWork)
            .Handle(new GetAmbalajPlanlamaProjeleriQuery
            {
                Arama = "bulunmayan",
                PageNumber = int.MaxValue,
                PageSize = 15,
                IncludeSummary = true
            }, CancellationToken.None);
        var bagimsizSonucu = await new GetAmbalajBagimsizSandiklarQueryHandler(unitOfWork)
            .Handle(new GetAmbalajBagimsizSandiklarQuery
            {
                Arama = "bulunmayan",
                PageNumber = int.MaxValue,
                PageSize = 25,
                IncludeSummary = true
            }, CancellationToken.None);

        Assert.True(projeSonucu.IsSuccess);
        Assert.Empty(projeSonucu.Value!.Items);
        Assert.Equal(1, projeSonucu.Value.PageNumber);
        Assert.Equal(0, projeSonucu.Value.TotalPages);
        Assert.Equal(0, projeSonucu.Value.TotalCount);
        Assert.Equal(0, projeSonucu.Value.FilteredSummary!.ProjeSayisi);
        Assert.Equal(0m, projeSonucu.Value.FilteredSummary.ToplamHacimM3);

        Assert.True(bagimsizSonucu.IsSuccess);
        Assert.Empty(bagimsizSonucu.Value!.Items);
        Assert.Equal(1, bagimsizSonucu.Value.PageNumber);
        Assert.Equal(0, bagimsizSonucu.Value.TotalPages);
        Assert.Equal(0, bagimsizSonucu.Value.TotalCount);
        Assert.Equal(0, bagimsizSonucu.Value.FilteredSummary!.KayitSayisi);
        Assert.Equal(4, bagimsizSonucu.Value.FilteredSummary.TurOzetleri.Count);
        Assert.All(bagimsizSonucu.Value.FilteredSummary.TurOzetleri, x =>
        {
            Assert.Equal(0, x.ToplamSandikAdedi);
            Assert.Equal(0m, x.ToplamHacimM3);
        });
    }

    [Fact]
    public async Task SayfaGezintisi_OzetiAtlarVeAsyncExecutoraCancellationTokeniAktarir()
    {
        var cancellationToken = new CancellationTokenSource().Token;
        var executor = new RecordingReadQueryExecutor();
        var unitOfWork = new FakeUnitOfWork()
            .AddRepository(new Proje { Id = 1, ProjeNo = "PA900-01", Musteri = "ACME", ProjeTipiId = 1 })
            .AddRepository(Array.Empty<Sandik>())
            .AddRepository(Array.Empty<AmbalajUretimKaydi>())
            .AddRepository(new LookupProjeTipi { Id = 1, Anahtar = 1, Deger = "Normal" });

        var sonuc = await new GetAmbalajPlanlamaProjeleriQueryHandler(unitOfWork, executor)
            .Handle(new GetAmbalajPlanlamaProjeleriQuery
            {
                PageNumber = 1,
                PageSize = 15,
                IncludeSummary = false
            }, cancellationToken);

        Assert.True(sonuc.IsSuccess);
        Assert.Null(sonuc.Value!.FilteredSummary);
        Assert.Equal(1, sonuc.Value.TotalCount);
        Assert.True(executor.AsNoTrackingCallCount >= 4);
        Assert.Equal(1, executor.CountAsyncCallCount);
        Assert.True(executor.ToListAsyncCallCount >= 4);
        Assert.All(executor.CancellationTokens, token => Assert.Equal(cancellationToken, token));
    }

    [Fact]
    public async Task TalepEdenKullanicilari_MinimalTekilVeSiraliSeceneklerDoner()
    {
        var handler = new GetAmbalajTalepEdenKullanicilarQueryHandler(
            new FakeUnitOfWork().AddRepository(
                new Kullanici { Id = 3, AdSoyad = "  Zeynep Şahin  " },
                new Kullanici { Id = 2, AdSoyad = "Ahmet Yılmaz" },
                new Kullanici { Id = 5, AdSoyad = "ahmet yılmaz" },
                new Kullanici { Id = 8, AdSoyad = " " }));

        var sonuc = await handler.Handle(
            new GetAmbalajTalepEdenKullanicilarQuery(),
            CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.Collection(
            sonuc.Value!,
            kullanici =>
            {
                Assert.Equal(2, kullanici.Id);
                Assert.Equal("Ahmet Yılmaz", kullanici.AdSoyad);
            },
            kullanici =>
            {
                Assert.Equal(3, kullanici.Id);
                Assert.Equal("Zeynep Şahin", kullanici.AdSoyad);
            });
    }

    [Fact]
    public async Task ProjeSandikSecenekleri_YalnizSecilenProjeninHafifAlanlariniDoner()
    {
        var handler = new GetAmbalajProjeSandikSecenekleriQueryHandler(
            new FakeUnitOfWork().AddRepository(
                new Sandik
                {
                    Id = 12, ProjeId = 7, SandikNo = "2", Ad = "Radyatör",
                    Boy = 2500, En = 1500, Yukseklik = 2180
                },
                new Sandik
                {
                    Id = 11, ProjeId = 7, SandikNo = "1", Ad = "Genleşme Kabı",
                    Boy = 7300, En = 2450, Yukseklik = 2955
                },
                new Sandik
                {
                    Id = 13, ProjeId = 7, SandikNo = "10", Ad = "Aksesuar",
                    Boy = 1000, En = 800, Yukseklik = 600
                },
                new Sandik { Id = 99, ProjeId = 8, SandikNo = "1", Ad = "Başka proje" }));

        var sonuc = await handler.Handle(
            new GetAmbalajProjeSandikSecenekleriQuery { ProjeId = 7 },
            CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.Collection(
            sonuc.Value!,
            sandik => Assert.Equal(11, sandik.Id),
            sandik => Assert.Equal(12, sandik.Id),
            sandik => Assert.Equal(13, sandik.Id));
    }

    [Fact]
    public void AmbalajUretimKaydi_ModeliBagimsizListeIndeksiniIcerir()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=test;Password=test")
            .Options;
        using var context = new AppDbContext(options);
        var entity = context.Model.FindEntityType(typeof(AmbalajUretimKaydi));

        Assert.NotNull(entity);
        Assert.Contains(entity!.GetIndexes(), index =>
            index.Properties.Select(p => p.Name).SequenceEqual(
                new[] { nameof(AmbalajUretimKaydi.BagimsizKayitMi), nameof(AmbalajUretimKaydi.Tur), nameof(AmbalajUretimKaydi.ProjeId) }));
    }

    private static AmbalajUretimKaydi BagimsizKayit(
        int id,
        int projeId,
        AmbalajSandikTuru tur,
        int adet,
        decimal hacim,
        bool uretimeAlindi,
        AmbalajSandikCinsi cins = AmbalajSandikCinsi.AhsapKapali,
        bool iptalMi = false) => new()
        {
            Id = id,
            ProjeId = projeId,
            BagimsizKayitMi = true,
            Tur = tur,
            SandikNo = id.ToString(),
            Ad = $"Sandık {id}",
            SandikCinsi = cins,
            Adet = adet,
            Boy = 1000,
            En = 800,
            Yukseklik = 700,
            UretimeAlindi = uretimeAlindi,
            HesaplananToplamM3 = hacim,
            IptalMi = iptalMi
        };

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private readonly Dictionary<Type, object> _repositories = new();

        public bool HasActiveTransaction => false;

        public FakeUnitOfWork AddRepository<T>(params T[] entities) where T : BaseEntity
        {
            _repositories[typeof(T)] = new FakeRepository<T>(entities);
            return this;
        }

        public IGenericRepository<T> GetRepository<T>() where T : BaseEntity =>
            (IGenericRepository<T>)_repositories[typeof(T)];

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default) => operation(cancellationToken);

        public void RegisterAfterCommit(Func<CancellationToken, Task> callback) =>
            throw new NotSupportedException();

        public void RegisterAfterRollback(Func<CancellationToken, Task> callback) =>
            throw new NotSupportedException();

        public void Dispose()
        {
        }
    }

    private sealed class FakeRepository<T>(IEnumerable<T> items) : IGenericRepository<T> where T : BaseEntity
    {
        private readonly List<T> _items = items.ToList();

        public Task<T?> GetByIdAsync(int id) => Task.FromResult(_items.SingleOrDefault(x => x.Id == id));
        public Task<IEnumerable<T>> GetAllAsync() => Task.FromResult<IEnumerable<T>>(_items);
        public Task<IEnumerable<T>> GetAllWithIncludeAsync<TProp>(Expression<Func<T, TProp>> include) =>
            Task.FromResult<IEnumerable<T>>(_items);
        public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
            Task.FromResult<IEnumerable<T>>(_items.AsQueryable().Where(predicate));
        public IQueryable<T> Queryable() => _items.AsQueryable();
        public Task AddAsync(T entity)
        {
            _items.Add(entity);
            return Task.CompletedTask;
        }
        public void Update(T entity)
        {
        }
        public void Remove(T entity) => _items.Remove(entity);
    }

    private sealed class RecordingReadQueryExecutor : IReadQueryExecutor
    {
        public int AsNoTrackingCallCount { get; private set; }
        public int CountAsyncCallCount { get; private set; }
        public int ToListAsyncCallCount { get; private set; }
        public List<CancellationToken> CancellationTokens { get; } = [];

        public IQueryable<TEntity> AsNoTracking<TEntity>(IQueryable<TEntity> query)
            where TEntity : class
        {
            AsNoTrackingCallCount++;
            return query;
        }

        public Task<int> CountAsync<T>(
            IQueryable<T> query,
            CancellationToken cancellationToken = default)
        {
            CountAsyncCallCount++;
            CancellationTokens.Add(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(query.Count());
        }

        public Task<List<T>> ToListAsync<T>(
            IQueryable<T> query,
            CancellationToken cancellationToken = default)
        {
            ToListAsyncCallCount++;
            CancellationTokens.Add(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(query.ToList());
        }
    }
}
