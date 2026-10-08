using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Application.Features.SevkiyatIslemleri.Commands;
using _3K.Application.Features.SevkiyatIslemleri.Queries;
using _3K.Application.Features.SevkiyatIslemleri.Validators;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

/// <summary>
/// Çok projeli toplu sevk (tır seferi) ve sevkiyat istatistikleri (P9): yetki her projenin gerçek tipinin sevk iznidir,
/// sevk hepsi ya da hiçbiridir, aynı anahtar tekrar güvenlidir, tekil sevk de sefer açar, istatistik sefer ve sevkiyat
/// kayıtlarından okunur (eski sevkiyatlar plaka + gün ile gruplanır).
/// </summary>
public sealed class TopluSevkEtTests
{
    [Theory]
    [InlineData("34 ABC 123", "34ABC123", "34 ABC 123")]
    [InlineData("34-abc-123", "34ABC123", "34 ABC 123")]
    [InlineData("  06 a 1234 ", "06A1234", "06 A 1234")]
    [InlineData("DE-AB-1234", "DEAB1234", "DEAB1234")]
    public void Plaka_BoslukVeTireAtilir_BuyukHarf_TurkPlakasiBicimlenir(string girdi, string normal, string gosterim)
    {
        Assert.Equal(normal, AracPlakaKurali.Normalize(girdi));
        Assert.Equal(gosterim, AracPlakaKurali.Bicimle(girdi));
        Assert.Null(AracPlakaKurali.Normalize(" - "));
    }

    [Fact]
    public void SeferNo_YilinSonNumarasindanSonrakiDir()
    {
        Assert.Equal("2026-000001", SevkSeferiKurali.SonrakiSeferNo(2026, null));
        Assert.Equal("2026-000124", SevkSeferiKurali.SonrakiSeferNo(2026, "2026-000123"));
        Assert.Equal("2027-000001", SevkSeferiKurali.SonrakiSeferNo(2027, "2026-000123"));
    }

    [Fact]
    public void Dogrulayici_ProjeSandikPlakaVeTarihSinirlariniUygular()
    {
        var gecerli = Istek(Guid.NewGuid(), (10, [1, 2]), (11, [3]));
        var dogrulayici = new TopluSevkEtCommandValidator();
        Assert.True(dogrulayici.Validate(gecerli).IsValid);
        Assert.False(dogrulayici.Validate(Istek(Guid.Empty, (10, [1]))).IsValid);
        Assert.False(dogrulayici.Validate(Istek(Guid.NewGuid(), (10, [1]), (10, [2]))).IsValid);
        Assert.False(dogrulayici.Validate(Istek(Guid.NewGuid(), (10, [1]), (11, [1]))).IsValid);
        Assert.False(dogrulayici.Validate(Istek(Guid.NewGuid(), (10, []))).IsValid);
        var plakasiz = Istek(Guid.NewGuid(), (10, [1]));
        plakasiz.AracPlaka = " - ";
        Assert.False(dogrulayici.Validate(plakasiz).IsValid);
        var eskiTarih = Istek(Guid.NewGuid(), (10, [1]));
        eskiTarih.SevkTarihi = TurkeyTime.Now.AddYears(-3);
        Assert.False(dogrulayici.Validate(eskiTarih).IsValid);
        var cokProje = Istek(Guid.NewGuid(), Enumerable.Range(1, 26).Select(i => (i, new[] { i })).ToArray());
        Assert.False(dogrulayici.Validate(cokProje).IsValid);
    }

