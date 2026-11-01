using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using _3K.Application.Common;
using _3K.Application.Features.CekiIslemleri.Commands;
using _3K.Application.Features.GridIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.DTOs;
using _3K.Application.Features.SandikIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Data.Interceptors;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

/// <summary>
/// Sevk sonrası düzeltmenin gerçek EF + PostgreSQL + proje kilidiyle uçtan uca akışı: kilitliyken reddedilir;
/// sevkiyat kaydı korunarak açılınca unutulan ürün eklenir, ürün sandıklar arası taşınır; tamamlanınca sandıklar ve proje
/// sevkli kalır, sevkiyat bağları korunur, fark özeti kalıcı yazılır.
/// </summary>
public sealed class SevkSonrasiDuzeltmePostgresTests
{
    [SandikPostgresFact]
    [Trait("Category", "Postgres")]
    public async Task DuzeltmeAcEkleTasiTamamla_SevkiyatKorunurVeFarkYazilir()
    {
        await using var kurgu = await Kurgu.CreateAsync();

        // Kilitliyken unutulan ürün eklenemez; veri değişmez.
        var kilitli = await kurgu.UrunEkleAsync(kurgu.Sandik1Id);
        Assert.False(kilitli.IsSuccess);
        Assert.Equal(2, await kurgu.SayAsync(db => db.CekiSatirlari));

        Assert.True((await kurgu.KilidiAcAsync(kurgu.Sandik1Id)).IsSuccess);
        Assert.True((await kurgu.KilidiAcAsync(kurgu.Sandik2Id)).IsSuccess);

        var ekle = await kurgu.UrunEkleAsync(kurgu.Sandik1Id);
        Assert.True(ekle.IsSuccess, ekle.Error?.Message);
        var tasi = await kurgu.TasiAsync(kurgu.Sandik2IcerikId, kurgu.Sandik1Id, 3);
        Assert.True(tasi.IsSuccess, tasi.Error?.Message);

        var durum = await kurgu.DurumAsync(kurgu.Sandik1Id);
        Assert.True(durum.AcikMi);
        Assert.Equal("Sevkte unutulan konsol", durum.Gerekce);
        Assert.Contains(durum.Degisiklikler, d => d.StartsWith("Eklendi: Unutulan konsol (B-YENI)", StringComparison.Ordinal));
        Assert.Contains(durum.Degisiklikler, d => d.StartsWith("Eklendi: Ürün 2 (B-2)", StringComparison.Ordinal));

        // Projedeki bütün açık düzeltmeler tek işlemde tamamlanır.
        var toplu = await kurgu.TopluTamamlaAsync();
        Assert.True(toplu.IsSuccess, toplu.Error?.Message);
        Assert.Equal(2, toplu.Value);
        Assert.Equal(409, (await kurgu.TopluTamamlaAsync()).StatusCode);

        await using (var db = kurgu.Db())
        {
            Assert.Equal((int)ProjeDurum.SevkEdildi, (await db.Projeler.SingleAsync(p => p.Id == kurgu.ProjeId)).DurumId);
            var sandiklar = await db.Sandiklar.Where(s => s.ProjeId == kurgu.ProjeId).OrderBy(s => s.SandikNo).ToListAsync();
            Assert.Equal(["1", "2"], sandiklar.Select(s => s.SandikNo));
            Assert.All(sandiklar, s => Assert.Equal(((int)SandikDurum.Sevkedildi, false), (s.DurumId, s.SevkiyatDuzeltmeAcikMi)));
            Assert.Equal(2, await db.SevkiyatSandiklari.CountAsync(ss => ss.SevkiyatId == kurgu.SevkiyatId));
            var kayitlar = await db.SandikDuzeltmeKayitlari.OrderBy(k => k.Id).ToListAsync();
            Assert.Equal(2, kayitlar.Count);
            Assert.All(kayitlar, k => Assert.NotNull(k.TamamlanmaTarihi));
            var sandik1Kaydi = kayitlar.Single(k => k.SandikId == kurgu.Sandik1Id);
            Assert.Contains("Eklendi: Unutulan konsol (B-YENI) — 2 adet", sandik1Kaydi.FarkOzeti);
            Assert.Contains("Çıkarıldı: Ürün 2 (B-2) — 3 adet", kayitlar.Single(k => k.SandikId == kurgu.Sandik2Id).FarkOzeti);
        }

        // Tamamlandıktan sonra sandık yeniden kilitlidir.
        Assert.False((await kurgu.UrunEkleAsync(kurgu.Sandik1Id)).IsSuccess);
        Assert.Equal(3, await kurgu.SayAsync(db => db.CekiSatirlari));
    }

