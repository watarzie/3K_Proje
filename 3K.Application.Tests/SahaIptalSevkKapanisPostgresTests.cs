using Microsoft.EntityFrameworkCore;
using Npgsql;
using _3K.Application.Features.ProjeIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Data.Interceptors;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

/// <summary>
/// S-27, canlı PA675-01 ve PA677-03 (11.11.2026): bütün normal sandıkları sevk edilmiş projenin tek açık ürünü sahaya
/// aktarılmış, sahada Grid İptal edilmişti (PA675-01'de ürün sahadan sahaya başka sahaya taşınmış, ilk saha silinmişti).
/// Sevk durumu sahadan yalnız konulan adedi saydığı için proje Eksik Sevk Edildi kalıyor ve Sandık Yönetimi listesinden
/// çıkmıyordu. Sahada iptal edilen aktarılmış miktar, ana projede iptal edilen ürün gibi kapanmış sayılır; saha
/// sandığının sevki beklenmez. İptal geri alınınca durum Eksik Sevk Edildi'ye döner. Liste ve kalıcı senkron aynıdır;
/// gerçekleşen saha sevki haritası (fiziksel teslim) değişmez.
/// </summary>
public sealed class SahaIptalSevkKapanisPostgresTests
{
    [SandikPostgresFact]
    [Trait("Category", "Postgres")]
    public async Task SahadaIptalEdilenTekAcikUrun_AnaProjeyiSevkEdildiYapar()
    {
        await WithDatabase(async options =>
        {
            await Kur(options,
                new Senaryo("QA-IPTAL-SEVKLI", SahaSandikSevkli: true),
                new Senaryo("QA-IPTAL-ELDE", SahaSandikSevkli: false),
                new Senaryo("QA-IPTAL-LEGACY", SahaSandikSevkli: false, Legacy: true),
                new Senaryo("QA-IPTAL-TASINMIS", SahaSandikSevkli: true, SahaProjeNo: "QA-IPTAL-BASKA-SAHA"));

            var durumlar = await DurumlariHesapla(options);

            Assert.Equal(4, durumlar.Count);
            Assert.All(durumlar, d => Assert.Equal(((int)ProjeDurum.SevkEdildi, (int)ProjeDurum.SevkEdildi), d.Value));

            // Kapanış fiziksel teslim değildir: gerçekleşen saha sevki haritası iptal satırına bir şey yazmaz.
            await using var db = new AppDbContext(options);
            var kaynakIds = await KaynakSatirIdsAsync(db);
            Assert.All(await new SahaTamamlamaService(db).GetSevkEdilenGerceklesenTamamlamaMapAsync(kaynakIds),
                x => Assert.Equal(0, x.Value));
            Assert.Equal(4, (await new SahaTamamlamaService(db).GetSahaIptalKapananMapAsync(kaynakIds)).Values.Count(v => v == 2));
        });
    }

    [SandikPostgresFact]
    [Trait("Category", "Postgres")]
    public async Task IptalYokKismiVeyaAktarimGeriAlinmis_EksikSevkKorunur()
    {
        await WithDatabase(async options =>
        {
            await Kur(options,
                new Senaryo("QA-IPTAL-YOK", SahaSandikSevkli: true, SahaGrid: GridDurum.Gelmedi),
                new Senaryo("QA-IPTAL-KISMI", SahaSandikSevkli: true, Aktarilan: 1),
                new Senaryo("QA-IPTAL-GERIALINDI", SahaSandikSevkli: false, KalemDurum: SahaAktarimDurum.GeriAlindi));

            var durumlar = await DurumlariHesapla(options);

            Assert.Equal(3, durumlar.Count);
            Assert.All(durumlar, d =>
                Assert.Equal(((int)ProjeDurum.EksikSevkEdildi, (int)ProjeDurum.EksikSevkEdildi), d.Value));
        });
    }

