using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using _3K.Application.Common;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

/// <summary>
/// Saha projesinde sandıklar arası taşımanın gerçek EF izleyicisi, gerçek saha senkronizasyonu ve PostgreSQL
/// transaction'ıyla doğrulanması. Canlı hata (2026-10-20): ilk satırdan sonra saha senkronizasyonu kaynak
/// sandıktaki bütün aktarım kalemlerini izlemeye alıyordu; ikinci satır aynı kalemin izlenmeyen kopyasını
/// Update edince EF "already being tracked" hatası 500 dönüyordu. Sahte depo bu izleme davranışını göstermez.
/// </summary>
public sealed class SandikTopluTasimaSahaPostgresTests
{
    [SandikPostgresFact]
    [Trait("Category", "Postgres")]
    public async Task AyniSandiktakiAktarimKalemleri_TekIstekteTasininca_KalemlerFizikselHedefiIzler()
    {
        await using var kurgu = await SahaKurgu.CreateAsync();

        var sonuc = await kurgu.TopluTasiAsync(kurgu.Istek(1, 1, 1, 1));

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        await using var db = kurgu.Db();
        var kalemSandiklari = await db.SahaAktarimKalemleri
            .ToDictionaryAsync(k => k.SahaCekiSatiriId!.Value, k => k.SahaSandikId);
        Assert.Equal(kurgu.HedefId, kalemSandiklari[kurgu.SatirIdleri[0]]);
        Assert.Equal(kurgu.HedefId, kalemSandiklari[kurgu.SatirIdleri[1]]);
        Assert.Equal(kurgu.HedefId, kalemSandiklari[kurgu.SatirIdleri[2]]);
        // Parçalı tahsiste tek sandık gerçeği yoktur; taşınmayan kalem kaynakta kalır.
        Assert.Null(kalemSandiklari[kurgu.SatirIdleri[3]]);
        Assert.Equal(kurgu.KaynakId, kalemSandiklari[kurgu.TasinmayanSatirId]);

        var kaynak = await db.SandikIcerikleri.Where(i => i.SandikId == kurgu.KaynakId).ToListAsync();
        var hedef = await db.SandikIcerikleri.Where(i => i.SandikId == kurgu.HedefId).ToListAsync();
        Assert.Equal(
            new[] { kurgu.SatirIdleri[3], kurgu.TasinmayanSatirId },
            kaynak.Select(i => i.CekiSatiriId!.Value).Order());
        Assert.All(kaynak, i => Assert.Equal(1m, i.TahsisMiktari));
        Assert.Equal(kurgu.SatirIdleri.Order(), hedef.Select(i => i.CekiSatiriId!.Value).Order());
        Assert.All(hedef, i => Assert.Equal(1m, i.TahsisMiktari));
        Assert.Equal(6m, kaynak.Sum(i => i.TahsisMiktari) + hedef.Sum(i => i.TahsisMiktari));
        Assert.Equal(4, await db.SandikUrunTransferleri.CountAsync());
        Assert.Single(await db.SandikTopluTasimaIslemleri.ToListAsync());
        Assert.Equal((int)SandikDurum.Hazirlaniyor, (await db.Sandiklar.SingleAsync(s => s.Id == kurgu.HedefId)).DurumId);
    }

    [SandikPostgresFact]
    [Trait("Category", "Postgres")]
    public async Task SonSatirReddedilince_OncekiSatirlarinKalemVeTahsisDegisiklikleriGeriAlinir()
    {
        await using var kurgu = await SahaKurgu.CreateAsync();

        // İlk üç satır taşınıp saha senkronizasyonu çalıştıktan sonra son satır tahsisi aşar.
        var sonuc = await kurgu.TopluTasiAsync(kurgu.Istek(1, 1, 1, 3));

        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains($"#{kurgu.IcerikIdleri[3]}", sonuc.Error!.Message);
        await kurgu.BaslangicKorunurAsync();
    }

