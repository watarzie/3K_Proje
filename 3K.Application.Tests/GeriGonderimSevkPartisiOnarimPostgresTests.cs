using Microsoft.EntityFrameworkCore;
using Npgsql;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Infrastructure.Data;

namespace _3K.Application.Tests;

/// <summary>
/// Canlı PA558-01 sandık 14 satır 58 (19.11.2026): 15.09'daki geri gönderim aktif Grid sevk partisi sayaçlarını boş
/// bıraktı; uygulama kaydı belirsiz eski parti sayıp Grid yeniden sevkini kilitliyordu (G-037, U-244). 36 numaralı onarım
/// betiği (<c>uygulandi/2026-11/20261119_01_Geri_Gonderim_Sevk_Partisi_Sayaci.sql</c>) yalnız canlı Id 34371'i, hâlâ aynı
/// durumdaysa, bugünkü geri gönderimin yazdığı sayaçlara getirir. Sonrasında C# kuralı yeniden sevki açar; aynı desendeki
/// başka satır değişmez; tekrar çalıştırma hiçbir şey değiştirmez; geri alma yedeğe döndürür. Handler akışı
/// <see cref="GridUcKParcaliSevkPartisiRegresyonTests"/> içindedir.
/// </summary>
public sealed class GeriGonderimSevkPartisiOnarimPostgresTests
{
    private const int CanliSatirId = 34371;
    private const string Betik = "uygulandi/2026-11/20261119_01_Geri_Gonderim_Sevk_Partisi_Sayaci.sql";
    private const string Yedek = "geri-alma/20261119_01_Yedek.sql";
    private const string GeriAlma = "geri-alma/20261119_01_Geri_Al.sql";

    [SandikPostgresFact]
    [Trait("Category", "Postgres")]
    public async Task OnarimBetigi_YalnizCanliSatiriOnarir_YenidenSevkAcilir_TekrarVeGeriAlmaTutarli()
    {
        await WithDatabase(async options =>
        {
            var baskaSatirId = await Kur(options);

            await using var db = new AppDbContext(options);
            await db.Database.OpenConnectionAsync();
            Assert.Equal("UYGULANMAMIŞ", await BetikDurumuAsync(db));
            var oncesi = await SayaclarAsync(db);

            await BetigiCalistirAsync(db, Yedek);
            await BetigiCalistirAsync(db, Betik);

            Assert.Equal("uygulanmış", await BetikDurumuAsync(db));
            var sonrasi = await SayaclarAsync(db);
            Assert.Equal((0m, (bool?)true, Path.GetFileName(Betik)), (sonrasi[CanliSatirId].Satir, sonrasi[CanliSatirId].Bayrak, sonrasi[CanliSatirId].UpdatedBy));
            Assert.Equal(new decimal?[] { 0m, 0m }, sonrasi[CanliSatirId].Icerikler);
            // Aynı desendeki başka satır (canlıda PA599-01 satır 109) bilerek dışarıda kalır.
            Assert.Equal(oncesi[baskaSatirId], sonrasi[baskaSatirId]);

            var onarilan = await db.CekiSatirlari.AsNoTracking().SingleAsync(cs => cs.Id == CanliSatirId);
            // Miktar ve durumlar değişmez; yalnız sayaçlar yazılır.
            Assert.Equal(((int)UcKDurum.GeriGonderildi, (int)GridSevkDurum.YenidenSevkGerekli, 1m, 0m, 1m, 1m),
                (onarilan.UcKDurumuId, onarilan.GridSevkDurumuId, onarilan.GridSevkMiktari!.Value, onarilan.GelenMiktar,
                 onarilan.GeriGonderilenMiktar, onarilan.YenidenSevkGerekliAdet));
            var karar = GridUcKSevkPartisiKurali.DevamSevkiniDegerlendir(onarilan);
            Assert.Equal((true, GridUcKDevamSevkTipi.YenidenSevk, 1m), (karar.YeniPartiMi, karar.Tip, karar.UstSinir));
            var baska = await db.CekiSatirlari.AsNoTracking().SingleAsync(cs => cs.Id == baskaSatirId);
            Assert.True(GridUcKSevkPartisiKurali.LegacyAktifPartiBelirsizMi(baska));

            // Tekrar çalıştırma hiçbir şey değiştirmez (UpdatedDate dahil).
            var tarih = await GuncellemeTarihiAsync(db);
            await BetigiCalistirAsync(db, Yedek);
            await BetigiCalistirAsync(db, Betik);
            Assert.Equal(sonrasi, await SayaclarAsync(db));
            Assert.Equal(tarih, await GuncellemeTarihiAsync(db));

            // Geri alma satırı ve içeriklerini birlikte yedeğe döndürür; durum sorgusu yeniden UYGULANMAMIŞ gösterir.
            await BetigiCalistirAsync(db, GeriAlma);
            Assert.Equal(oncesi, await SayaclarAsync(db));
            Assert.Equal("UYGULANMAMIŞ", await BetikDurumuAsync(db));

            await BetigiCalistirAsync(db, Betik);
            Assert.Equal(sonrasi, await SayaclarAsync(db));
        });
    }

