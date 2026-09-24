using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using _3K.Application.Features.AmbalajIslemleri.Commands;
using _3K.Application.Features.AmbalajIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

public sealed class AmbalajUretimV2PostgresTests
{
    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task PlanlamaOngorusu_SecimOncesi_SqlSayfalamaVeDurumErisimiyleTutarlidir()
    {
        await WithDatabase(async options =>
        {
            await using var ctx = new AppDbContext(options);
            ctx.Projeler.AddRange(new Proje { Id = 11, ProjeNo = "TEST-11" },
                new Proje { Id = 12, ProjeNo = "TEST-12" });
            ctx.Sandiklar.AddRange(new Sandik { Id = 11, ProjeId = 11, SandikNo = "1", Ad = "Radyatör",
                Boy = 1800, En = 900, Yukseklik = 800 },
                new Sandik { Id = 12, ProjeId = 12, SandikNo = "1", Ad = "Radyatör",
                    Boy = 1800, En = 900, Yukseklik = 800 });
            ctx.Set<AmbalajUretimKaydi>().AddRange(
                new AmbalajUretimKaydi { Id = 11, ProjeId = 11, KaynakKayitId = 11,
                    Boy = 1800, En = 900, Yukseklik = 800,
                    HesaplananToplamM3 = 7m, M3Override = 2m, AmbalajaDahil = true, UretimeAlindi = false },
                new AmbalajUretimKaydi { Id = 12, ProjeId = 12, KaynakKayitId = 12,
                    Boy = 1800, En = 900, Yukseklik = 800,
                    HesaplananToplamM3 = 3m, AmbalajaDahil = true, UretimeAlindi = true,
                    UretimDurumu = AmbalajUretimDurumu.Tamamlandi });
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();
            using var uow = new UnitOfWork(ctx, NullLogger<UnitOfWork>.Instance);
            var handler = new GetAmbalajPlanlamaProjeleriQueryHandler(uow, new EntityFrameworkReadQueryExecutor());
            var firstPage = await handler.Handle(new() { PageSize = 1, PageNumber = 1 }, default);
            var secondPage = await handler.Handle(new() { PageSize = 1, PageNumber = 2 }, default);
            Assert.True(firstPage.IsSuccess);
            Assert.Equal(5m, firstPage.Value!.FilteredSummary!.ToplamHacimM3);
            Assert.Equal(5m, secondPage.Value!.FilteredSummary!.ToplamHacimM3);
            Assert.Equal(2m, Assert.Single(secondPage.Value.Items).ProjeSandiklariHacimM3);
            Assert.Equal(0m, Assert.Single(secondPage.Value.Items).UretimHacimM3);

            var waitingOnly = await handler.Handle(new() { IzinliDurumlar = [1] }, default);
            Assert.Equal(2m, waitingOnly.Value!.FilteredSummary!.ToplamHacimM3);
            Assert.Equal(11, Assert.Single(waitingOnly.Value.Items).ProjeId);
            Assert.Empty(ctx.ChangeTracker.Entries());
            Assert.Empty(await ctx.Set<AmbalajUretimFormuSurumu>().ToListAsync());
            Assert.Empty(await ctx.Set<AmbalajUretimHareketi>().ToListAsync());
        });
    }

    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task Form_AyniAnahtarParalelRetry_TekSurumVeTekDurumGecisi()
    {
        await WithDatabase(async options =>
        {
            var request = new AmbalajFormOlusturCommand { KayitIdleri = [1], IdempotencyAnahtari = Guid.NewGuid() };
            async Task<int> Create()
            {
                await using var ctx = new AppDbContext(options);
                using var uow = new UnitOfWork(ctx, NullLogger<UnitOfWork>.Instance);
                var result = await Handler(ctx, uow).Handle(request, default);
                Assert.True(result.IsSuccess, result.Error?.Message);
                return result.Value!.Id;
            }
            var ids = await Task.WhenAll(Create(), Create());
            Assert.Equal(ids[0], ids[1]);
            await using var verify = new AppDbContext(options);
            Assert.Single(await verify.Set<AmbalajUretimFormuSurumu>().ToListAsync());
            Assert.Single(await verify.Set<AmbalajUretimFormuKaydi>().ToListAsync());
            Assert.Equal(AmbalajUretimDurumu.Uretimde, (await verify.Set<AmbalajUretimKaydi>().SingleAsync()).UretimDurumu);
            Assert.Single(await verify.Set<AmbalajUretimHareketi>().Where(x => x.AlanAdi == "UretimDurumu").ToListAsync());
            Assert.DoesNotContain("\"NetM3\":0", (await verify.Set<AmbalajUretimFormuSurumu>().SingleAsync()).SnapshotJson);
        });
    }

    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task Form_FinansHatasi_SnapshotSecimAuditAtomikRollback()
    {
        await WithDatabase(async options =>
        {
            await using (var ctx = new AppDbContext(options))
            {
                using var uow = new UnitOfWork(ctx, NullLogger<UnitOfWork>.Instance);
                await Assert.ThrowsAsync<InvalidOperationException>(() => Handler(ctx, uow, fail: true).Handle(
                    new() { KayitIdleri = [1], IdempotencyAnahtari = Guid.NewGuid() }, default));
            }
            await using var verify = new AppDbContext(options);
            var record = await verify.Set<AmbalajUretimKaydi>().SingleAsync();
            Assert.False(record.UretimeAlindi); Assert.False(record.KaynakSenkronizasyonuKilitliMi);
            Assert.Equal(AmbalajUretimDurumu.Planlandi, record.UretimDurumu);
            Assert.Empty(await verify.Set<AmbalajUretimFormuSurumu>().ToListAsync());
            Assert.Empty(await verify.Set<AmbalajUretimFormuKaydi>().ToListAsync());
            Assert.Empty(await verify.Set<AmbalajUretimHareketi>().ToListAsync());
        });
    }

    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task Form_XminEskiYazmayiReddeder_VeSnapshotBaglantisiFizikselSilmeyiEngeller()
    {
        await WithDatabase(async options =>
        {
            await using var oldContext = new AppDbContext(options);
            var old = await oldContext.Set<AmbalajUretimKaydi>().SingleAsync();
            await using (var ctx = new AppDbContext(options))
            {
                using var uow = new UnitOfWork(ctx, NullLogger<UnitOfWork>.Instance);
                Assert.True((await Handler(ctx, uow).Handle(new() { KayitIdleri = [1], IdempotencyAnahtari = Guid.NewGuid() }, default)).IsSuccess);
            }
            old.Adet = 99;
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => oldContext.SaveChangesAsync());
            await using var deleteContext = new AppDbContext(options);
            deleteContext.Remove(await deleteContext.Set<AmbalajUretimKaydi>().SingleAsync());
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => deleteContext.SaveChangesAsync());
            Assert.Contains(((PostgresException)exception.InnerException!).SqlState,
                new[] { PostgresErrorCodes.ForeignKeyViolation, PostgresErrorCodes.RestrictViolation });
            await using var verify = new AppDbContext(options);
            Assert.Single(await verify.Set<AmbalajUretimFormuSurumu>().ToListAsync());
            Assert.Single(await verify.Set<AmbalajUretimKaydi>().ToListAsync());
        });
    }

    private static AmbalajFormOlusturCommandHandler Handler(AppDbContext ctx, IUnitOfWork uow, bool fail = false) =>
        new(uow, new User(), new Roles(), new Finans(fail), islemKilidi: new AmbalajFormIslemKilidi(ctx));

    private static async Task WithDatabase(Func<DbContextOptions<AppDbContext>, Task> test)
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("THREEK_TEST_POSTGRES"));
        Assert.True(connection.Host is "127.0.0.1" or "localhost");
        Assert.Equal(55439, connection.Port); Assert.Equal("postgres", connection.Database);
        var database = $"uretim_v2_tests_{Guid.NewGuid():N}";
        await using var admin = new NpgsqlConnection(connection.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin)) await create.ExecuteNonQueryAsync();
        try
        {
            connection.Database = database;
            var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection.ConnectionString).Options;
            await using (var ctx = new AppDbContext(options))
            {
                await ctx.Database.EnsureCreatedAsync();
                ctx.Kullanicilar.Add(new() { Id = 7, RolId = 1, AdSoyad = "Sentetik Üretim Operatörü", Email = "uretim@example.invalid" });
                ctx.Set<AmbalajUretimKaydi>().Add(new()
                {
                    Id = 1, ManuelProjeNo = "SENTETIK", SandikNo = "1", Ad = "Kontra test", SandikCinsi = AmbalajSandikCinsi.Kontrplak,
                    Adet = 4, Boy = 50, En = 60, Yukseklik = 70, M3HesaplamaVersiyonu = "uygulanmaz-v1"
                });
                await ctx.SaveChangesAsync();
            }
            await test(options);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            // Yalnız bu testin oluşturduğu, sabit önek + GUID isimli sentetik veritabanı kaldırılır.
            Assert.Matches("^uretim_v2_tests_[a-f0-9]{32}$", database);
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
    private sealed class User : ICurrentUserService { public int? UserId => 7; public bool IsAuthenticated => true; public string? MenuKod => null; }
    private sealed class Roles : IRolService
    {
        public Task<bool> HasUserPermissionAsync(int id, string kod, YetkiTipi tip, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> IsAdminAsync(int id, CancellationToken ct = default) => Task.FromResult(false);
        public Task<List<MenuTanimi>> GetMenuAgaciAsync(CancellationToken ct = default) => Task.FromResult(new List<MenuTanimi>());
        public Task<List<RolYetki>> GetRolYetkileriAsync(int id, CancellationToken ct = default) => Task.FromResult(new List<RolYetki>());
        public Task YetkileriGuncelleAsync(int id, List<RolYetki> list, CancellationToken ct = default) => Task.CompletedTask;
    }
    private sealed class Finans(bool fail) : IFinansUretimAktarimService
    {
        public Task<FinansSenkronizasyonSonucModel> UretimKayitlariniAktarAsync(IReadOnlyList<FinansUretimAktarimModel> m, CancellationToken ct) =>
            fail ? throw new InvalidOperationException("Sentetik aktarım hatası") : Task.FromResult(new FinansSenkronizasyonSonucModel(0, m.Count, 0));
    }
}