    [SandikPostgresFact]
    [Trait("Category", "Postgres")]
    public async Task TekilTasima_AyniKalemKuraliniUygular()
    {
        await using var kurgu = await SahaKurgu.CreateAsync();

        var sonuc = await kurgu.TekilTasiAsync(kurgu.IcerikIdleri[0], 1);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        await using var db = kurgu.Db();
        var kalemSandiklari = await db.SahaAktarimKalemleri
            .ToDictionaryAsync(k => k.SahaCekiSatiriId!.Value, k => k.SahaSandikId);
        Assert.Equal(kurgu.HedefId, kalemSandiklari[kurgu.SatirIdleri[0]]);
        Assert.All(
            kalemSandiklari.Where(k => k.Key != kurgu.SatirIdleri[0]),
            k => Assert.Equal(kurgu.KaynakId, k.Value));
    }

    /// <summary>
    /// Canlıdaki veri şekli: saha projesinde her içerik ayrı saha çeki satırına ve ayrı aktarım kalemine bağlıdır;
    /// kalemlerin hepsi aynı kaynak saha sandığını gösterir. Dördüncü satırın tahsisi 2'dir (parçalı taşıma),
    /// beşinci satır taşınmaz.
    /// </summary>
    private sealed class SahaKurgu : IAsyncDisposable
    {
        private const string VeritabaniOneki = "sandik_saha_toplu_tests_";
        private readonly string _connectionString;
        private SahaKurgu(string connectionString) => _connectionString = connectionString;
        public int ProjeId { get; private set; }
        public int KaynakId { get; private set; }
        public int HedefId { get; private set; }
        public int[] IcerikIdleri { get; private set; } = [];
        public int[] SatirIdleri { get; private set; } = [];
        public int TasinmayanSatirId { get; private set; }
        public Dictionary<int, int> BaslangicKalemDurumlari { get; private set; } = [];

        public static async Task<SahaKurgu> CreateAsync()
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
            var kurgu = new SahaKurgu(connection.ConnectionString);
            try
            {
                await kurgu.TohumlaAsync();
                return kurgu;
            }
            catch
            {
                // Kurulum yarıda kalırsa geçici veritabanı test sunucusunda kalmasın.
                await kurgu.DisposeAsync();
                throw;
            }
        }

        private async Task TohumlaAsync()
        {
            await using var db = Db();
            await db.Database.EnsureCreatedAsync();

            var kullanici = new Kullanici { Id = 7, AdSoyad = "Test", Email = "saha-toplu@example.invalid", RolId = 1 };
            var normal = new Proje { ProjeNo = "TEST-NORMAL", ProjeTipiId = (int)ProjeTipi.Normal };
            var saha = new Proje { ProjeNo = "TEST-SAHA", ProjeTipiId = (int)ProjeTipi.Saha };
            var normalCeki = new Ceki { Proje = normal, OrijinalDosyaYolu = "normal.xlsx", YuklemeTarihi = new DateTime(2026, 10, 1, 9, 0, 0) };
            var sahaCeki = new Ceki { Proje = saha, OrijinalDosyaYolu = "saha.xlsx", YuklemeTarihi = new DateTime(2026, 10, 2, 9, 0, 0) };
            var kaynak = new Sandik { Proje = saha, SandikNo = "1", DurumId = (int)SandikDurum.Hazirlaniyor };
            var hedef = new Sandik { Proje = saha, SandikNo = "2", DurumId = (int)SandikDurum.Bos };
            var aktarim = new SahaAktarim { SahaProje = saha, KaynakProje = normal, Kullanici = kullanici, Tarih = new DateTime(2026, 10, 2) };

            var icerikler = new List<SandikIcerik>();
            var sahaSatirlari = new List<CekiSatiri>();
            foreach (var (barkod, adet, durum) in new[]
                     {
                         ("S-1", 1m, SahaAktarimDurum.Planlandi),
                         ("S-2", 1m, SahaAktarimDurum.Planlandi),
                         ("S-3", 1m, SahaAktarimDurum.Hazirlaniyor),
                         ("S-4", 2m, SahaAktarimDurum.Planlandi),
                         ("S-5", 1m, SahaAktarimDurum.Hazirlaniyor)
                     })
            {
                var normalSatir = new CekiSatiri { Ceki = normalCeki, BarkodNo = barkod, Aciklama = barkod, IstenenAdet = adet };
                var sahaSatir = new CekiSatiri { Ceki = sahaCeki, BarkodNo = barkod, Aciklama = barkod, IstenenAdet = adet, KaynakCekiSatiri = normalSatir };
                var icerik = new SandikIcerik { Sandik = kaynak, CekiSatiri = sahaSatir, BarkodNo = barkod, TahsisMiktari = adet, Miktar = adet };
                db.SandikIcerikleri.Add(icerik);
                aktarim.Kalemler.Add(new SahaAktarimKalemi
                {
                    SahaAktarim = aktarim, KaynakProje = normal, SahaProje = saha, KaynakCekiSatiri = normalSatir,
                    SahaCekiSatiri = sahaSatir, SahaSandik = kaynak, Miktar = adet, DurumId = (int)durum
                });
                icerikler.Add(icerik);
                sahaSatirlari.Add(sahaSatir);
                // Kimlik sırası taşıma sırasıdır; her satırı ayrı kaydederek sırayı sabitliyoruz.
                await db.SaveChangesAsync();
            }

            db.Sandiklar.Add(hedef);
            db.SahaAktarimlari.Add(aktarim);
            await db.SaveChangesAsync();

            ProjeId = saha.Id;
            KaynakId = kaynak.Id;
            HedefId = hedef.Id;
            IcerikIdleri = icerikler.Take(4).Select(i => i.Id).ToArray();
            SatirIdleri = sahaSatirlari.Take(4).Select(s => s.Id).ToArray();
            TasinmayanSatirId = sahaSatirlari[4].Id;
            BaslangicKalemDurumlari = aktarim.Kalemler.ToDictionary(k => k.SahaCekiSatiriId!.Value, k => k.DurumId);
        }

