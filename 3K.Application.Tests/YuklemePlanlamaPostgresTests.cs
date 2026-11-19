using Microsoft.EntityFrameworkCore;
using _3K.Application.Features.YuklemePlanlamaIslemleri.Queries;
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
/// önizlemeli, atomik ve revizyonlu içe aktarımı ile doğrulaması.
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

            // Kabul 1: projelerin bütün sandıkları (sevk edilmiş "3" dahil; saha geri bildirimi 19.11.2026 madde 6) seçili
            // gelir; proje ve doğal numara sırasıyla.
            Assert.Equal(["PA600-35-1", "PA600-35-2", "PA600-35-3", "PA600-35-10", "PA600-35-SAHA-1"], plan.Yukler.Select(y => y.Kod));
            Assert.All(plan.Yukler, y => Assert.True(y.Dahil));
            Assert.Equal((1, $"YP-{TurkeyTime.Now.Year}-000001"), (plan.RevizyonNo, plan.PlanNo));
            // Kabul 2: sandık varsayılanları; üst yük sınırı kendi brütü.
            var y2 = plan.Yukler.Single(y => y.SandikId == s2);
            Assert.Equal((false, true, true, 500m), (y2.DondurulebilirMi, y2.UsteKonabilirMi, y2.AltaKalabilirMi, y2.UstYukSiniriKg));
            // Kabul 17: verisi eksik yük gizlenmez, hesaplanmaz; sayaçlar ayrı.
            var y10 = plan.Yukler.Single(y => y.SandikId == s10);
            Assert.Equal(PlanYukDurumu.EksikVeri, y10.Durum);
            Assert.Equal(["Ölçü giriniz."], y10.VeriSorunlari);
            Assert.Equal((5, 5, 4, 1, 0), (plan.Sayaclar.Toplam, plan.Sayaclar.Secilen, plan.Sayaclar.Hesaplanabilir,
                plan.Sayaclar.VeriEksik, plan.Sayaclar.HaricTutulan));
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
            Assert.Equal((2, 7, (int?)40, YuklemeProjeKarisimi.ProjeBazindaAyri), (kayitli.RevizyonNo, kayitli.Yukler.Count, kayitli.Ayarlar.YerlesimBosluguMm, kayitli.ProjeKarisimi));
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
                Assert.Equal((5, true), (ilk.Count, ilk.Single(y => y.SandikId == s2).Dahil));
                // Eski revizyonla kaydetme 409; plana ait olmayan sandık 400; ikisinde de yeni revizyon yazılmaz.
                var cakisma = await Assert.ThrowsAsync<IsKuraliException>(() => PlanServisi(db).KaydetAsync(Kayit(1, secimler), null, Ct));
                Assert.Equal(409, cakisma.StatusCode);
                var yabanciSecim = new List<PlanSandikYukuKaydi>(secimler) { new(yabanci, true, false, true, true, null, PlanYukIzni.Yok) };
                Assert.Equal(400, (await Assert.ThrowsAsync<IsKuraliException>(() => PlanServisi(db).KaydetAsync(Kayit(2, yabanciSecim), null, Ct))).StatusCode);
                Assert.Equal(2, await db.YuklemePlanRevizyonlari.CountAsync(r => r.PlanId == plan.Id));
            }

            // Kaynak değişir: ölçü ve brüt değişir, eksik verili sandık sevk edilir (plan farkı değildir), projeye yeni sandık eklenir.
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
                    [PlanKaynakFarkTuru.OlcuDegisti, PlanKaynakFarkTuru.AgirlikDegisti, PlanKaynakFarkTuru.YeniSandik],
                    acilan.KaynakFarklari.Select(f => f.Tur));
                Assert.Equal(("1.400 × 1.200 × 1.000 mm", "1.500 × 1.200 × 1.000 mm"),
                    (acilan.KaynakFarklari[0].Onceki, acilan.KaynakFarklari[0].Guncel));

                var guncel = await PlanServisi(db).KaynaktanGuncelleAsync(plan.Id, beklenenRevizyonNo: 2, kullaniciId: null, Ct);
                Assert.Equal(3, guncel.RevizyonNo);
                Assert.Empty(guncel.KaynakFarklari);
                Assert.Equal(["PA600-35-1", "PA600-35-2", "PA600-35-3", "PA600-35-10", "PA600-35-11", "PA600-35-SAHA-1", "M-1", "M-1"],
                    guncel.Yukler.Select(y => y.Kod));
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
                Assert.Equal((3, 8, "PA600-35, PA600-35-SAHA"), (ozet.RevizyonNo, ozet.YukSayisi, ozet.Projeler));
                var adaylar = await PlanServisi(db).ProjeAdaylariAsync("pa700", null, null, Ct);
                Assert.Equal((projeC, 1), (Assert.Single(adaylar.Items).ProjeId, adaylar.Items[0].PlanaGelecekSandikSayisi));
            }
        });
    }

    /// <summary>
    /// Tek adımlı plan hesabı (saha geri bildirimi 19.11.2026 madde 5–6): kaydetme adımı olmadan proje + aile/tip + paylarla
    /// plan açılır ve yerleşim aynı revizyonda yazılır; bütün sandıkları sevk edilmiş proje de seçilir ve sandıkları gelir.
    /// Yeniden hesap güncel kaynaktan kurar; eski revizyonla 409; hesaplanacak yük yoksa hiçbir şey yazılmaz.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_PlanHesapla_TekAdimdaPlanVeYerlesim()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            int projeId, olcusuzProje, sandik2;
            await using (var db = new AppDbContext(options))
            {
                db.EkipmanKonfigurasyonlari.Add(new EkipmanKonfigurasyonu
                {
                    Model = new EkipmanModeli { TipId = 2, Uretici = "Test", ModelAdi = "K40" },
                    Kod = "K40DV", Ad = "K40DV", SonRevizyonNo = 1,
                    Revizyonlar =
                    [
                        new EkipmanKonfigurasyonRevizyonu
                        {
                            RevizyonNo = 1, KaynakTuru = EkipmanKaynakTuru.Diger, DogrulamaDurumu = EkipmanDogrulamaDurumu.Dogrulandi,
                            TeknikVeriJson = _3K.YuklemeMotoru.Ekipman.EkipmanTeknikVeriSerilestirici.Yaz(YerlesimMotoruTests.K40.TeknikVeri)
                        }
                    ]
                });
                // Bütün sandıkları sevk edilmiş proje.
                var proje = new Proje { ProjeNo = "PA920-01", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Normal };
                var olcusuz = new Proje { ProjeNo = "PA920-02", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Normal };
                var s2 = Sandik(proje, "2", 1400, 500, (int)SandikDurum.Sevkedildi);
                db.Sandiklar.AddRange(Sandik(proje, "1", 2000, 600, (int)SandikDurum.Sevkedildi), s2, Sandik(olcusuz, "1", null, 100));
                await db.SaveChangesAsync();
                (projeId, olcusuzProje, sandik2) = (proje.Id, olcusuz.Id, s2.Id);
            }

            YuklemePlaniDetayi plan;
            await using (var db = new AppDbContext(options))
            {
                var servis = PlanServisi(db);
                Assert.Equal(2, Assert.Single((await servis.ProjeAdaylariAsync("pa920-01", null, null, Ct)).Items).PlanaGelecekSandikSayisi);

                // Hesaplanacak yük yoksa 400 ve plan açılmaz.
                var bos = await Assert.ThrowsAsync<IsKuraliException>(() =>
                    servis.PlanHesaplaAsync(new YuklemePlaniHesaplaModel(null, null, [olcusuzProje], 1, new YuklemePlanAyarlari()), null, Ct));
                Assert.Equal(400, bos.StatusCode);
                Assert.Equal(0, await db.YuklemePlanlari.CountAsync());

                plan = await servis.PlanHesaplaAsync(new YuklemePlaniHesaplaModel(null, null, [projeId], 1,
                    new YuklemePlanAyarlari { YerlesimBosluguMm = 50, TipIdleri = [2] }), kullaniciId: null, Ct);
            }
            Assert.Equal(("PA920-01", 1, "Hesaplandı.", (int?)50), (plan.Ad, plan.RevizyonNo, plan.RevizyonAciklamasi, plan.Ayarlar.YerlesimBosluguMm));
            // Güvenlik payları kaldırıldı: kontroller paysız (0) yapılır, teyitsiz kalmaz; ağırlık kontrolü uygun yazılır.
            Assert.Equal(YerlesimKontrolSonucu.Uygun, Assert.Single(plan.Yerlesim!.Araclar).Kontroller.Single(k => k.Tur == YerlesimKontrolTuru.Agirlik).Sonuc);
            Assert.Equal([2], plan.Ayarlar.TipIdleri);
            Assert.Equal(["PA920-01-1", "PA920-01-2"], plan.Yukler.Select(y => y.Kod));
            Assert.Equal("K40DV", Assert.Single(plan.Yerlesim!.Araclar).EkipmanKodu);
            Assert.Equal((YuklemeCalismaBicimi.IhtiyacaGoreArac, YuklemeProjeKarisimi.Karisik, YuklemeLokasyonTercihi.Karisik),
                (plan.CalismaBicimi, plan.ProjeKarisimi, plan.LokasyonTercihi));

            // Kaynak değişir; yeniden hesap güncel veriden kurar (ayrı "kaynaktan güncelle" ya da kaydetme yok), tek revizyon.
            await using (var db = new AppDbContext(options))
            {
                (await db.Sandiklar.SingleAsync(s => s.Id == sandik2)).Boy = 1600;
                await db.SaveChangesAsync();
            }
            await using (var db = new AppDbContext(options))
            {
                var servis = PlanServisi(db);
                var eski = await Assert.ThrowsAsync<IsKuraliException>(() => servis.PlanHesaplaAsync(
                    new YuklemePlaniHesaplaModel(plan.Id, 2, [projeId], 1, new YuklemePlanAyarlari()), null, Ct));
                Assert.Equal(409, eski.StatusCode);
                // Aile/tip reddi hiçbir şey yazmaz.
                var tipRed = await Assert.ThrowsAsync<IsKuraliException>(() => servis.PlanHesaplaAsync(
                    new YuklemePlaniHesaplaModel(plan.Id, 1, [projeId], 1, new YuklemePlanAyarlari { TipIdleri = [21] }), null, Ct));
                Assert.Equal(400, tipRed.StatusCode);
                Assert.Equal(1, await db.YuklemePlanRevizyonlari.CountAsync(r => r.PlanId == plan.Id));
                // Yükler arası boşluk istemciden gelir (ekranda, isteğe bağlı); otomatik ad proje kümesiyle güncellenir.
                var yeni = await servis.PlanHesaplaAsync(new YuklemePlaniHesaplaModel(plan.Id, 1, [projeId, olcusuzProje], 1,
                    new YuklemePlanAyarlari { YerlesimBosluguMm = 300 }), null, Ct);
                Assert.Equal((2, "PA920-01, PA920-02", 1600m, (int?)300), (yeni.RevizyonNo, yeni.Ad, yeni.Yukler.Single(y => y.SandikId == sandik2).BoyMm,
                    yeni.Ayarlar.YerlesimBosluguMm));
                Assert.Equal(3, yeni.Yukler.Count);
                Assert.Empty(yeni.KaynakFarklari);
                Assert.NotNull(yeni.Yerlesim);
                Assert.Equal(2, await db.YuklemePlanRevizyonlari.CountAsync(r => r.PlanId == plan.Id));
            }

            // Eski akışla kaydedilmiş plan (elle yük, hariç sandık, proje bazında ayrı): tek adımlı hesap seçimleri
            // varsayılana çeker; seçili projedeki ve projesiz elle yük kalır, çıkarılan projeninki düşer.
            await using (var db = new AppDbContext(options))
            {
                var servis = PlanServisi(db);
                var eski = await servis.OlusturAsync(new YuklemePlaniOlusturModel("Eski plan", YuklemeCalismaBicimi.IhtiyacaGoreArac, 1,
                    YuklemeProjeKarisimi.ProjeBazindaAyri, null, null, [projeId, olcusuzProje]), null, Ct);
                var secimler = eski.Yukler.Select(y => new PlanSandikYukuKaydi(y.SandikId!.Value, y.SandikId != sandik2, false, true, true, y.UstYukSiniriKg, PlanYukIzni.Yok)).ToList();
                eski = await servis.KaydetAsync(new YuklemePlaniKaydetModel(eski.Id, 1, "Eski plan", YuklemeCalismaBicimi.IhtiyacaGoreArac, 1,
                    YuklemeProjeKarisimi.ProjeBazindaAyri, null, null, null, new YuklemePlanAyarlari(), [projeId, olcusuzProje], secimler,
                    [new PlanManuelYukKaydi("M-A", null, (int)YukTuru.DigerOzelYuk, 1000, 800, 600, 300, 1, projeId, null, null, []),
                     new PlanManuelYukKaydi("M-B", null, (int)YukTuru.DigerOzelYuk, 1000, 800, 600, 300, 1, olcusuzProje, null, null, []),
                     new PlanManuelYukKaydi("M-P", null, (int)YukTuru.DigerOzelYuk, 1000, 800, 600, 300, 1, null, null, null, [])]), null, Ct);
                Assert.False(eski.Yukler.Single(y => y.SandikId == sandik2).Dahil);

                var hesap = await servis.PlanHesaplaAsync(new YuklemePlaniHesaplaModel(eski.Id, 2, [projeId], 1, new YuklemePlanAyarlari()), null, Ct);
                Assert.Equal(["PA920-01-1", "PA920-01-2", "M-A", "M-P"], hesap.Yukler.Select(y => y.Kod));
                Assert.All(hesap.Yukler, y => Assert.True(y.Dahil));
                Assert.Equal(("Eski plan", YuklemeProjeKarisimi.Karisik), (hesap.Ad, hesap.ProjeKarisimi));
            }
        });
    }

    private static EkipmanKonfigurasyonu K40Konfigurasyonu() => new()
    {
        Model = new EkipmanModeli { TipId = 2, Uretici = "Test", ModelAdi = "K40" },
        Kod = "K40DV", Ad = "K40DV", SonRevizyonNo = 1,
        Revizyonlar =
        [
            new EkipmanKonfigurasyonRevizyonu
            {
                RevizyonNo = 1, KaynakTuru = EkipmanKaynakTuru.Diger, DogrulamaDurumu = EkipmanDogrulamaDurumu.Dogrulandi,
                TeknikVeriJson = _3K.YuklemeMotoru.Ekipman.EkipmanTeknikVeriSerilestirici.Yaz(YerlesimMotoruTests.K40.TeknikVeri)
            }
        ]
    };

    private static Sandik Sandik(Proje proje, string no, decimal? boy, decimal? brut, DepoLokasyon lokasyon)
    {
        var sandik = Sandik(proje, no, boy, brut);
        sandik.DepoLokasyonId = (int)lokasyon;
        return sandik;
    }

    /// <summary>
    /// Tek ekranlı simülasyon (saha isteği, 20 Kasım 2026): hangi sandıkların yükleneceği ve izinleri kullanıcıdan gelir
    /// (hariç tutulan sandık yerleşime girmez, seçilen projelerde olmayan sandık yok sayılır); yükleme yeri tercihi plana
    /// yazılır ve lokasyona göre ayrı hesapta her araç tek lokasyondan yüklenir; plan verilmeyen hesap aynı projelerin en
    /// son planına revizyon yazar, farklı proje kümesi yeni plan açar.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_PlanHesapla_SandikSecimleriLokasyonTercihiVeAyniProjelerinPlani()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            int projeA, projeB, s1, s2, s3, s4, yabanci;
            await using (var db = new AppDbContext(options))
            {
                db.EkipmanKonfigurasyonlari.Add(K40Konfigurasyonu());
                var a = new Proje { ProjeNo = "PA930-01", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Normal };
                var b = new Proje { ProjeNo = "PA930-02", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Normal };
                var c = new Proje { ProjeNo = "PA930-03", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Normal };
                var sandiklar = new[]
                {
                    Sandik(a, "1", 2000, 600), Sandik(a, "2", 1400, 500), Sandik(a, "3", 1400, 400, DepoLokasyon.Seymen),
                    Sandik(a, "4", 1400, 300, DepoLokasyon.Belirsiz), Sandik(b, "1", 1400, 200), Sandik(c, "1", 1400, 100)
                };
                db.Sandiklar.AddRange(sandiklar);
                await db.SaveChangesAsync();
                (projeA, projeB) = (a.Id, b.Id);
                (s1, s2, s3, s4, yabanci) = (sandiklar[0].Id, sandiklar[1].Id, sandiklar[2].Id, sandiklar[3].Id, sandiklar[5].Id);
            }

            YuklemePlaniHesaplaModel Hesap(IReadOnlyList<int> projeler, int? planId = null, int? revizyon = null,
                YuklemeLokasyonTercihi? tercih = null, IReadOnlyList<PlanSandikYukuKaydi>? secimler = null) =>
                new(planId, revizyon, projeler, 1, new YuklemePlanAyarlari(), tercih, secimler);

            // (1) Sandık seçimleri: 2 hariç, 3'ün izinleri elle değişti; başka projenin sandığı yok sayılır. Tercih
            // gönderilmediği için tek yerden (karışık) hesaplanır.
            var elle = PlanYukIzni.Dondurme | PlanYukIzni.UsteKonma | PlanYukIzni.UstYukSiniri;
            YuklemePlaniDetayi ilk;
            await using (var db = new AppDbContext(options))
            {
                ilk = await PlanServisi(db).PlanHesaplaAsync(Hesap([projeA], secimler:
                [
                    new PlanSandikYukuKaydi(s2, false, false, true, true, 500m, PlanYukIzni.Yok),
                    new PlanSandikYukuKaydi(s3, true, true, false, true, 150m, elle),
                    new PlanSandikYukuKaydi(yabanci, true, true, true, true, 100m, PlanYukIzni.Yok)
                ]), kullaniciId: null, Ct);
            }
            Assert.Equal(["PA930-01-1", "PA930-01-2", "PA930-01-3", "PA930-01-4"], ilk.Yukler.Select(y => y.Kod));
            Assert.Equal((1, YuklemeLokasyonTercihi.Karisik, false), (ilk.RevizyonNo, ilk.LokasyonTercihi, ilk.Yerlesim!.LokasyonaGoreAyri));
            var haric = ilk.Yukler.Single(y => y.SandikId == s2);
            Assert.Equal((false, PlanYukDurumu.HaricTutuldu, (int?)null), (haric.Dahil, haric.Durum, haric.AracNo));
            Assert.DoesNotContain(ilk.Yerlesim.Araclar.SelectMany(a => a.Konumlar), k => k.YukAnahtari == "s:" + s2);
            Assert.DoesNotContain(ilk.Yerlesim.Yerlesemeyenler, y => y.YukAnahtari == "s:" + s2);
            var izinli = ilk.Yukler.Single(y => y.SandikId == s3);
            Assert.Equal((true, true, false, true, 150m, elle, PlanYukDurumu.Yerlesti),
                (izinli.Dahil, izinli.DondurulebilirMi, izinli.UsteKonabilirMi, izinli.AltaKalabilirMi, izinli.UstYukSiniriKg, izinli.ElleDegisenIzinler, izinli.Durum));
            // Seçimi gönderilmeyen sandık dahil ve varsayılan izinlidir (üst yük sınırı kendi brütü).
            var varsayilan = ilk.Yukler.Single(y => y.SandikId == s1);
            Assert.Equal((true, false, true, true, 600m, PlanYukIzni.Yok),
                (varsayilan.Dahil, varsayilan.DondurulebilirMi, varsayilan.UsteKonabilirMi, varsayilan.AltaKalabilirMi, varsayilan.UstYukSiniriKg, varsayilan.ElleDegisenIzinler));
            Assert.Equal((4, 3, 1, 3), (ilk.Sayaclar.Toplam, ilk.Sayaclar.Secilen, ilk.Sayaclar.HaricTutulan, ilk.Sayaclar.Yerlesen));
            // Tek yerden: üç lokasyonun yükü aynı araçta; bilinçli seçim olduğu için uyarı yazılmaz.
            var tekArac = Assert.Single(ilk.Yerlesim.Araclar);
            Assert.Equal((3, 3), (tekArac.Lokasyonlar.Count, ilk.FarkliLokasyonSayisi));
            Assert.Contains(YuklemeEtiketleri.BelirsizLokasyon, tekArac.Lokasyonlar);
            Assert.DoesNotContain(ilk.Yerlesim.Uyarilar, u => u.Contains("lokasyon", StringComparison.OrdinalIgnoreCase));

            await using (var db = new AppDbContext(options))
            {
                // Seçimler satıra yazıldı (ekran modeli değil, saklanan revizyon).
                var satirlar = await db.YuklemePlanYukleri.AsNoTracking().Where(y => y.Revizyon.PlanId == ilk.Id && y.Revizyon.RevizyonNo == 1).ToListAsync();
                Assert.Equal(4, satirlar.Count);
                Assert.False(satirlar.Single(y => y.SandikId == s2).Dahil);
                var saklanan = satirlar.Single(y => y.SandikId == s3);
                Assert.Equal((true, false, 150m, elle), (saklanan.DondurulebilirMi, saklanan.UsteKonabilirMi, saklanan.UstYukSiniriKg, saklanan.ElleDegisenIzinler));
                Assert.DoesNotContain(satirlar, y => y.SandikId == yabanci);
            }

            // (2) + (3) Plan verilmeden aynı projeler: yeni plan açılmaz, aynı planın revizyonu artar (revizyon beklenmez).
            // Lokasyona göre ayrı: her araçta tek lokasyon; tercih plana yazılır. Seçim gönderilmediği için 2 yeniden dahildir.
            YuklemePlaniDetayi ayri;
            await using (var db = new AppDbContext(options))
            {
                ayri = await PlanServisi(db).PlanHesaplaAsync(Hesap([projeA], tercih: YuklemeLokasyonTercihi.LokasyonaGoreAyri), null, Ct);
                Assert.Equal((ilk.Id, ilk.PlanNo, 2), (ayri.Id, ayri.PlanNo, ayri.RevizyonNo));
                Assert.Equal((1, 2), (await db.YuklemePlanlari.CountAsync(), await db.YuklemePlanRevizyonlari.CountAsync()));
                Assert.Equal(YuklemeLokasyonTercihi.LokasyonaGoreAyri, (await db.YuklemePlanlari.AsNoTracking().SingleAsync()).LokasyonTercihi);
            }
            Assert.Equal((YuklemeLokasyonTercihi.LokasyonaGoreAyri, true), (ayri.LokasyonTercihi, ayri.Yerlesim!.LokasyonaGoreAyri));
            Assert.All(ayri.Yukler, y => Assert.True(y.Dahil));
            Assert.Equal(3, ayri.Yerlesim.Araclar.Count);
            Assert.All(ayri.Yerlesim.Araclar, a => Assert.Single(a.Lokasyonlar));
            Assert.Equal(3, ayri.Yerlesim.Araclar.Select(a => a.Lokasyonlar[0]).Distinct().Count());
            // Aynı lokasyondaki iki sandık (1 ve 2) aynı araçtadır.
            Assert.Equal(ayri.Yukler.Single(y => y.SandikId == s1).AracNo, ayri.Yukler.Single(y => y.SandikId == s2).AracNo);
            Assert.NotEqual(ayri.Yukler.Single(y => y.SandikId == s1).AracNo, ayri.Yukler.Single(y => y.SandikId == s3).AracNo);
            Assert.NotEqual(ayri.Yukler.Single(y => y.SandikId == s3).AracNo, ayri.Yukler.Single(y => y.SandikId == s4).AracNo);

            await using (var db = new AppDbContext(options))
            {
                var servis = PlanServisi(db);
                // Farklı proje kümesi yeni plan açar; aynı küme (sıra ve tekrar fark etmez) yine o plana yazar.
                var ikili = await servis.PlanHesaplaAsync(Hesap([projeA, projeB]), null, Ct);
                Assert.NotEqual(ilk.Id, ikili.Id);
                Assert.Equal((1, "PA930-01, PA930-02"), (ikili.RevizyonNo, ikili.Ad));
                var ikiliTekrar = await servis.PlanHesaplaAsync(Hesap([projeB, projeA, projeB]), null, Ct);
                Assert.Equal((ikili.Id, 2), (ikiliTekrar.Id, ikiliTekrar.RevizyonNo));
                // Alt küme başka kümedir: tek projeli hesap ilk plana döner. "Henüz seçilmedi" tek yerden sayılır ve saklanır.
                var tekrar = await servis.PlanHesaplaAsync(Hesap([projeA], tercih: YuklemeLokasyonTercihi.HenuzSecilmedi), null, Ct);
                Assert.Equal((ilk.Id, 3, YuklemeLokasyonTercihi.Karisik), (tekrar.Id, tekrar.RevizyonNo, tekrar.LokasyonTercihi));
                Assert.Single(tekrar.Yerlesim!.Araclar);
                Assert.Equal(2, await db.YuklemePlanlari.CountAsync());

                // Plan açıkça verilirse beklenen revizyon denetimi sürer (409) ve hiçbir şey yazılmaz.
                var cakisma = await Assert.ThrowsAsync<IsKuraliException>(() => servis.PlanHesaplaAsync(Hesap([projeA], ilk.Id, 2), null, Ct));
                Assert.Equal(409, cakisma.StatusCode);
                Assert.Equal(5, await db.YuklemePlanRevizyonlari.CountAsync());

                // Elle değişmemiş (bayraksız) izin istemciden alınmaz: güncel kaynaktan (brüt 600 kg) yeniden kurulur.
                var bayraksiz = await servis.PlanHesaplaAsync(Hesap([projeA, projeB], secimler:
                    [new PlanSandikYukuKaydi(s1, true, true, false, false, 5m, PlanYukIzni.Yok)]), null, Ct);
                Assert.Equal((ikili.Id, 3), (bayraksiz.Id, bayraksiz.RevizyonNo));
                var s1Satiri = bayraksiz.Yukler.Single(y => y.SandikId == s1);
                Assert.Equal((false, true, true, 600m, PlanYukIzni.Yok),
                    (s1Satiri.DondurulebilirMi, s1Satiri.UsteKonabilirMi, s1Satiri.AltaKalabilirMi, s1Satiri.UstYukSiniriKg, s1Satiri.ElleDegisenIzinler));

                // Sahada yüklemesi süren kayda plan verilmeden yazılmaz: aynı projeler için yeni kayıt açılır, eskisi değişmez.
                await servis.BaslatAsync(ikili.Id, 3, null, Ct);
                var oturumVarken = await servis.PlanHesaplaAsync(Hesap([projeA, projeB]), null, Ct);
                Assert.NotEqual(ikili.Id, oturumVarken.Id);
                Assert.Equal(1, oturumVarken.RevizyonNo);
                Assert.Equal(3, await db.YuklemePlanRevizyonlari.CountAsync(r => r.PlanId == ikili.Id));
                Assert.Equal(3, await db.YuklemePlanlari.CountAsync());
            }
        });
    }

    /// <summary>
    /// Yük önizlemesi: seçilen projelerin plana gelecek sandıkları hesapla ile aynı satırlardır (aynı kaynak ve sıra, sevk
    /// edilmişler dahil, varsayılan izinli, verisi eksik olan sorunuyla) ve hiçbir şey yazılmaz.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_YukOnizleme_HesaplaIleAyniSatirlar_Yazmaz()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            int projeA, projeB, olcusuz;
            await using (var db = new AppDbContext(options))
            {
                db.EkipmanKonfigurasyonlari.Add(K40Konfigurasyonu());
                var a = new Proje { ProjeNo = "PA940-01", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Normal };
                var b = new Proje { ProjeNo = "PA940-02", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Saha };
                var c = new Proje { ProjeNo = "PA940-03", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Normal };
                var eksik = Sandik(a, "10", null, 300);
                db.Sandiklar.AddRange(eksik, Sandik(a, "2", 1400, 500, (int)SandikDurum.Sevkedildi), Sandik(a, "1", 2000, 600, DepoLokasyon.Seymen),
                    Sandik(b, "1", 1400, 200, DepoLokasyon.Belirsiz), Sandik(c, "1", 1400, 100));
                await db.SaveChangesAsync();
                (projeA, projeB, olcusuz) = (a.Id, b.Id, eksik.Id);
            }

            PlanYukOnizlemesi onizleme;
            await using (var db = new AppDbContext(options))
            {
                var sonuc = await new YuklemePlaniQueryHandlers(PlanServisi(db)).Handle(new PlanYukOnizlemeQuery { ProjeIdleri = [projeB, projeA] }, Ct);
                Assert.True(sonuc.IsSuccess);
                onizleme = sonuc.Value!;
                // Sorgu durum değiştirmez: plan, revizyon ve yük satırı yazılmadı; bağlamda bekleyen değişiklik yok.
                Assert.False(db.ChangeTracker.HasChanges());
                Assert.Equal((0, 0, 0), (await db.YuklemePlanlari.CountAsync(), await db.YuklemePlanRevizyonlari.CountAsync(), await db.YuklemePlanYukleri.CountAsync()));
                await Assert.ThrowsAsync<KayitBulunamadiException>(() => PlanServisi(db).YukOnizlemeAsync([projeA, 999_999], Ct));
            }
            // Proje ve doğal sandık numarası sırası; sevk edilmiş sandık (2) de gelir; başka projenin sandığı gelmez.
            Assert.Equal(["PA940-01-1", "PA940-01-2", "PA940-01-10", "PA940-02-1"], onizleme.Yukler.Select(y => y.Kod));
            Assert.Equal([1, 2, 3, 4], onizleme.Yukler.Select(y => y.SiraNo));
            Assert.All(onizleme.Yukler, y => Assert.Equal((true, PlanYukKaynagi.Sandik, (int?)null, (string?)null, PlanYukIzni.Yok),
                (y.Dahil, y.Kaynak, y.AracNo, y.YerlesimNotu, y.ElleDegisenIzinler)));
            var eksikSatir = onizleme.Yukler.Single(y => y.SandikId == olcusuz);
            Assert.Equal((PlanYukDurumu.EksikVeri, "Ölçü giriniz."), (eksikSatir.Durum, Assert.Single(eksikSatir.VeriSorunlari)));
            Assert.All(onizleme.Yukler.Where(y => y.SandikId != olcusuz), y => Assert.Equal(PlanYukDurumu.Bekliyor, y.Durum));
            var ilkSatir = onizleme.Yukler[0];
            Assert.Equal((false, true, true, 600m, (int?)(int)DepoLokasyon.Seymen), (ilkSatir.DondurulebilirMi, ilkSatir.UsteKonabilirMi, ilkSatir.AltaKalabilirMi,
                ilkSatir.UstYukSiniriKg, ilkSatir.LokasyonId));
            // Verisi tam yükler üç lokasyonda: Seymen, 3K ve belirsiz (verisi eksik sandık sayılmaz).
            Assert.Equal(3, onizleme.FarkliLokasyonSayisi);

            // Hesap aynı satırları üretir (aynı kaynak, sıra ve varsayılan izinler); farklı lokasyon sayısı aynı hesaptır.
            await using (var db = new AppDbContext(options))
            {
                var hesap = await PlanServisi(db).PlanHesaplaAsync(new YuklemePlaniHesaplaModel(null, null, [projeB, projeA], 1, new YuklemePlanAyarlari()), null, Ct);
                static object Ozet(PlanYukSatiri y) => (y.SiraNo, y.Kaynak, y.SandikId, y.Kod, y.Aciklama, y.ProjeId, y.ProjeNo, y.YukTuruId, y.YukTuruAdi, y.BoyMm, y.EnMm,
                    y.YukseklikMm, y.BrutKg, y.LokasyonId, y.LokasyonAdi, y.Dahil, y.DondurulebilirMi, y.UsteKonabilirMi, y.AltaKalabilirMi, y.UstYukSiniriKg,
                    y.ElleDegisenIzinler, string.Join("|", y.VeriSorunlari));
                Assert.Equal(onizleme.Yukler.Select(Ozet), hesap.Yukler.Select(Ozet));
                Assert.Equal(onizleme.FarkliLokasyonSayisi, hesap.FarkliLokasyonSayisi);
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

    /// <summary>
    /// Faz 2: "Hesapla" yerleşimi yeni revizyona yazar; sandıklar konteynere, trafo gerekçeli lowbed'e gider (kabul 604),
    /// satır durumları yerleşti/eksik veri olur, onaylı kural setinin genişlik sınırı uygulanır. Sonra kaydetme yeni
    /// revizyonu yerleşimsiz açar; eski revizyonla hesap 409; mevcut araç listesi boş "mevcut araçlara yerleştirme" reddedilir.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_Hesapla_YerlesimRevizyondaVeDurumlar()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            int projeId, trafoId;
            await using (var db = new AppDbContext(options))
            {
                EkipmanKonfigurasyonu Konfigurasyon(int tipId, string kod, _3K.YuklemeMotoru.Ekipman.EkipmanTeknikVerisi veri) => new()
                {
                    Model = new EkipmanModeli { TipId = tipId, Uretici = "Test", ModelAdi = kod },
                    Kod = kod, Ad = kod, SonRevizyonNo = 1,
                    Revizyonlar =
                    [
                        new EkipmanKonfigurasyonRevizyonu
                        {
                            RevizyonNo = 1, KaynakTuru = EkipmanKaynakTuru.Diger, DogrulamaDurumu = EkipmanDogrulamaDurumu.Dogrulandi,
                            TeknikVeriJson = _3K.YuklemeMotoru.Ekipman.EkipmanTeknikVeriSerilestirici.Yaz(veri)
                        }
                    ]
                };
                db.EkipmanKonfigurasyonlari.AddRange(
                    Konfigurasyon(2, "K40", YerlesimMotoruTests.K40.TeknikVeri),
                    Konfigurasyon(21, "LB-3", YerlesimMotoruTests.Lowbed.TeknikVeri));
                var proje = new Proje { ProjeNo = "PA800-01", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Normal };
                var trafo = new Sandik
                {
                    Proje = proje, SandikNo = "1", Boy = 6000, En = 3000, Yukseklik = 3800, GrossKg = 45_000, YukTuruId = (int)YukTuru.Trafo,
                    DurumId = (int)SandikDurum.Hazirlaniyor, TipId = (int)SandikTipi.AhsapKapali, DepoLokasyonId = (int)DepoLokasyon.UcK
                };
                db.Sandiklar.AddRange(trafo, Sandik(proje, "2", 1400, 500), Sandik(proje, "3", 1400, 400), Sandik(proje, "4", null, 300));
                await db.SaveChangesAsync();
                (projeId, trafoId) = (proje.Id, trafo.Id);
            }

            YuklemePlaniDetayi plan;
            await using (var db = new AppDbContext(options))
                plan = await PlanServisi(db).OlusturAsync(new YuklemePlaniOlusturModel("Konteyner + trafo", YuklemeCalismaBicimi.IhtiyacaGoreArac, 1,
                    YuklemeProjeKarisimi.Karisik, null, null, [projeId]), kullaniciId: null, Ct);
            Assert.Null(plan.Yerlesim);
            Assert.Equal(1, plan.FarkliLokasyonSayisi);
            // Yerleşim hesaplanmadan rapor üretilmez (400).
            await using (var db = new AppDbContext(options))
            {
                var rapor = await new YuklemePlaniRaporQueryHandlers(PlanServisi(db), new YuklemePlaniPdfOlusturucu())
                    .Handle(new YuklemePlaniAracTalepRaporuQuery { PlanId = plan.Id }, Ct);
                Assert.Equal((false, 400), (rapor.IsSuccess, rapor.StatusCode));
            }

            YuklemePlaniDetayi hesap;
            await using (var db = new AppDbContext(options))
                hesap = await PlanServisi(db).HesaplaAsync(plan.Id, 1, YuklemeLokasyonTercihi.Karisik, kullaniciId: null, Ct);

            Assert.Equal((2, YuklemeLokasyonTercihi.Karisik), (hesap.RevizyonNo, hesap.LokasyonTercihi));
            var yerlesim = Assert.IsType<PlanYerlesimi>(hesap.Yerlesim);
            Assert.Equal(["K40", "LB-3"], yerlesim.Araclar.Select(a => a.EkipmanKodu).Order());
            var lowbed = yerlesim.Araclar.Single(a => a.EkipmanKodu == "LB-3");
            Assert.True(lowbed.AileDisi);
            Assert.Contains("PA800-01-1 trafo için Lowbed gereklidir", lowbed.AileDisiGerekce);
            Assert.Contains("\"yuzeyler\"", lowbed.TeknikVeriJson);
            // Mevzuat kontrolü yapılmaz; teyit edilemeyen kontroller araçta gösterilmez.
            Assert.DoesNotContain(lowbed.Kontroller, k => k.Sonuc == YerlesimKontrolSonucu.TeyitEdilmedi ||
                k.Tur is YerlesimKontrolTuru.YasalGenislik or YerlesimKontrolTuru.YasalYukseklik or YerlesimKontrolTuru.Mevzuat);
            var trafoSatiri = hesap.Yukler.Single(y => y.SandikId == trafoId);
            Assert.Equal((PlanYukDurumu.Yerlesti, lowbed.AracNo), (trafoSatiri.Durum, trafoSatiri.AracNo!.Value));
            Assert.Equal(PlanYukDurumu.EksikVeri, hesap.Yukler.Single(y => y.Kod == "PA800-01-4").Durum);
            Assert.Equal((3, 0, 3), (hesap.Sayaclar.Yerlesen, hesap.Sayaclar.Yerlesemeyen, hesap.Sayaclar.Hesaplanabilir));
            Assert.Equal("s:" + trafoId, Assert.Single(lowbed.Konumlar).YukAnahtari);

            // Raporlar son revizyonun yerleşimiyle aynı plan ve revizyon numarasını taşır.
            await using (var db = new AppDbContext(options))
            {
                var raporlar = new YuklemePlaniRaporQueryHandlers(PlanServisi(db), new YuklemePlaniPdfOlusturucu());
                var talep = await raporlar.Handle(new YuklemePlaniAracTalepRaporuQuery { PlanId = plan.Id }, Ct);
                var yukleme = await raporlar.Handle(new YuklemePlaniYuklemeRaporuQuery { PlanId = plan.Id }, Ct);
                Assert.True(talep.IsSuccess && yukleme.IsSuccess);
                Assert.StartsWith($"{plan.PlanNo}_R2_AracTalebi_", talep.Value!.DosyaAdi, StringComparison.Ordinal);
                Assert.StartsWith($"{plan.PlanNo}_R2_YuklemePlani_", yukleme.Value!.DosyaAdi, StringComparison.Ordinal);
                Assert.Equal(DosyaIcerigi.PdfTuru, yukleme.Value.IcerikTuru);
                Assert.Equal(404, (await raporlar.Handle(new YuklemePlaniYuklemeRaporuQuery { PlanId = 999_999 }, Ct)).StatusCode);
            }

            await using (var db = new AppDbContext(options))
            {
                // Eski revizyonla hesap 409, yeni revizyon yazılmaz.
                Assert.Equal(409, (await Assert.ThrowsAsync<IsKuraliException>(() =>
                    PlanServisi(db).HesaplaAsync(plan.Id, 1, YuklemeLokasyonTercihi.Karisik, null, Ct))).StatusCode);
                Assert.Equal(2, await db.YuklemePlanRevizyonlari.CountAsync(r => r.PlanId == plan.Id));

                // Kaydetme yeni revizyonu yerleşimsiz açar (yeniden hesap gerekir). Mevcut araçlara yerleştirmede araç listesi zorunludur.
                var secimler = hesap.Yukler.Select(y => new PlanSandikYukuKaydi(y.SandikId!.Value, y.Dahil, y.DondurulebilirMi,
                    y.UsteKonabilirMi, y.AltaKalabilirMi, y.UstYukSiniriKg, y.ElleDegisenIzinler)).ToList();
                var kayit = await PlanServisi(db).KaydetAsync(new YuklemePlaniKaydetModel(plan.Id, 2, "Konteyner + trafo",
                    YuklemeCalismaBicimi.MevcutAraclaraYerlestirme, 1, YuklemeProjeKarisimi.Karisik, null, null, null,
                    new YuklemePlanAyarlari(), [projeId], secimler, []), null, Ct);
                Assert.Equal(3, kayit.RevizyonNo);
                Assert.Null(kayit.Yerlesim);
                Assert.All(kayit.Yukler.Where(y => y.Kod != "PA800-01-4"), y => Assert.Equal(PlanYukDurumu.Bekliyor, y.Durum));
                var bos = await Assert.ThrowsAsync<IsKuraliException>(() => PlanServisi(db).HesaplaAsync(plan.Id, 3, YuklemeLokasyonTercihi.Karisik, null, Ct));
                Assert.Equal(YuklemePlaniKurali.MevcutAracGerekliMesaji, bos.Message);
            }
        });
    }

    /// <summary>
    /// Faz 4 elle yerleşim: denetleme kaydetmez; planda olmayan yük ve kullanılamayan ekipman 400, eski revizyon 409;
    /// kaydetme yeni revizyon açar, elle konumları ve boş eklenen aracı saklar, çakışmayı yük sorunu olarak taşır.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_ElleYerlesim_DenetleKaydetVeReddeder()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            int projeId, k40Id, tenteliId;
            await using (var db = new AppDbContext(options))
            {
                // Plan ailesi dışında (tenteli, özel yük ekipmanı değil) ve son yerleşimde kullanılmamış konfigürasyon.
                var tenteli = new EkipmanKonfigurasyonu
                {
                    Model = new EkipmanModeli { TipId = 27, Uretici = "Test", ModelAdi = "TNT" },
                    Kod = "TNT", Ad = "TNT", SonRevizyonNo = 1,
                    Revizyonlar =
                    [
                        new EkipmanKonfigurasyonRevizyonu
                        {
                            RevizyonNo = 1, KaynakTuru = EkipmanKaynakTuru.Diger, DogrulamaDurumu = EkipmanDogrulamaDurumu.Dogrulandi,
                            TeknikVeriJson = _3K.YuklemeMotoru.Ekipman.EkipmanTeknikVeriSerilestirici.Yaz(YerlesimMotoruTests.K40.TeknikVeri)
                        }
                    ]
                };
                db.EkipmanKonfigurasyonlari.Add(tenteli);
                var k40 = new EkipmanKonfigurasyonu
                {
                    Model = new EkipmanModeli { TipId = 2, Uretici = "Test", ModelAdi = "K40" },
                    Kod = "K40", Ad = "K40", SonRevizyonNo = 1,
                    Revizyonlar =
                    [
                        new EkipmanKonfigurasyonRevizyonu
                        {
                            RevizyonNo = 1, KaynakTuru = EkipmanKaynakTuru.Diger, DogrulamaDurumu = EkipmanDogrulamaDurumu.Dogrulandi,
                            TeknikVeriJson = _3K.YuklemeMotoru.Ekipman.EkipmanTeknikVeriSerilestirici.Yaz(YerlesimMotoruTests.K40.TeknikVeri)
                        }
                    ]
                };
                db.EkipmanKonfigurasyonlari.Add(k40);
                var proje = new Proje { ProjeNo = "PA801-01", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Normal };
                db.Sandiklar.AddRange(Sandik(proje, "1", 1400, 500), Sandik(proje, "2", 1400, 400));
                await db.SaveChangesAsync();
                (projeId, k40Id, tenteliId) = (proje.Id, k40.Id, tenteli.Id);
            }

            YuklemePlaniDetayi hesap;
            await using (var db = new AppDbContext(options))
            {
                var plan = await PlanServisi(db).OlusturAsync(new YuklemePlaniOlusturModel("Elle", YuklemeCalismaBicimi.IhtiyacaGoreArac, 1,
                    YuklemeProjeKarisimi.Karisik, null, null, [projeId]), kullaniciId: null, Ct);
                hesap = await PlanServisi(db).HesaplaAsync(plan.Id, 1, YuklemeLokasyonTercihi.Karisik, kullaniciId: null, Ct);
            }
            var otomatik = Assert.Single(hesap.Yerlesim!.Araclar);
            Assert.Equal(2, otomatik.Konumlar.Count);
            List<ElleKonumModeli> Konumlar(PlanAraci a) => a.Konumlar.Select(k => new ElleKonumModeli(k.YukAnahtari, k.XMm, k.YMm, k.ZMm, k.Donduruldu)).ToList();

            await using (var db = new AppDbContext(options))
            {
                var servis = PlanServisi(db);
                // İki yük aynı yerde: denetim çakışmayı yük sorunu olarak döner, revizyon yazılmaz.
                var ilk = otomatik.Konumlar[0];
                var ust = Konumlar(otomatik).Select(k => k with { XMm = ilk.XMm, YMm = ilk.YMm, ZMm = ilk.ZMm }).ToList();
                var denetim = await servis.ElleDenetleAsync(hesap.Id, [new ElleAracModeli(k40Id, ust)], Ct);
                Assert.True(denetim.ElleDuzenlendi);
                var arac = Assert.Single(denetim.Araclar);
                Assert.Equal(YerlesimKontrolSonucu.Uygunsuz, arac.Sonuc);
                Assert.Contains(arac.YukSorunlari, s => s.Sonuc == YerlesimKontrolSonucu.Uygunsuz && s.Mesaj.StartsWith("Çakışma", StringComparison.Ordinal));
                Assert.Equal(2, await db.YuklemePlanRevizyonlari.CountAsync(r => r.PlanId == hesap.Id));

                // Planda olmayan / tekrarlanan yük ve plan ailesinde kullanılamayan ekipman 400.
                var yabanci = await Assert.ThrowsAsync<IsKuraliException>(() => servis.ElleDenetleAsync(hesap.Id,
                    [new ElleAracModeli(k40Id, [new ElleKonumModeli("s:999999", 0, 0, 0, false)])], Ct));
                Assert.Equal((400, YuklemePlaniKurali.ElleYukGecersizMesaji), (yabanci.StatusCode, yabanci.Message));
                var tekrar = Konumlar(otomatik);
                tekrar.Add(tekrar[0]);
                Assert.Equal(400, (await Assert.ThrowsAsync<IsKuraliException>(() => servis.ElleDenetleAsync(hesap.Id, [new ElleAracModeli(k40Id, tekrar)], Ct))).StatusCode);
                var ekipman = await Assert.ThrowsAsync<IsKuraliException>(() => servis.ElleDenetleAsync(hesap.Id, [new ElleAracModeli(999_999, Konumlar(otomatik))], Ct));
                Assert.Equal((400, YuklemePlaniKurali.ElleEkipmanGecersizMesaji), (ekipman.StatusCode, ekipman.Message));
                var aileDisi = await Assert.ThrowsAsync<IsKuraliException>(() => servis.ElleDenetleAsync(hesap.Id, [new ElleAracModeli(tenteliId, Konumlar(otomatik))], Ct));
                Assert.Equal((400, YuklemePlaniKurali.ElleEkipmanGecersizMesaji), (aileDisi.StatusCode, aileDisi.Message));

                // Ret yolunda kaydetme revizyon yazmaz.
                Assert.Equal(400, (await Assert.ThrowsAsync<IsKuraliException>(() => servis.ElleKaydetAsync(
                    new ElleYerlesimModeli(hesap.Id, 2, [new ElleAracModeli(k40Id, [new ElleKonumModeli("s:999999", 0, 0, 0, false)])]), null, Ct))).StatusCode);
                Assert.Equal(2, await db.YuklemePlanRevizyonlari.CountAsync(r => r.PlanId == hesap.Id));

                // Eski revizyonla kaydetme 409 ve yazmaz.
                Assert.Equal(409, (await Assert.ThrowsAsync<IsKuraliException>(() => servis.ElleKaydetAsync(
                    new ElleYerlesimModeli(hesap.Id, 1, [new ElleAracModeli(k40Id, Konumlar(otomatik))]), null, Ct))).StatusCode);
                Assert.Equal(2, await db.YuklemePlanRevizyonlari.CountAsync(r => r.PlanId == hesap.Id));
            }

            // İkinci yük boş eklenen aynı tip araca taşınır; kaydetme yeni revizyonda elle konumları saklar.
            var tasinan = otomatik.Konumlar[1];
            YuklemePlaniDetayi kayit;
            await using (var db = new AppDbContext(options))
                kayit = await PlanServisi(db).ElleKaydetAsync(new ElleYerlesimModeli(hesap.Id, 2,
                [
                    new ElleAracModeli(k40Id, Konumlar(otomatik).Take(1).ToList()),
                    new ElleAracModeli(k40Id, [new ElleKonumModeli(tasinan.YukAnahtari, 0, tasinan.YMm, 0, tasinan.Donduruldu)]),
                    new ElleAracModeli(k40Id, [])
                ]), null, Ct);
            Assert.Equal(3, kayit.RevizyonNo);
            var elle = Assert.IsType<PlanYerlesimi>(kayit.Yerlesim);
            Assert.True(elle.ElleDuzenlendi);
            Assert.Equal(3, elle.Araclar.Count);
            Assert.Equal((0, tasinan.YMm, 0), (elle.Araclar[1].Konumlar.Single().XMm, elle.Araclar[1].Konumlar.Single().YMm, elle.Araclar[1].Konumlar.Single().ZMm));
            Assert.Empty(elle.Araclar[2].Konumlar);
            Assert.Equal(tasinan.YukAnahtari, elle.Araclar[1].Konumlar.Single().YukAnahtari);
            Assert.All(kayit.Yukler, y => Assert.Equal(PlanYukDurumu.Yerlesti, y.Durum));
            Assert.Equal(otomatik.Konumlar[0].XMm, elle.Araclar[0].Konumlar.Single().XMm);

            // Kaydedilen elle yerleşim yeniden okunur ve raporu üretilir.
            await using (var db = new AppDbContext(options))
            {
                var okunan = await PlanServisi(db).PlanGetirAsync(hesap.Id, Ct);
                Assert.True(okunan!.Yerlesim!.ElleDuzenlendi);
                var rapor = await new YuklemePlaniRaporQueryHandlers(PlanServisi(db), new YuklemePlaniPdfOlusturucu())
                    .Handle(new YuklemePlaniYuklemeRaporuQuery { PlanId = hesap.Id }, Ct);
                Assert.True(rapor.IsSuccess);
                Assert.StartsWith($"{hesap.PlanNo}_R3_YuklemePlani_", rapor.Value!.DosyaAdi, StringComparison.Ordinal);
            }

            // Elle eklenebilecek ekipmanlar teknik verisiyle döner (aile dışı tenteli yok). "Kalanı otomatik diz": araçtaki
            // yük yerinde kalır, bekleyen yük aynı araçtaki boşluğa yerleşir; sonuç kaydedilmez.
            await using (var db = new AppDbContext(options))
            {
                var servis = PlanServisi(db);
                var ekipmanlar = await servis.ElleEkipmanlariAsync(hesap.Id, Ct);
                Assert.Contains("\"yuzeyler\"", Assert.Single(ekipmanlar, e => e.KonfigurasyonId == k40Id).TeknikVeriJson);
                Assert.DoesNotContain(ekipmanlar, e => e.KonfigurasyonId == tenteliId);
                await Assert.ThrowsAsync<KayitBulunamadiException>(() => servis.ElleEkipmanlariAsync(999_999, Ct));

                var sabit = elle.Araclar[0].Konumlar.Single();
                var tamam = await servis.ElleTamamlaAsync(hesap.Id,
                    [new ElleAracModeli(k40Id, [new ElleKonumModeli(sabit.YukAnahtari, sabit.XMm, sabit.YMm, sabit.ZMm, sabit.Donduruldu)])], Ct);
                var arac = Assert.Single(tamam.Araclar);
                Assert.Equal(2, arac.Konumlar.Count);
                var korunan = arac.Konumlar.Single(k => k.YukAnahtari == sabit.YukAnahtari);
                Assert.Equal((sabit.XMm, sabit.YMm, sabit.ZMm), (korunan.XMm, korunan.YMm, korunan.ZMm));
                Assert.Empty(tamam.Yerlesemeyenler);
                Assert.True(tamam.ElleDuzenlendi);
                Assert.Equal(3, await db.YuklemePlanRevizyonlari.CountAsync(r => r.PlanId == hesap.Id));
            }
        });
    }

    /// <summary>
    /// Araç yükleme (saha): oturum son revizyonun yerleşimine sabitlenir ve planda tek açık oturum olur; barkod çözme
    /// durum değiştirmez; sıra dışı yük onay ister; aynı yük tekrar ya da iki tabletten aynı anda okutulursa tek kayıt
    /// kalır; geri alma iz bırakır; plan revize edilse de oturum başladığı yerleşimle sürer; kapanan oturuma yazılmaz.
    /// Hiçbir adım sandık durumunu, hareket geçmişini, sevk kayıtlarını ya da plan revizyonlarını değiştirmez.
    /// </summary>
    /// <summary>
    /// İki kademeli araç seçimi (saha geri bildirimi 19.11.2026 madde 1, 3): tanımlar aileleri ve tiplerini ayrı verir
    /// (lowbed kendi ailesinde); tip seçilmeyen planda sade tip (40' DV) kullanılır; 40' High Cube seçilince yalnız o tip
    /// gelir; başka ailenin tipi seçilemez.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_AracTipleri_AileVeTipSecimi()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            int projeId;
            await using (var db = new AppDbContext(options))
            {
                EkipmanKonfigurasyonu Konfig(int tipId, string kod) => new()
                {
                    Model = new EkipmanModeli { TipId = tipId, Uretici = "Test", ModelAdi = kod },
                    Kod = kod, Ad = kod, SonRevizyonNo = 1,
                    Revizyonlar =
                    [
                        new EkipmanKonfigurasyonRevizyonu
                        {
                            RevizyonNo = 1, KaynakTuru = EkipmanKaynakTuru.Diger, DogrulamaDurumu = EkipmanDogrulamaDurumu.Dogrulandi,
                            TeknikVeriJson = _3K.YuklemeMotoru.Ekipman.EkipmanTeknikVeriSerilestirici.Yaz(YerlesimMotoruTests.K40.TeknikVeri)
                        }
                    ]
                };
                // Tip 2 = 40' DV (sıra 1), 3 = 40' High Cube (sıra 2), 13 = 40' Reefer HC (sırasız).
                db.EkipmanKonfigurasyonlari.AddRange(Konfig(2, "K40DV"), Konfig(3, "K40HC"), Konfig(13, "K40RF"));
                var proje = new Proje { ProjeNo = "PA910-01", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Normal };
                db.Sandiklar.AddRange(Sandik(proje, "1", 2000, 500), Sandik(proje, "2", 2000, 500));
                await db.SaveChangesAsync();
                projeId = proje.Id;
            }

            await using (var db = new AppDbContext(options))
            {
                var servis = PlanServisi(db);
                var tanimlar = await servis.TanimlarAsync(Ct);
                Assert.Equal(["Konteyner", "Standart açık kasa / platform", "Lowbed / ağır yük", "Tenteli", "Kapalı kasa / frigo", "Diğer özel amaçlı"],
                    tanimlar.TasimaAileleri.Select(a => a.Ad));
                Assert.Contains(tanimlar.TasimaTipleri, t => t is { Id: 21, AileId: 8, OtomatikSecilir: true });
                Assert.DoesNotContain(tanimlar.TasimaTipleri, t => t.AileId == 2 && t.Ad.Contains("owbed", StringComparison.Ordinal));
                Assert.Contains(tanimlar.TasimaTipleri, t => t is { Id: 13, AileId: 1, OtomatikSecilir: false });
                // Konteyner tipleri sadelik sırasıyla: DV, High Cube, … ; sırasızlar (Reefer) sonda.
                var konteyner = tanimlar.TasimaTipleri.Where(t => t.AileId == 1).ToList();
                Assert.True(konteyner.FindIndex(t => t.Id == 2) < konteyner.FindIndex(t => t.Id == 3));
                Assert.All(konteyner.SkipWhile(t => t.OtomatikSecilir), t => Assert.False(t.OtomatikSecilir));

                var plan = await servis.OlusturAsync(new YuklemePlaniOlusturModel("Tip seçimi", YuklemeCalismaBicimi.IhtiyacaGoreArac, 1,
                    YuklemeProjeKarisimi.Karisik, null, null, [projeId]), kullaniciId: null, Ct);
                var secimler = plan.Yukler.Select(y => new PlanSandikYukuKaydi(y.SandikId!.Value, true, false, false, false, y.UstYukSiniriKg,
                    PlanYukIzni.UsteKonma | PlanYukIzni.AltaKalma)).ToList();
                YuklemePlaniKaydetModel Kaydet(int revizyon, IReadOnlyList<int> tipler) => new(plan.Id, revizyon, "Tip seçimi",
                    YuklemeCalismaBicimi.IhtiyacaGoreArac, 1, YuklemeProjeKarisimi.Karisik, null, null, null,
                    new YuklemePlanAyarlari { TipIdleri = tipler }, [projeId], secimler, []);

                // Tip seçilmeyen planda sade tip: 40' DV (Reefer otomatik gelmez).
                plan = await servis.HesaplaAsync(plan.Id, 1, YuklemeLokasyonTercihi.Karisik, kullaniciId: null, Ct);
                Assert.All(plan.Yerlesim!.Araclar, a => Assert.Equal("K40DV", a.EkipmanKodu));

                // Başka ailenin tipi (21 = lowbed) seçilemez; kayıt yazılmaz.
                var aileDisi = await Assert.ThrowsAsync<IsKuraliException>(() => servis.KaydetAsync(Kaydet(plan.RevizyonNo, [21]), null, Ct));
                Assert.Equal((400, YuklemePlaniKurali.TipAileDisiMesaji), (aileDisi.StatusCode, aileDisi.Message));

                plan = await servis.KaydetAsync(Kaydet(plan.RevizyonNo, [3]), null, Ct);
                Assert.Equal([3], plan.Ayarlar.TipIdleri);
                plan = await servis.HesaplaAsync(plan.Id, plan.RevizyonNo, YuklemeLokasyonTercihi.Karisik, kullaniciId: null, Ct);
                Assert.All(plan.Yerlesim!.Araclar, a => Assert.Equal("K40HC", a.EkipmanKodu));

                // Açıkça seçilen Reefer kullanılır.
                plan = await servis.KaydetAsync(Kaydet(plan.RevizyonNo, [13]), null, Ct);
                plan = await servis.HesaplaAsync(plan.Id, plan.RevizyonNo, YuklemeLokasyonTercihi.Karisik, kullaniciId: null, Ct);
                Assert.All(plan.Yerlesim!.Araclar, a => Assert.Equal("K40RF", a.EkipmanKodu));
            }
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_AracYukleme_OturumIsaretGeriAlVeKapat()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            int projeA, projeB, disSandik;
            await using (var db = new AppDbContext(options))
            {
                db.EkipmanKonfigurasyonlari.Add(new EkipmanKonfigurasyonu
                {
                    Model = new EkipmanModeli { TipId = 2, Uretici = "Test", ModelAdi = "K40" },
                    Kod = "K40", Ad = "K40", SonRevizyonNo = 1,
                    Revizyonlar =
                    [
                        new EkipmanKonfigurasyonRevizyonu
                        {
                            RevizyonNo = 1, KaynakTuru = EkipmanKaynakTuru.Diger, DogrulamaDurumu = EkipmanDogrulamaDurumu.Dogrulandi,
                            TeknikVeriJson = _3K.YuklemeMotoru.Ekipman.EkipmanTeknikVeriSerilestirici.Yaz(YerlesimMotoruTests.K40.TeknikVeri)
                        }
                    ]
                });
                var a = new Proje { ProjeNo = "PA900-01", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Normal };
                var b = new Proje { ProjeNo = "PA900-02", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Normal };
                var c = new Proje { ProjeNo = "PA900-03", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Normal };
                // Üste konamayan sandıklar: her biri ayrı yükleme adımıdır (sıra dışı denemesi için).
                var dis = Sandik(c, "1", 1400, 300);
                db.Sandiklar.AddRange(Sandik(a, "1", 3000, 900), Sandik(a, "2", 2600, 800), Sandik(a, "3", 2200, 700), Sandik(b, "1", 1400, 500), dis);
                await db.SaveChangesAsync();
                (projeA, projeB, disSandik) = (a.Id, b.Id, dis.Id);
            }

            YuklemePlaniDetayi plan;
            await using (var db = new AppDbContext(options))
            {
                var servis = PlanServisi(db);
                plan = await servis.OlusturAsync(new YuklemePlaniOlusturModel("Saha yükleme", YuklemeCalismaBicimi.IhtiyacaGoreArac, 1,
                    YuklemeProjeKarisimi.ProjeBazindaAyri, null, null, [projeA, projeB]), kullaniciId: null, Ct);
                // Yerleşim yokken oturum başlatılamaz ve plan yükleme listesinde görünmez.
                var yerlesimsiz = await Assert.ThrowsAsync<IsKuraliException>(() => servis.BaslatAsync(plan.Id, 1, null, Ct));
                Assert.Equal((400, YuklemeOturumuKurali.YerlesimYokMesaji), (yerlesimsiz.StatusCode, yerlesimsiz.Message));
                Assert.Empty((await servis.YuklenecekPlanlarAsync(null, null, null, Ct)).Items);
                // Ne yüklemesi ne yerleşimi olan plan, listede olduğu gibi durum ucunda da görünmez (404).
                Assert.Null(await servis.DurumAsync(plan.Id, Ct));

                // Üst üste konmasın: her sandık ayrı adım olsun.
                var secimler = plan.Yukler.Select(y => new PlanSandikYukuKaydi(y.SandikId!.Value, true, false, false, false, y.UstYukSiniriKg,
                    PlanYukIzni.UsteKonma | PlanYukIzni.AltaKalma)).ToList();
                plan = await servis.KaydetAsync(new YuklemePlaniKaydetModel(plan.Id, 1, "Saha yükleme", YuklemeCalismaBicimi.IhtiyacaGoreArac, 1,
                    YuklemeProjeKarisimi.ProjeBazindaAyri, null, null, null, new YuklemePlanAyarlari(), [projeA, projeB], secimler, []), null, Ct);
                plan = await servis.HesaplaAsync(plan.Id, 2, YuklemeLokasyonTercihi.Karisik, kullaniciId: null, Ct);
            }
            Assert.Equal((3, 2), (plan.RevizyonNo, plan.Yerlesim!.Araclar.Count));
            var aracA = plan.Yerlesim.Araclar.Single(x => x.ProjeNolari.Contains("PA900-01"));
            var aracB = plan.Yerlesim.Araclar.Single(x => x.ProjeNolari.Contains("PA900-02"));
            var adimlar = YuklemeSirasiKurali.Adimlar(aracA.Konumlar);
            Assert.Equal(3, adimlar.Count);
            var (yuk1, yuk2, yuk3) = (adimlar[0].Yukler.Single().Konum.YukAnahtari, adimlar[1].Yukler.Single().Konum.YukAnahtari, adimlar[2].Yukler.Single().Konum.YukAnahtari);
            int SandikKimligi(string anahtar) => int.Parse(anahtar.AsSpan(2), System.Globalization.CultureInfo.InvariantCulture);
            string Barkod(string anahtar) => BarkodEtiketKurali.SandikBarkodu(SandikKimligi(anahtar));
            var anahtarB = aracB.Konumlar.Single().YukAnahtari;
            YukIsaretleModeli Isaret(int oturumId, string anahtar, int aracNo, bool onay = false, YuklemeIsaretKaynagi kaynak = YuklemeIsaretKaynagi.Barkod) =>
                new(oturumId, aracNo, anahtar, kaynak, onay, kaynak == YuklemeIsaretKaynagi.Barkod ? Barkod(anahtar) : null);

            int oturumId;
            await using (var db = new AppDbContext(options))
            {
                var servis = PlanServisi(db);
                // Eski revizyonla başlatma 409; doğru revizyonla başlar; ikinci başlatma 409 (planda tek açık oturum).
                Assert.Equal(409, (await Assert.ThrowsAsync<IsKuraliException>(() => servis.BaslatAsync(plan.Id, 2, null, Ct))).StatusCode);
                var durum = await servis.BaslatAsync(plan.Id, 3, null, Ct);
                oturumId = durum.Oturum!.Id;
                Assert.Equal((YuklemeOturumDurumu.Acik, 3, 2, 4, 0, false), (durum.Oturum.Durum, durum.Oturum.RevizyonNo, durum.Oturum.AracSayisi,
                    durum.Oturum.ToplamYuk, durum.Oturum.YuklenenYuk, durum.Oturum.PlanDegisti));
                Assert.Equal([(aracA.AracNo, 3, 0), (aracB.AracNo, 1, 0)], durum.Araclar.OrderBy(x => x.AracNo == aracA.AracNo ? 0 : 1).Select(x => (x.AracNo, x.ToplamYuk, x.YuklenenYuk)));
                Assert.False(durum.Baslatilabilir);
                var ikinci = await Assert.ThrowsAsync<IsKuraliException>(() => servis.BaslatAsync(plan.Id, 3, null, Ct));
                Assert.Equal((409, YuklemeOturumuKurali.AcikOturumVarMesaji), (ikinci.StatusCode, ikinci.Message));
                Assert.Equal(1, await db.YuklemeOturumlari.CountAsync());
                // Veritabanı da ikinci açık oturumu reddeder.
                db.YuklemeOturumlari.Add(new YuklemeOturumu { PlanId = plan.Id, RevizyonId = await db.YuklemePlanRevizyonlari.Where(r => r.PlanId == plan.Id && r.RevizyonNo == 3).Select(r => r.Id).SingleAsync(), RevizyonNo = 3, BaslamaTarihi = TurkeyTime.Now });
                await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            }

            await using (var db = new AppDbContext(options))
            {
                var servis = PlanServisi(db);
                Assert.Equal(oturumId, Assert.Single((await servis.YuklenecekPlanlarAsync("saha", null, null, Ct)).Items).AcikOturum!.Id);
                Assert.Equal(oturumId, (await servis.PlanGetirAsync(plan.Id, Ct))!.SurenYukleme!.Id);
                var arac = (await servis.AracAsync(oturumId, aracA.AracNo, Ct))!;
                Assert.Equal((1, 0, 3), (arac.SiradakiAdimNo!.Value, arac.Isaretler.Count, arac.Arac.Konumlar.Count));
                Assert.Null(await servis.AracAsync(oturumId, 99, Ct));
                Assert.Null(await servis.AracAsync(oturumId + 99, aracA.AracNo, Ct));

                // Barkod çözme: geçersiz, plan dışı, başka araç, sırada, sıra dışı. Hiçbiri kayıt yazmaz.
                Assert.Equal(YukCozumSonucu.GecersizBarkod, (await servis.CozumleAsync(oturumId, "FCT00967433", aracA.AracNo, Ct)).Sonuc);
                Assert.Equal(YukCozumSonucu.PlandaYok, (await servis.CozumleAsync(oturumId, BarkodEtiketKurali.SandikBarkodu(disSandik), aracA.AracNo, Ct)).Sonuc);
                var baska = await servis.CozumleAsync(oturumId, Barkod(anahtarB), aracA.AracNo, Ct);
                Assert.Equal((YukCozumSonucu.BaskaAracta, aracB.AracNo), (baska.Sonuc, baska.AracNo!.Value));
                var sirada = await servis.CozumleAsync(oturumId, Barkod(yuk1), aracA.AracNo, Ct);
                Assert.Equal((YukCozumSonucu.Yuklenebilir, yuk1, 1, 1), (sirada.Sonuc, sirada.YukAnahtari, sirada.AdimNo!.Value, sirada.No!.Value));
                Assert.Equal(YukCozumSonucu.SiraDisi, (await servis.CozumleAsync(oturumId, Barkod(yuk3), aracA.AracNo, Ct)).Sonuc);
                Assert.Equal(0, await db.YuklemeOturumYukleri.CountAsync());

                // İşaretleme retleri kayıt yazmaz: onaysız sıra dışı 409, yanlış araç ve plan dışı yük 400, olmayan araç 404.
                Assert.Equal(409, (await Assert.ThrowsAsync<IsKuraliException>(() => servis.IsaretleAsync(Isaret(oturumId, yuk3, aracA.AracNo), null, Ct))).StatusCode);
                Assert.Equal(400, (await Assert.ThrowsAsync<IsKuraliException>(() => servis.IsaretleAsync(Isaret(oturumId, anahtarB, aracA.AracNo), null, Ct))).StatusCode);
                var planDisi = await Assert.ThrowsAsync<IsKuraliException>(() => servis.IsaretleAsync(Isaret(oturumId, $"s:{disSandik}", aracA.AracNo), null, Ct));
                Assert.Equal((400, YuklemeOturumuKurali.PlandaYokMesaji), (planDisi.StatusCode, planDisi.Message));
                await Assert.ThrowsAsync<KayitBulunamadiException>(() => servis.IsaretleAsync(Isaret(oturumId, yuk1, 99), null, Ct));
                // Barkod kaynağı sunucuda doğrulanır: barkodsuz, başka sandığın barkoduyla ya da ürün barkoduyla
                // "barkodla okutuldu" kaydı yazılamaz.
                foreach (var okutulan in new string?[] { null, "", Barkod(yuk2), "FCT00967433" })
                {
                    var uyusmaz = await Assert.ThrowsAsync<IsKuraliException>(() => servis.IsaretleAsync(
                        new YukIsaretleModeli(oturumId, aracA.AracNo, yuk1, YuklemeIsaretKaynagi.Barkod, false, okutulan), null, Ct));
                    Assert.Equal((400, YuklemeOturumuKurali.BarkodUyusmuyorMesaji), (uyusmaz.StatusCode, uyusmaz.Message));
                }
                Assert.Equal(0, await db.YuklemeOturumYukleri.CountAsync());

                // Sıradaki yük işaretlenir; aynı okutma ikinci kez gelirse yeni kayıt yazılmaz.
                var ilk = await servis.IsaretleAsync(Isaret(oturumId, yuk1, aracA.AracNo), null, Ct);
                Assert.Equal((false, 2, 1), (ilk.ZatenYuklendi, ilk.Arac.SiradakiAdimNo!.Value, ilk.Arac.Oturum.YuklenenYuk));
                var tekrar = await servis.IsaretleAsync(Isaret(oturumId, yuk1, aracA.AracNo), null, Ct);
                Assert.Equal((true, 1), (tekrar.ZatenYuklendi, tekrar.Arac.Isaretler.Count));
                var zaten = await servis.CozumleAsync(oturumId, Barkod(yuk1), aracA.AracNo, Ct);
                Assert.Equal((YukCozumSonucu.ZatenYuklendi, ilk.Arac.Isaretler[0].Id), (zaten.Sonuc, zaten.Isaret!.Id));
            }

            // Veritabanı da aynı yükün ikinci etkin işaretini reddeder (kısmi benzersiz indeks).
            await using (var db = new AppDbContext(options))
            {
                db.YuklemeOturumYukleri.Add(new YuklemeOturumYuku
                {
                    OturumId = oturumId, AracNo = aracA.AracNo, YukAnahtari = yuk1, Kod = "x", AdimNo = 1,
                    Kaynak = YuklemeIsaretKaynagi.Elle, YuklemeTarihi = TurkeyTime.Now
                });
                await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            }

            // İki tablet aynı yükü aynı anda okutur (sıra dışı, onaylı): tek etkin kayıt kalır, biri "zaten yüklendi" alır.
            await using (var db1 = new AppDbContext(options))
            await using (var db2 = new AppDbContext(options))
            {
                var sonuclar = await Task.WhenAll(
                    PlanServisi(db1).IsaretleAsync(Isaret(oturumId, yuk3, aracA.AracNo, onay: true), null, Ct),
                    PlanServisi(db2).IsaretleAsync(Isaret(oturumId, yuk3, aracA.AracNo, onay: true, YuklemeIsaretKaynagi.Elle), null, Ct));
                Assert.Single(sonuclar, s => !s.ZatenYuklendi);
            }

            int siraDisiIsaret;
            await using (var db = new AppDbContext(options))
            {
                var servis = PlanServisi(db);
                var kayit = await db.YuklemeOturumYukleri.AsNoTracking().SingleAsync(y => y.YukAnahtari == yuk3);
                Assert.Equal((true, 3, (int?)SandikKimligi(yuk3)), (kayit.SiraDisi, kayit.AdimNo, kayit.SandikId));
                siraDisiIsaret = kayit.Id;

                // Geri alma gerekçe ister, kaydı silmez; yük yeniden işaretlenebilir. Geri alınmış işaret ikinci kez geri alınmaz.
                Assert.Equal(400, (await Assert.ThrowsAsync<IsKuraliException>(() => servis.GeriAlAsync(oturumId, siraDisiIsaret, "x", null, Ct))).StatusCode);
                var geri = await servis.GeriAlAsync(oturumId, siraDisiIsaret, "  yanlış sandık okutuldu  ", null, Ct);
                Assert.Equal((1, 2), (geri.Isaretler.Count, geri.SiradakiAdimNo!.Value));
                var geriAlinan = await db.YuklemeOturumYukleri.AsNoTracking().SingleAsync(y => y.Id == siraDisiIsaret);
                Assert.Equal(("yanlış sandık okutuldu", true), (geriAlinan.GeriAlmaGerekcesi, geriAlinan.GeriAlinmaTarihi != null));
                await Assert.ThrowsAsync<KayitBulunamadiException>(() => servis.GeriAlAsync(oturumId, siraDisiIsaret, "ikinci deneme", null, Ct));
                await servis.IsaretleAsync(Isaret(oturumId, yuk2, aracA.AracNo, kaynak: YuklemeIsaretKaynagi.Elle), null, Ct);
                var yeniden = await servis.IsaretleAsync(Isaret(oturumId, yuk3, aracA.AracNo), null, Ct);
                Assert.Equal((false, (int?)null, 3), (yeniden.ZatenYuklendi, yeniden.Arac.SiradakiAdimNo, yeniden.Arac.Isaretler.Count));
                Assert.Equal((2, 1), (await db.YuklemeOturumYukleri.CountAsync(y => y.YukAnahtari == yuk3),
                    await db.YuklemeOturumYukleri.CountAsync(y => y.YukAnahtari == yuk3 && y.GeriAlinmaTarihi == null)));
            }

            // Plan revize edilir (yeni revizyon yerleşimsiz): oturum başladığı yerleşimle sürer.
            await using (var db = new AppDbContext(options))
            {
                var servis = PlanServisi(db);
                var secimler = plan.Yukler.Select(y => new PlanSandikYukuKaydi(y.SandikId!.Value, y.Dahil, y.DondurulebilirMi,
                    y.UsteKonabilirMi, y.AltaKalabilirMi, y.UstYukSiniriKg, y.ElleDegisenIzinler)).ToList();
                var revize = await servis.KaydetAsync(new YuklemePlaniKaydetModel(plan.Id, 3, "Saha yükleme", YuklemeCalismaBicimi.IhtiyacaGoreArac, 1,
                    YuklemeProjeKarisimi.ProjeBazindaAyri, null, null, null, new YuklemePlanAyarlari(), [projeA, projeB], secimler, []), null, Ct);
                Assert.Equal((4, true, (PlanYerlesimi?)null), (revize.RevizyonNo, revize.SurenYukleme!.PlanDegisti, revize.Yerlesim));
                var durum = (await servis.DurumAsync(plan.Id, Ct))!;
                Assert.Equal((false, false, true, 3, 3), (durum.YerlesimVar, durum.Baslatilabilir, durum.Oturum!.PlanDegisti, durum.Oturum.RevizyonNo, durum.Oturum.YuklenenYuk));
                Assert.Equal(2, durum.Araclar.Count);
                Assert.Equal(3, (await servis.AracAsync(oturumId, aracA.AracNo, Ct))!.Isaretler.Count);
                // Revize edilmiş planda da yükleme sürer.
                Assert.False((await servis.IsaretleAsync(Isaret(oturumId, anahtarB, aracB.AracNo), null, Ct)).ZatenYuklendi);
                await servis.GeriAlAsync(oturumId, (await db.YuklemeOturumYukleri.AsNoTracking().SingleAsync(y => y.YukAnahtari == anahtarB)).Id, "araç değişti", null, Ct);

                // Kapanış: iptal gerekçe ister; eksik yükle tamamlama gerekçe ister. Ret durumunda oturum açık kalır.
                Assert.Equal(400, (await Assert.ThrowsAsync<IsKuraliException>(() => servis.KapatAsync(oturumId, iptal: true, null, null, Ct))).StatusCode);
                var eksik = await Assert.ThrowsAsync<IsKuraliException>(() => servis.KapatAsync(oturumId, iptal: false, null, null, Ct));
                Assert.Equal((400, YuklemeOturumuKurali.EksikYukGerekceMesaji), (eksik.StatusCode, eksik.Message));
                Assert.Equal(YuklemeOturumDurumu.Acik, (await db.YuklemeOturumlari.AsNoTracking().SingleAsync()).Durum);
            }

            // Kapatma ile işaretleme yarışı: oturuma yazan işlemler oturum satırının kilidinde sıraya girer. Kapatma
            // sürerken gelen okutma bekler ve kapanış commit edilince "oturum kapalı" alır; kapanmış oturuma işaret düşmez.
            await using (var kapatan = new AppDbContext(options))
            await using (var okutan = new AppDbContext(options))
            {
                await using var transaction = await kapatan.Database.BeginTransactionAsync();
                await kapatan.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"YuklemeOturumlari\" WHERE \"Id\" = {oturumId} FOR UPDATE");
                var bekleyen = PlanServisi(okutan).IsaretleAsync(Isaret(oturumId, anahtarB, aracB.AracNo), null, Ct);
                var kilitBekleyen = 0;
                for (var deneme = 0; deneme < 100 && kilitBekleyen == 0; deneme++)
                {
                    await Task.Delay(50);
                    kilitBekleyen = (await kapatan.Database.SqlQueryRaw<int>(
                        "SELECT count(*)::int AS \"Value\" FROM pg_stat_activity WHERE datname = current_database() AND wait_event_type = 'Lock'")
                        .ToListAsync())[0];
                }
                Assert.Equal((1, false), (kilitBekleyen, bekleyen.IsCompleted));

                var kapali = await PlanServisi(kapatan).KapatAsync(oturumId, iptal: false, "PA900-02 sandığı sonraki araçla gidecek", null, Ct);
                Assert.Equal((YuklemeOturumDurumu.Tamamlandi, 3, "PA900-02 sandığı sonraki araçla gidecek", false), (kapali.Oturum!.Durum, kapali.Oturum.YuklenenYuk,
                    kapali.Oturum.KapanisGerekcesi, kapali.Baslatilabilir));
                await transaction.CommitAsync();

                var gecKalan = await Assert.ThrowsAsync<IsKuraliException>(() => bekleyen);
                Assert.Equal((409, YuklemeOturumuKurali.OturumKapaliMesaji), (gecKalan.StatusCode, gecKalan.Message));
                Assert.Equal(0, await okutan.YuklemeOturumYukleri.CountAsync(y => y.YukAnahtari == anahtarB && y.GeriAlinmaTarihi == null));
            }

            await using (var db = new AppDbContext(options))
            {
                var servis = PlanServisi(db);
                // Kapanan oturuma yazılmaz ve yeniden kapatılmaz; son revizyonda yerleşim olmadığı için yenisi de başlamaz.
                var yazma = await Assert.ThrowsAsync<IsKuraliException>(() => servis.IsaretleAsync(Isaret(oturumId, anahtarB, aracB.AracNo), null, Ct));
                Assert.Equal((409, YuklemeOturumuKurali.OturumKapaliMesaji), (yazma.StatusCode, yazma.Message));
                Assert.Equal(409, (await Assert.ThrowsAsync<IsKuraliException>(() => servis.GeriAlAsync(oturumId, siraDisiIsaret, "kapalı oturum", null, Ct))).StatusCode);
                Assert.Equal(409, (await Assert.ThrowsAsync<IsKuraliException>(() => servis.KapatAsync(oturumId, iptal: true, "ikinci kapatma", null, Ct))).StatusCode);
                Assert.Equal(409, (await Assert.ThrowsAsync<IsKuraliException>(() => servis.CozumleAsync(oturumId, Barkod(anahtarB), aracB.AracNo, Ct))).StatusCode);
                Assert.Equal(400, (await Assert.ThrowsAsync<IsKuraliException>(() => servis.BaslatAsync(plan.Id, 4, null, Ct))).StatusCode);
                Assert.Empty((await servis.YuklenecekPlanlarAsync(null, null, null, Ct)).Items);
                Assert.Null((await servis.PlanGetirAsync(plan.Id, Ct))!.SurenYukleme);

                // Yükleme işaretleri planlama kaydıdır: sandık, hareket geçmişi, sevk ve plan revizyonu değişmedi.
                Assert.All(await db.Sandiklar.AsNoTracking().ToListAsync(), s => Assert.Equal(((int)SandikDurum.Hazirlaniyor, (DateTime?)null), (s.DurumId, s.UpdatedDate)));
                Assert.Equal((0, 0, 0, 4), (await db.HareketGecmisleri.CountAsync(), await db.Sevkiyatlar.CountAsync(), await db.SevkSeferleri.CountAsync(),
                    await db.YuklemePlanRevizyonlari.CountAsync(r => r.PlanId == plan.Id)));
                // Sandığı sonradan silinen yüklerin bağı boşalır; kapanmış oturumun yerleşimi ve işaretleri yine okunur
                // (iki sandık birden silinince aynı boş anahtar ekranı çökertmemeli).
                var (silinen1, silinen2) = (SandikKimligi(yuk1), SandikKimligi(yuk2));
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"Sandiklar\" WHERE \"Id\" = {silinen1} OR \"Id\" = {silinen2}");
                var silinmis = (await servis.AracAsync(oturumId, aracA.AracNo, Ct))!;
                Assert.Equal((3, 3), (silinmis.Arac.Konumlar.Count, silinmis.Isaretler.Count));
                Assert.Equal(2, (await servis.DurumAsync(plan.Id, Ct))!.Araclar.Count);
                Assert.Equal(2, await db.YuklemeOturumYukleri.CountAsync(y => y.SandikId == null && y.GeriAlinmaTarihi == null));
                // Yükleme kaydı olan plan revizyonu silinemez (FK RESTRICT).
                await Assert.ThrowsAnyAsync<Exception>(() => db.Database.ExecuteSqlRawAsync("DELETE FROM \"YuklemePlanRevizyonlari\" WHERE \"RevizyonNo\" = 3"));
            }
        });
    }
}
