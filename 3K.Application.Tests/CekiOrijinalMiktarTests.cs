using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

public sealed class CekiOrijinalMiktarTests
{
    [Fact]
    public void OrijinalMiktar_NullableVeOndalikHassasiyetiGuncelMiktarlaAyni()
    {
        using var context = Context();
        var property = context.Model.FindEntityType(typeof(CekiSatiri))!.FindProperty(nameof(CekiSatiri.OrijinalIstenenAdet))!;
        Assert.True(property.IsNullable);
        Assert.Equal(18, property.GetPrecision());
        Assert.Equal(4, property.GetScale());
        Assert.Equal("numeric(18,4)", property.GetColumnType());
    }

    [Fact]
    public void OrijinalMiktar_HicbirAktifMiktarHesabiniDegistirmez()
    {
        var satir = new CekiSatiri { IstenenAdet = 3.5m, GridGelenAdet = 1, GelenMiktar = 1 };
        var once = (satir.KalanMiktar, satir.EksikMiktar, satir.GridEksikMiktar, satir.KumulatifToplam);
        satir.OrijinalIstenenAdet = 1000.125m;
        Assert.Equal(once, (satir.KalanMiktar, satir.EksikMiktar, satir.GridEksikMiktar, satir.KumulatifToplam));
        Assert.Equal(2.5m, satir.KalanMiktar);
    }

    [Theory]
    [InlineData(null, 2, 3, 2d)]
    [InlineData(1d, 2, 3, 1d)]
    [InlineData(1d, 3, 2, 1d)]
    [InlineData(2d, 3, 2, 2d)]
    [InlineData(null, 2, 2, null)]
    [InlineData(1d, 2, 2, 1d)]
    [InlineData(null, 1.125, 2.25, 1.125)]
    [InlineData(0d, 2, 3, 0d)]
    public async Task RevizyonU_IlkMiktarDegisikligindeOncekiDegeriKorur_SonraEzmez(
        double? orijinal, decimal onceki, decimal yeni, double? beklenen)
    {
        await using var context = Context();
        var sandik = new Sandik { Id = 30, ProjeId = 10, SandikNo = "1" };
        context.Sandiklar.Attach(sandik);
        var satir = new CekiSatiri
        {
            Id = 20, CekiId = 15, SiraNo = 1, BarkodNo = "TEST", Aciklama = "TEST",
            CekideGecenSandikNo = "1", FiiliSandikNo = "1", IstenenAdet = onceki,
            OrijinalIstenenAdet = orijinal.HasValue ? (decimal)orijinal.Value : null,
            GelenMiktar = .5m, GridGelenAdet = 1
        };
        context.CekiSatirlari.Attach(satir);

        await InvokeAsync(Service(context), "RevizyonSatiriniGuncelleAsync", 10, satir,
            ImportSatiri(yeni), new Dictionary<string, Sandik> { ["1"] = sandik }, SandikBilgileri(), 7);

        Assert.Equal(beklenen.HasValue ? (decimal)beklenen.Value : null, satir.OrijinalIstenenAdet);
        Assert.Equal(yeni, satir.IstenenAdet);
        Assert.Equal(.5m, satir.GelenMiktar);
        Assert.Equal(1, satir.GridGelenAdet);
    }

    [Fact]
    public async Task RevizyonA_MiktarDegisikligiOlmadigiIcinOrijinalMiktarBosKalir()
    {
        await using var context = Context();
        var sandik = new Sandik { Id = 30, ProjeId = 10, SandikNo = "1" };
        context.Sandiklar.Attach(sandik);

        await InvokeAsync(Service(context), "RevizyonSatiriEkleAsync", 10, 15,
            ImportSatiri(2.125m), new Dictionary<string, Sandik> { ["1"] = sandik }, SandikBilgileri(), 7);

        var satir = Assert.Single(context.ChangeTracker.Entries<CekiSatiri>()).Entity;
        Assert.Equal(2.125m, satir.IstenenAdet);
        Assert.Null(satir.OrijinalIstenenAdet);
        Assert.Equal(EntityState.Added, context.Entry(satir).State);
    }

