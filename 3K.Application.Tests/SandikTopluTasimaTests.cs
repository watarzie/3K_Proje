using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Validators;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;

namespace _3K.Application.Tests;

public sealed class SandikTopluTasimaTests
{
    [Fact]
    public async Task LegacyAyniCekiSatirininIkiIcerigi_BirlestirilmedenVeYazmadanReddedilir()
    {
        using var uow = new OrtakMemoryUow();
        uow.Repo<SandikIcerik>().Rows.AddRange([
            new SandikIcerik { Id = 1, SandikId = 1, CekiSatiriId = 9 },
            new SandikIcerik { Id = 2, SandikId = 1, CekiSatiriId = 9 }
        ]);
        var sonuc = await new SandikUrunleriTopluTasiCommandHandler(uow, new OrtakUser(), new OrtakSaha()).Handle(new()
        {
            ProjeId = 1, KaynakSandikId = 1, HedefSandikId = 2, IslemAnahtari = Guid.NewGuid(),
            Satirlar = [new() { KaynakSandikIcerikId = 1, TasinanAdet = 1 }, new() { KaynakSandikIcerikId = 2, TasinanAdet = 1 }]
        }, default);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains("birleştirilemez", sonuc.Error!.Message);
        Assert.Equal(0, uow.SaveCount);
    }

    [Theory]
    [InlineData("bos")]
    [InlineData("mukerrer")]
    [InlineData("hassasiyet")]
    [InlineData("negatif")]
    [InlineData("anahtarsiz")]
    [InlineData("ayni-sandik")]
    [InlineData("limit")]
    public void Validator_GecersizIstekleriReddeder(string senaryo)
    {
        var request = new SandikUrunleriTopluTasiCommand
        {
            ProjeId = 1, KaynakSandikId = 1, HedefSandikId = 2, IslemAnahtari = Guid.NewGuid(),
            Satirlar = [new() { KaynakSandikIcerikId = 1, TasinanAdet = 1.2345m }]
        };
        switch (senaryo)
        {
            case "bos": request.Satirlar.Clear(); break;
            case "mukerrer": request.Satirlar.Add(new() { KaynakSandikIcerikId = 1, TasinanAdet = 1 }); break;
            case "hassasiyet": request.Satirlar[0].TasinanAdet = 1.23456m; break;
            case "negatif": request.Satirlar[0].TasinanAdet = -1; break;
            case "anahtarsiz": request.IslemAnahtari = Guid.Empty; break;
            case "ayni-sandik": request.HedefSandikId = 1; break;
            case "limit": request.Satirlar = Enumerable.Range(1, 251).Select(x => new SandikTopluTasimaSatiri { KaynakSandikIcerikId = x, TasinanAdet = 1 }).ToList(); break;
        }
        Assert.False(new SandikUrunleriTopluTasiCommandValidator().Validate(request).IsValid);
    }
}

