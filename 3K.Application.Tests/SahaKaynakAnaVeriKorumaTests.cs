using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using _3K.Application.Common;
using _3K.Application.Features.CekiIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

public sealed class SahaKaynakAnaVeriKorumaTests
{
    [Theory]
    [InlineData(true, false, ProjeDurum.SevkEdildi)]
    [InlineData(false, false, ProjeDurum.SevkEdildi)]
    [InlineData(true, true, ProjeDurum.SevkEdildi)]
    [InlineData(false, true, ProjeDurum.SevkEdildi)]
    [InlineData(true, false, ProjeDurum.EksikSevkEdildi)]
    [InlineData(false, false, ProjeDurum.EksikSevkEdildi)]
    [InlineData(true, true, ProjeDurum.EksikSevkEdildi)]
    [InlineData(false, true, ProjeDurum.EksikSevkEdildi)]
    public async Task AktifSahaKaynakSatiri_OnaylaSandikAcilsaDaDegistirilemez(
        bool anaVeri, bool legacy, ProjeDurum projeDurumu)
    {
        using var kurgu = new Kurgu(projeDurumu, yeniDefter: !legacy, legacy: legacy);

        var sonuc = await kurgu.CalistirAsync(anaVeri);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(SahaAktarimBlokajHelper.SandikMesaji, sonuc.Error!.Message);
        Assert.Equal(0, kurgu.Uow.YazmaSayisi);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Equal(4, kurgu.Satir.IstenenAdet);
        Assert.Equal("ESKI", kurgu.Satir.Aciklama);
        Assert.Equal("1", kurgu.Satir.FiiliSandikNo);
        Assert.Equal(20, Assert.Single(kurgu.Uow.Repo<SandikIcerik>().Rows).SandikId);
        Assert.Empty(kurgu.Uow.Repo<Revizyon>().Rows);
        Assert.Equal(legacy ? 2 : 1, kurgu.SahaOkumalari.OkumaSayisi);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SahaAktarimiOlmayanSatir_MevcutKomutAkisiniKorur(bool anaVeri)
    {
        using var kurgu = new Kurgu(ProjeDurum.EksikSevkEdildi, yeniDefter: false, legacy: false);

        var sonuc = await kurgu.CalistirAsync(anaVeri);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(1, kurgu.Uow.SaveCount);
        Assert.Equal(2, kurgu.SahaOkumalari.OkumaSayisi);
        Assert.Equal(4, kurgu.Satir.IstenenAdet);
        if (anaVeri)
        {
            Assert.Equal("YENI", kurgu.Satir.Aciklama);
            Assert.Equal("1", kurgu.Satir.FiiliSandikNo);
            Assert.Equal(4, Assert.Single(kurgu.Uow.Repo<SandikIcerik>().Rows).TahsisMiktari);
        }
        else
        {
            Assert.Equal("2", kurgu.Satir.FiiliSandikNo);
            Assert.Equal(21, Assert.Single(kurgu.Uow.Repo<SandikIcerik>().Rows).SandikId);
            Assert.Single(kurgu.Uow.Repo<Revizyon>().Rows);
        }
    }

    private sealed class Kurgu : IDisposable
    {
        public TestUnitOfWork Uow { get; } = new();
        public CekiSatiri Satir { get; }
        public SahaOkumaStub SahaOkumalari { get; }
        private readonly AppDbContext _context;
        private readonly SahaTamamlamaService _saha;

        public Kurgu(ProjeDurum durum, bool yeniDefter, bool legacy)
        {
            SahaOkumalari = new SahaOkumaStub(yeniDefter, legacy);
            _context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql("Host=database-must-not-be-contacted.invalid;Database=test;Username=test;Password=test")
                .AddInterceptors(new BaglantiyiEngelle(), SahaOkumalari).Options);
            _saha = new SahaTamamlamaService(_context);
            Uow.Repo<Proje>().Rows.Add(new Proje { Id = 10, ProjeTipiId = (int)ProjeTipi.Normal, DurumId = (int)durum });
            Uow.Repo<Ceki>().Rows.Add(new Ceki { Id = 60, ProjeId = 10 });
            Satir = new CekiSatiri { Id = 50, CekiId = 60, IstenenAdet = 4, Aciklama = "ESKI",
                BarkodNo = "TEST", CekideGecenSandikNo = "1", FiiliSandikNo = "1", BirimId = (int)Birim.Adet };
            Uow.Repo<CekiSatiri>().Rows.Add(Satir);
            Uow.Repo<Sandik>().Rows.Add(new Sandik { Id = 20, ProjeId = 10, SandikNo = "1",
                DurumId = (int)SandikDurum.Sevkedildi, SevkiyatDuzeltmeAcikMi = true });
            Uow.Repo<Sandik>().Rows.Add(new Sandik { Id = 21, ProjeId = 10, SandikNo = "2",
                DurumId = (int)SandikDurum.Kapandi });
            Uow.Repo<SandikIcerik>().Rows.Add(new SandikIcerik { Id = 70, CekiSatiriId = 50, SandikId = 20,
                TahsisMiktari = 4, KonulanAdet = 3, EksikAdet = 1 });
        }

        public async Task<Result> CalistirAsync(bool anaVeri)
        {
            if (!anaVeri)
                return await new FiiliSandikDegistirCommandHandler(Uow, new HareketStub(), _saha).Handle(
                    new FiiliSandikDegistirCommand { CekiSatiriId = 50, ProjeId = 10, YeniFiiliSandikNo = "2", KullaniciId = 7 }, default);

            var sonuc = await new CekiSatiriAnaVeriGuncelleCommandHandler(Uow, new DurumStub(), _saha).Handle(
                new CekiSatiriAnaVeriGuncelleCommand { CekiSatiriId = 50, SiraNo = 1, BarkodNo = "TEST",
                    Aciklama = "YENI", IstenenAdet = 4, BirimId = (int)Birim.Adet, SandikNo = "1" }, default);
            return sonuc.IsSuccess ? Result.Success() : Result.Failure(sonuc.Error!.Message);
        }

        public void Dispose() { _context.Dispose(); Uow.Dispose(); }
    }