    [SandikPostgresFact]
    [Trait("Category", "Postgres")]
    public async Task SahadakiIptalGeriAlininca_AnaProjeEksikSevkEdildiyeDoner()
    {
        await WithDatabase(async options =>
        {
            await Kur(options, new Senaryo("QA-IPTAL-GERI", SahaSandikSevkli: false));
            // Gerçek proje kilidiyle: Sevk Edildi olan ana proje kilitlidir; saha satırı saha projesine aittir ve
            // değişebilir, senkron da ana projeyi kilitten çıkararak Eksik Sevk Edildi'ye döndürebilir.
            var kilitli = new DbContextOptionsBuilder<AppDbContext>(options).AddInterceptors(new ProjectLockInterceptor()).Options;
            Assert.Equal(((int)ProjeDurum.SevkEdildi, (int)ProjeDurum.SevkEdildi), (await DurumlariHesapla(kilitli))["QA-IPTAL-GERI"]);

            await using (var db = new AppDbContext(kilitli))
            {
                // Kilitli ana projenin satırı değiştirilemez (kilit gerçekten devrede).
                var kok = await db.CekiSatirlari.SingleAsync(cs => cs.BarkodNo == "AKS-CONTA" && cs.KaynakCekiSatiriId == null);
                kok.Aciklama = "değişmemeli";
                await Assert.ThrowsAsync<ProjectLockedException>(() => db.SaveChangesAsync());
            }

            await using (var db = new AppDbContext(kilitli))
            {
                var sahaSatir = await db.CekiSatirlari.SingleAsync(cs => cs.KaynakCekiSatiriId != null);
                sahaSatir.GridDurumuId = (int)GridDurum.Gelmedi;
                await db.SaveChangesAsync();
            }

            Assert.Equal(((int)ProjeDurum.EksikSevkEdildi, (int)ProjeDurum.EksikSevkEdildi),
                (await DurumlariHesapla(kilitli))["QA-IPTAL-GERI"]);
        });
    }

