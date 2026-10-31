using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Services;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

/// <summary>
/// Elle eklenen sandığın numara değişikliği gerçek EF izleyicisi, Serializable transaction ve PostgreSQL FK'leriyle:
/// numara ve bağlı kopyalar (çeki satırı, durum olayı), hareket geçmişi ve planlı Ambalaj kaydı tek işlemde değişir;
/// çakışma ve çekiden gelen sandık 409 döner, veritabanında hiçbir satır değişmez.
/// </summary>
public sealed class SandikNoDegisikligiPostgresTests
{
    [SandikPostgresFact]
    [Trait("Category", "Postgres")]
    public async Task ElleEklenenSandik_NumarasiBagliKayitlarlaBirlikteDegisir_RetVeriyiDegistirmez()
    {
        await using var kurgu = await Kurgu.CreateAsync();
        var once = await kurgu.DurumAsync();

        var cakisma = await kurgu.GuncelleAsync(kurgu.ManuelSandikId, "014");
        Assert.Equal((false, 409), (cakisma.IsSuccess, cakisma.StatusCode));
        var cekiden = await kurgu.GuncelleAsync(kurgu.CekiSandikId, "50");
        Assert.Equal((409, SandikNoDegisikligiKurali.CekiSandigiMesaji), (cekiden.StatusCode, cekiden.Error!.Message));
        Assert.Equal(once, await kurgu.DurumAsync());

        var sonuc = await kurgu.GuncelleAsync(kurgu.ManuelSandikId, "21");
        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(once with
        {
            ManuelSandikNo = "21",
            ManuelSatir = ("21", "21"),
            TasinanCekiSatiri = ("5", "21"),
            OlaySandikNo = "21",
            AmbalajSandikNo = "21",
            HareketSayisi = 1
        }, await kurgu.DurumAsync());

        await using var db = kurgu.Db();
        var hareket = await db.HareketGecmisleri.SingleAsync();
        Assert.Equal((SandikNoDegistirmeIslemi.HareketIslemi, "12", "21", 7), (hareket.Islem, hareket.EskiDeger, hareket.YeniDeger, hareket.KullaniciId));
        Assert.Equal("5", (await db.Sandiklar.SingleAsync(s => s.Id == kurgu.CekiSandikId)).SandikNo);
    }

    private sealed record Durum(
        string ManuelSandikNo,
        (string? Cekide, string? Fiili) ManuelSatir,
        (string? Cekide, string? Fiili) TasinanCekiSatiri,
        string OlaySandikNo,
        string AmbalajSandikNo,
        int HareketSayisi);

    private sealed class SessizFinans : IFinansUretimAktarimService
    {
        public Task<FinansSenkronizasyonSonucModel> UretimKayitlariniAktarAsync(
            IReadOnlyList<FinansUretimAktarimModel> modeller, CancellationToken cancellationToken) =>
            Task.FromResult(new FinansSenkronizasyonSonucModel(0, modeller.Count, 0));
    }

