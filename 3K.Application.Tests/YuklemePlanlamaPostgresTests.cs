using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Helpers;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services.YuklemePlanlama;

namespace _3K.Application.Tests;

/// <summary>
/// Yükleme Simülasyonu Faz 1, gerçek PostgreSQL şemasıyla: plan oluşturma (kabul 1, 2, 17), değişmez revizyonla kaydetme
/// ve eşzamanlı kayıt, kaynak farkının plana sessizce yazılmaması ve kaynaktan güncelleme; ekipman kitaplığının
/// önizlemeli, atomik ve revizyonlu içe aktarımı ile doğrulaması; mevzuat kural seti okuması.
/// </summary>
public sealed class YuklemePlanlamaPostgresTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    private static YuklemePlaniService PlanServisi(AppDbContext db) => new(db, new UnitOfWork(db, NullLogger<UnitOfWork>.Instance));

    private static EkipmanKitapligiService EkipmanServisi(AppDbContext db) => new(db, new UnitOfWork(db, NullLogger<UnitOfWork>.Instance));

    private static Sandik Sandik(Proje proje, string no, decimal? boy, decimal? brut, int durum = (int)SandikDurum.Hazirlaniyor) => new()
    {
        Proje = proje, SandikNo = no, Ad = "Sandık " + no, Boy = boy, En = boy == null ? null : 1200, Yukseklik = boy == null ? null : 1000,
        GrossKg = brut, DurumId = durum, TipId = (int)SandikTipi.AhsapKapali, DepoLokasyonId = (int)DepoLokasyon.UcK
    };

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_PlanOlusturKaydetVeKaynaktanGuncelle()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            int projeA, projeB, projeC, s2, s10, yabanci;
            await using (var db = new AppDbContext(options))
            {
                var a = new Proje { ProjeNo = "PA600-35", Musteri = "MÜŞTERİ A", ProjeTipiId = (int)ProjeTipi.Normal };
                var b = new Proje { ProjeNo = "PA600-35-SAHA", Musteri = "MÜŞTERİ A", ProjeTipiId = (int)ProjeTipi.Saha };
                var c = new Proje { ProjeNo = "PA700-01", Musteri = "MÜŞTERİ C", ProjeTipiId = (int)ProjeTipi.Normal };
                var sandiklar = new[]
                {
                    Sandik(a, "10", null, 300), Sandik(a, "2", 1400, 500), Sandik(a, "1", 6000, 110_000),
                    Sandik(a, "3", 1400, 400, (int)SandikDurum.Sevkedildi), Sandik(b, "1", 800, 50), Sandik(c, "1", 900, 60)
                };
                db.Sandiklar.AddRange(sandiklar);
                await db.SaveChangesAsync();
                (projeA, projeB, projeC) = (a.Id, b.Id, c.Id);
                (s10, s2, yabanci) = (sandiklar[0].Id, sandiklar[1].Id, sandiklar[5].Id);
            }

            YuklemePlaniDetayi plan;
            await using (var db = new AppDbContext(options))
            {
                plan = await PlanServisi(db).OlusturAsync(new YuklemePlaniOlusturModel("Wasit sevki", YuklemeCalismaBicimi.IhtiyacaGoreArac, 1,
                    YuklemeProjeKarisimi.Karisik, null, null, [projeA, projeB]), kullaniciId: null, Ct);
            }

            // Kabul 1: projelerin sevk edilmemiş bütün sandıkları seçili gelir; proje ve doğal numara sırasıyla.
            Assert.Equal(["PA600-35-1", "PA600-35-2", "PA600-35-10", "PA600-35-SAHA-1"], plan.Yukler.Select(y => y.Kod));
            Assert.All(plan.Yukler, y => Assert.True(y.Dahil));
            Assert.Equal((1, $"YP-{TurkeyTime.Now.Year}-000001"), (plan.RevizyonNo, plan.PlanNo));
            // Kabul 2: sandık varsayılanları; üst yük sınırı kendi brütü.
            var y2 = plan.Yukler.Single(y => y.SandikId == s2);
            Assert.Equal((false, true, true, 500m), (y2.DondurulebilirMi, y2.UsteKonabilirMi, y2.AltaKalabilirMi, y2.UstYukSiniriKg));
            // Kabul 17: verisi eksik yük gizlenmez, hesaplanmaz; sayaçlar ayrı.
            var y10 = plan.Yukler.Single(y => y.SandikId == s10);
            Assert.Equal(PlanYukDurumu.EksikVeri, y10.Durum);
            Assert.Equal(["Ölçü giriniz."], y10.VeriSorunlari);
            Assert.Equal((4, 4, 3, 1, 0, 1), (plan.Sayaclar.Toplam, plan.Sayaclar.Secilen, plan.Sayaclar.Hesaplanabilir,
                plan.Sayaclar.VeriEksik, plan.Sayaclar.HaricTutulan, plan.Sayaclar.SevkEdilmisSandik));
            Assert.Empty(plan.KaynakFarklari);

            PlanSandikYukuKaydi Secim(PlanYukSatiri y) =>
                new(y.SandikId!.Value, y.Dahil, y.DondurulebilirMi, y.UsteKonabilirMi, y.AltaKalabilirMi, y.UstYukSiniriKg, y.ElleDegisenIzinler);
            List<PlanSandikYukuKaydi> secimler = [.. plan.Yukler.Select(Secim)];
            secimler[secimler.FindIndex(s => s.SandikId == s2)] = new(s2, false, false, true, true, 200m, PlanYukIzni.UstYukSiniri);
            YuklemePlaniKaydetModel Kayit(int beklenen, List<PlanSandikYukuKaydi> sandiklar) => new(plan.Id, beklenen, "Wasit sevki",
                YuklemeCalismaBicimi.IhtiyacaGoreArac, 1, YuklemeProjeKarisimi.ProjeBazindaAyri, null, null, "İlk düzenleme",
                new YuklemePlanAyarlari { YerlesimBosluguMm = 40 }, [projeA, projeB], sandiklar,
                [new PlanManuelYukKaydi("M-1", "Yedek kablo", (int)YukTuru.DigerOzelYuk, 1200, 800, 600, 300, 2, projeA, (int)DepoLokasyon.Seymen, "Forkliftle",
                    [new PlanManuelYukBirimi(2, false, false, true, false, null, PlanYukIzni.AltaKalma)])]);

            YuklemePlaniDetayi kayitli;
            await using (var db = new AppDbContext(options))
                kayitli = await PlanServisi(db).KaydetAsync(Kayit(1, secimler), kullaniciId: null, Ct);
            Assert.Equal((2, 6, 40, YuklemeProjeKarisimi.ProjeBazindaAyri), (kayitli.RevizyonNo, kayitli.Yukler.Count, kayitli.Ayarlar.YerlesimBosluguMm, kayitli.ProjeKarisimi));
            var kayitli2 = kayitli.Yukler.Single(y => y.SandikId == s2);
            Assert.Equal((false, 200m, PlanYukDurumu.HaricTutuldu), (kayitli2.Dahil, kayitli2.UstYukSiniriKg, kayitli2.Durum));
            var manuel = kayitli.Yukler.Where(y => y.Kaynak == PlanYukKaynagi.Manuel).ToList();
            // Adetli elle yük birim birim izlenir; gönderilmeyen birim özel yük varsayılanını (kapalı) alır.
            Assert.Equal([(1, 2, true, false), (2, 2, false, false)], manuel.Select(m => (m.BirimNo, m.BirimSayisi, m.Dahil, m.AltaKalabilirMi)));
            Assert.All(manuel, m => Assert.Equal(("Seymen", "PA600-35"), (m.LokasyonAdi, m.ProjeNo)));

            await using (var db = new AppDbContext(options))
            {
                // Önceki revizyon değişmez.
                var ilk = await db.YuklemePlanYukleri.AsNoTracking()
                    .Where(y => y.Revizyon.PlanId == plan.Id && y.Revizyon.RevizyonNo == 1).ToListAsync();
                Assert.Equal((4, true), (ilk.Count, ilk.Single(y => y.SandikId == s2).Dahil));
                // Eski revizyonla kaydetme 409; plana ait olmayan sandık 400; ikisinde de yeni revizyon yazılmaz.
                var cakisma = await Assert.ThrowsAsync<IsKuraliException>(() => PlanServisi(db).KaydetAsync(Kayit(1, secimler), null, Ct));
                Assert.Equal(409, cakisma.StatusCode);
                var yabanciSecim = new List<PlanSandikYukuKaydi>(secimler) { new(yabanci, true, false, true, true, null, PlanYukIzni.Yok) };
                Assert.Equal(400, (await Assert.ThrowsAsync<IsKuraliException>(() => PlanServisi(db).KaydetAsync(Kayit(2, yabanciSecim), null, Ct))).StatusCode);
                Assert.Equal(2, await db.YuklemePlanRevizyonlari.CountAsync(r => r.PlanId == plan.Id));
            }

            // Kaynak değişir: ölçü ve brüt değişir, eksik verili sandık sevk edilir, projeye yeni sandık eklenir.
            await using (var db = new AppDbContext(options))
            {
                var sandik2 = await db.Sandiklar.SingleAsync(s => s.Id == s2);
                (sandik2.Boy, sandik2.GrossKg) = (1500, 1000);
                (await db.Sandiklar.SingleAsync(s => s.Id == s10)).DurumId = (int)SandikDurum.Sevkedildi;
                db.Sandiklar.Add(Sandik(await db.Projeler.SingleAsync(p => p.Id == projeA), "11", 1000, 250));
                await db.SaveChangesAsync();
            }

            await using (var db = new AppDbContext(options))
            {
                var acilan = (await PlanServisi(db).PlanGetirAsync(plan.Id, Ct))!;
                // Plan sessizce değişmez: kopya eski, farklar listelenir.
                Assert.Equal(1400m, acilan.Yukler.Single(y => y.SandikId == s2).BoyMm);
                Assert.Equal(
                    [PlanKaynakFarkTuru.OlcuDegisti, PlanKaynakFarkTuru.AgirlikDegisti, PlanKaynakFarkTuru.SandikSevkEdildi, PlanKaynakFarkTuru.YeniSandik],
                    acilan.KaynakFarklari.Select(f => f.Tur));
                Assert.Equal(("1.400 × 1.200 × 1.000 mm", "1.500 × 1.200 × 1.000 mm"),
                    (acilan.KaynakFarklari[0].Onceki, acilan.KaynakFarklari[0].Guncel));

                var guncel = await PlanServisi(db).KaynaktanGuncelleAsync(plan.Id, beklenenRevizyonNo: 2, kullaniciId: null, Ct);
                Assert.Equal(3, guncel.RevizyonNo);
                Assert.Empty(guncel.KaynakFarklari);
                Assert.Equal(["PA600-35-1", "PA600-35-2", "PA600-35-11", "PA600-35-SAHA-1", "M-1", "M-1"], guncel.Yukler.Select(y => y.Kod));
                var g2 = guncel.Yukler.Single(y => y.SandikId == s2);
                // Seçim ve elle değişen üst yük korunur; ölçü/brüt yeni kopyadır.
                Assert.Equal((false, 200m, 1500m, 1000m), (g2.Dahil, g2.UstYukSiniriKg, g2.BoyMm, g2.BrutKg));
                Assert.Equal(40, guncel.Ayarlar.YerlesimBosluguMm);
                Assert.Equal(400, (await Assert.ThrowsAsync<IsKuraliException>(() =>
                    PlanServisi(db).KaynaktanGuncelleAsync(plan.Id, 3, null, Ct))).StatusCode);
            }

            await using (var db = new AppDbContext(options))
            {
                var liste = await PlanServisi(db).PlanlarAsync("wasit", null, null, Ct);
                var ozet = Assert.Single(liste.Items);
                Assert.Equal((3, 6, "PA600-35, PA600-35-SAHA"), (ozet.RevizyonNo, ozet.YukSayisi, ozet.Projeler));
                var adaylar = await PlanServisi(db).ProjeAdaylariAsync("pa700", null, null, Ct);
                Assert.Equal((projeC, 1, 0), (Assert.Single(adaylar.Items).ProjeId, adaylar.Items[0].PlanaGelecekSandikSayisi, adaylar.Items[0].SevkEdilmisSandikSayisi));
            }
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_EkipmanIceAktarim_OnizlemeYazmaz_RevizyonluVeDogrulanir()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            EkipmanExcelTipi[] tipler = [new("lowbed", "Lowbed", "Açık kasa / açık platform / lowbed", EkipmanKategorisi.Lowbed)];
            Stream Dosya(params EkipmanExcelKaydi[] kayitlar) => new MemoryStream(EkipmanExcelDosyasi.Yaz(tipler, kayitlar));
            var kayit = YuklemePlanlamaTests.LowbedKaydi();

            await using (var db = new AppDbContext(options))
            {
                var onizleme = await EkipmanServisi(db).IceAktarAsync(Dosya(kayit), uygula: false, kullaniciId: null, Ct);
                Assert.Equal((false, 1, 0), (onizleme.Uygulandi, onizleme.YeniSayisi, onizleme.Sorunlar.Count));
                Assert.Empty(await db.EkipmanKonfigurasyonlari.ToListAsync());

                // Tanımsız tip: uygulamada da hiçbir şey yazılmaz.
                var hatali = await EkipmanServisi(db).IceAktarAsync(Dosya(kayit with { TipKodu = "yok" }), uygula: true, kullaniciId: null, Ct);
                Assert.False(hatali.Uygulandi);
                Assert.Contains("TipKodu 'yok'", Assert.Single(hatali.Sorunlar).Mesaj);
                Assert.Empty(await db.EkipmanModelleri.ToListAsync());

                var ilk = await EkipmanServisi(db).IceAktarAsync(Dosya(kayit), uygula: true, kullaniciId: null, Ct);
                Assert.Equal((true, 1), (ilk.Uygulandi, ilk.YeniSayisi));
            }

            await using (var db = new AppDbContext(options))
            {
                var ayni = await EkipmanServisi(db).IceAktarAsync(Dosya(kayit), uygula: true, kullaniciId: null, Ct);
                Assert.Equal((1, 0, 0), (ayni.DegismeyenSayisi, ayni.YeniRevizyonSayisi, ayni.YeniSayisi));
                var degisen = kayit with { TeknikVeri = kayit.TeknikVeri with { TeknikKapasiteKg = 42_000 } };
                var ikinci = await EkipmanServisi(db).IceAktarAsync(Dosya(degisen), uygula: true, kullaniciId: null, Ct);
                Assert.Equal((1, 2), (ikinci.YeniRevizyonSayisi, Assert.Single(ikinci.Konfigurasyonlar).RevizyonNo));
            }

            await using (var db = new AppDbContext(options))
            {
                var konf = await db.EkipmanKonfigurasyonlari.AsNoTracking().SingleAsync();
                Assert.Equal((2, 1), (konf.SonRevizyonNo, await db.EkipmanModelleri.CountAsync()));
                // Yalnız son revizyon doğrulanır; doğrulanmış revizyon tekrar doğrulanmaz.
                Assert.Equal(409, (await Assert.ThrowsAsync<IsKuraliException>(() => EkipmanServisi(db).DogrulaAsync(konf.Id, 1, 7, Ct))).StatusCode);
                var dogrulanan = await EkipmanServisi(db).DogrulaAsync(konf.Id, 2, 7, Ct);
                Assert.Equal((EkipmanDogrulamaDurumu.Dogrulandi, 2), (dogrulanan.Revizyon.DogrulamaDurumu, dogrulanan.Revizyonlar.Count));
                Assert.Equal(EkipmanDogrulamaDurumu.Dogrulanmadi, dogrulanan.Revizyonlar.Single(r => r.RevizyonNo == 1).DogrulamaDurumu);
                Assert.Equal(409, (await Assert.ThrowsAsync<IsKuraliException>(() => EkipmanServisi(db).DogrulaAsync(konf.Id, 2, 7, Ct))).StatusCode);
                Assert.Equal((YuklemeYontemi.Yandan, true), (dogrulanan.YuklemeYontemleri, dogrulanan.SandikTasimayaUygun));
                Assert.Empty(dogrulanan.VeriUyarilari);
            }

            await using (var db = new AppDbContext(options))
            {
                var liste = await EkipmanServisi(db).KonfigurasyonlariListeleAsync(
                    new EkipmanListeKriteri("lb-3", null, null, EkipmanDogrulamaDurumu.Dogrulandi, false), null, null, Ct);
                var satir = Assert.Single(liste.Items);
                Assert.Equal(("LB-3D", 42_000, 15_000, "Lowbed"), (satir.Kod, satir.TeknikKapasiteKg, satir.DisUzunlukMm, satir.TipAdi));
                // Dışa aktarılan dosya yeniden içe aktarılınca değişiklik yoktur.
                var disa = await EkipmanServisi(db).DisaAktarAsync(Ct);
                var tekrar = await EkipmanServisi(db).IceAktarAsync(new MemoryStream(disa), uygula: false, kullaniciId: null, Ct);
                Assert.Equal((1, 0), (tekrar.DegismeyenSayisi, tekrar.Sorunlar.Count));
            }
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_MevzuatKuralSeti_KurallarSiraylaVeKaynaklariyla()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            int setId;
            await using (var db = new AppDbContext(options))
            {
                var kaynak = new MevzuatKaynagi { YetkiAlani = "TR", Ad = "Yönetmelik", Tur = MevzuatKaynakTuru.Yonetmelik };
                var set = new MevzuatKuralSeti
                {
                    YetkiAlani = "TR", Ad = "TR kuralları", SurumNo = 1, GecerlilikBaslangici = new DateTime(2026, 11, 5),
                    Kurallar =
                    [
                        new MevzuatKurali { Kod = "B", Tip = MevzuatKuralTipi.AzamiYukseklik, ParametrelerJson = "{\"genelMm\":4000}", Aciklama = "y", Sira = 2, Kaynak = kaynak },
                        new MevzuatKurali { Kod = "A", Tip = MevzuatKuralTipi.AzamiGenislik, ParametrelerJson = "{\"genelMm\":2550}", Aciklama = "g", Sira = 1, Kaynak = kaynak }
                    ]
                };
                db.MevzuatKuralSetleri.Add(set);
                await db.SaveChangesAsync();
                setId = set.Id;
                // Parametre JSON nesne olmalıdır (veritabanı kısıtı).
                db.MevzuatKurallari.Add(new MevzuatKurali { KuralSetiId = set.Id, Kod = "C", Tip = MevzuatKuralTipi.Isaretleme, ParametrelerJson = "[]", Aciklama = "x" });
                await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            }

            await using (var db = new AppDbContext(options))
            {
                var servis = new MevzuatKuralService(db);
                var ozet = Assert.Single(await servis.KuralSetleriAsync(Ct));
                Assert.Equal((MevzuatKuralSetiDurumu.Taslak, 2), (ozet.Durum, ozet.KuralSayisi));
                var detay = (await servis.KuralSetiGetirAsync(setId, Ct))!;
                Assert.Equal(["A", "B"], detay.Kurallar.Select(k => k.Kod));
                Assert.Equal("Yönetmelik", Assert.Single(detay.Kaynaklar).Ad);
                Assert.Null(await servis.KuralSetiGetirAsync(setId + 99, Ct));
            }
        });
    }
}
