using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using _3K.Application.Features.AmbalajIslemleri;
using _3K.Application.Features.AmbalajIslemleri.Commands;
using _3K.Application.Features.AmbalajIslemleri.Queries;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;

namespace _3K.Application.Tests;

/// <summary>
/// Kayıtlı iç sandık tipi yönetimi (2026-11-23): liste kullanım sayısı ve aktiflikle döner; düzenleme mevcut kayıtları
/// değiştirmez; kullanılan tip (iptal edilmiş ambalaj kaydı, finans işi veya tarife eşleşmesi dahil) silinmez, pasife
/// alınır; pasif tip yeni bağlantıda reddedilir, kaydın zaten bağlı olduğu tip korunur.
/// </summary>
public sealed class AmbalajIcSandikTipiPostgresTests
{
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task IcSandikTipi_ListeDuzenlePasifSil_KullanilanSilinmez()
    {
        await WithDatabase(async options =>
        {
            await using var ctx = new AppDbContext(options);
            using var uow = new UnitOfWork(ctx, NullLogger<UnitOfWork>.Instance);
            var roller = OrtakRoller.TumIzinler();
            var kullanici = new OrtakUser();

            var eklenen = await new AmbalajIcSandikSablonuEkleCommandHandler(uow, kullanici, roller).Handle(new()
            {
                Ad = "QA iç katlanır", SandikTipi = IcSandikKatlanirKurali.SandikTipiAdi, Boy = 800, En = 400, Yukseklik = 350
            }, default);
            Assert.True(eklenen.IsSuccess, eklenen.Error?.Message);
            Assert.Equal((IcSandikKatlanirKurali.SandikTipiAdi, true, 0), (eklenen.Value!.SandikTipi, eklenen.Value.Aktif, eklenen.Value.KullanimSayisi));
            var kullanilmayan = await new AmbalajIcSandikSablonuEkleCommandHandler(uow, kullanici, roller).Handle(new()
            {
                Ad = "QA boş tip", SandikTipi = "Ahşap Kapalı", Boy = 500, En = 500, Yukseklik = 500
            }, default);
            var id = eklenen.Value.Id;

            // Tipten kopyalanmış iptal edilmiş ambalaj kaydı ve finans eşleşmesi kullanım sayılır.
            ctx.Set<AmbalajUretimKaydi>().Add(new AmbalajUretimKaydi
            {
                ManuelProjeNo = "SENTETIK", SandikNo = "IC-1", Ad = "QA iç katlanır", Tur = AmbalajSandikTuru.Ic,
                SandikCinsi = AmbalajSandikCinsi.IcKatlanir, IcSandikSablonId = id, Adet = 1, Boy = 800, En = 400, Yukseklik = 350,
                M3HesaplamaVersiyonu = "uygulanmaz-v1", IptalMi = true
            });
            var urun = new FinansUrun { Kod = "ICA", Ad = "İç katlanır A", FiyatlandirmaBirimi = FinansFiyatlandirmaBirimi.Adet };
            urun.Eslesmeler.Add(new FinansUrunEslesmesi { IsTuru = FinansIsTuru.IcSandik, IcSandikSablonId = id, Aktif = true });
            ctx.Add(urun);
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();

            // Düzenleme: ad/ölçü değişir, mevcut ambalaj kaydı kopyaladığı değerleri korur; aynı ad başka tipte 409.
            var guncel = await new AmbalajIcSandikSablonuGuncelleCommandHandler(uow, kullanici, roller).Handle(new()
            {
                SablonId = id, Ad = "QA iç katlanır v2", SandikTipi = IcSandikKatlanirKurali.SandikTipiAdi, Boy = 820, En = 410, Yukseklik = 360
            }, default);
            Assert.True(guncel.IsSuccess, guncel.Error?.Message);
            Assert.Equal(("QA iç katlanır v2", (decimal?)820m, 2), (guncel.Value!.Ad, guncel.Value.Boy, guncel.Value.KullanimSayisi));
            Assert.Equal(800m, await ctx.Set<AmbalajUretimKaydi>().AsNoTracking().Where(x => x.IcSandikSablonId == id).Select(x => x.Boy).SingleAsync());
            ctx.ChangeTracker.Clear();
            var ayniAd = await new AmbalajIcSandikSablonuGuncelleCommandHandler(uow, kullanici, roller).Handle(new()
            {
                SablonId = kullanilmayan.Value!.Id, Ad = " QA iç katlanır v2 ", SandikTipi = "Ahşap Kapalı", Boy = 1, En = 1, Yukseklik = 1
            }, default);
            Assert.Equal(409, ayniAd.StatusCode);
            ctx.ChangeTracker.Clear();

            // Kullanılan tip silinmez (iptal edilmiş kayıt dahil); pasife alınır.
            var sil = await new AmbalajIcSandikSablonuSilCommandHandler(uow).Handle(new() { SablonId = id }, default);
            Assert.Equal((409, AmbalajIcSandikSablonKullanimi.KullanimdaMesaji), (sil.StatusCode, sil.Error!.Message));
            ctx.ChangeTracker.Clear();
            var pasif = await new AmbalajIcSandikSablonuDurumCommandHandler(uow, kullanici, roller).Handle(new() { SablonId = id, Aktif = false }, default);
            Assert.False(pasif.Value!.Aktif);
            ctx.ChangeTracker.Clear();

            var liste = await new GetAmbalajIcSandikSablonlariQueryHandler(uow, roller, kullanici).Handle(new(), default);
            Assert.Equal([("QA boş tip", true, 0), ("QA iç katlanır v2", false, 2)],
                liste.Value!.Select(x => (x.Ad, x.Aktif, x.KullanimSayisi)));

            // Pasif tip yeni bağlantıda reddedilir; kaydın zaten bağlı olduğu tip korunur.
            var sablonlar = ctx.Set<AmbalajIcSandikSablonu>().AsNoTracking();
            const AmbalajSandikCinsi cins = AmbalajSandikCinsi.IcKatlanir;
            Assert.Equal("Seçilen iç sandık tipi pasif; yeni kayıtta kullanılamaz.",
                AmbalajPlanlamaYardimcisi.IcSandikSablonuHatasi(sablonlar, id, null, cins));
            Assert.Null(AmbalajPlanlamaYardimcisi.IcSandikSablonuHatasi(sablonlar, id, id, cins));
            Assert.Equal(AmbalajPlanlamaYardimcisi.IcKatlanirTipUyumMesaji,
                AmbalajPlanlamaYardimcisi.IcSandikSablonuHatasi(sablonlar, id, id, AmbalajSandikCinsi.AhsapKapali));
            Assert.Equal("Seçilen kayıtlı iç sandık tipi bulunamadı.", AmbalajPlanlamaYardimcisi.IcSandikSablonuHatasi(sablonlar, 9999, null, cins));

            // Hiç kullanılmamış tip kalıcı silinir.
            var bosSil = await new AmbalajIcSandikSablonuSilCommandHandler(uow).Handle(new() { SablonId = kullanilmayan.Value.Id }, default);
            Assert.True(bosSil.IsSuccess, bosSil.Error?.Message);
            Assert.False(await ctx.Set<AmbalajIcSandikSablonu>().AnyAsync(x => x.Id == kullanilmayan.Value.Id));
        });
    }

    /// <summary>Sentetik izole veritabanı (THREEK_TEST_POSTGRES, port 55439); test sonunda silinir.</summary>
    private static async Task WithDatabase(Func<DbContextOptions<AppDbContext>, Task> test)
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("THREEK_TEST_POSTGRES"));
        Assert.True(connection.Host is "127.0.0.1" or "localhost");
        Assert.Equal(55439, connection.Port); Assert.Equal("postgres", connection.Database);
        var database = $"ic_sandik_tipi_tests_{Guid.NewGuid():N}";
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
                ctx.Kullanicilar.Add(new() { Id = 7, RolId = SistemRolleri.AdminId, AdSoyad = "Sentetik Ambalaj Kullanıcısı", Email = "ambalaj@example.invalid" });
                await ctx.SaveChangesAsync();
            }
            await test(options);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            // Yalnız bu testin oluşturduğu, sabit önek + GUID isimli sentetik veritabanı kaldırılır.
            Assert.Matches("^ic_sandik_tipi_tests_[a-f0-9]{32}$", database);
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
