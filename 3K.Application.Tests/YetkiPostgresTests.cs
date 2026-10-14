using Microsoft.EntityFrameworkCore;
using Npgsql;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

/// <summary>
/// Yalnız izole sentetik PostgreSQL'de (localhost:55439) çalışır; gerçek sorgularla rol yetkisinin,
/// rol değişikliğinin sonraki istekte etkisinin ve rol atama denetiminin doğruluğunu sınar. Uygulama
/// veritabanını kullanmaz.
/// </summary>
public sealed class YetkiPostgresTests
{
    private const int FinansKok = 47, SekmeIsTakibi = 5239, SekmePoFatura = 5240, SekmeGiderler = 5241;

    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task IzolePostgres_RolDegisikligiSonrakiIstekteEtkin_RolAtamaDenetlenir()
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
            await YetkiVerisiniHazirlaAsync(context);

            // Her çağrı ayrı istek gibi yeni scoped servis kullanır.
            Task<bool> Yetkili(int kullanici, string kod) =>
                new RolService(context).HasUserPermissionAsync(kullanici, kod, YetkiTipi.W);

            Assert.True(await Yetkili(8, YetkiKodlari.Finans.SekmePoFatura));
            Assert.False(await Yetkili(8, YetkiKodlari.Finans.SekmeGiderler));
            Assert.False(await Yetkili(8, YetkiKodlari.Finans.SekmeIsTakibi));
            Assert.True(await Yetkili(7, YetkiKodlari.Finans.SekmeGiderler));

            // Rol atama: yönetici, sahip olduğu izinlerden oluşan rolü atayabilir; kullanıcı yazma izni
            // olmayan operatör rol atayamaz.
            Assert.True((await new RolAtamaService(context, new RolService(context), new OrtakUser(7))
                .RolAtamayiDogrulaAsync(8, 77)).Basarili);
            Assert.Equal(403, (await new RolAtamaService(context, new RolService(context), new OrtakUser(8))
                .RolAtamayiDogrulaAsync(7, 77)).DurumKodu);

            // Rolden kaldırılan izin bir sonraki istekte geçersizdir.
            var poIzni = await context.RolYetkileri.SingleAsync(x => x.RolId == 77 && x.MenuTanimiId == SekmePoFatura);
            context.RolYetkileri.Remove(poIzni);
            var giderlerIzni = await context.RolYetkileri.SingleAsync(x => x.RolId == 1 && x.MenuTanimiId == SekmeGiderler);
            context.RolYetkileri.Remove(giderlerIzni);
            await context.SaveChangesAsync();
            Assert.False(await Yetkili(8, YetkiKodlari.Finans.SekmePoFatura));
            Assert.False(await Yetkili(7, YetkiKodlari.Finans.SekmeGiderler));
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static async Task YetkiVerisiniHazirlaAsync(AppDbContext context)
    {
        var menuler = new[]
        {
            (FinansKok, YetkiKodlari.Finans.Modul, (int?)null),
            (SekmePoFatura, YetkiKodlari.Finans.SekmePoFatura, (int?)FinansKok),
            (SekmeIsTakibi, YetkiKodlari.Finans.SekmeIsTakibi, (int?)FinansKok),
            (SekmeGiderler, YetkiKodlari.Finans.SekmeGiderler, (int?)FinansKok),
            (11, YetkiKodlari.Kullanicilar, (int?)null)
        };
        foreach (var (id, kod, ust) in menuler)
            if (!await context.MenuTanimlari.AnyAsync(x => x.Id == id))
                context.MenuTanimlari.Add(new MenuTanimi { Id = id, Kod = kod, LabelKey = kod, Icon = "", ParentId = ust });
        if (!await context.Roller.AnyAsync(x => x.Id == SistemRolleri.AdminId))
            context.Roller.Add(new Rol { Id = SistemRolleri.AdminId, Ad = SistemRolleri.AdminAdi });
        context.Roller.Add(new Rol { Id = 77, Ad = "Finans operatörü" });
        await context.SaveChangesAsync();

        context.Kullanicilar.AddRange(
            new() { Id = 7, RolId = SistemRolleri.AdminId, AdSoyad = "Sentetik Yetki Yöneticisi", Email = "admin@example.invalid" },
            new() { Id = 8, RolId = 77, AdSoyad = "Sentetik Operatör", Email = "operator@example.invalid" });
        context.RolYetkileri.RemoveRange(await context.RolYetkileri.Where(x => x.RolId == SistemRolleri.AdminId).ToListAsync());
        context.RolYetkileri.AddRange(
            new RolYetki { RolId = SistemRolleri.AdminId, MenuTanimiId = 11, YetkiTipiId = (int)YetkiTipi.W },
            new RolYetki { RolId = SistemRolleri.AdminId, MenuTanimiId = FinansKok, YetkiTipiId = (int)YetkiTipi.W },
            new RolYetki { RolId = SistemRolleri.AdminId, MenuTanimiId = SekmePoFatura, YetkiTipiId = (int)YetkiTipi.W },
            new RolYetki { RolId = SistemRolleri.AdminId, MenuTanimiId = SekmeGiderler, YetkiTipiId = (int)YetkiTipi.W },
            new RolYetki { RolId = 77, MenuTanimiId = FinansKok, YetkiTipiId = (int)YetkiTipi.W },
            new RolYetki { RolId = 77, MenuTanimiId = SekmePoFatura, YetkiTipiId = (int)YetkiTipi.W });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }
}
