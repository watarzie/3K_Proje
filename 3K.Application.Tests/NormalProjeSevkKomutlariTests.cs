using System.Linq.Expressions;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Tests;

public sealed class NormalProjeSevkKomutlariTests
{
    [Theory]
    [InlineData(true, ProjeTipi.Normal, 0, ProjeDurum.EksikSevkEdildi)]
    [InlineData(false, ProjeTipi.Normal, 0, ProjeDurum.EksikSevkEdildi)]
    [InlineData(true, ProjeTipi.Normal, 1, ProjeDurum.SevkEdildi)]
    [InlineData(false, ProjeTipi.Normal, 1, ProjeDurum.SevkEdildi)]
    [InlineData(true, ProjeTipi.Saha, 0, ProjeDurum.SevkEdildi)]
    [InlineData(false, ProjeTipi.Saha, 0, ProjeDurum.SevkEdildi)]
    [InlineData(true, ProjeTipi.Yedek, 0, ProjeDurum.SevkEdildi)]
    [InlineData(false, ProjeTipi.Yedek, 0, ProjeDurum.SevkEdildi)]
    public async Task SonSandikSevki_NormaldeEksigiDikkateAlir_SahaYedekKurallariDegismez(
        bool toplu, ProjeTipi tip, decimal gerceklesenSaha, ProjeDurum beklenen)
    {
        using var kurgu = new Kurgu(tip);
        kurgu.Saha.Gerceklesen = gerceklesenSaha;

        var sonuc = await kurgu.SevkEtAsync(toplu);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal((int)beklenen, kurgu.Proje.DurumId);
        Assert.Equal((int)SandikDurum.Sevkedildi, kurgu.Sandik.DurumId);
        Assert.Equal((int)SandikDurum.Kapandi, kurgu.Sandik.SevkOncesiDurumId);
        Assert.Equal(kurgu.EskiSevkTarihi, kurgu.Proje.GerceklesenSevkTarihi);
        Assert.Equal(3, kurgu.Satir.GelenMiktar);
        Assert.Equal(4, kurgu.Satir.IstenenAdet);
        Assert.Single(kurgu.Uow.Repo<Sevkiyat>().Rows);
        Assert.Single(kurgu.Uow.Repo<SevkiyatSandik>().Rows);
        Assert.Equal(1, kurgu.Uow.SaveCount);
        Assert.Equal(tip == ProjeTipi.Normal ? 1 : 0, kurgu.Saha.GerceklesenOkumaSayisi);
        Assert.Equal(tip == ProjeTipi.Saha ? 1 : 0, kurgu.Saha.SenkronizasyonSayisi);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NormalSevk_SahaUzerindenTamSandikSevkiIstisnasiniVeKaynakKilitleriniKorur(bool toplu)
    {
        using var kurgu = new Kurgu(ProjeTipi.Normal);
        var sahaSandigi = new Sandik { Id = 21, ProjeId = 10, DurumId = (int)SandikDurum.Kapandi };
        kurgu.Uow.Repo<Sandik>().Rows.Add(sahaSandigi);
        kurgu.Saha.SandikDurumu = new KaynakSandikSahaAktarimDurumu
        {
            AktifAktarimaBagliSandikIds = new HashSet<int> { 21 },
            SahaUzerindenSevkEdilenSandikIds = new HashSet<int> { 21 }
        };

        var sonuc = await kurgu.SevkEtAsync(toplu);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal((int)ProjeDurum.SevkEdildi, kurgu.Proje.DurumId);
        Assert.Equal((int)SandikDurum.Kapandi, sahaSandigi.DurumId);
        Assert.Equal(20, Assert.Single(kurgu.Uow.Repo<SevkiyatSandik>().Rows).SandikId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AktifSahaAktarimliSandik_AnaProjedenYenidenSevkEdilemez(bool toplu)
    {
        using var kurgu = new Kurgu(ProjeTipi.Normal);
        kurgu.Saha.SandikDurumu = new KaynakSandikSahaAktarimDurumu
        { AktifAktarimaBagliSandikIds = new HashSet<int> { 20 } };

        var sonuc = await kurgu.SevkEtAsync(toplu);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Empty(kurgu.Uow.Repo<Sevkiyat>().Rows);
        Assert.Equal((int)SandikDurum.Kapandi, kurgu.Sandik.DurumId);
    }

    private sealed class Kurgu : IDisposable
    {
        public TestUnitOfWork Uow { get; } = new();
        public SahaStub Saha { get; } = new();
        public Proje Proje { get; }
        public Sandik Sandik { get; }
        public CekiSatiri Satir { get; }
        public DateTime EskiSevkTarihi { get; } = new(2026, 9, 1);

        public Kurgu(ProjeTipi tip)
        {
            Proje = new Proje { Id = 10, ProjeTipiId = (int)tip, DurumId = (int)ProjeDurum.EksikSevkEdildi,
                GerceklesenSevkTarihi = EskiSevkTarihi };
            Sandik = new Sandik { Id = 20, ProjeId = 10, DurumId = (int)SandikDurum.Kapandi };
            Satir = new CekiSatiri { Id = 50, CekiId = 60, Ceki = new Ceki { Id = 60, ProjeId = 10 },
                IstenenAdet = 4, GelenMiktar = 3 };
            Uow.Repo<Proje>().Rows.Add(Proje);
            Uow.Repo<Sandik>().Rows.Add(Sandik);
            Uow.Repo<CekiSatiri>().Rows.Add(Satir);
            // Başka proje eksikleri bu projenin durum hesabına sızmamalı.
            Uow.Repo<CekiSatiri>().Rows.Add(new CekiSatiri { Id = 51, Ceki = new Ceki { ProjeId = 11 }, IstenenAdet = 100 });
        }

        public Task<Result> SevkEtAsync(bool toplu) => toplu
            ? new ProjeSevkEtCommandHandler(Uow, new HareketStub(), new KullaniciStub(), Saha, new ReadQueries()).Handle(
                new ProjeSevkEtCommand { ProjeId = 10, SandikIds = [20] }, default)
            : new SandikSevkEtCommandHandler(Uow, new HareketStub(), new KullaniciStub(), Saha, new ReadQueries()).Handle(
                new SandikSevkEtCommand { ProjeId = 10, SandikId = 20 }, default);

        public void Dispose() => Uow.Dispose();
    }

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        private readonly Dictionary<Type, object> _repositories = new();
        public int SaveCount { get; private set; }
        public bool HasActiveTransaction { get; private set; }
        public Repo<T> Repo<T>() where T : BaseEntity
        {
            if (!_repositories.TryGetValue(typeof(T), out var repo))
                _repositories.Add(typeof(T), repo = new Repo<T>());
            return (Repo<T>)repo;
        }
        public IGenericRepository<T> GetRepository<T>() where T : BaseEntity => Repo<T>();
        public Task<int> SaveChangesAsync(CancellationToken token = default) { SaveCount++; return Task.FromResult(1); }
        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken token = default)
        { HasActiveTransaction = true; try { return await operation(token); } finally { HasActiveTransaction = false; } }
        public void RegisterAfterCommit(Func<CancellationToken, Task> callback) => throw new NotSupportedException();
        public void RegisterAfterRollback(Func<CancellationToken, Task> callback) => throw new NotSupportedException();
        public void Dispose() { }
    }

    private sealed class Repo<T> : IGenericRepository<T> where T : BaseEntity
    {
        public List<T> Rows { get; } = [];
        public Task<T?> GetByIdAsync(int id) => Task.FromResult(Rows.SingleOrDefault(r => r.Id == id));
        public IQueryable<T> Queryable() => Rows.AsQueryable();
        public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) => Task.FromResult(Rows.Where(predicate.Compile()));
        public Task AddAsync(T entity) { Rows.Add(entity); return Task.CompletedTask; }
        public void Update(T entity) { }
        public void Remove(T entity) => throw new NotSupportedException();
        public Task<IEnumerable<T>> GetAllAsync() => throw new NotSupportedException();
        public Task<IEnumerable<T>> GetAllWithIncludeAsync<TProp>(Expression<Func<T, TProp>> include) => throw new NotSupportedException();
    }