    /// <summary>
    /// Veri onarım betiği (README 34) yalnız yeni kuralın Sevk Edildi yapacağı projeleri 6 → 5 çeker; seçimi C# senkronuyla
    /// aynıdır. Durum sorgusu öncesinde UYGULANMAMIŞ, sonrasında uygulanmış gösterir; tekrar çalıştırma hiçbir şey değiştirmez.
    /// </summary>
    [SandikPostgresFact]
    [Trait("Category", "Postgres")]
    public async Task VeriOnarimBetigi_KuralinSectigiProjeleriSevkEdildiYapar_TekrarindaDegistirmez()
    {
        await WithDatabase(async options =>
        {
            await Kur(options,
                new Senaryo("QA-IPTAL-SEVKLI", SahaSandikSevkli: true),
                new Senaryo("QA-IPTAL-ELDE", SahaSandikSevkli: false),
                new Senaryo("QA-IPTAL-LEGACY", SahaSandikSevkli: false, Legacy: true),
                new Senaryo("QA-IPTAL-TASINMIS", SahaSandikSevkli: true, SahaProjeNo: "QA-IPTAL-BASKA-SAHA"),
                new Senaryo("QA-IPTAL-YOK", SahaSandikSevkli: true, SahaGrid: GridDurum.Gelmedi),
                new Senaryo("QA-IPTAL-KISMI", SahaSandikSevkli: true, Aktarilan: 1),
                new Senaryo("QA-IPTAL-GERIALINDI", SahaSandikSevkli: false, KalemDurum: SahaAktarimDurum.GeriAlindi));

            await using var db = new AppDbContext(options);
            await db.Database.OpenConnectionAsync();
            Assert.Equal("UYGULANMAMIŞ", await BetikDurumuAsync(db));
            var betikOncesi = await db.Projeler.AsNoTracking()
                .Where(p => p.ProjeTipiId == (int)ProjeTipi.Normal)
                .ToDictionaryAsync(p => p.ProjeNo, p => (p.DurumId, p.UpdatedBy, p.UpdatedDate));

            await BetigiCalistirAsync(db, YedekBetigi);
            await BetigiCalistirAsync(db);

            var betikSonrasi = await db.Projeler.AsNoTracking()
                .Where(p => p.ProjeTipiId == (int)ProjeTipi.Normal)
                .ToDictionaryAsync(p => p.ProjeNo, p => (p.DurumId, p.UpdatedBy, p.UpdatedDate));
            Assert.Equal("uygulanmış", await BetikDurumuAsync(db));
            var degisenler = betikSonrasi.Where(x => x.Value.DurumId == (int)ProjeDurum.SevkEdildi).Select(x => x.Key).Order();
            Assert.Equal(["QA-IPTAL-ELDE", "QA-IPTAL-LEGACY", "QA-IPTAL-SEVKLI", "QA-IPTAL-TASINMIS"], degisenler);
            Assert.All(betikSonrasi, x => Assert.Equal(
                x.Value.DurumId == (int)ProjeDurum.SevkEdildi ? BetikAdi : null, x.Value.UpdatedBy));

            // Tekrar: onarılacak proje kalmadığı için hiçbir kayıt değişmez.
            await BetigiCalistirAsync(db);
            Assert.Equal(betikSonrasi, await db.Projeler.AsNoTracking()
                .Where(p => p.ProjeTipiId == (int)ProjeTipi.Normal)
                .ToDictionaryAsync(p => p.ProjeNo, p => (p.DurumId, p.UpdatedBy, p.UpdatedDate)));

            // Yedek onarımdan sonra tekrar çalıştırılsa da ilk (onarım öncesi) değer korunur; geri alma her şeyi
            // onarım öncesine döndürür, tekrarında bir şey değiştirmez.
            await BetigiCalistirAsync(db, YedekBetigi);
            await BetigiCalistirAsync(db, GeriAlmaBetigi);
            var normalProjeler = () => db.Projeler.AsNoTracking()
                .Where(p => p.ProjeTipiId == (int)ProjeTipi.Normal)
                .ToDictionaryAsync(p => p.ProjeNo, p => (p.DurumId, p.UpdatedBy, p.UpdatedDate));
            Assert.Equal(betikOncesi, await normalProjeler());
            await BetigiCalistirAsync(db, GeriAlmaBetigi);
            Assert.Equal(betikOncesi, await normalProjeler());
            Assert.Equal("UYGULANMAMIŞ", await BetikDurumuAsync(db));

            // Onarım yeniden uygulanınca aynı sonuç; betiğin sonucu yeni API'nin senkron ve liste hesabıyla aynıdır.
            await BetigiCalistirAsync(db);
            Assert.Equal(betikSonrasi.ToDictionary(x => x.Key, x => x.Value.DurumId),
                (await normalProjeler()).ToDictionary(x => x.Key, x => x.Value.DurumId));
            var api = await DurumlariHesapla(options);
            Assert.All(api, x => Assert.Equal((betikSonrasi[x.Key].DurumId, betikSonrasi[x.Key].DurumId), x.Value));
        });
    }

    [SandikPostgresFact]
    [Trait("Category", "Postgres")]
    public async Task GeriAlmaBetigi_YedekYoksaHataVerir_HicbirKayitDegismez()
    {
        await WithDatabase(async options =>
        {
            await Kur(options, new Senaryo("QA-IPTAL-YEDEKSIZ", SahaSandikSevkli: true));
            await using var db = new AppDbContext(options);
            await db.Database.OpenConnectionAsync();
            await BetigiCalistirAsync(db);

            var hata = await Assert.ThrowsAsync<Xunit.Sdk.XunitException>(() => BetigiCalistirAsync(db, GeriAlmaBetigi));
            Assert.Contains("Yedek tablosu", hata.Message);
            Assert.Equal((int)ProjeDurum.SevkEdildi,
                (await db.Projeler.AsNoTracking().SingleAsync(p => p.ProjeNo == "QA-IPTAL-YEDEKSIZ")).DurumId);
        });
    }

    private const string BetikAdi = "20261112_01_Saha_Iptal_Proje_Sevk_Durumu.sql";
    private const string YedekBetigi = "geri-alma/20261112_01_Yedek.sql";
    private const string GeriAlmaBetigi = "geri-alma/20261112_01_Geri_Al.sql";

    private static async Task BetigiCalistirAsync(AppDbContext db, string ad = BetikAdi)
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

