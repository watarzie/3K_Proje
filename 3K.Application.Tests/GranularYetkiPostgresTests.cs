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
    public async Task IzolePostgres_KokFinansYetkisiKritikIslemVermez_KisiselRetVeRolDegisikligiAnindaEtkin()
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
            var yetkiAtama = YetkiKatalogu.Bul(YetkiKodlari.YetkiAtama)!;
            var poGir = YetkiKatalogu.Bul(YetkiKodlari.Finans.PoGir)!;
            var kaliciSil = YetkiKatalogu.Bul(YetkiKodlari.Finans.KaliciSil)!;
            Assert.True(await context.MenuTanimlari.AnyAsync(x => x.Id == poGir.Id && x.Kod == poGir.Kod));
            Assert.True(await context.RolYetkileri.AnyAsync(x => x.RolId == 1 && x.MenuTanimiId == yetkiAtama.Id));
            Assert.True(await context.RolYetkileri.AnyAsync(x => x.RolId == 1 && x.MenuTanimiId == kaliciSil.Id));
            context.Roller.Add(new() { Id = 77, Ad = "Eski Finans W" });
            context.Kullanicilar.AddRange(
                new() { Id = 7, RolId = 1, AdSoyad = "Sentetik Yetki Yöneticisi", Email = "admin@example.invalid" },
                new() { Id = 8, RolId = 77, AdSoyad = "Sentetik Operatör", Email = "operator@example.invalid" });
            context.RolYetkileri.AddRange(
                new RolYetki { Id = 20047, RolId = 77, MenuTanimiId = 47, YetkiTipiId = (int)YetkiTipi.W },
                new RolYetki { Id = 25211, RolId = 77, MenuTanimiId = poGir.Id, YetkiTipiId = (int)YetkiTipi.W });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var roles = new RolService(context);
            Assert.True(await roles.HasUserPermissionAsync(8, YetkiKodlari.Finans.PoGir, YetkiTipi.W));
            Assert.False(await roles.HasUserPermissionAsync(8, YetkiKodlari.Finans.KaliciSil, YetkiTipi.W));
            Assert.False(await roles.HasUserPermissionAsync(8, YetkiKodlari.Finans.PoDegistir, YetkiTipi.W));
            Assert.True(await roles.HasUserPermissionAsync(7, YetkiKodlari.Finans.KaliciSil, YetkiTipi.W));

            using var uow = new UnitOfWork(context, NullLogger<UnitOfWork>.Instance);
            var service = new KullaniciYetkiService(context, roles, new OrtakUser(7), uow);
            var denied = await service.UpdateAsync(8, [new KullaniciYetkiKarari(poGir.Id, false)]);
            Assert.True(denied.Basarili, denied.Hata);
            Assert.False(await roles.HasUserPermissionAsync(8, YetkiKodlari.Finans.PoGir, YetkiTipi.W));
            Assert.True(await context.RolYetkileri.AnyAsync(x => x.RolId == 77 && x.MenuTanimiId == poGir.Id));
            Assert.Single(await context.YetkiDegisiklikleri.Where(x => x.HedefTuru == "Kullanici" && x.HedefId == 8).ToListAsync());

            var permanentDelete = await context.RolYetkileri.SingleAsync(x =>
                x.RolId == 1 && x.MenuTanimiId == kaliciSil.Id);
            context.RolYetkileri.Remove(permanentDelete);
            await context.SaveChangesAsync();
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
}
