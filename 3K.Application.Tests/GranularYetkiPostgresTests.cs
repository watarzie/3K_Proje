using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

public sealed class GranularYetkiPostgresTests
{
    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task IzolePostgres_GecisKritikYetkiVermez_KisiselRetAnindaEtkin_RerunIptaliGeriAlmaz()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("THREEK_TEST_POSTGRES"));
        Assert.True(connection.Host is "127.0.0.1" or "localhost");
        Assert.Equal(55439, connection.Port);
        Assert.Equal("postgres", connection.Database);
        var database = $"permission_tests_{Guid.NewGuid():N}";
        await using var admin = new NpgsqlConnection(connection.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin))
            await create.ExecuteNonQueryAsync();
        try
        {
            connection.Database = database;
            var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection.ConnectionString).Options;
            await using var context = new AppDbContext(options);
            await context.Database.EnsureCreatedAsync();
            context.Roller.Add(new() { Id = 77, Ad = "Eski Finans W" });
            context.Kullanicilar.AddRange(
                new() { Id = 7, RolId = 1, AdSoyad = "Sentetik Yetki Yöneticisi", Email = "admin@example.invalid" },
                new() { Id = 8, RolId = 77, AdSoyad = "Sentetik Operatör", Email = "operator@example.invalid" });
            context.RolYetkileri.Add(new() { RolId = 77, MenuTanimiId = 47, YetkiTipiId = 3 });
            await context.SaveChangesAsync();
            var script = await File.ReadAllTextAsync(ScriptPath());
            await using (var command = new NpgsqlCommand(script, new NpgsqlConnection(connection.ConnectionString)))
            {
                await command.Connection!.OpenAsync();
                await command.ExecuteNonQueryAsync();
                await command.Connection.CloseAsync();
            }
            context.ChangeTracker.Clear();
            var roles = new RolService(context);
            Assert.True(await roles.HasUserPermissionAsync(8, YetkiKodlari.Finans.PoGir, YetkiTipi.W));
            Assert.False(await roles.HasUserPermissionAsync(8, YetkiKodlari.Finans.KaliciSil, YetkiTipi.W));
            Assert.False(await roles.HasUserPermissionAsync(8, YetkiKodlari.Finans.PoDegistir, YetkiTipi.W));
            Assert.True(await roles.HasUserPermissionAsync(7, YetkiKodlari.Finans.KaliciSil, YetkiTipi.W));

            using var uow = new UnitOfWork(context, NullLogger<UnitOfWork>.Instance);
            var service = new KullaniciYetkiService(context, roles, new OrtakUser(7), uow);
            var po = YetkiKatalogu.Bul(YetkiKodlari.Finans.PoGir)!.Id;
            var denied = await service.UpdateAsync(8, [new KullaniciYetkiKarari(po, false)]);
            Assert.True(denied.Basarili, denied.Hata);
            Assert.False(await roles.HasUserPermissionAsync(8, YetkiKodlari.Finans.PoGir, YetkiTipi.W));
            Assert.Single(await context.YetkiDegisiklikleri.Where(x => x.HedefTuru == "Kullanici" && x.HedefId == 8).ToListAsync());

            var permanentDelete = await context.RolYetkileri.SingleAsync(x =>
                x.RolId == 1 && x.MenuTanimiId == YetkiKatalogu.Bul(YetkiKodlari.Finans.KaliciSil)!.Id);
            context.RolYetkileri.Remove(permanentDelete);
            await context.SaveChangesAsync();
            await using (var repeatConnection = new NpgsqlConnection(connection.ConnectionString))
            {
                await repeatConnection.OpenAsync();
                await using var repeat = new NpgsqlCommand(script, repeatConnection);
                await repeat.ExecuteNonQueryAsync();
            }
            Assert.False(await roles.HasUserPermissionAsync(7, YetkiKodlari.Finans.KaliciSil, YetkiTipi.W));
            Assert.False(await roles.HasUserPermissionAsync(8, YetkiKodlari.Finans.PoGir, YetkiTipi.W));

            var invalid = await service.UpdateAsync(8, [new KullaniciYetkiKarari(-1, true)]);
            Assert.False(invalid.Basarili);
            Assert.False(await roles.HasUserPermissionAsync(8, YetkiKodlari.Finans.PoGir, YetkiTipi.W));
            Assert.Single(await context.KullaniciYetkileri.Where(x => x.KullaniciId == 8).ToListAsync());
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static string ScriptPath()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "scripts", "database", "20260919_04_Granular_Yetkiler.sql");
            if (File.Exists(candidate)) return candidate;
        }
        throw new FileNotFoundException("Granular izin geçiş SQL dosyası bulunamadı.");
    }
}
