using Microsoft.EntityFrameworkCore;
using Npgsql;
using _3K.Application.Features.ProjeIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

/// <summary>
/// Canlı PA600-20: sahaya aktarılan ürün sahada sandığa konmadan Trafo Sevk ile kapandı, saha sandığı sevk edildi.
/// Bütün normal sandıklar sevkliyken ana proje "Kısmi Sevk" kalıyordu; sevk kapsamındaki saha tamamlaması yalnız
/// konulan adedi sayıyordu. Trafoyla giden adet, saha satırının sevkli sandığıyla gitmiş sayılır (yeni defter ve legacy);
/// trafo adedi eksikse veya saha sandığı eldeyse Kısmi Sevk korunur. Liste ve kalıcı senkronizasyon aynı sonucu verir.
/// </summary>
public sealed class SahaTrafoSevkTamamlamaPostgresTests
{
    [SandikPostgresFact]
    [Trait("Category", "Postgres")]
    public async Task SahadaTrafoylaGiden_SevkKapsamindaTamamlanir_ProjeSevkEdildi()
    {
        await WithDatabase(async options =>
        {
            await Kur(options,
                new Senaryo("QA-TRAFO-YENI", Legacy: false, SahaKonulan: 0, SahaTrafo: 2, SahaSandikSevkli: true),
                new Senaryo("QA-TRAFO-LEGACY", Legacy: true, SahaKonulan: 0, SahaTrafo: 2, SahaSandikSevkli: true),
                new Senaryo("QA-TRAFO-KARMA", Legacy: false, SahaKonulan: 1, SahaTrafo: 1, SahaSandikSevkli: true));

            var durumlar = await DurumlariHesapla(options);

            Assert.Equal(3, durumlar.Count);
            Assert.All(durumlar, d => Assert.Equal(((int)ProjeDurum.SevkEdildi, (int)ProjeDurum.SevkEdildi), d.Value));
        });
    }

    [SandikPostgresFact]
    [Trait("Category", "Postgres")]
    public async Task TrafoEksikVeyaSahaSandigiElde_KismiSevkKorunur()
    {
        await WithDatabase(async options =>
        {
            await Kur(options,
                new Senaryo("QA-TRAFO-EKSIK", Legacy: false, SahaKonulan: 0, SahaTrafo: 1, SahaSandikSevkli: true),
                new Senaryo("QA-TRAFO-YOK", Legacy: false, SahaKonulan: 0, SahaTrafo: 0, SahaSandikSevkli: true),
                new Senaryo("QA-TRAFO-ELDE", Legacy: false, SahaKonulan: 0, SahaTrafo: 2, SahaSandikSevkli: false),
                new Senaryo("QA-TRAFO-ELDE-LEGACY", Legacy: true, SahaKonulan: 0, SahaTrafo: 2, SahaSandikSevkli: false));

            var durumlar = await DurumlariHesapla(options);

            Assert.Equal(4, durumlar.Count);
            Assert.All(durumlar, d =>
                Assert.Equal(((int)ProjeDurum.EksikSevkEdildi, (int)ProjeDurum.EksikSevkEdildi), d.Value));
        });
    }

    private sealed record Senaryo(string ProjeNo, bool Legacy, decimal SahaKonulan, decimal SahaTrafo, bool SahaSandikSevkli);