    // Gerçek saha servisi ve EF SQL'i çalışır; sadece SELECT EXISTS cevabı taklit edilir.
    // Her türlü fiziksel bağlantı açılması engellenir, production/local DB kullanılmaz.
    private sealed class SahaOkumaStub(bool yeniDefter, bool legacy) : DbCommandInterceptor
    {
        public int OkumaSayisi { get; private set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Assert.StartsWith("SELECT EXISTS", command.CommandText);
            Assert.Contains(command.Parameters.Cast<DbParameter>(), p => Equals(p.Value, 50));
            var defterSorgusu = command.CommandText.Contains("FROM \"SahaAktarimKalemleri\"");
            if (defterSorgusu)
                Assert.Contains("\"DurumId\" <>", command.CommandText);
            else
            {
                Assert.Contains("FROM \"CekiSatirlari\"", command.CommandText);
                Assert.Contains("\"ProjeTipiId\" = 2", command.CommandText);
            }
            OkumaSayisi++;
            var table = new DataTable();
            table.Columns.Add("exists", typeof(bool));
            table.Rows.Add(defterSorgusu ? yeniDefter : legacy);
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
        }
    }

    private sealed class BaglantiyiEngelle : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult.Suppress());
    }

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        private readonly Dictionary<Type, object> _repos = new();
        public int YazmaSayisi { get; private set; }
        public int SaveCount { get; private set; }
        public bool HasActiveTransaction => false;
        public Repo<T> Repo<T>() where T : BaseEntity
        {
            if (!_repos.TryGetValue(typeof(T), out var repo)) _repos.Add(typeof(T), repo = new Repo<T>(() => YazmaSayisi++));
            return (Repo<T>)repo;
        }
        public IGenericRepository<T> GetRepository<T>() where T : BaseEntity => Repo<T>();
        public Task<int> SaveChangesAsync(CancellationToken token = default) { SaveCount++; return Task.FromResult(1); }
        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken token = default) => operation(token);
        public void RegisterAfterCommit(Func<CancellationToken, Task> callback) => throw new NotSupportedException();
        public void RegisterAfterRollback(Func<CancellationToken, Task> callback) => throw new NotSupportedException();
        public void Dispose() { }
    }

    private sealed class Repo<T>(Action yazma) : IGenericRepository<T> where T : BaseEntity
    {
        public List<T> Rows { get; } = [];
        public Task<T?> GetByIdAsync(int id) => Task.FromResult(Rows.SingleOrDefault(r => r.Id == id));
        public IQueryable<T> Queryable() => Rows.AsQueryable();
        public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) => Task.FromResult(Rows.Where(predicate.Compile()));
        public Task AddAsync(T entity) { yazma(); Rows.Add(entity); return Task.CompletedTask; }
        public void Update(T entity) => yazma();
        public void Remove(T entity) { yazma(); Rows.Remove(entity); }
        public Task<IEnumerable<T>> GetAllAsync() => throw new NotSupportedException();
        public Task<IEnumerable<T>> GetAllWithIncludeAsync<TProp>(Expression<Func<T, TProp>> include) => throw new NotSupportedException();
    }

    private sealed class DurumStub : IDurumHesaplaService
    {
        public void HesaplaKalanVeDurum(CekiSatiri satir) { }
        public int HesaplaGenelDurum(int grid, int uck) => throw new NotSupportedException();
    }

    private sealed class HareketStub : IHareketService
    {
        public Task HareketKaydetAsync(HareketGecmisi hareket) => Task.CompletedTask;
        public Task<IEnumerable<HareketGecmisi>> GetProjeHareketleriAsync(int id) => throw new NotSupportedException();
        public Task<IEnumerable<HareketGecmisi>> GetUrunHareketleriAsync(string tip, string id) => throw new NotSupportedException();
        public Task<(IEnumerable<HareketGecmisi> Items, int TotalCount)> GetPaginatedProjeHareketleriAsync(
            int id, string? arama, int? tip, int sayfa, int boyut) => throw new NotSupportedException();
    }
}