        public AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_connectionString).Options);

        public SandikUrunleriTopluTasiCommand Istek(params decimal[] adetler) => new()
        {
            ProjeId = ProjeId, KaynakSandikId = KaynakId, HedefSandikId = HedefId, IslemAnahtari = Guid.NewGuid(),
            Satirlar = adetler
                .Select((adet, i) => new SandikTopluTasimaSatiri { KaynakSandikIcerikId = IcerikIdleri[i], TasinanAdet = adet })
                .ToList()
        };

        public async Task<Result> TopluTasiAsync(SandikUrunleriTopluTasiCommand istek)
        {
            // Uygulamadaki gibi tek DbContext: depo, iş birimi ve saha senkronizasyonu aynı izleyiciyi paylaşır.
            await using var db = Db();
            using var uow = new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
            return await new SandikUrunleriTopluTasiCommandHandler(uow, new OrtakUser(), new SahaTamamlamaService(db))
                .Handle(istek, default);
        }

        public async Task<Result> TekilTasiAsync(int icerikId, decimal adet)
        {
            await using var db = Db();
            using var uow = new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
            return await new SandikUrunTasiCommandHandler(uow, new OrtakUser(), new SahaTamamlamaService(db))
                .Handle(new SandikUrunTasiCommand
                {
                    ProjeId = ProjeId, KaynakSandikIcerikId = icerikId, HedefSandikId = HedefId,
                    TasinanAdet = adet, IslemAnahtari = Guid.NewGuid()
                }, default);
        }

        public async Task BaslangicKorunurAsync()
        {
            await using var db = Db();
            var kalemler = await db.SahaAktarimKalemleri.ToListAsync();
            Assert.Equal(5, kalemler.Count);
            Assert.All(kalemler, k => Assert.Equal(KaynakId, k.SahaSandikId));
            // Saha senkronizasyonunun transaction içinde yazdığı durum değişiklikleri de geri alınmış olmalı.
            Assert.Equal(
                BaslangicKalemDurumlari.OrderBy(x => x.Key),
                kalemler.ToDictionary(k => k.SahaCekiSatiriId!.Value, k => k.DurumId).OrderBy(x => x.Key));
            var kaynak = await db.SandikIcerikleri.Where(i => i.SandikId == KaynakId).ToListAsync();
            Assert.Equal(5, kaynak.Count);
            Assert.Equal(6m, kaynak.Sum(i => i.TahsisMiktari));
            Assert.Equal(0, await db.SandikIcerikleri.CountAsync(i => i.SandikId == HedefId));
            Assert.Equal((int)SandikDurum.Bos, (await db.Sandiklar.SingleAsync(s => s.Id == HedefId)).DurumId);
            Assert.Equal(0, await db.SandikUrunTransferleri.CountAsync());
            Assert.Equal(0, await db.SandikTopluTasimaIslemleri.CountAsync());
            Assert.Equal(0, await db.HareketGecmisleri.CountAsync());
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