    [Fact]
    public async Task IlkNormalCekiYuklemesi_MiktarDegisikligiOlmadigiIcinOrijinalMiktarBosKalir()
    {
        await using var context = Context();
        var uow = new UowStub();
        var dosyaAdi = $"orijinal-miktar-test-{Guid.NewGuid():N}.xlsx";
        var projeId = Random.Shared.Next(1_800_000_000, int.MaxValue);
        var uploads = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", projeId.ToString());
        Assert.False(Directory.Exists(uploads));
        uow.Repo<Proje>().YeniId = projeId;
        try
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("ÇIKTI SAYFASI");
            sheet.Cell(1, 1).Value = "FB NO";
            sheet.Cell(1, 2).Value = "TEST-ORIGINAL";
            sheet.Cell(6, 1).Value = 1;
            sheet.Cell(6, 3).Value = "TEST-BARKOD";
            sheet.Cell(6, 4).Value = "Test ürün";
            sheet.Cell(6, 5).Value = "1";
            sheet.Cell(6, 6).Value = 4;
            sheet.Cell(6, 7).Value = "Adet";
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            await Service(context, uow).CekiYukleAsync(stream, dosyaAdi);

            var satir = Assert.Single(uow.Repo<CekiSatiri>().Rows);
            Assert.Equal(4, satir.IstenenAdet);
            Assert.Null(satir.OrijinalIstenenAdet);
        }
        finally
        {
            var testDosyasi = Path.Combine(uploads, dosyaAdi);
            if (File.Exists(testDosyasi)) File.Delete(testDosyasi);
            if (Directory.Exists(uploads)) Directory.Delete(uploads); // yalnızca bu teste ait boş dizin
        }
    }

    private static AppDbContext Context() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql("Host=database-must-not-be-contacted.invalid;Database=test;Username=test;Password=test")
        .AddInterceptors(new BaglantiyiEngelle(), new BosSelectSonucu()).Options);

    private static CekiService Service(AppDbContext context, IUnitOfWork? uow = null) =>
        new(uow!, context, null!, new DurumStub(), null!, NullLogger<CekiService>.Instance);

    private static object ImportSatiri(decimal miktar)
    {
        var type = typeof(CekiService).GetNestedType("CiktiSatirImportBilgisi", BindingFlags.NonPublic)!;
        var satir = Activator.CreateInstance(type)!;
        foreach (var (property, value) in new Dictionary<string, object>
        {
            ["ExcelSatirNo"] = 6, ["SiraNo"] = 1, ["BarkodNo"] = "TEST", ["Aciklama"] = "TEST",
            ["KoliNo"] = "1", ["IstenenAdet"] = miktar, ["BirimId"] = (int)Birim.Adet
        }) type.GetProperty(property)!.SetValue(satir, value);
        return satir;
    }

    private static object SandikBilgileri() => Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(
        typeof(string), typeof(CekiService).GetNestedType("SandikImportBilgisi", BindingFlags.NonPublic)!))!;

    private static Task InvokeAsync(CekiService servis, string name, params object[] args) =>
        Assert.IsAssignableFrom<Task>(typeof(CekiService).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(servis, args));

    private sealed class BaglantiyiEngelle : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult.Suppress());
    }

    private sealed class BosSelectSonucu : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Assert.StartsWith("SELECT ", command.CommandText);
            Assert.True(command.CommandText.Contains("FROM \"SandikIcerikleri\"") || command.CommandText.Contains("FROM \"Projeler\""));
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(new DataTable().CreateDataReader()));
        }
    }

    private sealed class DurumStub : IDurumHesaplaService
    {
        public void HesaplaKalanVeDurum(CekiSatiri satir) { }
        public int HesaplaGenelDurum(int grid, int uck) => (int)UrunDurum.Bekliyor;
    }

    private sealed class UowStub : IUnitOfWork
    {
        private readonly Dictionary<Type, object> _repos = new();
        public RepoStub<T> Repo<T>() where T : BaseEntity
        {
            if (!_repos.TryGetValue(typeof(T), out var repo)) _repos[typeof(T)] = repo = new RepoStub<T>();
            return (RepoStub<T>)repo;
        }
        public IGenericRepository<T> GetRepository<T>() where T : BaseEntity => Repo<T>();
        public bool HasActiveTransaction => false;
        public Task<int> SaveChangesAsync(CancellationToken token = default) => Task.FromResult(1);
        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken token = default) => operation(token);
        public void RegisterAfterCommit(Func<CancellationToken, Task> callback) => throw new NotSupportedException();
        public void RegisterAfterRollback(Func<CancellationToken, Task> callback) => throw new NotSupportedException();
        public void Dispose() { }
    }

    private sealed class RepoStub<T> : IGenericRepository<T> where T : BaseEntity
    {
        public List<T> Rows { get; } = [];
        public int YeniId { get; set; } = 100;
        public Task AddAsync(T entity) { entity.Id = YeniId++; Rows.Add(entity); return Task.CompletedTask; }
        public void Update(T entity) { }
        public void Remove(T entity) => throw new NotSupportedException();
        public Task<T?> GetByIdAsync(int id) => throw new NotSupportedException();
        public IQueryable<T> Queryable() => Rows.AsQueryable();
        public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) => throw new NotSupportedException();
        public Task<IEnumerable<T>> GetAllAsync() => throw new NotSupportedException();
        public Task<IEnumerable<T>> GetAllWithIncludeAsync<TProp>(Expression<Func<T, TProp>> include) => throw new NotSupportedException();
    }
}
