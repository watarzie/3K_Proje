using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Application.Features.ProjeIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

/// <summary>
/// Otomatik sahaya aktarımın gerçek EF çevirisi, saha servisi, Serializable transaction ve FK Restrict sırasıyla
/// doğrulanması: önizle → uygula → aktarımın tamamını geri al → aynı planla yeniden uygula; ret yolunda veri değişmez.
/// </summary>
public sealed class OtomatikSahaAktarimPostgresTests
{
    [SandikPostgresFact]
    [Trait("Category", "Postgres")]
    public async Task UygulaGeriAlYenidenUygula_AyniSandiklarlaVeTekAktifDefterle()
    {
        await using var kurgu = await Kurgu.CreateAsync();

        var ilk = await kurgu.OnizleAsync();
        Assert.True(ilk.AktarilacakVarMi);
        var uygulama = await kurgu.UygulaAsync(ilk.PlanOzeti);
        Assert.True(uygulama.IsSuccess, uygulama.Error?.Message);
        var sahaId = uygulama.Value!.SahaProjeId;

        await using (var db = kurgu.Db())
        {
            var sandiklar = await db.Sandiklar.Where(s => s.ProjeId == sahaId).OrderBy(s => s.SandikNo).ToListAsync();
            Assert.Equal(["1", "3"], sandiklar.Select(s => s.SandikNo));
            // Sandık 3 sandık olarak; 2'nin eksiği ve sandık olarak gidemeyen (işlenmiş) 4'ün eksiği "1" eksikler sandığına.
            Assert.Equal(3, await db.SahaAktarimKalemleri.CountAsync(k => k.DurumId == (int)SahaAktarimDurum.Planlandi));
            Assert.Equal(
                (int)SahaAktarimTipi.SandikBazli,
                (await db.SahaAktarimKalemleri.SingleAsync(k => k.KaynakSandikId == kurgu.SevksizSandikId)).AktarimTipiId);
        }

        var ikinci = await kurgu.OnizleAsync();
        Assert.False(ikinci.AktarilacakVarMi);

        var geriAl = await kurgu.GeriAlAsync(uygulama.Value.SahaAktarimId);
        Assert.True(geriAl.IsSuccess, geriAl.Error?.Message);

        await using (var db = kurgu.Db())
        {
            Assert.Equal(0, await db.CekiSatirlari.CountAsync(cs => cs.KaynakCekiSatiriId.HasValue));
            Assert.Equal(0, await db.SandikIcerikleri.CountAsync(i => i.Sandik.ProjeId == sahaId));
            Assert.All(await db.SahaAktarimKalemleri.ToListAsync(), k =>
            {
                Assert.Equal((int)SahaAktarimDurum.GeriAlindi, k.DurumId);
                Assert.Null(k.SahaCekiSatiriId);
            });
            Assert.All(await db.Sandiklar.Where(s => s.ProjeId == sahaId).ToListAsync(),
                s => Assert.Equal((int)SandikDurum.Bos, s.DurumId));
        }

        var yeniden = await kurgu.OnizleAsync();
        Assert.Equal(
            ilk.Sandiklar.Select(s => (s.KaynakSandikId, s.ToplamAdet)),
            yeniden.Sandiklar.Select(s => (s.KaynakSandikId, s.ToplamAdet)));
        Assert.Equal(
            ilk.EksiklerSandigi!.Urunler.Select(u => (u.CekiSatiriId, u.Miktar)),
            yeniden.EksiklerSandigi!.Urunler.Select(u => (u.CekiSatiriId, u.Miktar)));
        Assert.True(yeniden.EksiklerSandigi.MevcutSandigaEklenir);
        Assert.Equal(sahaId, yeniden.HedefSahaProjeId);
        var ikinciUygulama = await kurgu.UygulaAsync(yeniden.PlanOzeti, sahaId);
        Assert.True(ikinciUygulama.IsSuccess, ikinciUygulama.Error?.Message);

        await using (var db = kurgu.Db())
        {
            Assert.Equal(2, await db.Sandiklar.CountAsync(s => s.ProjeId == sahaId));
            Assert.Equal(3, await db.SahaAktarimKalemleri.CountAsync(k => k.DurumId == (int)SahaAktarimDurum.Planlandi));
        }
    }