    private sealed class Kurgu : IAsyncDisposable
    {
        private const string VeritabaniOneki = "sandik_no_tests_";
        private readonly string _connectionString;
        private Kurgu(string connectionString) => _connectionString = connectionString;
        public int ProjeId { get; private set; }
        public int CekiSandikId { get; private set; }
        public int ManuelSandikId { get; private set; }
        private int _manuelSatirId;
        private int _cekiSatirId;

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
        /// PA-1 (normal): çekiden gelen sandık 5, elle eklenen sandık 12 ve 014. Sandık 12'de elle eklenen satır ve
        /// 5'ten taşınan çeki satırı, bir durum olayı; Ambalaj kayıtları kaynak senkronuyla planlı oluşur.
        /// </summary>
        private async Task TohumlaAsync()
        {
            await using (var db = Db())
            {
                await db.Database.EnsureCreatedAsync();
                var kullanici = new Kullanici { Id = 7, AdSoyad = "Test", Email = "sandik-no@example.invalid", RolId = 1 };
                var proje = new Proje { ProjeNo = "PA-1", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Normal, DurumId = (int)ProjeDurum.Hazirlaniyor };
                var ceki = new Ceki { Proje = proje, OrijinalDosyaYolu = "ceki.xlsx", YuklemeTarihi = new DateTime(2026, 10, 1) };
                var cekiSandik = new Sandik { Proje = proje, SandikNo = "5", DurumId = (int)SandikDurum.Hazirlaniyor };
                var manuel = new Sandik { Proje = proje, SandikNo = "12", Ad = "Manuel", DurumId = (int)SandikDurum.Hazirlaniyor };
                var digerManuel = new Sandik { Proje = proje, SandikNo = "14", DurumId = (int)SandikDurum.Bos };
                var cekiSatiri = new CekiSatiri
                {
                    Ceki = ceki, SiraNo = 1, BarkodNo = "B-1", Aciklama = "Pompa", IstenenAdet = 2, CekideGecenSandikNo = "5", FiiliSandikNo = "12"
                };
                var manuelSatir = new CekiSatiri
                {
                    Ceki = ceki, SiraNo = 9999, BarkodNo = "M-1", Aciklama = "Ek parça", IstenenAdet = 1, IsManuelEklenen = true,
                    CekideGecenSandikNo = "12", FiiliSandikNo = "12"
                };
                db.Kullanicilar.Add(kullanici);
                db.SandikIcerikleri.AddRange(
                    new SandikIcerik { Sandik = manuel, CekiSatiri = cekiSatiri, TahsisMiktari = 2, Miktar = 2 },
                    new SandikIcerik { Sandik = manuel, CekiSatiri = manuelSatir, TahsisMiktari = 1, Miktar = 1 });
                db.Sandiklar.AddRange(cekiSandik, digerManuel);
                db.SandikDurumOlaylari.Add(new SandikDurumOlayi
                {
                    Proje = proje, Sandik = manuel, SandikNo = "12", Tur = SandikDurumOlayTuru.Kapandi, Kaynak = SandikDurumOlayKaynagi.Elle,
                    YeniDurumId = (int)SandikDurum.Kapandi, KullaniciId = 7
                });
                await db.SaveChangesAsync();
                ProjeId = proje.Id;
                CekiSandikId = cekiSandik.Id;
                ManuelSandikId = manuel.Id;
                _manuelSatirId = manuelSatir.Id;
                _cekiSatirId = cekiSatiri.Id;
            }

            await using (var db = Db())
            {
                using var uow = new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
                var senkron = await new AmbalajKaynakSenkronizasyonService(uow, new SessizFinans()).SenkronizeEtAsync(ProjeId, new OrtakUser(), default);
                Assert.True(senkron.IsSuccess, senkron.Error?.Message);
            }
        }

        public AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_connectionString).Options);

        /// <summary>Uygulamadaki gibi tek DbContext: depo, iş birimi, hareket ve Ambalaj senkronu aynı izleyiciyi paylaşır.</summary>
        public async Task<Result> GuncelleAsync(int sandikId, string sandikNo)
        {
            await using var db = Db();
            using var uow = new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
            var sandik = await db.Sandiklar.AsNoTracking().SingleAsync(s => s.Id == sandikId);
            return await new SandikOzellikGuncelleCommandHandler(
                    uow, new HareketService(uow, db), new AmbalajKaynakSenkronizasyonService(uow, new SessizFinans()), new OrtakUser())
                .Handle(new SandikOzellikGuncelleCommand
                {
                    SandikId = sandikId, SandikNo = sandikNo, SandikIsmi = "Değişti", TipId = sandik.TipId
                }, default);
        }

        public async Task<Durum> DurumAsync()
        {
            await using var db = Db();
            var manuel = await db.Sandiklar.AsNoTracking().SingleAsync(s => s.Id == ManuelSandikId);
            var manuelSatir = await db.CekiSatirlari.AsNoTracking().SingleAsync(s => s.Id == _manuelSatirId);
            var cekiSatiri = await db.CekiSatirlari.AsNoTracking().SingleAsync(s => s.Id == _cekiSatirId);
            var olay = await db.SandikDurumOlaylari.AsNoTracking().SingleAsync(o => o.SandikId == ManuelSandikId);
            var ambalaj = await db.Set<AmbalajUretimKaydi>().AsNoTracking().SingleAsync(k => k.KaynakKayitId == ManuelSandikId);
            // Sandık adı da numarayla aynı işlemdedir: ret durumunda "Değişti" yazılmaz.
            Assert.Equal(manuel.SandikNo == "12" ? "Manuel" : "Değişti", manuel.Ad);
            return new Durum(
                manuel.SandikNo,
                (manuelSatir.FiiliSandikNo, manuelSatir.CekideGecenSandikNo),
                (cekiSatiri.CekideGecenSandikNo, cekiSatiri.FiiliSandikNo),
                olay.SandikNo,
                ambalaj.SandikNo,
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
}
