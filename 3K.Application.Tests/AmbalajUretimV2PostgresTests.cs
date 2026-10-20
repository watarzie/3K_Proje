using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Application.Features.AmbalajIslemleri.Commands;
using _3K.Application.Features.AmbalajIslemleri.Queries;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Constants;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

public sealed class AmbalajUretimV2PostgresTests
{
    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task PlanlamaOngorusu_SecimOncesi_SqlSayfalamaVeDurumErisimiyleTutarlidir()
    {
        await WithDatabase(async options =>
        {
            await using var ctx = new AppDbContext(options);
            ctx.Projeler.AddRange(new Proje { Id = 11, ProjeNo = "TEST-11" },
                new Proje { Id = 12, ProjeNo = "TEST-12" });
            ctx.Sandiklar.AddRange(new Sandik { Id = 11, ProjeId = 11, SandikNo = "1", Ad = "Radyatör",
                Boy = 1800, En = 900, Yukseklik = 800 },
                new Sandik { Id = 12, ProjeId = 12, SandikNo = "1", Ad = "Radyatör",
                    Boy = 1800, En = 900, Yukseklik = 800 });
            ctx.Set<AmbalajUretimKaydi>().AddRange(
                new AmbalajUretimKaydi { Id = 11, ProjeId = 11, KaynakKayitId = 11,
                    Boy = 1800, En = 900, Yukseklik = 800,
                    HesaplananToplamM3 = 7m, M3Override = 2m, AmbalajaDahil = true, UretimeAlindi = false },
                new AmbalajUretimKaydi { Id = 12, ProjeId = 12, KaynakKayitId = 12,
                    Boy = 1800, En = 900, Yukseklik = 800,
                    HesaplananToplamM3 = 3m, AmbalajaDahil = true, UretimeAlindi = true,
                    UretimDurumu = AmbalajUretimDurumu.Tamamlandi });
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();
            using var uow = new UnitOfWork(ctx, NullLogger<UnitOfWork>.Instance);
            var handler = new GetAmbalajPlanlamaProjeleriQueryHandler(uow, OrtakRoller.TumIzinler(), new OrtakUser(), new EntityFrameworkReadQueryExecutor());
            var firstPage = await handler.Handle(new() { PageSize = 1, PageNumber = 1 }, default);
            var secondPage = await handler.Handle(new() { PageSize = 1, PageNumber = 2 }, default);
            Assert.True(firstPage.IsSuccess);
            Assert.Equal(5m, firstPage.Value!.FilteredSummary!.ToplamHacimM3);
            Assert.Equal(5m, secondPage.Value!.FilteredSummary!.ToplamHacimM3);
            Assert.Equal(2m, Assert.Single(secondPage.Value.Items).ProjeSandiklariHacimM3);
            Assert.Equal(0m, Assert.Single(secondPage.Value.Items).UretimHacimM3);

            var waitingOnly = await new GetAmbalajPlanlamaProjeleriQueryHandler(uow,
                OrtakRoller.Haric(YetkiKodlari.Ambalaj.UretimdeGoruntule, YetkiKodlari.Ambalaj.TamamlananGoruntule),
                new OrtakUser(), new EntityFrameworkReadQueryExecutor()).Handle(new(), default);
            Assert.Equal(2m, waitingOnly.Value!.FilteredSummary!.ToplamHacimM3);
            Assert.Equal(11, Assert.Single(waitingOnly.Value.Items).ProjeId);
            Assert.Empty(ctx.ChangeTracker.Entries());
            Assert.Empty(await ctx.Set<AmbalajUretimFormuSurumu>().ToListAsync());
            Assert.Empty(await ctx.Set<AmbalajUretimHareketi>().ToListAsync());
        });
    }

    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task ProjeListesi_UretimDurumuFiltresi_BekleyenVeUretimdeProjeleriAyirir()
    {
        await WithDatabase(async options =>
        {
            await using var ctx = new AppDbContext(options);
            ctx.Projeler.AddRange(new Proje { Id = 31, ProjeNo = "BEKLEYEN" }, new Proje { Id = 32, ProjeNo = "URETIMDE" },
                new Proje { Id = 33, ProjeNo = "TAMAM" });
            AmbalajUretimKaydi Kayit(int id, int projeId, AmbalajUretimDurumu durum) => new()
            {
                Id = id, ProjeId = projeId, KaynakKayitId = id, SandikNo = id.ToString(), Boy = 1000, En = 800, Yukseklik = 600,
                AmbalajaDahil = true, UretimDurumu = durum
            };
            ctx.Set<AmbalajUretimKaydi>().AddRange(
                Kayit(311, 31, AmbalajUretimDurumu.Planlandi), Kayit(312, 31, AmbalajUretimDurumu.Planlandi),
                Kayit(321, 32, AmbalajUretimDurumu.Tamamlandi), Kayit(322, 32, AmbalajUretimDurumu.Planlandi),
                Kayit(331, 33, AmbalajUretimDurumu.Tamamlandi));
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();
            using var uow = new UnitOfWork(ctx, NullLogger<UnitOfWork>.Instance);
            var handler = new GetAmbalajPlanlamaProjeleriQueryHandler(uow, OrtakRoller.TumIzinler(), new OrtakUser(), new EntityFrameworkReadQueryExecutor());
            async Task<int[]> Projeler(int? durum) => (await handler.Handle(new() { Tamamlanan = false, UretimDurumu = durum }, default))
                .Value!.Items.Select(x => x.ProjeId).Order().ToArray();
            Assert.Equal(new[] { 31, 32 }, await Projeler(null));
            Assert.Equal(new[] { 31 }, await Projeler(1));
            Assert.Equal(new[] { 32 }, await Projeler(2));
        });
    }

    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task Form_AyniAnahtarParalelRetry_TekSurumVeTekDurumGecisi()
    {
        await WithDatabase(async options =>
        {
            var request = new AmbalajFormOlusturCommand { KayitIdleri = [1], IdempotencyAnahtari = Guid.NewGuid() };
            async Task<int> Create()
            {
                await using var ctx = new AppDbContext(options);
                using var uow = new UnitOfWork(ctx, NullLogger<UnitOfWork>.Instance);
                var result = await Handler(ctx, uow).Handle(request, default);
                Assert.True(result.IsSuccess, result.Error?.Message);
                return result.Value!.Id;
            }
            var ids = await Task.WhenAll(Create(), Create());
            Assert.Equal(ids[0], ids[1]);
            await using var verify = new AppDbContext(options);
            Assert.Single(await verify.Set<AmbalajUretimFormuSurumu>().ToListAsync());
            Assert.Single(await verify.Set<AmbalajUretimFormuKaydi>().ToListAsync());
            Assert.Equal(AmbalajUretimDurumu.Uretimde, (await verify.Set<AmbalajUretimKaydi>().SingleAsync()).UretimDurumu);
            Assert.Single(await verify.Set<AmbalajUretimHareketi>().Where(x => x.AlanAdi == "UretimDurumu").ToListAsync());
            Assert.DoesNotContain("\"NetM3\":0", (await verify.Set<AmbalajUretimFormuSurumu>().SingleAsync()).SnapshotJson);
        });
    }

    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task Form_FinansHatasi_SnapshotSecimAuditAtomikRollback()
    {
        await WithDatabase(async options =>
        {
            await using (var ctx = new AppDbContext(options))
            {
                using var uow = new UnitOfWork(ctx, NullLogger<UnitOfWork>.Instance);
                await Assert.ThrowsAsync<InvalidOperationException>(() => Handler(ctx, uow, fail: true).Handle(
                    new() { KayitIdleri = [1], IdempotencyAnahtari = Guid.NewGuid() }, default));
            }
            await using var verify = new AppDbContext(options);
            var record = await verify.Set<AmbalajUretimKaydi>().SingleAsync();
            Assert.False(record.UretimeAlindi); Assert.False(record.KaynakSenkronizasyonuKilitliMi);
            Assert.Equal(AmbalajUretimDurumu.Planlandi, record.UretimDurumu);
            Assert.Empty(await verify.Set<AmbalajUretimFormuSurumu>().ToListAsync());
            Assert.Empty(await verify.Set<AmbalajUretimFormuKaydi>().ToListAsync());
            Assert.Empty(await verify.Set<AmbalajUretimHareketi>().ToListAsync());
        });
    }

    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task Form_XminEskiYazmayiReddeder_VeSnapshotBaglantisiFizikselSilmeyiEngeller()
    {
        await WithDatabase(async options =>
        {
            await using var oldContext = new AppDbContext(options);
            var old = await oldContext.Set<AmbalajUretimKaydi>().SingleAsync();
            await using (var ctx = new AppDbContext(options))
            {
                using var uow = new UnitOfWork(ctx, NullLogger<UnitOfWork>.Instance);
                Assert.True((await Handler(ctx, uow).Handle(new() { KayitIdleri = [1], IdempotencyAnahtari = Guid.NewGuid() }, default)).IsSuccess);
            }
            old.Adet = 99;
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => oldContext.SaveChangesAsync());
            await using var deleteContext = new AppDbContext(options);
            deleteContext.Remove(await deleteContext.Set<AmbalajUretimKaydi>().SingleAsync());
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => deleteContext.SaveChangesAsync());
            Assert.Contains(((PostgresException)exception.InnerException!).SqlState,
                new[] { PostgresErrorCodes.ForeignKeyViolation, PostgresErrorCodes.RestrictViolation });
            await using var verify = new AppDbContext(options);
            Assert.Single(await verify.Set<AmbalajUretimFormuSurumu>().ToListAsync());
            Assert.Single(await verify.Set<AmbalajUretimKaydi>().ToListAsync());
        });
    }

    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task Form_PlanKaydedilmedenKaynakSandiklarla_KayitlariSenkronizeEderVeOlusturur()
    {
        // Sıfırlamadan sonra plan kaydedilmemiş proje: kayıtlar form isteğindeki senkronizasyonla oluşur ve aynı
        // DbContext tarafından izlenir; form bu kayıtları ikinci bir örnekle yeniden izlemeye çalışmamalıdır.
        await WithDatabase(async options =>
        {
            await using (var seed = new AppDbContext(options))
            {
                seed.Projeler.Add(new Proje { Id = 20, ProjeNo = "PA-20" });
                seed.Sandiklar.AddRange(
                    new Sandik { Id = 201, ProjeId = 20, SandikNo = "1", Ad = "Radyatör", Boy = 1800, En = 900, Yukseklik = 800 },
                    new Sandik { Id = 202, ProjeId = 20, SandikNo = "2", Ad = "Fan", Boy = 1200, En = 800, Yukseklik = 700 });
                await seed.SaveChangesAsync();
                // Ortak kurulum kaydı elle kimlik aldığı için kimlik dizisi ilerletilir; senkronizasyon yeni kayıt ekler.
                await seed.Database.ExecuteSqlRawAsync(
                    "SELECT setval(pg_get_serial_sequence('\"AmbalajUretimKayitlari\"', 'Id'), (SELECT MAX(\"Id\") FROM \"AmbalajUretimKayitlari\"))");
            }
            int formId;
            await using (var ctx = new AppDbContext(options))
            {
                using var uow = new UnitOfWork(ctx, NullLogger<UnitOfWork>.Instance);
                var sync = new AmbalajKaynakSenkronizasyonService(uow, new Finans(false));
                var handler = new AmbalajFormOlusturCommandHandler(uow, new User(), new Roles(), new Finans(false), sync,
                    new AmbalajFormIslemKilidi(ctx));
                var result = await handler.Handle(new()
                {
                    ProjeId = 20, KaynakSandikIdleri = [202, 201], FinansTarihi = new DateTime(2026, 9, 30),
                    IdempotencyAnahtari = Guid.NewGuid()
                }, default);
                Assert.True(result.IsSuccess, result.Error?.Message);
                formId = result.Value!.Id;
            }
            await using var verify = new AppDbContext(options);
            var kayitlar = await verify.Set<AmbalajUretimKaydi>().Where(k => k.ProjeId == 20)
                .OrderBy(k => k.KaynakKayitId).ToListAsync();
            Assert.Equal([201, 202], kayitlar.Select(k => k.KaynakKayitId!.Value));
            Assert.All(kayitlar, k =>
            {
                Assert.Equal(AmbalajUretimDurumu.Uretimde, k.UretimDurumu);
                Assert.True(k.KaynakSenkronizasyonuKilitliMi);
                Assert.Equal(new DateTime(2026, 9, 30), k.FinansTarihi);
            });
            var formKayitlari = await verify.Set<AmbalajUretimFormuKaydi>().Where(x => x.FormSurumuId == formId)
                .Select(x => x.AmbalajUretimKaydiId).OrderBy(x => x).ToListAsync();
            Assert.Equal(kayitlar.Select(k => k.Id).Order(), formKayitlari);
        });
    }

    /// <summary>
    /// Canlı bildirim (PA727-03) gerçek SQL ile: hiç ambalaj kaydı olmayan projede çekiden gelen sandıklar ve sonradan
    /// elle eklenen sandık. Çeki satırı sorgusu (elle eklenen ürün satırı hariç) PostgreSQL'de çevrilir; proje listesi
    /// öngörüsü ve Özel Sandıklar aday listesi kayıt oluşturmadan elle ekleneni ilave/ambalaj dışı okur, senkron da
    /// kaydı İlave ve "Yapılmaz" oluşturur.
    /// </summary>
    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task Senkron_CekiDisindanEklenenSandigiIlaveVeAmbalajDisiOlusturur_OkumaTarafiAyniKuraliUygular()
    {
        await WithDatabase(async options =>
        {
            await using (var seed = new AppDbContext(options))
            {
                var proje = new Proje { Id = 40, ProjeNo = "PA-40" };
                var ceki = new Ceki { Proje = proje, OrijinalDosyaYolu = "sentetik.xlsx", YuklemeTarihi = new DateTime(2026, 9, 23, 12, 0, 0) };
                foreach (var no in new[] { "1", "2", "2" })
                    ceki.CekiSatirlari.Add(new CekiSatiri
                    {
                        SiraNo = ceki.CekiSatirlari.Count + 1, BarkodNo = $"B-{ceki.CekiSatirlari.Count}", Aciklama = "Çeki ürünü",
                        CekideGecenSandikNo = no, FiiliSandikNo = no, IstenenAdet = 1
                    });
                ceki.CekiSatirlari.Add(new CekiSatiri
                {
                    SiraNo = 9999, BarkodNo = "M-5", Aciklama = "Elle eklenen ürün", CekideGecenSandikNo = "5", FiiliSandikNo = "5",
                    IstenenAdet = 1, IsManuelEklenen = true
                });
                proje.Cekiler.Add(ceki);
                seed.Projeler.Add(proje);
                seed.Sandiklar.AddRange(
                    new Sandik { Id = 401, ProjeId = 40, SandikNo = "1", Ad = "Radyatör", Boy = 1800, En = 900, Yukseklik = 800 },
                    new Sandik { Id = 402, ProjeId = 40, SandikNo = "2", Ad = "Fan", Boy = 1200, En = 800, Yukseklik = 700 },
                    new Sandik { Id = 405, ProjeId = 40, SandikNo = "5", Ad = "deneme", Boy = 1000, En = 800, Yukseklik = 600 });
                await seed.SaveChangesAsync();
                await seed.Database.ExecuteSqlRawAsync(
                    "SELECT setval(pg_get_serial_sequence('\"AmbalajUretimKayitlari\"', 'Id'), (SELECT MAX(\"Id\") FROM \"AmbalajUretimKayitlari\"))");
            }
            await using (var ctx = new AppDbContext(options))
            {
                using var uow = new UnitOfWork(ctx, NullLogger<UnitOfWork>.Instance);
                var liste = await new GetAmbalajPlanlamaProjeleriQueryHandler(uow, OrtakRoller.TumIzinler(), new OrtakUser(),
                    new EntityFrameworkReadQueryExecutor()).Handle(new(), default);
                var adaylar = await new GetAmbalajIlaveSandikAdaylariQueryHandler(uow, OrtakRoller.TumIzinler(), new OrtakUser())
                    .Handle(new() { ProjeId = 40 }, default);

                Assert.True(liste.IsSuccess, liste.Error?.Message);
                var ozet = Assert.Single(liste.Value!.Items);
                Assert.Equal((2, 0), (ozet.ToplamSandikAdedi, ozet.IlaveSandikSayisi));
                Assert.Equal(2, liste.Value.FilteredSummary!.ToplamSandikAdedi);
                Assert.True(adaylar.IsSuccess, adaylar.Error?.Message);
                Assert.Equal(405, Assert.Single(adaylar.Value!).Id);
                Assert.Empty(await ctx.Set<AmbalajUretimKaydi>().Where(k => k.ProjeId == 40).ToListAsync());
            }
            await using (var ctx = new AppDbContext(options))
            {
                using var uow = new UnitOfWork(ctx, NullLogger<UnitOfWork>.Instance);
                var sonuc = await new AmbalajKaynakSenkronizasyonService(uow, new Finans(false))
                    .SenkronizeEtAsync(40, new User(), default);
                Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
            }
            await using var verify = new AppDbContext(options);
            var kayitlar = await verify.Set<AmbalajUretimKaydi>().Where(k => k.ProjeId == 40)
                .ToDictionaryAsync(k => k.KaynakKayitId!.Value);
            Assert.Equal((AmbalajSandikTuru.Ilave, false, false),
                (kayitlar[405].Tur, kayitlar[405].AmbalajaDahil, kayitlar[405].UretimeAlindi));
            Assert.All([kayitlar[401], kayitlar[402]], k =>
                Assert.Equal((AmbalajSandikTuru.Normal, true), (k.Tur, k.AmbalajaDahil)));
        });
    }

    /// <summary>
    /// Özel Sandıklar > İlave formu kaydı olmayan kaynakla gerçek transaction'da: çekiden gelen (ambalaja dahil) sandığın
    /// dönüşümü 400'dür ve komutun içindeki senkronun eklediği kayıtlar dahil hiçbir satır kalmaz; elle eklenen sandık
    /// senkronlanıp ambalaj dışı İlave kaydı olarak özel sandığa dönüşür ve plan çekmecesinde görünmez.
    /// </summary>
    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task OzelIlave_KayitsizKaynaktan_CekiSandigiReddedilirGeriAlinir_ElleEkleneDonusur()
    {
        await WithDatabase(async options =>
        {
            await using (var seed = new AppDbContext(options))
            {
                var proje = new Proje { Id = 50, ProjeNo = "PA-50" };
                var ceki = new Ceki { Proje = proje, OrijinalDosyaYolu = "sentetik.xlsx", YuklemeTarihi = new DateTime(2026, 9, 23, 12, 0, 0) };
                ceki.CekiSatirlari.Add(new CekiSatiri
                {
                    SiraNo = 1, BarkodNo = "B-1", Aciklama = "Çeki ürünü", CekideGecenSandikNo = "1", FiiliSandikNo = "1", IstenenAdet = 1
                });
                proje.Cekiler.Add(ceki);
                seed.Projeler.Add(proje);
                seed.Sandiklar.AddRange(
                    new Sandik { Id = 501, ProjeId = 50, SandikNo = "1", Ad = "Radyatör", Boy = 1800, En = 900, Yukseklik = 800 },
                    new Sandik { Id = 505, ProjeId = 50, SandikNo = "5", Ad = "deneme", Boy = 1000, En = 800, Yukseklik = 600 });
                await seed.SaveChangesAsync();
                await seed.Database.ExecuteSqlRawAsync(
                    "SELECT setval(pg_get_serial_sequence('\"AmbalajUretimKayitlari\"', 'Id'), (SELECT MAX(\"Id\") FROM \"AmbalajUretimKayitlari\"))");
            }
            AmbalajBagimsizSandikKaydetCommand Istek(int kaynakSandikId) => new()
            {
                ProjeId = 50, Tur = 2, SandikNo = "", KaynakSandikId = kaynakSandikId, Ad = "İlave", SandikTipi = "Ahşap Kapalı",
                Adet = 1, Boy = 1000, En = 800, Yukseklik = 600, TalimatVeren = "Planlama"
            };
            async Task<Result<AmbalajBagimsizSandikDto>> Gonder(int kaynakSandikId)
            {
                await using var ctx = new AppDbContext(options);
                using var uow = new UnitOfWork(ctx, NullLogger<UnitOfWork>.Instance);
                var roller = OrtakRoller.TumIzinler();
                var finans = new Finans(false);
                return await new AmbalajBagimsizSandikKaydetCommandHandler(uow, new User(), roller,
                        new AmbalajKaynakSenkronizasyonService(uow, finans), finans, new AmbalajKritikIslemDenetimi(uow, roller, new User()))
                    .Handle(Istek(kaynakSandikId), default);
            }

            var cekiSandigi = await Gonder(501);
            Assert.Equal(400, cekiSandigi.StatusCode);
            await using (var verify = new AppDbContext(options))
                Assert.Empty(await verify.Set<AmbalajUretimKaydi>().Where(k => k.ProjeId == 50).ToListAsync());

            var elle = await Gonder(505);
            Assert.True(elle.IsSuccess, elle.Error?.Message);
            await using (var verify = new AppDbContext(options))
            {
                var kayitlar = await verify.Set<AmbalajUretimKaydi>().Where(k => k.ProjeId == 50)
                    .ToDictionaryAsync(k => k.KaynakKayitId!.Value);
                Assert.Equal((true, AmbalajSandikTuru.Ilave, false),
                    (kayitlar[505].BagimsizKayitMi, kayitlar[505].Tur, kayitlar[505].UretimeAlindi));
                Assert.Equal((false, AmbalajSandikTuru.Normal, true),
                    (kayitlar[501].BagimsizKayitMi, kayitlar[501].Tur, kayitlar[501].AmbalajaDahil));
                using var uow = new UnitOfWork(verify, NullLogger<UnitOfWork>.Instance);
                var plan = await new GetAmbalajPlanlamaPlanQueryHandler(uow, OrtakRoller.TumIzinler(), new OrtakUser())
                    .Handle(new() { ProjeId = 50 }, default);
                Assert.Equal(501, Assert.Single(plan.Value!.Kalemler).KaynakSandikId);
            }
        });
    }

    /// <summary>
    /// Gerçek sıra: kaynak sandıkla ilk form → sandık ölçüsü güncellenir → kuyruk senkronu kilitli kaydı atlar →
    /// yeniden oluşturma kaydı, çam m³'ünü ve form snapshot'ını (izlenmeden okunan form hesabı dahil) yeni ölçüyle yazar.
    /// </summary>
    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task Form_SandikOlcusuGuncellenince_YenidenOlusturmaGuncelOlcuyleHesaplar()
    {
        await WithDatabase(async options =>
        {
            var kayitId = await OlcuGuncellenmisUretimKaydiAsync(options);
            var finans = new FinansKaydedici();
            await using (var ctx = new AppDbContext(options))
            {
                using var uow = new UnitOfWork(ctx, NullLogger<UnitOfWork>.Instance);
                var handler = new AmbalajFormOlusturCommandHandler(uow, new User(), new Roles(), finans,
                    islemKilidi: new AmbalajFormIslemKilidi(ctx));
                var sonuc = await handler.Handle(new()
                {
                    ProjeId = 30, KayitIdleri = [kayitId], YenidenOlustur = true, IdempotencyAnahtari = Guid.NewGuid()
                }, default);
                Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
            }

            var beklenen = AmbalajHesaplayici.M3OzetiHesapla(1308, 1108, 1120, 1, AmbalajHesaplayici.VarsayilanSarfOrani,
                ayakHesapBoyu: 1400);
            await using var verify = new AppDbContext(options);
            var kayit = await verify.Set<AmbalajUretimKaydi>().SingleAsync(k => k.Id == kayitId);
            Assert.Equal((1400m, 1200m, 1375m), (kayit.Boy, kayit.En, kayit.Yukseklik));
            Assert.Equal(beklenen.HesaplananToplamM3, kayit.HesaplananToplamM3);
            Assert.Equal(beklenen.SarfM3, kayit.SarfM3);
            Assert.Equal(AmbalajUretimDurumu.Uretimde, kayit.UretimDurumu);
            Assert.True(kayit.KaynakSenkronizasyonuKilitliMi);
            var formlar = await verify.Set<AmbalajUretimFormuSurumu>().Where(f => f.ProjeId == 30)
                .OrderBy(f => f.Surum).ToListAsync();
            Assert.Equal([1, 2], formlar.Select(f => f.Surum));
            var ilk = JsonSerializer.Deserialize<AmbalajUretimFormuModel>(formlar[0].SnapshotJson)!.Kalemler.Single();
            var son = JsonSerializer.Deserialize<AmbalajUretimFormuModel>(formlar[1].SnapshotJson)!.Kalemler.Single();
            Assert.Equal(new AmbalajOlculeri(1400, 1000, 1375), ilk.DisOlculer);
            Assert.Equal(new AmbalajOlculeri(1400, 1200, 1375), son.DisOlculer);
            Assert.Equal(beklenen.NetM3, son.NetM3);
            Assert.Equal(beklenen.SarfM3, son.SarfM3);
            // Yalnız gerçekten değişen ölçü geçmişe yazılır: numeric(18,4) kayıt ile ölçeksiz sandık değeri (1400.0000
            // ile 1400) aynı sayılır.
            var olcuHareketleri = await verify.Set<AmbalajUretimHareketi>()
                .Where(h => h.AmbalajUretimKaydiId == kayitId && h.Islem == "Kaynak sandık değişiklikleri üretim formuna alındı" &&
                            (h.AlanAdi == "Boy" || h.AlanAdi == "En" || h.AlanAdi == "Yukseklik"))
                .Select(h => new { h.AlanAdi, h.EskiDeger, h.YeniDeger }).ToListAsync();
            var olcuHareketi = Assert.Single(olcuHareketleri);
            Assert.Equal("En", olcuHareketi.AlanAdi);
            Assert.Equal(1000m, decimal.Parse(olcuHareketi.EskiDeger!, System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal(1200m, decimal.Parse(olcuHareketi.YeniDeger!, System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal(1200, finans.Modeller.Single().En);
        });
    }

    /// <summary>
    /// Tazeleme formdan önce yazılır; aynı işlemde sonraki adım (finans aktarımı) başarısız olursa tazelenen ölçü,
    /// m³, geçmiş ve form sürümü veritabanında geri alınır.
    /// </summary>
    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task Form_YenidenOlusturmaFinansHatasi_KaynakTazelemesiniGeriAlir()
    {
        await WithDatabase(async options =>
        {
            var kayitId = await OlcuGuncellenmisUretimKaydiAsync(options);
            AmbalajUretimKaydi once;
            int hareketSayisi;
            await using (var oncesi = new AppDbContext(options))
            {
                once = await oncesi.Set<AmbalajUretimKaydi>().AsNoTracking().SingleAsync(k => k.Id == kayitId);
                hareketSayisi = await oncesi.Set<AmbalajUretimHareketi>().CountAsync(h => h.AmbalajUretimKaydiId == kayitId);
            }
            await using (var ctx = new AppDbContext(options))
            {
                using var uow = new UnitOfWork(ctx, NullLogger<UnitOfWork>.Instance);
                await Assert.ThrowsAsync<InvalidOperationException>(() => Handler(ctx, uow, fail: true).Handle(new()
                {
                    ProjeId = 30, KayitIdleri = [kayitId], YenidenOlustur = true, IdempotencyAnahtari = Guid.NewGuid()
                }, default));
            }

            await using var verify = new AppDbContext(options);
            var kayit = await verify.Set<AmbalajUretimKaydi>().SingleAsync(k => k.Id == kayitId);
            Assert.Equal((once.Boy, once.En, once.Yukseklik), (kayit.Boy, kayit.En, kayit.Yukseklik));
            Assert.Equal(1000, kayit.En);
            Assert.Equal((once.HesaplananToplamM3, once.SarfM3, once.ToplamM3), (kayit.HesaplananToplamM3, kayit.SarfM3, kayit.ToplamM3));
            Assert.Equal(once.KaynakSonSenkronizasyonTarihi, kayit.KaynakSonSenkronizasyonTarihi);
            Assert.Single(await verify.Set<AmbalajUretimFormuSurumu>().Where(f => f.ProjeId == 30).ToListAsync());
            Assert.Equal(hareketSayisi, await verify.Set<AmbalajUretimHareketi>().CountAsync(h => h.AmbalajUretimKaydiId == kayitId));
        });
    }

    /// <summary>
    /// Proje 30'daki 3 numaralı Ahşap Kapalı sandığı (1400×1000×1375) kaynak senkronuyla ilk forma alır, ölçüsünü
    /// sandık özellik güncellemesiyle 1400×1200×1375 yapar ve kuyruk senkronunun kilitli kaydı atladığını doğrular.
    /// </summary>
    private static async Task<int> OlcuGuncellenmisUretimKaydiAsync(DbContextOptions<AppDbContext> options)
    {
        await using (var seed = new AppDbContext(options))
        {
            seed.Projeler.Add(new Proje { Id = 30, ProjeNo = "PA-30" });
            seed.Sandiklar.Add(new Sandik
            {
                Id = 301, ProjeId = 30, SandikNo = "3", Ad = "Radyatör", TipId = (int)SandikTipi.AhsapKapali,
                Boy = 1400, En = 1000, Yukseklik = 1375, NetKg = 260, GrossKg = 410
            });
            await seed.SaveChangesAsync();
            // Ortak kurulum kaydı elle kimlik aldığı için kimlik dizisi ilerletilir; senkronizasyon yeni kayıt ekler.
            await seed.Database.ExecuteSqlRawAsync(
                "SELECT setval(pg_get_serial_sequence('\"AmbalajUretimKayitlari\"', 'Id'), (SELECT MAX(\"Id\") FROM \"AmbalajUretimKayitlari\"))");
        }
        await using (var ctx = new AppDbContext(options))
        {
            using var uow = new UnitOfWork(ctx, NullLogger<UnitOfWork>.Instance);
            var sync = new AmbalajKaynakSenkronizasyonService(uow, new Finans(false));
            var ilk = await new AmbalajFormOlusturCommandHandler(uow, new User(), new Roles(), new Finans(false), sync,
                new AmbalajFormIslemKilidi(ctx)).Handle(new()
            {
                ProjeId = 30, KaynakSandikIdleri = [301], IdempotencyAnahtari = Guid.NewGuid()
            }, default);
            Assert.True(ilk.IsSuccess, ilk.Error?.Message);
        }
        await using (var ctx = new AppDbContext(options))
        {
            using var uow = new UnitOfWork(ctx, NullLogger<UnitOfWork>.Instance);
            var guncelle = await new SandikOzellikGuncelleCommandHandler(uow).Handle(new()
            {
                SandikId = 301, SandikIsmi = "Radyatör", TipId = (int)SandikTipi.AhsapKapali,
                Boy = 1400, En = 1200, Yukseklik = 1375, NetKg = 260, GrossKg = 410
            }, default);
            Assert.True(guncelle.IsSuccess, guncelle.Error?.Message);
        }
        await using (var ctx = new AppDbContext(options))
        {
            using var uow = new UnitOfWork(ctx, NullLogger<UnitOfWork>.Instance);
            var kuyruk = await new AmbalajKaynakSenkronizasyonService(uow, new Finans(false))
                .SenkronizeEtAsync(30, new User(), default);
            Assert.True(kuyruk.IsSuccess, kuyruk.Error?.Message);
            Assert.Equal(1, kuyruk.Value!.Degismeyen);
        }
        await using var verify = new AppDbContext(options);
        var kayit = await verify.Set<AmbalajUretimKaydi>().SingleAsync(k => k.KaynakKayitId == 301);
        Assert.Equal(1000, kayit.En);
        Assert.True(kayit.KaynakSenkronizasyonuKilitliMi);
        return kayit.Id;
    }

    /// <summary>
    /// Finans aktarımı proje tamamlamayı iş kuralıyla reddederse (ör. üretim miktarı aktif siparişin altına düşer)
    /// bütün tamamlama geri alınır: kayıt durumu, tamamlanma tarihi, gerçekleşme ve hareket satırları yazılmaz.
    /// </summary>
    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task ProjeTamamlama_FinansReddindeHicbirKayitDegismez()
    {
        await WithDatabase(async options =>
        {
            await using (var seed = new AppDbContext(options))
            {
                seed.Projeler.Add(new Proje { Id = 40, ProjeNo = "PA-40" });
                seed.Set<AmbalajUretimKaydi>().Add(new AmbalajUretimKaydi
                {
                    Id = 41, ProjeId = 40, SandikNo = "1", Ad = "Radyatör", SandikCinsi = AmbalajSandikCinsi.AhsapKapali,
                    Adet = 1, Boy = 1800, En = 900, Yukseklik = 800, AmbalajaDahil = true
                });
                await seed.SaveChangesAsync();
            }
            await using (var ctx = new AppDbContext(options))
            {
                using var uow = new UnitOfWork(ctx, NullLogger<UnitOfWork>.Instance);
                var form = await Handler(ctx, uow).Handle(new() { ProjeId = 40, KayitIdleri = [41], IdempotencyAnahtari = Guid.NewGuid() }, default);
                Assert.True(form.IsSuccess, form.Error?.Message);
            }
            int hareketSayisi;
            await using (var once = new AppDbContext(options))
                hareketSayisi = await once.Set<AmbalajUretimHareketi>().CountAsync(h => h.AmbalajUretimKaydiId == 41);

            await using (var ctx = new AppDbContext(options))
            {
                using var uow = new UnitOfWork(ctx, NullLogger<UnitOfWork>.Instance);
                var handler = new AmbalajProjeUretimDurumuCommandHandler(uow, new User(), new FinansRed(), new Roles(),
                    new AmbalajKritikIslemDenetimi(uow, new Roles(), new User()));
                var hata = await Assert.ThrowsAsync<IsKuraliException>(
                    () => handler.Handle(new() { ProjeId = 40, Tamamla = true }, default));
                Assert.Equal(409, hata.StatusCode);
            }

            await using var verify = new AppDbContext(options);
            var kayit = await verify.Set<AmbalajUretimKaydi>().SingleAsync(k => k.Id == 41);
            Assert.Equal(AmbalajUretimDurumu.Uretimde, kayit.UretimDurumu);
            Assert.Null(kayit.TamamlanmaTarihi);
            Assert.Empty(await verify.Set<AmbalajUretimGerceklesmesi>().ToListAsync());
            Assert.Equal(hareketSayisi, await verify.Set<AmbalajUretimHareketi>().CountAsync(h => h.AmbalajUretimKaydiId == 41));
        });
    }

    private static AmbalajFormOlusturCommandHandler Handler(AppDbContext ctx, IUnitOfWork uow, bool fail = false) =>
        new(uow, new User(), new Roles(), new Finans(fail), islemKilidi: new AmbalajFormIslemKilidi(ctx));

    private static async Task WithDatabase(Func<DbContextOptions<AppDbContext>, Task> test)
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("THREEK_TEST_POSTGRES"));
        Assert.True(connection.Host is "127.0.0.1" or "localhost");
        Assert.Equal(55439, connection.Port); Assert.Equal("postgres", connection.Database);
        var database = $"uretim_v2_tests_{Guid.NewGuid():N}";
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
                ctx.Kullanicilar.Add(new() { Id = 7, RolId = 1, AdSoyad = "Sentetik Üretim Operatörü", Email = "uretim@example.invalid" });
                ctx.Set<AmbalajUretimKaydi>().Add(new()
                {
                    Id = 1, ManuelProjeNo = "SENTETIK", SandikNo = "1", Ad = "Kontra test", SandikCinsi = AmbalajSandikCinsi.Kontrplak,
                    Adet = 4, Boy = 50, En = 60, Yukseklik = 70, M3HesaplamaVersiyonu = "uygulanmaz-v1"
                });
                await ctx.SaveChangesAsync();
            }
            await test(options);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            // Yalnız bu testin oluşturduğu, sabit önek + GUID isimli sentetik veritabanı kaldırılır.
            Assert.Matches("^uretim_v2_tests_[a-f0-9]{32}$", database);
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
    private sealed class User : ICurrentUserService { public int? UserId => 7; public bool IsAuthenticated => true; }
    private sealed class Roles : IRolService
    {
        public Task<bool> HasUserPermissionAsync(int id, string kod, YetkiTipi tip, CancellationToken ct = default) => Task.FromResult(true);
        public Task<List<MenuTanimi>> GetMenuAgaciAsync(CancellationToken ct = default) => Task.FromResult(new List<MenuTanimi>());
        public Task<List<RolYetki>> GetRolYetkileriAsync(int id, CancellationToken ct = default) => Task.FromResult(new List<RolYetki>());
        public Task YetkileriGuncelleAsync(int id, List<RolYetki> list, CancellationToken ct = default) => Task.CompletedTask;
    }
    private sealed class Finans(bool fail) : IFinansUretimAktarimService
    {
        public Task<FinansSenkronizasyonSonucModel> UretimKayitlariniAktarAsync(IReadOnlyList<FinansUretimAktarimModel> m, CancellationToken ct) =>
            fail ? throw new InvalidOperationException("Sentetik aktarım hatası") : Task.FromResult(new FinansSenkronizasyonSonucModel(0, m.Count, 0));
    }
    private sealed class FinansKaydedici : IFinansUretimAktarimService
    {
        public List<FinansUretimAktarimModel> Modeller { get; } = [];
        public Task<FinansSenkronizasyonSonucModel> UretimKayitlariniAktarAsync(IReadOnlyList<FinansUretimAktarimModel> m, CancellationToken ct)
        {
            Modeller.AddRange(m);
            return Task.FromResult(new FinansSenkronizasyonSonucModel(0, m.Count, 0));
        }
    }
    /// <summary>Finans servisinin kullanıcıya gösterilen iş kuralı reddi (409).</summary>
    private sealed class FinansRed : IFinansUretimAktarimService
    {
        public Task<FinansSenkronizasyonSonucModel> UretimKayitlariniAktarAsync(IReadOnlyList<FinansUretimAktarimModel> m, CancellationToken ct) =>
            throw IsKuraliException.Cakisma("Sentetik: üretim miktarı aktif sipariş miktarının altına düşürülemez.");
    }
}