/// <summary>Yalnız açıkça belirtilen, localhost üzerindeki izole PostgreSQL test sunucusunda çalışır.</summary>
public sealed class SandikTopluTasimaPostgresTests
{
    [SandikPostgresFact]
    public async Task KurulumSqli_YeniTabloyuVeriKaybetmedenOlusturur_VeTekrarCalisir()
    {
        await using var kurgu = await PostgresKurgu.CreateAsync();
        await using var db = kurgu.Db();
        await db.Database.ExecuteSqlRawAsync("DROP TABLE public.\"SandikTopluTasimaIslemleri\"");
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "scripts", "database", "20260919_01_Sandik_Toplu_Tasima.sql")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        var sql = await File.ReadAllTextAsync(Path.Combine(directory.FullName, "scripts", "database", "20260919_01_Sandik_Toplu_Tasima.sql"));
        await db.Database.ExecuteSqlRawAsync(sql);
        await db.Database.ExecuteSqlRawAsync(sql);
        Assert.Equal(0, await db.SandikTopluTasimaIslemleri.CountAsync());
        Assert.True((await kurgu.TasiAsync(kurgu.Istek(.5m, .5m))).IsSuccess);
    }

    [SandikPostgresFact]
    public async Task CokluManuelIcerik_AyniBarkodBirlesmez_TahsisFizikselKirilimlarVeOndalikKorunur()
    {
        await using var kurgu = await PostgresKurgu.CreateAsync();
        var sonuc = await kurgu.TasiAsync(kurgu.Istek(1.2345m, 2m));
        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        await using var db = kurgu.Db();
        var hedef = await db.SandikIcerikleri.Where(x => x.SandikId == kurgu.HedefId).OrderBy(x => x.Id).ToListAsync();
        Assert.Equal(2, hedef.Count);
        Assert.All(hedef, x => Assert.Equal("ORTAK-BARKOD", x.BarkodNo));
        Assert.Equal(1.2345m, hedef[0].TahsisMiktari);
        Assert.Equal(1m, hedef[0].KonulanAdet);
        Assert.Equal(.2m, hedef[0].StokKarsilanan);
        Assert.Equal(.3m, hedef[0].ProjeKarsilanan);
        Assert.Equal(.1m, hedef[0].TedarikciKarsilanan);
        Assert.Equal(2m, hedef[1].TahsisMiktari);
        Assert.Equal(0m, hedef[1].KonulanAdet);
        Assert.Equal(2, await db.SandikUrunTransferleri.CountAsync());
        Assert.Equal(2, await db.HareketGecmisleri.CountAsync(x => x.Islem == "Sandık Ürün Taşıma"));
        Assert.Single(await db.SandikTopluTasimaIslemleri.ToListAsync());
    }

    [SandikPostgresFact]
    public async Task IkinciSatirKapasiteHatasi_IlkSatirinKayitlariVeHareketleriGercekTransactionIleGeriAlinir()
    {
        await using var kurgu = await PostgresKurgu.CreateAsync();
        var sonuc = await kurgu.TasiAsync(kurgu.Istek(1m, 99m));
        Assert.False(sonuc.IsSuccess);
        Assert.Contains($"#{kurgu.IkinciId}", sonuc.Error!.Message);
        await kurgu.BaslangicKorunurAsync();
    }

    [SandikPostgresFact]
    public async Task AyniAnahtarVePayload_TamTasimaKaynakSilinseDeTekrarUygulanmaz_FarkliPayloadVeyaKullanici409()
    {
        await using var kurgu = await PostgresKurgu.CreateAsync();
        var istek = kurgu.Istek(2m, 2m);
        Assert.True((await kurgu.TasiAsync(istek)).IsSuccess);
        istek.Satirlar.Reverse();
        Assert.True((await kurgu.TasiAsync(istek)).IsSuccess);
        Assert.Equal(409, (await kurgu.TasiAsync(istek, kullaniciId: 8)).StatusCode);
        istek.Satirlar[0].TasinanAdet = 1;
        Assert.Equal(409, (await kurgu.TasiAsync(istek)).StatusCode);
        await using var db = kurgu.Db();
        Assert.Equal(2, await db.SandikUrunTransferleri.CountAsync());
        Assert.Equal(2, await db.SandikIcerikleri.CountAsync());
    }

    [SandikPostgresFact]
    public async Task AyniAnahtarEszamanliAgTekrari_TekDefterVeIkiSatirUretir()
    {
        await using var kurgu = await PostgresKurgu.CreateAsync();
        var istek = kurgu.Istek(.5m, .5m);
        var sonuclar = await Task.WhenAll(kurgu.TasiAsync(istek), kurgu.TasiAsync(istek));
        Assert.All(sonuclar, s => Assert.True(s.IsSuccess, s.Error?.Message));
        await using var db = kurgu.Db();
        Assert.Single(await db.SandikTopluTasimaIslemleri.ToListAsync());
        Assert.Equal(2, await db.SandikUrunTransferleri.CountAsync());
        Assert.Equal(3m, await db.SandikIcerikleri.Where(x => x.SandikId == kurgu.KaynakId).SumAsync(x => x.TahsisMiktari));
    }

    [SandikPostgresFact]
    public async Task EszamanliFarkliAnahtarlar_KaynaginAyniMiktariniIkiKezTasiyamaz()
    {
        await using var kurgu = await PostgresKurgu.CreateAsync();
        var sonuclar = await Task.WhenAll(kurgu.TasiAsync(kurgu.Istek(1.5m, 1.5m)), kurgu.TasiAsync(kurgu.Istek(1.5m, 1.5m)));
        Assert.Single(sonuclar, s => s.IsSuccess);
        Assert.Equal(409, Assert.Single(sonuclar, s => !s.IsSuccess).StatusCode);
        await using var db = kurgu.Db();
        Assert.Equal(4m, await db.SandikIcerikleri.SumAsync(x => x.TahsisMiktari));
        Assert.Equal(1m, await db.SandikIcerikleri.SumAsync(x => x.KonulanAdet));
        Assert.Equal(2, await db.SandikUrunTransferleri.CountAsync());
    }

    [SandikPostgresFact]
    public async Task SevkKilidi_DuzeltmeAcikOlsaDaTopluTasimaYapilmaz()
    {
        await using var kurgu = await PostgresKurgu.CreateAsync();
        await using (var db = kurgu.Db())
        {
            var hedef = await db.Sandiklar.SingleAsync(x => x.Id == kurgu.HedefId);
            hedef.DurumId = (int)SandikDurum.Sevkedildi;
            hedef.SevkiyatDuzeltmeAcikMi = true;
            await db.SaveChangesAsync();
        }
        var sonuc = await kurgu.TasiAsync(kurgu.Istek(1, 1));
        Assert.False(sonuc.IsSuccess);
        Assert.Contains("sevkiyatı", sonuc.Error!.Message);
        await kurgu.BaslangicKorunurAsync();
    }

    [SandikPostgresFact]
    public async Task FarkliProjeVeyaKaynakSandik_TumIslemReddedilir()
    {
        await using var kurgu = await PostgresKurgu.CreateAsync();
        var istek = kurgu.Istek(1, 1);
        istek.ProjeId += 100;
        Assert.Equal(403, (await kurgu.TasiAsync(istek)).StatusCode);
        await kurgu.BaslangicKorunurAsync();
        istek.ProjeId = kurgu.ProjeId;
        istek.KaynakSandikId += 100;
        Assert.Equal(409, (await kurgu.TasiAsync(istek)).StatusCode);
        await kurgu.BaslangicKorunurAsync();
    }

    [SandikPostgresFact]
    public async Task CekiCokluTahsis_AktifGridSayaciKaynaklarVeKaliteKorunur_AktifSahaReddedilir()
    {
        await using var kurgu = await PostgresKurgu.CreateAsync();
        int satirId;
        await using (var db = kurgu.Db())
        {
            var ceki = new Ceki { ProjeId = kurgu.ProjeId };
            var satir = new CekiSatiri { Ceki = ceki, IstenenAdet = 4, GelenMiktar = 1, AktifGridSevkKarsilananMiktari = .4m, KaliteDurumId = 2 };
            db.CekiSatirlari.Add(satir);
            var ilk = await db.SandikIcerikleri.SingleAsync(x => x.Id == kurgu.IlkId);
            ilk.CekiSatiri = satir;
            ilk.AktifGridSevkKarsilananMiktari = .4m;
            db.SandikIcerikleri.Add(new SandikIcerik { SandikId = kurgu.HedefId, CekiSatiri = satir, TahsisMiktari = 2, Miktar = 2, AktifGridSevkKarsilananMiktari = 0 });
            await db.SaveChangesAsync();
            satirId = satir.Id;
        }
        var istek = kurgu.Istek(.5m, .5m);
        var saha = new OrtakSaha();
        saha.AktifKaynaklar.Add(satirId);
        Assert.False((await kurgu.TasiAsync(istek, saha: saha)).IsSuccess);
        await using (var db = kurgu.Db())
        {
            Assert.Equal(0, await db.SandikUrunTransferleri.CountAsync());
            Assert.Equal(0, await db.SandikTopluTasimaIslemleri.CountAsync());
        }
        Assert.True((await kurgu.TasiAsync(istek)).IsSuccess);
        await using (var db = kurgu.Db())
        {
            var satir = await db.CekiSatirlari.SingleAsync(x => x.Id == satirId);
            var paylar = await db.SandikIcerikleri.Where(x => x.CekiSatiriId == satirId).OrderBy(x => x.Id).ToListAsync();
            Assert.Equal(2, paylar.Count);
            Assert.Equal(4, paylar.Sum(x => x.TahsisMiktari));
            Assert.Equal(1, paylar.Sum(x => x.KonulanAdet));
            Assert.Equal(.4m, paylar.Sum(x => x.AktifGridSevkKarsilananMiktari));
            Assert.Equal(.2m, paylar[0].AktifGridSevkKarsilananMiktari);
            Assert.Equal(.2m, paylar[1].AktifGridSevkKarsilananMiktari);
            Assert.Equal(2, satir.KaliteDurumId);
            Assert.Equal(1, satir.GelenMiktar);
        }
    }

    private sealed class PostgresKurgu : IAsyncDisposable
    {
        private readonly string _connectionString;
        private PostgresKurgu(string connectionString) => _connectionString = connectionString;
        public int ProjeId { get; private set; }
        public int KaynakId { get; private set; }
        public int HedefId { get; private set; }
        public int IlkId { get; private set; }
        public int IkinciId { get; private set; }

        public static async Task<PostgresKurgu> CreateAsync()
        {
            var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("THREEK_TEST_POSTGRES"));
            if (connection.Host is not ("localhost" or "127.0.0.1") || connection.Database != "postgres")
                throw new InvalidOperationException("THREEK_TEST_POSTGRES yalnız izole localhost sunucusunda postgres yönetim bağlantısı olabilir.");
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            var database = "sandik_toplu_tests_" + Guid.NewGuid().ToString("N");
            await using (var admin = new NpgsqlConnection(connection.ConnectionString))
            {
                await admin.OpenAsync();
                await using var cmd = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin);
                await cmd.ExecuteNonQueryAsync();
            }
            connection.Database = database;
            var kurgu = new PostgresKurgu(connection.ConnectionString);
            await using var db = kurgu.Db();
            await db.Database.EnsureCreatedAsync();
            db.Kullanicilar.Add(new Kullanici { Id = 7, AdSoyad = "Test", Email = "bulk@example.invalid", RolId = 1 });
            var proje = new Proje { ProjeNo = "TEST-TOPLU" };
            var kaynak = new Sandik { Proje = proje, SandikNo = "1", DurumId = (int)SandikDurum.Hazirlaniyor };
            var hedef = new Sandik { Proje = proje, SandikNo = "2" };
            var ilk = new SandikIcerik { Sandik = kaynak, TahsisMiktari = 2, Miktar = 2, KonulanAdet = 1, EksikAdet = 1, BarkodNo = "ORTAK-BARKOD", Isim = "Birinci", StokKarsilanan = .2m, ProjeKarsilanan = .3m, TedarikciKarsilanan = .1m };
            var ikinci = new SandikIcerik { Sandik = kaynak, TahsisMiktari = 2, Miktar = 2, EksikAdet = 2, BarkodNo = "ORTAK-BARKOD", Isim = "İkinci" };
            db.SandikIcerikleri.AddRange(ilk, ikinci);
            db.Sandiklar.Add(hedef);
            await db.SaveChangesAsync();
            kurgu.ProjeId = proje.Id;
            kurgu.KaynakId = kaynak.Id;
            kurgu.HedefId = hedef.Id;
            kurgu.IlkId = ilk.Id;
            kurgu.IkinciId = ikinci.Id;
            return kurgu;
        }

        public AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_connectionString).Options);
        public SandikUrunleriTopluTasiCommand Istek(decimal ilk, decimal ikinci) => new()
        {
            ProjeId = ProjeId, KaynakSandikId = KaynakId, HedefSandikId = HedefId, IslemAnahtari = Guid.NewGuid(),
            Satirlar = [new() { KaynakSandikIcerikId = IlkId, TasinanAdet = ilk }, new() { KaynakSandikIcerikId = IkinciId, TasinanAdet = ikinci }]
        };

        public async Task<_3K.Application.Common.Result> TasiAsync(SandikUrunleriTopluTasiCommand istek, int kullaniciId = 7, OrtakSaha? saha = null)
        {
            await using var db = Db();
            using var uow = new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
            return await new SandikUrunleriTopluTasiCommandHandler(uow, new OrtakUser(kullaniciId), saha ?? new OrtakSaha()).Handle(istek, default);
        }

        public async Task BaslangicKorunurAsync()
        {
            await using var db = Db();
            var kaynak = await db.SandikIcerikleri.Where(x => x.SandikId == KaynakId).ToListAsync();
            Assert.Equal(2, kaynak.Count);
            Assert.Equal(4m, kaynak.Sum(x => x.TahsisMiktari));
            Assert.Equal(1m, kaynak.Sum(x => x.KonulanAdet));
            Assert.Equal(0, await db.SandikIcerikleri.CountAsync(x => x.SandikId == HedefId));
            Assert.Equal(0, await db.SandikUrunTransferleri.CountAsync());
            Assert.Equal(0, await db.HareketGecmisleri.CountAsync());
            Assert.Equal(0, await db.SandikTopluTasimaIslemleri.CountAsync());
        }

        public async ValueTask DisposeAsync()
        {
            if (!new NpgsqlConnectionStringBuilder(_connectionString).Database!.StartsWith("sandik_toplu_tests_", StringComparison.Ordinal))
                throw new InvalidOperationException("Test veritabanı sınırı dışında silme reddedildi.");
            await using var db = Db();
            await db.Database.EnsureDeletedAsync();
        }
    }
}

internal sealed class SandikPostgresFactAttribute : FactAttribute
{
    public SandikPostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("THREEK_TEST_POSTGRES")))
            Skip = "İzole PostgreSQL entegrasyonu için THREEK_TEST_POSTGRES gerekli; uygulama bağlantısı kullanılmaz.";
    }
}