    /// <summary>Durum sorgusunun (kontrol/Betik_Durumu.sql) bu betik için verdiği sonuç.</summary>
    private static async Task<string> BetikDurumuAsync(AppDbContext db)
    {
        await using var komut = db.Database.GetDbConnection().CreateCommand();
        komut.CommandText = await File.ReadAllTextAsync(BetikYolu(Path.Combine("kontrol", "Betik_Durumu.sql")));
        await using var okuyucu = await komut.ExecuteReaderAsync();
        while (await okuyucu.ReadAsync())
            if (okuyucu.GetString(1) + ".sql" == BetikAdi)
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

    private sealed record Senaryo(
        string ProjeNo,
        bool SahaSandikSevkli,
        bool Legacy = false,
        GridDurum SahaGrid = GridDurum.Iptal,
        decimal Aktarilan = 2,
        SahaAktarimDurum? KalemDurum = null,
        string? SahaProjeNo = null);

    /// <summary>
    /// Her senaryo ayrı bir normal proje: iki sandığı da sevk edilmiş, Eksik Sevk Edildi kayıtlı; biri tam gelen, diğeri
    /// 3K'ya hiç gelmeyip sahaya aktarılan 2 adetlik satır. Saha satırı sandığa konmamıştır.
    /// </summary>
    private static async Task Kur(DbContextOptions<AppDbContext> options, params Senaryo[] senaryolar)
    {
        await using var seed = new AppDbContext(options);
        foreach (var s in senaryolar)
        {
            var proje = new Proje { ProjeNo = s.ProjeNo, Musteri = "Sentetik", ProjeTipiId = (int)ProjeTipi.Normal, DurumId = (int)ProjeDurum.EksikSevkEdildi };
            var saha = new Proje { ProjeNo = s.SahaProjeNo ?? s.ProjeNo + "-SAHA", Musteri = "Sentetik", ProjeTipiId = (int)ProjeTipi.Saha, DurumId = (int)ProjeDurum.Hazirlaniyor };
            var ceki = new Ceki { Proje = proje, OrijinalDosyaYolu = "sentetik.xlsx", YuklemeTarihi = new DateTime(2026, 9, 1, 9, 0, 0) };
            var sahaCeki = new Ceki { Proje = saha, OrijinalDosyaYolu = "saha.xlsx", YuklemeTarihi = new DateTime(2026, 10, 20, 9, 0, 0) };
            proje.Cekiler.Add(ceki);
            saha.Cekiler.Add(sahaCeki);
            var sandik1 = new Sandik { Proje = proje, SandikNo = "1", Ad = "Transformatör", DurumId = (int)SandikDurum.Sevkedildi };
            var sandik19 = new Sandik { Proje = proje, SandikNo = "19", Ad = "Aksesuar", DurumId = (int)SandikDurum.Sevkedildi };
            var sahaSandik = new Sandik
            {
                Proje = saha, SandikNo = "1", Ad = "Saha",
                DurumId = s.SahaSandikSevkli ? (int)SandikDurum.Sevkedildi : (int)SandikDurum.Hazirlaniyor
            };
            proje.Sandiklar.Add(sandik1);
            proje.Sandiklar.Add(sandik19);
            saha.Sandiklar.Add(sahaSandik);

            static void Icerik(CekiSatiri satir, Sandik sandik, decimal tahsis, decimal konulan)
            {
                var icerik = new SandikIcerik { Sandik = sandik, CekiSatiri = satir, TahsisMiktari = tahsis, KonulanAdet = konulan, EksikAdet = tahsis - konulan };
                satir.SandikIcerikleri.Add(icerik);
                sandik.SandikIcerikleri.Add(icerik);
            }

            var tamGelen = new CekiSatiri
            {
                Ceki = ceki, SiraNo = 1, BarkodNo = "TRF-TAM", Aciklama = "Transformatör", CekideGecenSandikNo = "1",
                IstenenAdet = 1, GelenMiktar = 1, GridDurumuId = (int)GridDurum.TamGeldi
            };
            Icerik(tamGelen, sandik1, 1, 1);
            ceki.CekiSatirlari.Add(tamGelen);

            var kaynak = new CekiSatiri
            {
                Ceki = ceki, SiraNo = 2, BarkodNo = "AKS-CONTA", Aciklama = "Conta takımı", CekideGecenSandikNo = "19",
                IstenenAdet = 2, GridDurumuId = (int)GridDurum.Gelmedi
            };
            Icerik(kaynak, sandik19, 2, 0);
            ceki.CekiSatirlari.Add(kaynak);

            var sahaSatir = new CekiSatiri
            {
                Ceki = sahaCeki, SiraNo = 1, BarkodNo = "AKS-CONTA", Aciklama = "Saha satırı", CekideGecenSandikNo = "1",
                IstenenAdet = s.Aktarilan, GridDurumuId = (int)s.SahaGrid, KaynakCekiSatiri = kaynak
            };
            Icerik(sahaSatir, sahaSandik, s.Aktarilan, 0);
            sahaCeki.CekiSatirlari.Add(sahaSatir);

            if (!s.Legacy)
            {
                var aktarim = new SahaAktarim { SahaProje = saha, KaynakProje = proje, Tarih = new DateTime(2026, 10, 20) };
                aktarim.Kalemler.Add(new SahaAktarimKalemi
                {
                    SahaAktarim = aktarim, KaynakProje = proje, SahaProje = saha, KaynakCekiSatiri = kaynak,
                    SahaCekiSatiri = sahaSatir, SahaSandik = sahaSandik, Miktar = s.Aktarilan,
                    DurumId = (int)(s.KalemDurum ?? (s.SahaSandikSevkli ? SahaAktarimDurum.SevkEdildi : SahaAktarimDurum.Hazirlaniyor)),
                    SevkiyatKapsamindaMi = s.SahaSandikSevkli,
                    SevkTarihi = s.SahaSandikSevkli ? new DateTime(2026, 10, 21, 10, 28, 0) : null
                });
                seed.Add(aktarim);
            }

            seed.Projeler.AddRange(proje, saha);
        }

        await seed.SaveChangesAsync();
    }

    private static Task<List<int>> KaynakSatirIdsAsync(AppDbContext db) => db.CekiSatirlari
        .Where(cs => cs.Ceki.Proje.ProjeTipiId == (int)ProjeTipi.Normal)
        .Select(cs => cs.Id)
        .ToListAsync();

    /// <summary>Proje no → (liste durumu, senkronizasyonun kalıcı yazdığı durum).</summary>
    private static async Task<Dictionary<string, (int Liste, int Kalici)>> DurumlariHesapla(DbContextOptions<AppDbContext> options)
    {
        await using (var db = new AppDbContext(options))
            await new SahaTamamlamaService(db).SenkronizeKaynakProjelerAsync(await KaynakSatirIdsAsync(db));

        await using var okuma = new AppDbContext(options);
        var kalici = await okuma.Projeler
            .Where(p => p.ProjeTipiId == (int)ProjeTipi.Normal)
            .ToDictionaryAsync(p => p.ProjeNo, p => p.DurumId);
        var liste = await new ProjeListeleQueryHandler(
                new ProjeRepository(okuma), new SahaProjeTamamlanmaRegresyonTests.LookupStub(), new SahaTamamlamaService(okuma),
                OrtakRoller.TumIzinler(), new OrtakUser())
            .Handle(new ProjeListeleQuery { ProjeTipiId = (int)ProjeTipi.Normal, PageSize = 100 }, CancellationToken.None);

        Assert.True(liste.IsSuccess, liste.Error?.Message);
        return liste.Value!.Items.ToDictionary(p => p.ProjeNo, p => (p.DurumId, kalici[p.ProjeNo]));
    }

    private static async Task WithDatabase(Func<DbContextOptions<AppDbContext>, Task> test)
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("THREEK_TEST_POSTGRES"));
        Assert.True(connection.Host is "127.0.0.1" or "localhost", "Yalnız yerel izole test sunucusu kullanılabilir.");
        Assert.Equal(55439, connection.Port);
        Assert.Equal("postgres", connection.Database);
        var database = $"saha_iptal_sevk_tests_{Guid.NewGuid():N}";
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
            Assert.Matches("^saha_iptal_sevk_tests_[a-f0-9]{32}$", database);
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
