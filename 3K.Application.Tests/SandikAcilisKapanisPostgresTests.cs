using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Data.Interceptors;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services;
using _3K.Infrastructure.Services.Raporlar;

namespace _3K.Application.Tests;

/// <summary>
/// Sandık açılış/kapanış olayları gerçek PostgreSQL'de: <see cref="SandikDurumOlayInterceptor"/> izlenmeyen nesneyle
/// Update'te de eski durumu veritabanından okur, otomatik olayı ve kapanma tarihini yazar; sevk ve sevk kilidinin açılması
/// olay üretmez; sebepli elle geri almada ikinci (otomatik) olay yazılmaz; sandık silinince iz kalır. İstatistik
/// kırılımları ve Excel aynı veriden üretilir.
/// </summary>
public sealed class SandikAcilisKapanisPostgresTests
{
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_OlaylarTekKuraldanYazilirIstatistikVeExcelOnlariOkur()
    {
        await FinansPostgresTests.InDatabase(async temel =>
        {
            var options = new DbContextOptionsBuilder<AppDbContext>(temel)
                // Üretimdeki sırayla (DI): olay → denetim alanları → proje kilidi.
                .AddInterceptors(
                    new SandikDurumOlayInterceptor(new OrtakUser(UserId: null)),
                    new AuditInterceptor(new OrtakUser(UserId: null)),
                    new ProjectLockInterceptor()).Options;
            int sandikId, projeId, kullaniciId;
            await using (var db = new AppDbContext(options))
            {
                var kullanici = new Kullanici { AdSoyad = "Depo Sorumlusu", BasHarf = "DS", Email = "ds@ornek.com", RolId = 1 };
                var proje = new Proje { ProjeNo = "PA-OLAY", Musteri = "Örnek Müşteri", ProjeTipiId = (int)ProjeTipi.Normal };
                db.AddRange(kullanici, proje, new Sandik { Proje = proje, SandikNo = "7", DurumId = (int)SandikDurum.Hazirlaniyor });
                await db.SaveChangesAsync();
                (projeId, kullaniciId) = (proje.Id, kullanici.Id);
                sandikId = await db.Sandiklar.Select(s => s.Id).SingleAsync();
                Assert.Empty(db.SandikDurumOlaylari);
            }

            // İzlenmeyen nesneyle Update (çoğu yolun kalıbı): eski durum veritabanından okunur.
            await using (var db = new AppDbContext(options))
            {
                var sandik = await db.Sandiklar.AsNoTracking().SingleAsync();
                sandik.DurumId = (int)SandikDurum.Kapandi;
                db.Update(sandik);
                await db.SaveChangesAsync();
            }
            var kapanma = await KapanmaAsync(options);
            Assert.NotNull(kapanma);
            var ilk = Assert.Single(await OlaylarAsync(options));
            Assert.Equal((SandikDurumOlayTuru.Kapandi, SandikDurumOlayKaynagi.Otomatik, (int?)(int)SandikDurum.Hazirlaniyor, (int?)null),
                (ilk.Tur, ilk.Kaynak, ilk.EskiDurumId, ilk.KullaniciId));

            // Sevk ve sevk kilidinin açılıp sevk öncesi Kapandı'ya dönmesi olay değildir; kapanma tarihi korunur.
            await DurumAsync(options, (int)SandikDurum.Sevkedildi);
            await DurumAsync(options, (int)SandikDurum.Kapandi);
            Assert.Single(await OlaylarAsync(options));
            Assert.Equal(kapanma, await KapanmaAsync(options));

            // Sebepli elle geri alma: tek olay (Elle), kapanma tarihi boşalır.
            await using (var db = new AppDbContext(options))
            {
                using var uow = new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
                var sonuc = await new SandikKapatCommandHandler(uow, new HareketService(uow, db), new OrtakUser(kullaniciId))
                    .Handle(new SandikKapatCommand { SandikId = sandikId, Kapali = false, AcilmaSebebiId = 2, Not = "Kapak değişti" }, default);
                Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
            }
            Assert.Null(await KapanmaAsync(options));
            var olaylar = await OlaylarAsync(options);
            Assert.Equal(2, olaylar.Count);
            var geriAcma = olaylar[1];
            Assert.Equal((SandikDurumOlayTuru.Acildi, SandikDurumOlayKaynagi.Elle, (int?)2, "Kapak değişti", (int?)kullaniciId),
                (geriAcma.Tur, geriAcma.Kaynak, geriAcma.SebepId, geriAcma.Not, geriAcma.KullaniciId));

            // Ürün işlemiyle otomatik geri açılma için önce yeniden kapanır.
            await DurumAsync(options, (int)SandikDurum.Kapandi);
            await DurumAsync(options, (int)SandikDurum.Hazirlaniyor);

            await using (var db = new AppDbContext(options))
            {
                var servis = new SandikDurumIstatistikService(db);
                var gecmis = await servis.SandikGecmisiAsync(sandikId, default);
                Assert.Equal(["Geri açıldı", "Kapandı", "Geri açıldı", "Kapandı"], gecmis.Select(g => g.TurMetni));
                Assert.Equal(("Tadilat", "Depo Sorumlusu", "Kapandı", "Hazırlanıyor"),
                    (gecmis[2].SebepMetni, gecmis[2].KullaniciAdi, gecmis[2].EskiDurumMetni, gecmis[2].YeniDurumMetni));

                // Süzgeç Türkçe harf duyarsızdır (veritabanı ctype C olsa da): "pa-ol" ↔ "PA-OLAY", "ÖRNEK" ↔ "Örnek".
                var kriter = new SandikDurumIstatistikKriteri(null, null, "pa-ol", "ÖRNEK müş");
                var istatistik = await servis.IstatistikAsync(kriter, default);
                Assert.Equal((2, 0, 2, 2, 1, 1, 0, 1, 1),
                    (istatistik.Ozet.KapanmaSayisi, istatistik.Ozet.TopluKapanmaSayisi, istatistik.Ozet.OtomatikKapanmaSayisi,
                     istatistik.Ozet.AcilmaSayisi, istatistik.Ozet.ElleAcilmaSayisi, istatistik.Ozet.OtomatikAcilmaSayisi,
                     istatistik.Ozet.GecmistenAktarilanAcilmaSayisi, istatistik.Ozet.AcilanSandikSayisi, istatistik.Ozet.ProjeSayisi));
                Assert.Equal((1, 2, 4), (istatistik.Ozet.MusteriSayisi, istatistik.Ozet.KisiSayisi, istatistik.Ozet.OlaySayisi));
                Assert.Equal([("Sebepsiz – Otomatik", 1), ("Tadilat", 1)], istatistik.Sebepler.Select(s => (s.Sebep, s.Sayi)));
                var enCok = Assert.Single(istatistik.EnCokAcilanlar);
                Assert.Equal(("7", "Örnek Müşteri", 2, 1, 2),
                    (enCok.SandikNo, enCok.Musteri, enCok.AcilmaSayisi, enCok.ElleAcilmaSayisi, enCok.KapanmaSayisi));

                // Sayfalı tablolar veritabanında gruplanır ve sıralanır.
                var projeler = await servis.ProjelerAsync(kriter, null, null, default);
                var projeSatiri = Assert.Single(projeler.Items);
                Assert.Equal((projeId, "PA-OLAY", 2, 2, 1, 1),
                    (projeSatiri.ProjeId, projeSatiri.ProjeNo, projeSatiri.KapanmaSayisi, projeSatiri.AcilmaSayisi,
                     projeSatiri.ElleAcilmaSayisi, projeSatiri.AcilanSandikSayisi));
                Assert.Equal((1, SayfaKurali.VarsayilanBoyut, 1, 1, false), (projeler.PageNumber, projeler.PageSize, projeler.TotalCount, projeler.TotalPages, projeler.HasNextPage));
                var musteri = Assert.Single((await servis.MusterilerAsync(kriter, 1, 25, default)).Items);
                Assert.Equal(("Örnek Müşteri", 1, 2, 2, 1), (musteri.Musteri, musteri.ProjeSayisi, musteri.KapanmaSayisi, musteri.AcilmaSayisi, musteri.ElleAcilmaSayisi));
                // Kullanıcısız (arka plan) olaylar ayrı satırdır.
                Assert.Equal([("Bilinmiyor / sistem", 2, 1), ("Depo Sorumlusu", 0, 1)],
                    (await servis.KisilerAsync(kriter, 1, 25, default)).Items.Select(k => (k.KullaniciAdi, k.KapatmaSayisi, k.GeriAcmaSayisi)));
                // Olaylar en yeniden eskiye, sayfa sayfa: 4 olay, 3'lük sayfalar; sınır dışı sayfa son sayfaya çekilir.
                var ilkSayfa = await servis.OlaylarAsync(kriter, 1, 3, default);
                Assert.Equal((3, 4, 2, true, false), (ilkSayfa.Items.Count, ilkSayfa.TotalCount, ilkSayfa.TotalPages, ilkSayfa.HasNextPage, ilkSayfa.HasPreviousPage));
                var sonSayfa = await servis.OlaylarAsync(kriter, 9, 3, default);
                Assert.Equal((2, 1, true), (sonSayfa.PageNumber, sonSayfa.Items.Count, sonSayfa.HasPreviousPage));
                Assert.Equal("Kapandı", sonSayfa.Items[0].TurMetni);
                Assert.Equal(gecmis.Select(g => g.Id), ilkSayfa.Items.Concat(sonSayfa.Items).Select(o => o.Id));
                // Boyut üst sınırı aşılamaz.
                Assert.Equal(SayfaKurali.EnFazlaBoyut, (await servis.OlaylarAsync(kriter, 1, 10_000, default)).PageSize);

                // Süzgeç dışı proje ve gelecek tarih aralığı boş döner.
                Assert.Equal(0, (await servis.IstatistikAsync(new(null, null, "yok", null), default)).Ozet.KapanmaSayisi);
                var yarin = TurkeyTime.Now.Date.AddDays(1);
                var bos = await servis.OlaylarAsync(new(yarin, yarin.AddDays(3), null, null), null, null, default);
                Assert.Equal((0, 0, 1), (bos.Items.Count, bos.TotalCount, bos.PageNumber));

                using var excel = new XLWorkbook(new MemoryStream(await servis.ExcelOlusturAsync(new(null, null, null, null), default)));
                Assert.Equal(["Özet", "Sebepler", "Kişiler", "Projeler", "Müşteriler", "En Çok Açılanlar", "Olaylar"],
                    excel.Worksheets.Select(w => w.Name));
                // Başlık satırı ve 4 olay.
                Assert.Equal(5, excel.Worksheet("Olaylar").Column(2).CellsUsed().Count());
            }

            // Sandık silinince bağ boşalır, iz sandık numarasıyla kalır.
            await using (var db = new AppDbContext(options))
            {
                db.Sandiklar.Remove(await db.Sandiklar.SingleAsync());
                await db.SaveChangesAsync();
            }
            Assert.All(await OlaylarAsync(options), o => Assert.Equal((null, "7"), (o.SandikId, o.SandikNo)));
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_TopluZorlaKapatmaVeKapaliEklenenSandik_TekOlayYazar()
    {
        await FinansPostgresTests.InDatabase(async temel =>
        {
            var options = new DbContextOptionsBuilder<AppDbContext>(temel)
                .AddInterceptors(
                    new SandikDurumOlayInterceptor(new OrtakUser(UserId: null)),
                    new AuditInterceptor(new OrtakUser(UserId: null)),
                    new ProjectLockInterceptor()).Options;
            List<int> idler;
            int kullaniciId;
            await using (var db = new AppDbContext(options))
            {
                var kullanici = new Kullanici { AdSoyad = "Paketçi", BasHarf = "P", Email = "p@ornek.com", RolId = 1 };
                var proje = new Proje { ProjeNo = "PA-TOPLU", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Normal };
                db.AddRange(kullanici, proje,
                    new Sandik { Proje = proje, SandikNo = "1", DurumId = (int)SandikDurum.Hazirlaniyor },
                    new Sandik { Proje = proje, SandikNo = "2", DurumId = (int)SandikDurum.Hazirlaniyor },
                    // Yeni projeyle birlikte kapalı eklenen sandık: olay aynı kaydetmede doğru proje ve sandıkla yazılır.
                    new Sandik { Proje = proje, SandikNo = "3", DurumId = (int)SandikDurum.Kapandi });
                await db.SaveChangesAsync();
                idler = await db.Sandiklar.Where(s => s.SandikNo != "3").Select(s => s.Id).ToListAsync();
                kullaniciId = kullanici.Id;
            }

            // Zorla toplu kapatma, döngüde yüklenen izlenen sandıkları kapatır (ikinci okuma "already tracked" verirdi).
            await using (var db = new AppDbContext(options))
            {
                using var uow = new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
                var sonuc = await new _3K.Application.Features.SandikIslemleri.Commands.TopluSandikKapatCommandHandler(
                        uow, new OrtakUser(kullaniciId), new HareketService(uow, db), new OrtakSaha())
                    .Handle(new() { SandikIds = idler, ForceClose = true }, default);
                Assert.True(sonuc.IsSuccess, sonuc.Message);
            }

            await using (var kontrol = new AppDbContext(options))
            {
                var olaylar = await kontrol.SandikDurumOlaylari.AsNoTracking().OrderBy(o => o.SandikNo).ToListAsync();
                Assert.Equal(
                    [("1", SandikDurumOlayKaynagi.Toplu, (int?)kullaniciId), ("2", SandikDurumOlayKaynagi.Toplu, (int?)kullaniciId),
                     ("3", SandikDurumOlayKaynagi.Otomatik, (int?)null)],
                    olaylar.Select(o => (o.SandikNo, o.Kaynak, o.KullaniciId)));
                Assert.All(olaylar, o => Assert.Equal(SandikDurumOlayTuru.Kapandi, o.Tur));
                var sandiklar = await kontrol.Sandiklar.AsNoTracking().ToListAsync();
                Assert.All(sandiklar, s => Assert.Equal((int)SandikDurum.Kapandi, s.DurumId));
                Assert.All(sandiklar, s => Assert.NotNull(s.KapanmaTarihi));
                Assert.All(olaylar, o => Assert.Equal(sandiklar[0].ProjeId, o.ProjeId));
            }
        });
    }

    private static async Task DurumAsync(DbContextOptions<AppDbContext> options, int durumId)
    {
        await using var db = new AppDbContext(options);
        var sandik = await db.Sandiklar.SingleAsync();
        sandik.DurumId = durumId;
        await db.SaveChangesAsync();
    }

    private static async Task<DateTime?> KapanmaAsync(DbContextOptions<AppDbContext> options)
    {
        await using var db = new AppDbContext(options);
        return await db.Sandiklar.Select(s => s.KapanmaTarihi).SingleAsync();
    }

    private static async Task<List<SandikDurumOlayi>> OlaylarAsync(DbContextOptions<AppDbContext> options)
    {
        await using var db = new AppDbContext(options);
        return await db.SandikDurumOlaylari.AsNoTracking().OrderBy(o => o.Id).ToListAsync();
    }
}