    [SandikPostgresFact]
    [Trait("Category", "Postgres")]
    public async Task OnarimBetigi_SatirDurumuDegismisseDokunmaz()
    {
        await WithDatabase(async options =>
        {
            await Kur(options);
            await using var db = new AppDbContext(options);
            await db.Database.OpenConnectionAsync();
            // Grid bu arada satırı başka bir yoldan ilerletmiş olsun (ör. sevk durumu değişmiş): tahmin yürütülmez.
            await db.Database.ExecuteSqlAsync($"""UPDATE "CekiSatirlari" SET "GridSevkDurumuId" = 1 WHERE "Id" = {CanliSatirId}""");
            var oncesi = await SayaclarAsync(db);

            await BetigiCalistirAsync(db, Betik);

            Assert.Equal(oncesi, await SayaclarAsync(db));
            Assert.Equal("uygulanmış", await BetikDurumuAsync(db));

            // Ürünün sandığı sevk edilmişse de dokunulmaz (canlıda PA599-01 satır 109 gibi; ayrı karar).
            await db.Database.ExecuteSqlAsync($"""UPDATE "CekiSatirlari" SET "GridSevkDurumuId" = 4 WHERE "Id" = {CanliSatirId}""");
            await db.Database.ExecuteSqlRawAsync("""UPDATE "Sandiklar" SET "DurumId" = 4 WHERE "SandikNo" = '15'""");
            Assert.Equal("uygulanmış", await BetikDurumuAsync(db));
            await BetigiCalistirAsync(db, Betik);
            Assert.Equal(oncesi, await SayaclarAsync(db));
        });
    }

    /// <summary>
    /// Canlı satırın sentetik eşi (Id 34371, iki sandık içeriği) ve aynı desende başka bir satır: 2 istenir, Grid'e 2
    /// gelir, aktif parti 1, 1 adet geri gönderilmiş, sayaçlar boş. Başka satırın kimliğini döndürür.
    /// </summary>
    private static async Task<int> Kur(DbContextOptions<AppDbContext> options)
    {
        await using var seed = new AppDbContext(options);
        var proje = new Proje { ProjeNo = "QA-GERI-GONDERIM", Musteri = "Sentetik", ProjeTipiId = (int)ProjeTipi.Normal, DurumId = (int)ProjeDurum.Hazirlaniyor };
        var ceki = new Ceki { Proje = proje, OrijinalDosyaYolu = "sentetik.xlsx", YuklemeTarihi = new DateTime(2026, 8, 1, 9, 0, 0) };
        proje.Cekiler.Add(ceki);
        var sandik14 = new Sandik { Proje = proje, SandikNo = "14", Ad = "Aksesuar", DurumId = (int)SandikDurum.Hazirlaniyor };
        var sandik15 = new Sandik { Proje = proje, SandikNo = "15", Ad = "Aksesuar", DurumId = (int)SandikDurum.Hazirlaniyor };
        proje.Sandiklar.Add(sandik14);
        proje.Sandiklar.Add(sandik15);

        CekiSatiri Satir(int? id, int siraNo, params Sandik[] sandiklar)
        {
            var satir = new CekiSatiri
            {
                Ceki = ceki, SiraNo = siraNo, BarkodNo = $"VENTIL-{siraNo}", Aciklama = "Basınç ventili borusu",
                CekideGecenSandikNo = "14", IstenenAdet = 2,
                GridDurumuId = (int)GridDurum.TamGeldi, GridGelenAdet = 2, GridSevkMiktari = 1,
                GridSevkDurumuId = (int)GridSevkDurum.YenidenSevkGerekli, YenidenSevkGerekliAdet = 1,
                UcKDurumuId = (int)UcKDurum.GeriGonderildi, UcKKarsilamaTipiId = (int)UcKDurum.GeriGonderildi,
                GeriGonderilenMiktar = 1, GelenMiktar = 0,
                UpdatedBy = "uygulama", UpdatedDate = new DateTime(2026, 9, 15, 9, 48, 48)
            };
            if (id.HasValue)
                satir.Id = id.Value;
            foreach (var sandik in sandiklar)
            {
                var icerik = new SandikIcerik { Sandik = sandik, CekiSatiri = satir, TahsisMiktari = 0, KonulanAdet = 0 };
                satir.SandikIcerikleri.Add(icerik);
                sandik.SandikIcerikleri.Add(icerik);
            }

            ceki.CekiSatirlari.Add(satir);
            return satir;
        }

        Satir(CanliSatirId, 58, sandik14, sandik15);
        var baska = Satir(null, 109, sandik14);
        seed.Projeler.Add(proje);
        await seed.SaveChangesAsync();
        return baska.Id;
    }