    /// <summary>
    /// Düzeltmede mevcut işlemler sandığı sevkli bırakır: ürün güncelleme, çeki satırı silme (boşalan sevkli sandık ve
    /// sevkiyat bağı silinmez), lokasyonu belirsiz düzeltmedeki sandığa unutulan ürün (lokasyon değişmez, izleme
    /// çakışması yok). Grid manuel ürün sevkli sandığa eklenmez.
    /// </summary>
    [SandikPostgresFact]
    [Trait("Category", "Postgres")]
    public async Task DuzeltmedeMevcutIslemler_SandigiSevkliBirakir()
    {
        await using var kurgu = await Kurgu.CreateAsync();
        await kurgu.LokasyonuBelirsizYapAsync(kurgu.Sandik1Id);
        Assert.True((await kurgu.KilidiAcAsync(kurgu.Sandik1Id)).IsSuccess);
        Assert.True((await kurgu.KilidiAcAsync(kurgu.Sandik2Id)).IsSuccess);

        var guncelle = await kurgu.UrunGuncelleAsync(kurgu.Sandik1Id, kurgu.Sandik1SatirId, "Etiket yenilendi");
        Assert.True(guncelle.IsSuccess, guncelle.Error?.Message);
        var grid = await kurgu.GridManuelUrunEkleAsync("1");
        Assert.Equal(409, grid.StatusCode);
        var sil = await kurgu.SatirSilAsync(kurgu.Sandik2SatirId);
        Assert.True(sil.IsSuccess, sil.Error?.Message);
        var ekle = await kurgu.UrunEkleAsync(kurgu.Sandik1Id);
        Assert.True(ekle.IsSuccess, ekle.Error?.Message);

        await using var db = kurgu.Db();
        var sandiklar = await db.Sandiklar.Where(s => s.ProjeId == kurgu.ProjeId).OrderBy(s => s.SandikNo).ToListAsync();
        Assert.Equal(["1", "2"], sandiklar.Select(s => s.SandikNo));
        Assert.All(sandiklar, s => Assert.Equal(((int)SandikDurum.Sevkedildi, true), (s.DurumId, s.SevkiyatDuzeltmeAcikMi)));
        Assert.Equal((int)DepoLokasyon.Belirsiz, sandiklar.Single(s => s.SandikNo == "1").DepoLokasyonId);
        Assert.Equal(2, await db.SevkiyatSandiklari.CountAsync(ss => ss.SevkiyatId == kurgu.SevkiyatId));
        Assert.False(await db.CekiSatirlari.AnyAsync(cs => cs.Id == kurgu.Sandik2SatirId));
        Assert.Equal("Etiket yenilendi", (await db.CekiSatirlari.SingleAsync(cs => cs.Id == kurgu.Sandik1SatirId)).Remarks);
        Assert.Equal((int)ProjeDurum.SevkEdildi, (await db.Projeler.SingleAsync(p => p.Id == kurgu.ProjeId)).DurumId);
    }

