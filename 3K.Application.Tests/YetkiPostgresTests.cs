using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

/// <summary>
/// Yalnız izole sentetik PostgreSQL'de (localhost:55439) çalışır; gerçek sorgularla kişisel ret,
/// rol değişikliği ve commit sonrası yetki olayını doğrular. Uygulama veritabanını kullanmaz.
/// </summary>
public sealed class YetkiPostgresTests
{
    private const int FinansKok = 47, PoGir = 5211, PoDegistir = 5212, KaliciSil = 5231;

    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task IzolePostgres_KisiselRetVeRolDegisikligiSonrakiIstekteEtkin()
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

            Assert.True(await Yetkili(8, YetkiKodlari.Finans.PoGir));
            Assert.False(await Yetkili(8, YetkiKodlari.Finans.KaliciSil));
            Assert.False(await Yetkili(8, YetkiKodlari.Finans.PoDegistir));
            Assert.True(await Yetkili(7, YetkiKodlari.Finans.KaliciSil));

            using var uow = new UnitOfWork(context, NullLogger<UnitOfWork>.Instance);
            var events = new List<(int[] Idler, string Olay, bool TransactionAktif)>();
            var notifier = new RecordingNotifier((ids, olay) => events.Add((ids.ToArray(), olay, uow.HasActiveTransaction)));
            var service = new KullaniciYetkiService(context, new RolService(context), new OrtakUser(7), uow, notifier);
            var denied = await service.UpdateAsync(8, [new KullaniciYetkiKarari(PoGir, false)]);
            Assert.True(denied.Basarili, denied.Hata);
            var sent = Assert.Single(events);
            Assert.Equal([8], sent.Idler);
            Assert.Equal(SseOlaylari.YetkiGuncellendi, sent.Olay);
            Assert.False(sent.TransactionAktif);
            Assert.False(await Yetkili(8, YetkiKodlari.Finans.PoGir));
            Assert.True(await context.RolYetkileri.AnyAsync(x => x.RolId == 77 && x.MenuTanimiId == PoGir));
            Assert.Single(await context.YetkiDegisiklikleri.Where(x => x.HedefTuru == "Kullanici" && x.HedefId == 8).ToListAsync());

            var kaliciSilIzni = await context.RolYetkileri.SingleAsync(x => x.RolId == 1 && x.MenuTanimiId == KaliciSil);
            context.RolYetkileri.Remove(kaliciSilIzni);
            await context.SaveChangesAsync();
            Assert.False(await Yetkili(7, YetkiKodlari.Finans.KaliciSil));

            var invalid = await service.UpdateAsync(8, [new KullaniciYetkiKarari(-1, true)]);
            Assert.False(invalid.Basarili);
            Assert.Single(events);
            Assert.Single(await context.KullaniciYetkileri.Where(x => x.KullaniciId == 8).ToListAsync());

            notifier.ThrowAfterNotify = true;
            var tekrar = await new KullaniciYetkiService(context, new RolService(context), new OrtakUser(7), uow, notifier)
                .UpdateAsync(8, [new KullaniciYetkiKarari(PoGir, false)]);
            Assert.True(tekrar.Basarili, tekrar.Hata);
            Assert.Equal(2, events.Count);
            Assert.Single(await context.KullaniciYetkileri.Where(x => x.KullaniciId == 8).ToListAsync());
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
            (PoGir, YetkiKodlari.Finans.PoGir, (int?)FinansKok),
            (PoDegistir, YetkiKodlari.Finans.PoDegistir, (int?)FinansKok),
            (KaliciSil, YetkiKodlari.Finans.KaliciSil, (int?)FinansKok),
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
            new RolYetki { RolId = SistemRolleri.AdminId, MenuTanimiId = PoGir, YetkiTipiId = (int)YetkiTipi.W },
            new RolYetki { RolId = SistemRolleri.AdminId, MenuTanimiId = KaliciSil, YetkiTipiId = (int)YetkiTipi.W },
            new RolYetki { RolId = 77, MenuTanimiId = FinansKok, YetkiTipiId = (int)YetkiTipi.W },
            new RolYetki { RolId = 77, MenuTanimiId = PoGir, YetkiTipiId = (int)YetkiTipi.W });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private sealed class RecordingNotifier(Action<IEnumerable<int>, string> onNotify) : ISseNotifier
    {
        public bool ThrowAfterNotify { get; set; }
        public Task SubscribeAsync(object context, int kullaniciId) => Task.CompletedTask;
        public Task NotifyUsersAsync(IEnumerable<int> kullaniciIdleri, string eventName, string data = "refresh")
        {
            onNotify(kullaniciIdleri, eventName);
            if (ThrowAfterNotify) throw new IOException("Geçici SSE bağlantı hatası.");
            return Task.CompletedTask;
        }
        public Task BroadcastApprovalUpdateAsync() => Task.CompletedTask;
    }
}
