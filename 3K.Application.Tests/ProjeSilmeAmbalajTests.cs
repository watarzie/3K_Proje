using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

/// <summary>
/// Proje silme ile ambalaj üretim kayıtları: proje yüklenince her sandık için oluşan dokunulmamış otomatik
/// kayıtlar proje ile birlikte silinir (normal, saha, yedek); üretime alınmış kayıt, elle eklenmiş aktif sandık
/// veya finans izi varsa silme engellenir ve hiçbir veri silinmez. FK davranışı gerçek PostgreSQL'de doğrulanır.
/// </summary>
public sealed class ProjeSilmeAmbalajTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Dokunulmamis_otomatik_ambalaj_kayitlari_her_proje_tipinde_proje_ile_birlikte_silinir()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var kullanici = Kullanici();
            var korunan = Proje("KORUNAN", ProjeTipi.Normal);
            db.AddRange(kullanici, korunan);
            await db.SaveChangesAsync();
            var korunanKayit = await OtomatikKayitEkleAsync(db, korunan, kullanici, "K-1");

            foreach (var tip in new[] { ProjeTipi.Normal, ProjeTipi.Saha, ProjeTipi.Yedek })
            {
                var proje = Proje($"YANLIS-{tip}", tip);
                db.Add(proje);
                await db.SaveChangesAsync();
                var dis = await OtomatikKayitEkleAsync(db, proje, kullanici, "1");
                await OtomatikKayitEkleAsync(db, proje, kullanici, "2");
                // İç sandık bağı (RESTRICT öz-ilişki) ve kullanıcının iptal ettiği bağımsız kayıt da temizlenir.
                db.Add(new AmbalajUretimKaydi
                {
                    ProjeId = proje.Id, SandikNo = "1-IC", UstKayitId = dis.Id, BagimsizKayitMi = true,
                    IptalMi = true, IptalNedeni = "Kullanıcı tarafından silindi."
                });
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();

                Assert.True(await Servis(db).SilAsync(proje.Id, Ct));
                db.ChangeTracker.Clear();

                Assert.False(await db.Projeler.AnyAsync(p => p.Id == proje.Id));
                Assert.False(await db.AmbalajUretimKayitlari.AnyAsync(k => k.ProjeId == proje.Id));
                Assert.False(await db.AmbalajUretimHareketleri.AnyAsync(h => h.AmbalajUretimKaydiId == dis.Id));
            }

            // Başka projenin kaydı ve geçmişi etkilenmez.
            Assert.True(await db.AmbalajUretimKayitlari.AnyAsync(k => k.Id == korunanKayit.Id));
            Assert.True(await db.AmbalajUretimHareketleri.AnyAsync(h => h.AmbalajUretimKaydiId == korunanKayit.Id));
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Ilerlemis_ambalaj_veya_finans_izi_olan_proje_silinmez_ve_hicbir_kayit_silinmez()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var kullanici = Kullanici();
            db.Add(kullanici);
            await db.SaveChangesAsync();

            async Task EngellenirAsync(string projeNo, Func<Proje, AmbalajUretimKaydi, Task> hazirla, string beklenenEngel)
            {
                var proje = Proje(projeNo, ProjeTipi.Yedek);
                db.Add(proje);
                await db.SaveChangesAsync();
                var kayit = await OtomatikKayitEkleAsync(db, proje, kullanici, "1");
                await hazirla(proje, kayit);
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();

                var hata = await Assert.ThrowsAsync<ReferentialIntegrityConflictException>(
                    () => Servis(db).SilAsync(proje.Id, Ct));
                Assert.Contains("Proje silinemez", hata.Message);
                Assert.Contains(beklenenEngel, hata.Message);

                db.ChangeTracker.Clear();
                Assert.True(await db.Projeler.AnyAsync(p => p.Id == proje.Id));
                Assert.True(await db.Sandiklar.AnyAsync(s => s.ProjeId == proje.Id));
                Assert.True(await db.AmbalajUretimKayitlari.AnyAsync(k => k.Id == kayit.Id));
                Assert.True(await db.AmbalajUretimHareketleri.AnyAsync(h => h.AmbalajUretimKaydiId == kayit.Id));
            }

            await EngellenirAsync("URETIMDE", (_, kayit) =>
            {
                db.Attach(kayit);
                kayit.Boy = 1000; kayit.En = 1000; kayit.Yukseklik = 1000;
                kayit.UretimeAlindi = true;
                kayit.UretimDurumu = AmbalajUretimDurumu.Uretimde;
                return Task.CompletedTask;
            }, "1 üretime alınmış ambalaj kaydı");

            await EngellenirAsync("ELLE-SANDIK", (proje, _) =>
            {
                db.Add(new AmbalajUretimKaydi { ProjeId = proje.Id, SandikNo = "OZEL-1", BagimsizKayitMi = true });
                return Task.CompletedTask;
            }, "1 elle eklenmiş ambalaj sandığı");

            await EngellenirAsync("FINANSLI", (proje, kayit) =>
            {
                db.Add(FinansIsi(proje.ProjeNo, kayit.IsAkisKimligi));
                return Task.CompletedTask;
            }, "1 finans iş kaydı");
        });
    }

    private static ProjeSilmeService Servis(AppDbContext db) => new(
        db,
        new SahaTamamlamaService(db),
        new SahaAktarimSilmeKorumaService(db),
        new SahteSse(),
        NullLogger<ProjeSilmeService>.Instance);

    private static Proje Proje(string no, ProjeTipi tip) =>
        new() { ProjeNo = no, Musteri = "QA", ProjeTipiId = (int)tip };

    private static Kullanici Kullanici() =>
        new() { AdSoyad = "Sentetik kullanıcı", Email = "silme@example.invalid", SifreHash = "synthetic", RolId = 1 };

    /// <summary>Senkronizasyonun proje yüklenince oluşturduğu gibi: sandık + planlı otomatik kayıt + oluşum hareketi.</summary>
    private static async Task<AmbalajUretimKaydi> OtomatikKayitEkleAsync(AppDbContext db, Proje proje, Kullanici kullanici, string sandikNo)
    {
        var sandik = new Sandik { ProjeId = proje.Id, SandikNo = sandikNo, Ad = "Sentetik sandık" };
        db.Add(sandik);
        await db.SaveChangesAsync();
        var kaynakModul = proje.ProjeTipiId switch
        {
            (int)ProjeTipi.Saha => AmbalajKaynakModulu.Saha,
            (int)ProjeTipi.Yedek => AmbalajKaynakModulu.Yedek,
            _ => AmbalajKaynakModulu.Sandik
        };
        var kayit = new AmbalajUretimKaydi
        {
            ProjeId = proje.Id, KaynakModul = kaynakModul, KaynakKayitId = sandik.Id, SandikNo = sandikNo,
            UretimDurumu = AmbalajUretimDurumu.Planlandi
        };
        db.Add(kayit);
        await db.SaveChangesAsync();
        db.Add(new AmbalajUretimHareketi
        {
            AmbalajUretimKaydiId = kayit.Id, KullaniciId = kullanici.Id, Islem = "Kaynak sandık ambalaj üretimine aktarıldı",
            AlanAdi = "Kayit"
        });
        await db.SaveChangesAsync();
        return kayit;
    }

    private static FinansIsKaydi FinansIsi(string projeNo, Guid akis) => new()
    {
        IsAdi = "Sentetik ambalaj işi", ProjeNo = projeNo, Musteri = "QA", IsTuru = FinansIsTuru.OzelIs,
        KaynakTuru = FinansKaynakTurleri.AmbalajUretim, KaynakKayitId = akis.ToString("D"), KaynakAktif = true,
        Adet = 1, Birim = "Adet", FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.SabitTutar, BirimFiyatSnapshot = 100,
        ParaBirimiSnapshot = "EUR", KdvOraniSnapshot = 20, UretimTarihi = new DateTime(2026, 10, 1),
        FinansTarihi = new DateTime(2026, 10, 1), FinansDonemi = new DateTime(2026, 10, 1), KayitTarihi = new DateTime(2026, 10, 1)
    };

    private sealed class SahteSse : ISseNotifier
    {
        public Task SubscribeAsync(object context, int kullaniciId) => Task.CompletedTask;
        public Task NotifyUsersAsync(IEnumerable<int> kullaniciIdleri, string eventName, string data = "refresh") => Task.CompletedTask;
        public Task BroadcastApprovalUpdateAsync() => Task.CompletedTask;
    }
}