    /// <summary>
    /// Sahaya aktarılmış ürün sevk sonrası düzeltmede de ana projede değişmez (aynı miktar iki yerde sayılmasın);
    /// sandık içeriği ürünün takip edildiği saha projesini gösterir.
    /// </summary>
    [SandikPostgresFact]
    [Trait("Category", "Postgres")]
    public async Task SahayaAktarilanUrun_DuzeltmedeDegismez_SahaProjesiGosterilir()
    {
        await using var kurgu = await Kurgu.CreateAsync();
        var sahaProjeNo = await kurgu.SahaAktarimiEkleAsync();
        Assert.True((await kurgu.KilidiAcAsync(kurgu.Sandik1Id)).IsSuccess);
        Assert.True((await kurgu.KilidiAcAsync(kurgu.Sandik2Id)).IsSuccess);

        var tasi = await kurgu.TasiAsync(kurgu.Sandik2IcerikId, kurgu.Sandik1Id, 3);

        Assert.False(tasi.IsSuccess);
        await using (var db = kurgu.Db())
            Assert.Equal(kurgu.Sandik2Id, (await db.SandikIcerikleri.SingleAsync(i => i.Id == kurgu.Sandik2IcerikId)).SandikId);
        var icerik = Assert.Single((await kurgu.IcerikAsync(kurgu.Sandik2Id)).Icerikler);
        Assert.Equal([sahaProjeNo], icerik.AktifSahaProjeleri.Select(p => p.ProjeNo));
        Assert.Empty(Assert.Single((await kurgu.IcerikAsync(kurgu.Sandik1Id)).Icerikler).AktifSahaProjeleri);
    }

    [SandikPostgresFact]
    [Trait("Category", "Postgres")]
    public async Task DuzeltmeAcikDegilken_DogrudanYazimProjeKilidineTakilir()
    {
        await using var kurgu = await Kurgu.CreateAsync();
        await using var db = kurgu.Db();
        var satir = new CekiSatiri { CekiId = kurgu.CekiId, BarkodNo = "X", Aciklama = "X", IstenenAdet = 1 };
        db.SandikIcerikleri.Add(new SandikIcerik { SandikId = kurgu.Sandik1Id, CekiSatiri = satir, TahsisMiktari = 1 });

        await Assert.ThrowsAsync<ProjectLockedException>(() => db.SaveChangesAsync());
        Assert.Equal(2, await kurgu.SayAsync(x => x.CekiSatirlari));
    }

    private sealed class Kurgu : IAsyncDisposable
    {
        private const string VeritabaniOneki = "sevk_duzeltme_tests_";
        private readonly string _connectionString;
        private Kurgu(string connectionString) => _connectionString = connectionString;
        public int ProjeId { get; private set; }
        public int CekiId { get; private set; }
        public int Sandik1Id { get; private set; }
        public int Sandik2Id { get; private set; }
        public int Sandik2IcerikId { get; private set; }
        public int Sandik1SatirId { get; private set; }
        public int Sandik2SatirId { get; private set; }
        public int SevkiyatId { get; private set; }

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

        /// <summary>Tamamen sevk edilmiş normal proje: iki sevkli sandık, tek sevkiyat, her sandıkta tamamlanmış bir ürün.</summary>
        private async Task TohumlaAsync()
        {
            // Tohum kilitsiz yazılır; proje kilidi yalnız uygulama bağlamlarında (Db()) devrededir.
            await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_connectionString).Options);
            await db.Database.EnsureCreatedAsync();

            db.Kullanicilar.Add(new Kullanici { Id = 7, AdSoyad = "Test", Email = "sevk-duzeltme@example.invalid", RolId = 1 });
            var proje = new Proje { ProjeNo = "PA-DZ-01", Musteri = "Müşteri", ProjeTipiId = (int)ProjeTipi.Normal, DurumId = (int)ProjeDurum.SevkEdildi };
            var ceki = new Ceki { Proje = proje, OrijinalDosyaYolu = "ceki.xlsx", YuklemeTarihi = new DateTime(2026, 10, 1, 9, 0, 0) };
            var sevkiyat = new Sevkiyat { Proje = proje, SevkiyatNo = 1, SevkTarihi = new DateTime(2026, 10, 20), KullaniciId = 7 };
            Sandik YeniSandik(string no) => new()
            {
                Proje = proje, SandikNo = no, Ad = $"Sandık {no}", DurumId = (int)SandikDurum.Sevkedildi,
                SevkOncesiDurumId = (int)SandikDurum.Kapandi, DepoLokasyonId = (int)DepoLokasyon.UcK
            };
            var s1 = YeniSandik("1");
            var s2 = YeniSandik("2");
            SandikIcerik Urun(Sandik sandik, int sira, decimal adet)
            {
                var icerik = new SandikIcerik
                {
                    Sandik = sandik,
                    CekiSatiri = new CekiSatiri
                    {
                        Ceki = ceki, SiraNo = sira, BarkodNo = $"B-{sira}", Aciklama = $"Ürün {sira}", IstenenAdet = adet,
                        GelenMiktar = adet, DurumId = (int)UrunDurum.Tamamlandi, GridDurumuId = (int)GridDurum.TamGeldi,
                        GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi, UcKDurumuId = (int)UcKDurum.TamGeldi,
                        UcKKarsilamaTipiId = (int)UcKDurum.TamGeldi, CekideGecenSandikNo = sandik.SandikNo,
                        FiiliSandikNo = sandik.SandikNo
                    },
                    TahsisMiktari = adet,
                    KonulanAdet = adet
                };
                db.SandikIcerikleri.Add(icerik);
                return icerik;
            }
            var icerik1 = Urun(s1, 1, 1);
            var icerik2 = Urun(s2, 2, 3);
            db.SevkiyatSandiklari.AddRange(new SevkiyatSandik { Sevkiyat = sevkiyat, Sandik = s1 },
                new SevkiyatSandik { Sevkiyat = sevkiyat, Sandik = s2 });
            await db.SaveChangesAsync();