    private sealed record Sayac(decimal? Satir, bool? Bayrak, string? UpdatedBy, List<decimal?> Icerikler)
    {
        public bool Equals(Sayac? diger) => diger is not null && (Satir, Bayrak, UpdatedBy) == (diger.Satir, diger.Bayrak, diger.UpdatedBy)
            && Icerikler.SequenceEqual(diger.Icerikler);

        public override int GetHashCode() => HashCode.Combine(Satir, Bayrak, UpdatedBy, Icerikler.Count);
    }

    private static async Task<Dictionary<int, Sayac>> SayaclarAsync(AppDbContext db)
    {
        var satirlar = await db.CekiSatirlari.AsNoTracking()
            .Select(cs => new
            {
                cs.Id, cs.AktifGridSevkKarsilananMiktari, cs.AktifGridSevkPartisiErkenSonuclandirildiMi, cs.UpdatedBy,
                Icerikler = cs.SandikIcerikleri.OrderBy(i => i.Id).Select(i => i.AktifGridSevkKarsilananMiktari).ToList()
            })
            .ToListAsync();
        return satirlar.ToDictionary(s => s.Id,
            s => new Sayac(s.AktifGridSevkKarsilananMiktari, s.AktifGridSevkPartisiErkenSonuclandirildiMi, s.UpdatedBy, s.Icerikler));
    }

    private static Task<List<DateTime?>> GuncellemeTarihiAsync(AppDbContext db) =>
        db.SandikIcerikleri.AsNoTracking().OrderBy(i => i.Id).Select(i => i.UpdatedDate)
            .Concat(db.CekiSatirlari.AsNoTracking().OrderBy(cs => cs.Id).Select(cs => cs.UpdatedDate))
            .ToListAsync();

    private static async Task BetigiCalistirAsync(AppDbContext db, string ad)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(BetikYolu(ad)));
        }
        catch (PostgresException hata)
        {
            throw new Xunit.Sdk.XunitException($"{ad}: {hata.MessageText}");
        }
    }

    /// <summary>Durum sorgusunun (kontrol/Betik_Durumu.sql) 36 numaralı betik için verdiği sonuç.</summary>
    private static async Task<string> BetikDurumuAsync(AppDbContext db)
    {
        await using var komut = db.Database.GetDbConnection().CreateCommand();
        komut.CommandText = await File.ReadAllTextAsync(BetikYolu(Path.Combine("kontrol", "Betik_Durumu.sql")));
        await using var okuyucu = await komut.ExecuteReaderAsync();
        while (await okuyucu.ReadAsync())
            if (okuyucu.GetString(1) + ".sql" == Path.GetFileName(Betik))
                return okuyucu.GetString(2);
        throw new Xunit.Sdk.XunitException("Durum sorgusunda betik satırı yok.");
    }

    private static string BetikYolu(string ad)
    {
        var dizin = new DirectoryInfo(AppContext.BaseDirectory);
        while (dizin != null && !File.Exists(Path.Combine(dizin.FullName, "scripts", "database", ad)))
            dizin = dizin.Parent;
        Assert.NotNull(dizin);
        return Path.Combine(dizin.FullName, "scripts", "database", ad);
    }

    private static async Task WithDatabase(Func<DbContextOptions<AppDbContext>, Task> test)
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("THREEK_TEST_POSTGRES"));
        Assert.True(connection.Host is "127.0.0.1" or "localhost", "Yalnız yerel izole test sunucusu kullanılabilir.");
        Assert.Equal(55439, connection.Port);
        Assert.Equal("postgres", connection.Database);
        var database = $"geri_gonderim_onarim_tests_{Guid.NewGuid():N}";
        await using var admin = new NpgsqlConnection(connection.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin)) await create.ExecuteNonQueryAsync();
        try
        {
            connection.Database = database;
            var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection.ConnectionString).Options;
            await using (var ctx = new AppDbContext(options))
                await ctx.Database.EnsureCreatedAsync();
            await test(options);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            // Yalnız bu testin oluşturduğu, sabit önek + GUID isimli sentetik veritabanı kaldırılır.
            Assert.Matches("^geri_gonderim_onarim_tests_[a-f0-9]{32}$", database);
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