    /// <summary>
    /// Her senaryo ayrı bir normal proje: iki sandığı da sevk edilmiş; biri tam gelen, diğeri 3K'ya hiç gelmeyip
    /// 2 adet sahaya aktarılan satır.
    /// </summary>
    private static async Task Kur(DbContextOptions<AppDbContext> options, params Senaryo[] senaryolar)
    {
        await using var seed = new AppDbContext(options);
        foreach (var s in senaryolar)
        {
            var proje = new Proje { ProjeNo = s.ProjeNo, Musteri = "Sentetik Trafo", ProjeTipiId = (int)ProjeTipi.Normal, DurumId = (int)ProjeDurum.Hazirlaniyor };
            var saha = new Proje { ProjeNo = s.ProjeNo + "-SAHA", Musteri = "Sentetik Trafo", ProjeTipiId = (int)ProjeTipi.Saha, DurumId = (int)ProjeDurum.Hazirlaniyor };
            var ceki = new Ceki { Proje = proje, OrijinalDosyaYolu = "sentetik.xlsx", YuklemeTarihi = new DateTime(2026, 9, 1, 9, 0, 0) };
            var sahaCeki = new Ceki { Proje = saha, OrijinalDosyaYolu = "saha.xlsx", YuklemeTarihi = new DateTime(2026, 10, 20, 9, 0, 0) };
            proje.Cekiler.Add(ceki);
            saha.Cekiler.Add(sahaCeki);
            var sandik1 = new Sandik { Proje = proje, SandikNo = "1", Ad = "Transformatör", DurumId = (int)SandikDurum.Sevkedildi };
            var sandik20 = new Sandik { Proje = proje, SandikNo = "20", Ad = "Aksesuar", DurumId = (int)SandikDurum.Sevkedildi };
            var sahaSandik = new Sandik
            {
                Proje = saha, SandikNo = "1", Ad = "Saha",
                DurumId = s.SahaSandikSevkli ? (int)SandikDurum.Sevkedildi : (int)SandikDurum.Kapandi
            };
            proje.Sandiklar.Add(sandik1);
            proje.Sandiklar.Add(sandik20);
            saha.Sandiklar.Add(sahaSandik);

            static void Icerik(CekiSatiri satir, Sandik sandik, decimal tahsis, decimal konulan)
            {
                var icerik = new SandikIcerik { Sandik = sandik, CekiSatiri = satir, TahsisMiktari = tahsis, KonulanAdet = konulan, EksikAdet = tahsis - konulan };
                satir.SandikIcerikleri.Add(icerik);
                sandik.SandikIcerikleri.Add(icerik);
            }

            var tamGelen = new CekiSatiri
            {
                Ceki = ceki, SiraNo = 1, BarkodNo = "FCT-TAM", Aciklama = "Transformatör", CekideGecenSandikNo = "1",
                IstenenAdet = 1, GelenMiktar = 1, GridDurumuId = (int)GridDurum.TamGeldi
            };
            Icerik(tamGelen, sandik1, 1, 1);
            ceki.CekiSatirlari.Add(tamGelen);

            var kaynak = new CekiSatiri
            {
                Ceki = ceki, SiraNo = 2, BarkodNo = "SMC-DIRSEK", Aciklama = "Şalter dirsek boru", CekideGecenSandikNo = "20",
                IstenenAdet = 2, GridDurumuId = (int)GridDurum.Gelmedi
            };
            Icerik(kaynak, sandik20, 2, 0);
            ceki.CekiSatirlari.Add(kaynak);

            var sahaSatir = new CekiSatiri
            {
                Ceki = sahaCeki, SiraNo = 1, BarkodNo = "SMC-DIRSEK", Aciklama = "Saha satırı", CekideGecenSandikNo = "1",
                IstenenAdet = 2, GelenMiktar = s.SahaKonulan, TrafoSevkAdet = s.SahaTrafo,
                GridDurumuId = s.SahaTrafo > 0 ? (int)GridDurum.TrafoSevk : (int)GridDurum.Gelmedi,
                KaynakCekiSatiri = kaynak
            };
            Icerik(sahaSatir, sahaSandik, 2, s.SahaKonulan);
            sahaCeki.CekiSatirlari.Add(sahaSatir);

            if (!s.Legacy)
            {
                var aktarim = new SahaAktarim { SahaProje = saha, KaynakProje = proje, Tarih = new DateTime(2026, 10, 20) };
                aktarim.Kalemler.Add(new SahaAktarimKalemi
                {
                    SahaAktarim = aktarim, KaynakProje = proje, SahaProje = saha, KaynakCekiSatiri = kaynak,
                    SahaCekiSatiri = sahaSatir, SahaSandik = sahaSandik, Miktar = 2,
                    DurumId = s.SahaSandikSevkli ? (int)SahaAktarimDurum.SevkEdildi : (int)SahaAktarimDurum.Hazirlaniyor,
                    SevkiyatKapsamindaMi = s.SahaSandikSevkli,
                    SevkTarihi = s.SahaSandikSevkli ? new DateTime(2026, 10, 21, 10, 28, 0) : null
                });
                seed.Add(aktarim);
            }

            seed.Projeler.AddRange(proje, saha);
        }

        await seed.SaveChangesAsync();
    }

    /// <summary>Proje no → (liste durumu, senkronizasyonun kalıcı yazdığı durum).</summary>
    private static async Task<Dictionary<string, (int Liste, int Kalici)>> DurumlariHesapla(DbContextOptions<AppDbContext> options)
    {
        await using (var db = new AppDbContext(options))
        {
            var kaynakSatirIds = await db.CekiSatirlari
                .Where(cs => cs.Ceki.Proje.ProjeTipiId == (int)ProjeTipi.Normal)
                .Select(cs => cs.Id)
                .ToListAsync();
            await new SahaTamamlamaService(db).SenkronizeKaynakProjelerAsync(kaynakSatirIds);
        }

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
        var database = $"saha_trafo_sevk_tests_{Guid.NewGuid():N}";
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
            Assert.Matches("^saha_trafo_sevk_tests_[a-f0-9]{32}$", database);
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