    /// <summary>
    /// Aynı önizlemeyle aynı anda iki uygulama (ayrı bağlamlar, Serializable): yalnız biri yazar; öteki çakışma
    /// (serileştirme hatası) ya da plan değişti 409'u alır ve ikinci aktarım kaydı oluşmaz.
    /// </summary>
    [SandikPostgresFact]
    [Trait("Category", "Postgres")]
    public async Task EsZamanliIkiUygulama_YalnizBirAktarimYazar()
    {
        await using var kurgu = await Kurgu.CreateAsync();
        var onizleme = await kurgu.OnizleAsync();

        async Task<bool> Dene()
        {
            try
            {
                return (await kurgu.UygulaAsync(onizleme.PlanOzeti)).IsSuccess;
            }
            catch (Exception hata) when (hata is ConcurrencyConflictException or UniqueConstraintViolationException)
            {
                return false;
            }
        }

        var sonuclar = await Task.WhenAll(Dene(), Dene());

        Assert.Equal(1, sonuclar.Count(x => x));
        await using var db = kurgu.Db();
        Assert.Equal(1, await db.SahaAktarimlari.CountAsync());
        Assert.Equal(3, await db.SahaAktarimKalemleri.CountAsync());
        Assert.Equal(1, await db.Projeler.CountAsync(p => p.ProjeTipiId == (int)ProjeTipi.Saha));
    }

    [SandikPostgresFact]
    [Trait("Category", "Postgres")]
    public async Task PlanDegistiyse_TransactionHicbirKayitBirakmaz()
    {
        await using var kurgu = await Kurgu.CreateAsync();
        var once = await kurgu.SayilarAsync();

        var sonuc = await kurgu.UygulaAsync(new string('a', 64));

        Assert.Equal(409, sonuc.StatusCode);
        Assert.Equal(once, await kurgu.SayilarAsync());
    }

    private sealed class Kurgu : IAsyncDisposable
    {
        private const string VeritabaniOneki = "otomatik_saha_tests_";
        private readonly string _connectionString;
        private Kurgu(string connectionString) => _connectionString = connectionString;
        public int ProjeId { get; private set; }
        public int SevksizSandikId { get; private set; }