    private sealed class ReadQueries : IReadQueryExecutor
    {
        public IQueryable<T> AsNoTracking<T>(IQueryable<T> query) where T : class => query;
        public Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken token = default) => Task.FromResult(query.Count());
        public Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken token = default) => Task.FromResult(query.ToList());
    }

    private sealed class KullaniciStub : ICurrentUserService
    {
        public int? UserId => 7;
        public bool IsAuthenticated => true;
        public string? MenuKod => null;
    }

    private sealed class HareketStub : IHareketService
    {
        public Task HareketKaydetAsync(HareketGecmisi hareket) => Task.CompletedTask;
        public Task<IEnumerable<HareketGecmisi>> GetProjeHareketleriAsync(int id) => throw new NotSupportedException();
        public Task<IEnumerable<HareketGecmisi>> GetUrunHareketleriAsync(string tip, string id) => throw new NotSupportedException();
        public Task<(IEnumerable<HareketGecmisi> Items, int TotalCount)> GetPaginatedProjeHareketleriAsync(
            int id, string? arama, int? tip, int sayfa, int boyut) => throw new NotSupportedException();
    }

    private sealed class SahaStub : ISahaTamamlamaService
    {
        public decimal Gerceklesen { get; set; }
        public KaynakSandikSahaAktarimDurumu SandikDurumu { get; set; } = new();
        public int GerceklesenOkumaSayisi { get; private set; }
        public int SenkronizasyonSayisi { get; private set; }
        public Task<KaynakSandikSahaAktarimDurumu> GetKaynakSandikSahaAktarimDurumuAsync(
            IEnumerable<int> ids, CancellationToken token = default) => Task.FromResult(SandikDurumu);
        public Task<Dictionary<int, decimal>> GetSevkEdilenGerceklesenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken token = default)
        {
            Assert.Equal(new[] { 50 }, ids);
            GerceklesenOkumaSayisi++;
            return Task.FromResult(new Dictionary<int, decimal> { [50] = Gerceklesen });
        }
        public Task SenkronizeKaynakProjelerBySahaSandikIdsAsync(IEnumerable<int> ids, CancellationToken token = default)
        { Assert.Equal(new[] { 20 }, ids); SenkronizasyonSayisi++; return Task.CompletedTask; }
        // Planlanan/aktif miktar ve mutasyon yapan genel senkronizasyon bu hesapta kullanılamaz.
        public Task<Dictionary<int, decimal>> GetSevkEdilenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken token = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetAktifTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken token = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetAktifGerceklesenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken token = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetAktifIsTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken token = default) => GetAktifGerceklesenTamamlamaMapAsync(ids, token);
        public Task<HashSet<int>> GetAktifSandikBazliAktarimSatirIdsAsync(IEnumerable<int> ids, CancellationToken token = default) => throw new NotSupportedException();
        public Task<bool> AktifTamamlamaVarMiAsync(int id, CancellationToken token = default) => throw new NotSupportedException();
        public Task SenkronizeKaynakProjelerAsync(IEnumerable<int> ids, CancellationToken token = default) => throw new NotSupportedException();
    }
}