    [Fact]
    public void IstekOzeti_SiradanBagimsiz_IcerikDegisinceFarkli()
    {
        var anahtar = Guid.NewGuid();
        var a = TopluSevkEtCommandHandler.IstekHashOlustur(Istek(anahtar, (10, [1, 2]), (11, [3])));
        var b = TopluSevkEtCommandHandler.IstekHashOlustur(Istek(anahtar, (11, [3]), (10, [2, 1])));
        var c = TopluSevkEtCommandHandler.IstekHashOlustur(Istek(anahtar, (10, [1]), (11, [3])));
        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    /// <summary>Farklı tipte iki proje: her ikisinin gerçek tipine göre sevk etme (W) izni birlikte aranır.</summary>
    [Fact]
    public async Task Yetki_HerProjeninGercekTipininSevkIzniBirlikteAranir_BilinmeyenSandikReddedilir()
    {
        var uow = new OrtakMemoryUow();
        uow.Repo<Proje>().Rows.AddRange([new Proje { Id = 10, ProjeTipiId = (int)ProjeTipi.Normal }, new Proje { Id = 11, ProjeTipiId = (int)ProjeTipi.Saha }]);
        uow.Repo<Sandik>().Rows.AddRange([new Sandik { Id = 1, ProjeId = 10 }, new Sandik { Id = 3, ProjeId = 11 }]);
        var resolver = new RequestMenuPermissionResolver(uow, new BellekOkuyucu());

        var izin = await resolver.ResolveAsync(Istek(Guid.NewGuid(), (10, [1]), (11, [3])), default);
        var gereksinimler = izin.Groups.SelectMany(g => g.Requirements).ToList();
        Assert.Contains(MenuPermissionRequirement.Write(YetkiKodlari.Proje.SevkEt), gereksinimler);
        Assert.Contains(MenuPermissionRequirement.Write(YetkiKodlari.Saha.SevkEt), gereksinimler);
        Assert.All(izin.Groups, g => Assert.Equal(MenuPermissionMatch.All, g.Match));

        Assert.Empty((await resolver.ResolveAsync(Istek(Guid.NewGuid(), (10, [1, 99])), default)).Groups);
    }

    /// <summary>
    /// Gerçek SQL: normal + yedek projeyi tek seferde sevk eder; tekrar aynı sonucu verir; farklı istek aynı anahtarla 409;
    /// tekil sevk de sefer açar; kapanmamış sandık bütün isteği durdurur; istatistik eski sevkiyatı plaka + günle gruplar.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_TopluSevk_SeferVeProjeBasinaSevkiyat_Tekrar_Engel_TekilSefer_Istatistik()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            var k = await Kurgu.KurAsync(options);
            var anahtar = Guid.NewGuid();
            var istek = Istek(anahtar, (k.Normal, [k.N1, k.N2]), (k.Yedek, [k.Y1]));

            var sonuc = await k.TopluSevkAsync(istek);
            Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
            var yil = TurkeyTime.Now.Year;
            Assert.Equal(($"{yil}-000001", 2, 3), (sonuc.Value!.SeferNo, sonuc.Value.ProjeSayisi, sonuc.Value.SandikSayisi));

            await using (var db = new AppDbContext(options))
            {
                var sefer = await db.SevkSeferleri.SingleAsync();
                Assert.Equal(("34ABC123", "Ahmet", "34XYZ99", anahtar), (sefer.AracPlaka, sefer.Sofor, sefer.DorsePlaka, sefer.IslemAnahtari!.Value));
                var sevkiyatlar = await db.Sevkiyatlar.Include(s => s.Sandiklar).OrderBy(s => s.ProjeId).ToListAsync();
                Assert.Equal([(k.Normal, 2), (k.Yedek, 1)], sevkiyatlar.Select(s => (s.ProjeId, s.Sandiklar.Count)));
                Assert.All(sevkiyatlar, s => Assert.Equal((sefer.Id, 1, "34 ABC 123"), (s.SevkSeferiId!.Value, s.SevkiyatNo, s.AracPlaka)));
                Assert.All(await db.Sandiklar.Where(s => s.Id == k.N1 || s.Id == k.N2 || s.Id == k.Y1).ToListAsync(),
                    s => Assert.Equal(((int)SandikDurum.Sevkedildi, (int?)SandikDurum.Kapandi), (s.DurumId, s.SevkOncesiDurumId)));
                Assert.Equal((int)SandikDurum.Kapandi, (await db.Sandiklar.SingleAsync(s => s.Id == k.N3)).DurumId);
                var projeler = await db.Projeler.Where(p => p.Id == k.Normal || p.Id == k.Yedek).OrderBy(p => p.Id).ToListAsync();
                Assert.All(projeler, p => Assert.Equal(istek.SevkTarihi, p.GerceklesenSevkTarihi));
                // Yedek projenin iki sandığından biri sevk edildi: kısmi sevk (tekil sevkle aynı kural).
                Assert.Equal((int)ProjeDurum.EksikSevkEdildi, projeler.Single(p => p.Id == k.Yedek).DurumId);
                var hareketler = await db.HareketGecmisleri.Where(h => h.IslemTipiId == (int)IslemTipi.ProjeSevkEdildi).ToListAsync();
                Assert.Equal(2, hareketler.Count);
                Assert.All(hareketler, h => Assert.EndsWith($"Toplu sevk: sefer {yil}-000001, plaka 34 ABC 123.", h.Aciklama));
            }

            // Aynı anahtar + aynı istek: yeni kayıt yok, aynı sefer; farklı istek aynı anahtarla 409.
            var tekrar = await k.TopluSevkAsync(Istek(anahtar, (k.Yedek, [k.Y1]), (k.Normal, [k.N2, k.N1])));
            Assert.Equal((true, sonuc.Value.SeferId, 3), (tekrar.IsSuccess, tekrar.Value!.SeferId, tekrar.Value.SandikSayisi));
            var farkli = await k.TopluSevkAsync(Istek(anahtar, (k.Normal, [k.N1])));
            Assert.Equal(409, farkli.StatusCode);

            // Kapanmamış sandık: bütün istek 409, hiçbir kayıt yazılmaz; engel proje ve sandık bazında döner.
            var engelli = await k.TopluSevkAsync(Istek(Guid.NewGuid(), (k.Normal, [k.N3]), (k.Saha, [k.S1, k.S2])));
            Assert.Equal(409, engelli.StatusCode);
            var sorun = Assert.Single(Assert.IsAssignableFrom<IEnumerable<TopluSevkEtCommandHandler.TopluSevkSorunu>>(engelli.Error!.Issues));
            Assert.Equal((k.Saha, "SAHA-1"), (sorun.ProjeId, sorun.ProjeNo));
            Assert.Equal([k.S2], sorun.SandikIds);
            await using (var db = new AppDbContext(options))
            {
                Assert.Equal(1, await db.SevkSeferleri.CountAsync());
                Assert.Equal(2, await db.Sevkiyatlar.CountAsync());
                Assert.Equal((int)SandikDurum.Kapandi, (await db.Sandiklar.SingleAsync(s => s.Id == k.N3)).DurumId);
                Assert.Equal((int)SandikDurum.Kapandi, (await db.Sandiklar.SingleAsync(s => s.Id == k.S1)).DurumId);
            }

            // Tekil sevk de tek projelik sefer açar (sıradaki numara, plaka normalize).
            var tekil = await k.TekilSevkAsync(new ProjeSevkEtCommand { ProjeId = k.Normal, SandikIds = [k.N3], AracPlaka = "34-abc-123", SevkTarihi = istek.SevkTarihi });
            Assert.True(tekil.IsSuccess, tekil.Error?.Message);
            await using (var db = new AppDbContext(options))
            {
                var tekilSefer = await db.SevkSeferleri.OrderByDescending(x => x.Id).FirstAsync();
                Assert.Equal(($"{yil}-000002", "34ABC123", (Guid?)null), (tekilSefer.SeferNo, tekilSefer.AracPlaka, tekilSefer.IslemAnahtari));
                Assert.Equal(2, (await db.Sevkiyatlar.SingleAsync(s => s.SevkSeferiId == tekilSefer.Id)).SevkiyatNo);

                // Eski kayıt: sefere bağlı olmayan, aynı gün aynı plakalı (farklı yazımla) iki sevkiyat tek sanal sefer.
                var eskiGun = istek.SevkTarihi.AddDays(-10);
                var eski1 = new Sevkiyat { ProjeId = k.Saha, SevkiyatNo = 1, SevkTarihi = eskiGun, AracPlaka = "34 abc 123", KullaniciId = 7 };
                var eski2 = new Sevkiyat { ProjeId = k.Yedek, SevkiyatNo = 2, SevkTarihi = eskiGun.AddHours(3), AracPlaka = "34ABC123", KullaniciId = 7 };
                db.Sevkiyatlar.AddRange(eski1, eski2);
                await db.SaveChangesAsync();
                db.SevkiyatSandiklari.AddRange(new SevkiyatSandik { SevkiyatId = eski1.Id, SandikId = k.S1 }, new SevkiyatSandik { SevkiyatId = eski2.Id, SandikId = k.Y2 });
                db.Sevkiyatlar.Add(new Sevkiyat { ProjeId = k.Saha, SevkiyatNo = 3, SevkTarihi = eskiGun, AracPlaka = "06 A 1", KullaniciId = 7 });
                await db.SaveChangesAsync();
            }

            var seferler = await k.SorguAsync(new SevkSeferleriQuery { Plaka = "34 abc", PageSize = 10 });
            Assert.Equal(3, seferler.TotalCount);
            var eski = Assert.Single(seferler.Items, x => x.EskiKayit);
            Assert.Equal(($"E:34ABC123|{istek.SevkTarihi.AddDays(-10):yyyy-MM-dd}", 2, 2, "34 ABC 123"),
                (eski.Anahtar, eski.ProjeSayisi, eski.SandikSayisi, eski.AracPlaka));
            Assert.Equal(new[] { "SAHA-1", "YEDEK-1" }, eski.ProjeNolari);

            var detay = await k.DetayAsync(new SevkSeferiDetayQuery { SeferId = sonuc.Value.SeferId });
            Assert.Equal(["NORMAL-1", "YEDEK-1"], detay.Projeler.Select(p => p.ProjeNo));
            Assert.Equal(["1", "2"], detay.Projeler[0].Sandiklar.Select(s => s.SandikNo));
            var eskiDetay = await k.DetayAsync(new SevkSeferiDetayQuery { EskiPlaka = "34-ABC-123", EskiTarih = istek.SevkTarihi.AddDays(-10) });
            Assert.Equal(2, eskiDetay.Projeler.Count);

            var istatistik = await k.IstatistikAsync(new SevkiyatIstatistikQuery());
            Assert.Equal((3, 6, 3, "34 ABC 123"), (istatistik.SeferSayisi, istatistik.SandikSayisi, istatistik.ProjeSayisi, istatistik.EnCokKullanilanPlaka));
            var plaka = Assert.Single(istatistik.Plakalar);
            Assert.Equal((3, 6, 3), (plaka.SeferSayisi, plaka.SandikSayisi, plaka.ProjeSayisi));
            var normalIstatistik = istatistik.Projeler.Single(p => p.ProjeNo == "NORMAL-1");
            Assert.Equal((2, 3), (normalIstatistik.SeferSayisi, normalIstatistik.SandikSayisi));
            Assert.Equal(istatistik.SeferSayisi, istatistik.Aylik.Sum(x => x.SeferSayisi));
        });
    }

    /// <summary>Aynı sandıkları içeren iki toplu sevk eşzamanlı: yalnız biri uygulanır, diğeri hiçbir şey yazmadan reddedilir.</summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_EszamanliIkiTopluSevk_YalnizBiriUygulanir()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            var k = await Kurgu.KurAsync(options);
            var sonuclar = await Task.WhenAll(
                Task.Run(() => k.TopluSevkAsync(Istek(Guid.NewGuid(), (k.Normal, [k.N1, k.N2]), (k.Yedek, [k.Y1])))),
                Task.Run(() => k.TopluSevkAsync(Istek(Guid.NewGuid(), (k.Normal, [k.N1]), (k.Yedek, [k.Y1, k.Y2])))));
            Assert.Single(sonuclar, x => x.IsSuccess);
            Assert.Single(sonuclar, x => !x.IsSuccess && x.StatusCode is 409 or 400);
            await using var db = new AppDbContext(options);
            Assert.Equal(1, await db.SevkSeferleri.CountAsync());
            Assert.Equal(2, await db.Sevkiyatlar.CountAsync());
            Assert.Equal(await db.SevkiyatSandiklari.CountAsync(), await db.SevkiyatSandiklari.Select(x => x.SandikId).Distinct().CountAsync());
        });
    }

    /// <summary>Şema betiği EF modeliyle aynı tablo/kolon/indeksleri kurar ve tekrar çalıştırılabilir; yetki betiği düğüm ve izinleri ekler.</summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_SemaVeYetkiBetikleri_ModelleAyni_TekrarCalisir()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var model = await SemaAsync(db);
            await db.Database.ExecuteSqlRawAsync("""
                ALTER TABLE "Sevkiyatlar" DROP CONSTRAINT "FK_Sevkiyatlar_SevkSeferleri_SevkSeferiId";
                DROP INDEX "IX_Sevkiyatlar_SevkSeferiId";
                ALTER TABLE "Sevkiyatlar" DROP COLUMN "SevkSeferiId";
                DROP TABLE "SevkSeferleri";
                """);
            var sema = await File.ReadAllTextAsync(Betik("20261008_04_Sevk_Seferleri.sql"));
            await db.Database.ExecuteSqlRawAsync(sema);
            await db.Database.ExecuteSqlRawAsync(sema);
            Assert.Equal(model, await SemaAsync(db));

            // Yetki: Admin W; Projeler + sevk edilen gören rol R; yalnız Projeler gören rol yok.
            db.Roller.AddRange(new Rol { Id = 40, Ad = "Sevk izleyici" }, new Rol { Id = 41, Ad = "Yalnız proje" });
            await db.SaveChangesAsync();
            var projeler = await db.MenuTanimlari.SingleAsync(m => m.Kod == YetkiKodlari.Proje.Kok);
            var sevkEdilen = await db.MenuTanimlari.SingleAsync(m => m.Kod == YetkiKodlari.Proje.SevkEdilen);
            db.RolYetkileri.AddRange(
                new RolYetki { RolId = 40, MenuTanimiId = projeler.Id, YetkiTipiId = 2 },
                new RolYetki { RolId = 40, MenuTanimiId = sevkEdilen.Id, YetkiTipiId = 2 },
                new RolYetki { RolId = 41, MenuTanimiId = projeler.Id, YetkiTipiId = 2 });
            await db.SaveChangesAsync();
            var yetki = await File.ReadAllTextAsync(Betik("20261008_05_Sevkiyatlar_Yetkisi.sql"));
            await db.Database.ExecuteSqlRawAsync(yetki);
            await db.Database.ExecuteSqlRawAsync(yetki);
            db.ChangeTracker.Clear();
            var dugum = await db.MenuTanimlari.SingleAsync(m => m.Kod == YetkiKodlari.Proje.Sevkiyatlar);
            Assert.Equal((5246, projeler.Id, "/projeler/sevkiyatlar", "MENU.SEVKIYATLAR"), (dugum.Id, dugum.ParentId!.Value, dugum.Route, dugum.LabelKey));
            var izinler = await db.RolYetkileri.Where(r => r.MenuTanimiId == dugum.Id).OrderBy(r => r.RolId)
                .Select(r => new { r.RolId, r.YetkiTipiId }).ToListAsync();
            Assert.Contains(izinler, x => x.RolId == SistemRolleri.AdminId && x.YetkiTipiId == 3);
            Assert.Contains(izinler, x => x.RolId == 40 && x.YetkiTipiId == 2);
            Assert.DoesNotContain(izinler, x => x.RolId == 41);
        });
    }

    private static async Task<string> SemaAsync(AppDbContext db)
    {
        var kolonlar = await db.Database.SqlQueryRaw<string>("""
            SELECT table_name || '.' || column_name || ':' || data_type || ':' || coalesce(character_maximum_length::text, '') || ':' || is_nullable || ':' || coalesce(column_default, '') AS "Value"
            FROM information_schema.columns
            WHERE table_name = 'SevkSeferleri' OR (table_name = 'Sevkiyatlar' AND column_name = 'SevkSeferiId')
            """).ToListAsync();
        var indeksler = await db.Database.SqlQueryRaw<string>("""
            SELECT indexname || ':' || indexdef AS "Value" FROM pg_indexes
            WHERE tablename = 'SevkSeferleri' OR indexname = 'IX_Sevkiyatlar_SevkSeferiId'
            """).ToListAsync();
        var kisitlar = await db.Database.SqlQueryRaw<string>("""
            SELECT conname || ':' || pg_get_constraintdef(oid) AS "Value" FROM pg_constraint
            WHERE conrelid = '"SevkSeferleri"'::regclass OR conname = 'FK_Sevkiyatlar_SevkSeferleri_SevkSeferiId'
            """).ToListAsync();
        return string.Join("\n", kolonlar.Order().Concat(indeksler.Order()).Concat(kisitlar.Order()));
    }

    private static string Betik(string ad)
    {
        var dizin = new DirectoryInfo(AppContext.BaseDirectory);
        while (dizin != null && !File.Exists(Path.Combine(dizin.FullName, "scripts", "database", ad)))
            dizin = dizin.Parent;
        Assert.NotNull(dizin);
        return Path.Combine(dizin.FullName, "scripts", "database", ad);
    }

    private static TopluSevkEtCommand Istek(Guid anahtar, params (int ProjeId, int[] SandikIds)[] projeler) => new()
    {
        IslemAnahtari = anahtar,
        SevkTarihi = TurkeyTime.Now.Date.AddHours(9),
        AracPlaka = "34 abc 123",
        Sofor = " Ahmet ",
        DorsePlaka = "34-xyz-99",
        Aciklama = "Gebze çıkışlı tır",
        Projeler = projeler.Select(p => new TopluSevkProjesi { ProjeId = p.ProjeId, SandikIds = p.SandikIds.ToList() }).ToList()
    };

    private sealed class Kurgu(DbContextOptions<AppDbContext> options)
    {
        public int Normal, Yedek, Saha, N1, N2, N3, Y1, Y2, S1, S2;

        public static async Task<Kurgu> KurAsync(DbContextOptions<AppDbContext> options)
        {
            var k = new Kurgu(options);
            await using var db = new AppDbContext(options);
            db.Kullanicilar.Add(new Kullanici { Id = 7, AdSoyad = "Sevk Test", Email = "sevk@example.invalid", RolId = SistemRolleri.AdminId });
            var normal = new Proje { ProjeNo = "NORMAL-1", ProjeTipiId = (int)ProjeTipi.Normal, DurumId = (int)ProjeDurum.Hazirlaniyor };
            var yedek = new Proje { ProjeNo = "YEDEK-1", ProjeTipiId = (int)ProjeTipi.Yedek, DurumId = (int)ProjeDurum.Hazirlaniyor };
            var saha = new Proje { ProjeNo = "SAHA-1", ProjeTipiId = (int)ProjeTipi.Saha, DurumId = (int)ProjeDurum.Hazirlaniyor };
            Sandik S(Proje p, string no, SandikDurum durum, decimal kg) => new() { Proje = p, SandikNo = no, Ad = "Sandık " + no, DurumId = (int)durum, GrossKg = kg };
            var n1 = S(normal, "1", SandikDurum.Kapandi, 100); var n2 = S(normal, "2", SandikDurum.Kapandi, 200); var n3 = S(normal, "3", SandikDurum.Kapandi, 50);
            var y1 = S(yedek, "1", SandikDurum.Kapandi, 70); var y2 = S(yedek, "2", SandikDurum.Kapandi, 30);
            var s1 = S(saha, "1", SandikDurum.Kapandi, 10); var s2 = S(saha, "2", SandikDurum.Hazirlaniyor, 10);
            db.Sandiklar.AddRange(n1, n2, n3, y1, y2, s1, s2);
            await db.SaveChangesAsync();
            (k.Normal, k.Yedek, k.Saha) = (normal.Id, yedek.Id, saha.Id);
            (k.N1, k.N2, k.N3, k.Y1, k.Y2, k.S1, k.S2) = (n1.Id, n2.Id, n3.Id, y1.Id, y2.Id, s1.Id, s2.Id);
            return k;
        }

        private (AppDbContext Db, UnitOfWork Uow, HareketService Hareket) Baglam()
        {
            var db = new AppDbContext(options);
            var uow = new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
            return (db, uow, new HareketService(uow, db));
        }

        public async Task<Result<_3K.Application.Features.SevkiyatIslemleri.DTOs.TopluSevkSonucDto>> TopluSevkAsync(TopluSevkEtCommand istek)
        {
            var (db, uow, hareket) = Baglam();
            await using (db)
            using (uow)
            {
                var dogrulama = await new TopluSevkEtCommandValidator().ValidateAsync(istek);
                Assert.True(dogrulama.IsValid, string.Join(" ", dogrulama.Errors.Select(e => e.ErrorMessage)));
                return await new TopluSevkEtCommandHandler(uow, hareket, new OrtakUser(7), new OrtakSaha(), new EntityFrameworkReadQueryExecutor())
                    .Handle(istek, default);
            }
        }

        public async Task<Result> TekilSevkAsync(ProjeSevkEtCommand istek)
        {
            var (db, uow, hareket) = Baglam();
            await using (db)
            using (uow)
                return await new ProjeSevkEtCommandHandler(uow, hareket, new OrtakUser(7), new OrtakSaha(), new EntityFrameworkReadQueryExecutor())
                    .Handle(istek, default);
        }

        private async Task<T> Sorgu<T>(Func<SevkiyatQueryHandlers, Task<Result<T>>> calistir)
        {
            var (db, uow, _) = Baglam();
            await using (db)
            using (uow)
            {
                var sonuc = await calistir(new SevkiyatQueryHandlers(uow, new EntityFrameworkReadQueryExecutor()));
                Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
                return sonuc.Value!;
            }
        }

        public Task<PaginatedList<_3K.Application.Features.SevkiyatIslemleri.DTOs.SevkSeferiOzetDto>> SorguAsync(SevkSeferleriQuery q) => Sorgu(h => h.Handle(q, default));
        public Task<_3K.Application.Features.SevkiyatIslemleri.DTOs.SevkSeferiDetayDto> DetayAsync(SevkSeferiDetayQuery q) => Sorgu(h => h.Handle(q, default));
        public Task<_3K.Application.Features.SevkiyatIslemleri.DTOs.SevkiyatIstatistikDto> IstatistikAsync(SevkiyatIstatistikQuery q) => Sorgu(h => h.Handle(q, default));
    }

    private sealed class BellekOkuyucu : IReadQueryExecutor
    {
        public IQueryable<T> AsNoTracking<T>(IQueryable<T> query) where T : class => query;
        public Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken ct = default) => Task.FromResult(query.Count());
        public Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken ct = default) => Task.FromResult(query.ToList());
    }
}