        public static async Task<Kurgu> CreateAsync()
        {
            var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("THREEK_TEST_POSTGRES"));
            if (connection.Host is not ("localhost" or "127.0.0.1") || connection.Port != 55439 || connection.Database != "postgres")
                throw new InvalidOperationException("THREEK_TEST_POSTGRES yalnız izole localhost:55439 sunucusunda postgres yönetim bağlantısı olabilir.");
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            var database = VeritabaniOneki + Guid.NewGuid().ToString("N");
            await using (var admin = new NpgsqlConnection(connection.ConnectionString))
            {
                await admin.OpenAsync();
                await using var cmd = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin);
                await cmd.ExecuteNonQueryAsync();
            }
            connection.Database = database;
            var kurgu = new Kurgu(connection.ConnectionString);
            try
            {
                await kurgu.TohumlaAsync();
                return kurgu;
            }
            catch
            {
                await kurgu.DisposeAsync();
                throw;
            }
        }

        /// <summary>
        /// Kısmi sevk edilmiş normal proje: 1 (sevkli, tam), 2 (sevkli, 4 eksik), 3 (sevksiz, işlem görmemiş),
        /// 4 (sevksiz, 3K'ya kısmen gelmiş).
        /// </summary>
        private async Task TohumlaAsync()
        {
            await using var db = Db();
            await db.Database.EnsureCreatedAsync();

            db.Kullanicilar.Add(new Kullanici { Id = 7, AdSoyad = "Test", Email = "otomatik-saha@example.invalid", RolId = 1 });
            var proje = new Proje
            {
                ProjeNo = "PA723-01",
                Musteri = "Müşteri",
                ProjeTipiId = (int)ProjeTipi.Normal,
                DurumId = (int)ProjeDurum.EksikSevkEdildi
            };
            var ceki = new Ceki { Proje = proje, OrijinalDosyaYolu = "ceki.xlsx", YuklemeTarihi = new DateTime(2026, 10, 1, 9, 0, 0) };
            Sandik YeniSandik(string no, bool sevkli) => new()
            {
                Proje = proje,
                SandikNo = no,
                Ad = $"Sandık {no}",
                DurumId = sevkli ? (int)SandikDurum.Sevkedildi : (int)SandikDurum.Hazirlaniyor
            };
            var s1 = YeniSandik("1", true);
            var s2 = YeniSandik("2", true);
            var s3 = YeniSandik("3", false);
            var s4 = YeniSandik("4", false);
            var sira = 0;
            void Urun(Sandik sandik, decimal istenen, decimal gelen)
            {
                sira++;
                var satir = new CekiSatiri
                {
                    Ceki = ceki,
                    SiraNo = sira,
                    BarkodNo = $"B-{sira}",
                    Aciklama = $"Ürün {sira}",
                    IstenenAdet = istenen,
                    GelenMiktar = gelen,
                    GridDurumuId = gelen > 0 ? (int)GridDurum.TamGeldi : (int)GridDurum.Gelmedi,
                    GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi,
                    UcKDurumuId = gelen > 0 ? (int)UcKDurum.EksikGeldi : (int)UcKDurum.Bekliyor,
                    UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor
                };
                db.SandikIcerikleri.Add(new SandikIcerik
                {
                    Sandik = sandik,
                    CekiSatiri = satir,
                    BarkodNo = satir.BarkodNo,
                    TahsisMiktari = istenen,
                    KonulanAdet = gelen,
                    Miktar = istenen
                });
            }

            Urun(s1, 1, 1);
            Urun(s2, 10, 6);
            Urun(s3, 5, 0);
            Urun(s4, 5, 2);
            await db.SaveChangesAsync();

            ProjeId = proje.Id;
            SevksizSandikId = s3.Id;
        }

        public AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_connectionString).Options);

        public async Task<OtomatikSahaAktarimOnizlemeDto> OnizleAsync()
        {
            await using var db = Db();
            using var uow = new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
            var sonuc = await new OtomatikSahaAktarimOnizleQueryHandler(uow, new SandikService(uow, db), new SahaTamamlamaService(db), new LookupStub())
                .Handle(new OtomatikSahaAktarimOnizleQuery { ProjeId = ProjeId }, default);
            Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
            return sonuc.Value!;
        }

        public async Task<Result<OtomatikSahaAktarimSonucDto>> UygulaAsync(string planOzeti, int? hedef = null)
        {
            // Uygulamadaki gibi tek DbContext: depo, iş birimi, sandık ve saha servisleri aynı izleyiciyi paylaşır.
            await using var db = Db();
            using var uow = new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
            return await new OtomatikSahayaAktarCommandHandler(
                    uow, new SandikService(uow, db), new SahaTamamlamaService(db), new HareketService(uow, db), new LookupStub(), new OrtakUser())
                .Handle(new OtomatikSahayaAktarCommand { ProjeId = ProjeId, HedefSahaProjeId = hedef, PlanOzeti = planOzeti }, default);
        }

        public async Task<Result<SahaAktarimiGeriAlSonucDto>> GeriAlAsync(int sahaAktarimId)
        {
            await using var db = Db();
            using var uow = new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
            return await new SahaAktarimiGeriAlCommandHandler(uow, new OrtakUser(), new HareketService(uow, db), new SahaTamamlamaService(db))
                .Handle(new SahaAktarimiGeriAlCommand { SahaAktarimId = sahaAktarimId }, default);
        }

        public async Task<(int Proje, int Sandik, int Satir, int Icerik, int Aktarim, int Kalem, int Hareket)> SayilarAsync()
        {
            await using var db = Db();
            return (
                await db.Projeler.CountAsync(),
                await db.Sandiklar.CountAsync(),
                await db.CekiSatirlari.CountAsync(),
                await db.SandikIcerikleri.CountAsync(),
                await db.SahaAktarimlari.CountAsync(),
                await db.SahaAktarimKalemleri.CountAsync(),
                await db.HareketGecmisleri.CountAsync());
        }

        public async ValueTask DisposeAsync()
        {
            if (!new NpgsqlConnectionStringBuilder(_connectionString).Database!.StartsWith(VeritabaniOneki, StringComparison.Ordinal))
                throw new InvalidOperationException("Test veritabanı sınırı dışında silme reddedildi.");
            await using var db = Db();
            await db.Database.EnsureDeletedAsync();
        }
    }

    private sealed class LookupStub : ILookupCacheService
    {
        public string GetDeger<TLookup>(int id) where TLookup : LookupBase => id.ToString();
        public Task WarmupAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RefreshAsync<TLookup>(CancellationToken ct = default) where TLookup : LookupBase => Task.CompletedTask;
    }
}