            (ProjeId, CekiId, Sandik1Id, Sandik2Id, Sandik2IcerikId, SevkiyatId) =
                (proje.Id, ceki.Id, s1.Id, s2.Id, icerik2.Id, sevkiyat.Id);
            (Sandik1SatirId, Sandik2SatirId) = (icerik1.CekiSatiri!.Id, icerik2.CekiSatiri!.Id);
        }

        /// <summary>Sandık 2'deki ürünün sevk edilmiş aktif saha aktarımı (kilitsiz bağlamda yazılır).</summary>
        public async Task<string> SahaAktarimiEkleAsync()
        {
            await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_connectionString).Options);
            var saha = new Proje { ProjeNo = "PA-DZ-SAHA", Musteri = "Müşteri", ProjeTipiId = (int)ProjeTipi.Saha };
            var satirId = (await db.SandikIcerikleri.SingleAsync(i => i.Id == Sandik2IcerikId)).CekiSatiriId!.Value;
            db.SahaAktarimKalemleri.Add(new SahaAktarimKalemi
            {
                SahaAktarim = new SahaAktarim { SahaProje = saha, KaynakProjeId = ProjeId },
                KaynakProjeId = ProjeId,
                SahaProje = saha,
                KaynakCekiSatiriId = satirId,
                KaynakSandikId = Sandik2Id,
                Miktar = 1,
                DurumId = (int)SahaAktarimDurum.SevkEdildi
            });
            await db.SaveChangesAsync();
            return saha.ProjeNo;
        }

        public async Task<SandikDetayDto> IcerikAsync(int sandikId)
        {
            await using var db = Db();
            using var uow = new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
            var sonuc = await new GetSandikIcerikQueryHandler(new SandikService(uow, db), new LookupStub(),
                    new SahaTamamlamaService(db), new SahaAktarimSilmeKorumaService(db), uow)
                .Handle(new GetSandikIcerikQuery { SandikId = sandikId }, default);
            Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
            return sonuc.Value!;
        }

        /// <summary>Sandığın lokasyonunu kilitsiz bağlamda Belirsiz yapar (izleme çakışması senaryosu).</summary>
        public async Task LokasyonuBelirsizYapAsync(int sandikId)
        {
            await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_connectionString).Options);
            (await db.Sandiklar.SingleAsync(s => s.Id == sandikId)).DepoLokasyonId = (int)DepoLokasyon.Belirsiz;
            await db.SaveChangesAsync();
        }

        public AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_connectionString)
            .AddInterceptors(new AuditInterceptor(new OrtakUser()), new ProjectLockInterceptor()).Options);

        public async Task<int> SayAsync<T>(Func<AppDbContext, DbSet<T>> tablo) where T : class
        {
            await using var db = Db();
            return await tablo(db).CountAsync();
        }

        public Task<Result> KilidiAcAsync(int sandikId) => CalistirAsync((uow, db) =>
            new SandikKilidiAcCommandHandler(uow, new HareketService(uow, db), new OrtakUser(), new SahaTamamlamaService(db),
                    new EntityFrameworkReadQueryExecutor())
                .Handle(new SandikKilidiAcCommand
                {
                    ProjeId = ProjeId, SandikId = sandikId, Aciklama = "Sevkte unutulan konsol",
                    KilitAcmaTipiId = (int)SevkiyatKilitAcmaTipi.SevkiyatKaydiKorunarakAc
                }, default));

        public Task<Result> UrunEkleAsync(int sandikId) => CalistirAsync((uow, db) =>
            new ManuelUrunEkleCommandHandler(uow, new HareketService(uow, db), new OrtakUser())
                .Handle(new ManuelUrunEkleCommand
                {
                    ProjeId = ProjeId, SandikId = sandikId, BarkodNo = "B-YENI", Aciklama = "Unutulan konsol",
                    IstenenAdet = 2, EklemeNedeni = "Sevkte unutuldu", KullaniciId = 7
                }, default));

        public Task<Result> TasiAsync(int icerikId, int hedefSandikId, decimal adet) => CalistirAsync((uow, db) =>
            new SandikUrunTasiCommandHandler(uow, new OrtakUser(), new SahaTamamlamaService(db))
                .Handle(new SandikUrunTasiCommand
                {
                    ProjeId = ProjeId, KaynakSandikIcerikId = icerikId, HedefSandikId = hedefSandikId,
                    TasinanAdet = adet, IslemAnahtari = Guid.NewGuid()
                }, default));

        public Task<Result> UrunGuncelleAsync(int sandikId, int satirId, string not) => CalistirAsync((uow, db) =>
            new UrunGuncelleCommandHandler(uow, new HareketService(uow, db), new SahaTamamlamaService(db))
                .Handle(new UrunGuncelleCommand
                {
                    ProjeId = ProjeId, SandikId = sandikId, CekiSatiriId = satirId, Aciklama = not, KullaniciId = 7
                }, default));

        public Task<Result> GridManuelUrunEkleAsync(string sandikNo) => CalistirAsync((uow, db) =>
            new GridManuelUrunEkleCommandHandler(uow, new HareketService(uow, db), new OrtakUser())
                .Handle(new GridManuelUrunEkleCommand
                {
                    ProjeId = ProjeId, SandikNo = sandikNo, BarkodNo = "G-1", Aciklama = "Grid ürünü", IstenenAdet = 1
                }, default));

        public async Task<Result> SatirSilAsync(int satirId)
        {
            await using var db = Db();
            using var uow = new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
            var sonuc = await new CekiSatirlariSilCommandHandler(uow, new OrtakUser(), new DurumHesaplaService(),
                    new SahaAktarimSilmeKorumaService(db))
                .Handle(new CekiSatirlariSilCommand { CekiSatiriIds = [satirId] }, default);
            return sonuc.IsSuccess ? Result.Success() : Result.Failure(sonuc.Error!.Message, sonuc.StatusCode);
        }

        public async Task<Result<int>> TopluTamamlaAsync()
        {
            await using var db = Db();
            using var uow = new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
            return await new SandikSevkiyatDuzeltmeTamamlaCommandHandler(uow, new HareketService(uow, db), new OrtakUser(),
                    new SahaTamamlamaService(db), new EntityFrameworkReadQueryExecutor(), new LookupStub())
                .Handle(new SandikSevkiyatDuzeltmeTopluTamamlaCommand { ProjeId = ProjeId }, default);
        }

        public async Task<SandikDuzeltmeDurumuDto> DurumAsync(int sandikId)
        {
            await using var db = Db();
            using var uow = new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
            var sonuc = await new GetSandikDuzeltmeDurumuQueryHandler(uow, new LookupStub())
                .Handle(new GetSandikDuzeltmeDurumuQuery { SandikId = sandikId }, default);
            Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
            return sonuc.Value!;
        }

        // Uygulamadaki gibi tek DbContext: depo, iş birimi ve servisler aynı izleyiciyi paylaşır. Kilit reddi
        // (ProjectLockedException) işlem sonucuna çevrilir; pipeline'da da kullanıcıya hata olarak döner.
        private async Task<Result> CalistirAsync(Func<UnitOfWork, AppDbContext, Task<Result>> islem)
        {
            await using var db = Db();
            using var uow = new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
            try
            {
                return await islem(uow, db);
            }
            catch (ProjectLockedException hata)
            {
                return Result.Failure(hata.Message, 409);
            }
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
