using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services;
using _3K.Infrastructure.Services.Raporlar;

namespace _3K.Application.Tests;

/// <summary>
/// Sahadan sahaya taşımanın gerçek EF izleyicisi, xmin ve PostgreSQL kısıtlarıyla doğrulanması; kaynak sahanın sevk
/// sonrası eksik raporundaki "Saha izi" bölümü (PDF ve Excel aynı içerik) ve iz tablosunun silmede SET NULL kuralı.
/// </summary>
public sealed class SahadanSahayaTasimaPostgresTests
{
    [SandikPostgresFact]
    [Trait("Category", "Postgres")]
    public async Task Tasima_SatiriHedefeGecirir_RapordaSahaIziGorunur_SandikSilinseIzKalir()
    {
        await using var kurgu = await Kurgu.CreateAsync();

        var sonuc = await kurgu.TasiAsync();

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        await using (var db = kurgu.Db())
        {
            var satir = await db.CekiSatirlari.Include(cs => cs.Ceki).SingleAsync(cs => cs.Id == kurgu.SahaSatirId);
            Assert.Equal((kurgu.HedefSahaId, 2m, kurgu.KokSatirId), (satir.Ceki.ProjeId, satir.GelenMiktar, satir.KaynakCekiSatiriId!.Value));
            Assert.Equal(kurgu.HedefSandikId, (await db.SandikIcerikleri.SingleAsync(i => i.CekiSatiriId == kurgu.SahaSatirId)).SandikId);
            var kalem = await db.SahaAktarimKalemleri.SingleAsync();
            Assert.Equal((kurgu.HedefSahaId, (int?)kurgu.HedefSandikId), (kalem.SahaProjeId, kalem.SahaSandikId));
            Assert.Equal((int)SandikDurum.Bos, (await db.Sandiklar.SingleAsync(s => s.Id == kurgu.KaynakSandikId)).DurumId);
            Assert.Single(await db.SahaTasimaKalemleri.ToListAsync());
        }

        await using (var db = kurgu.Db())
        {
            var rapor = new EksikUrunlerRaporService(db);
            var iz = Assert.Single(await rapor.GetSahaIziSatirlariAsync(kurgu.KaynakSahaId, default));
            Assert.Equal(("PA723-01", "PA573-01-SAHA", "3", "PA723-01-SAHA (1) → PA573-01-SAHA (3)", "Bekliyor"),
                (iz.KaynakProjeNo, iz.SahaProjeNo, iz.SahaSandikNo, iz.Iz, iz.Durum));

            var pdf = await rapor.EksikUrunlerRaporuPdfOlusturAsync(kurgu.KaynakSahaId);
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));

            var excel = await rapor.EksikUrunlerRaporuExcelOlusturAsync(kurgu.KaynakSahaId);
            using var kitap = new XLWorkbook(new MemoryStream(excel));
            var sayfa = kitap.Worksheet("Saha Izi");
            Assert.Equal("Gittiği Saha", sayfa.Cell(4, 7).GetString());
            Assert.Equal("PA573-01-SAHA", sayfa.Cell(5, 7).GetString());
            Assert.Equal("PA723-01-SAHA (1) → PA573-01-SAHA (3)", sayfa.Cell(5, 9).GetString());
        }

        // Boşalan kaynak sandık silinse de iz okunur: bağ boşalır, numara kopyası kalır.
        await using (var db = kurgu.Db())
        {
            await db.Sandiklar.Where(s => s.Id == kurgu.KaynakSandikId).ExecuteDeleteAsync();
            var iz = await db.SahaTasimaKalemleri.AsNoTracking().SingleAsync();
            Assert.Null(iz.KaynakSahaSandikId);
            Assert.Equal("1", iz.KaynakSahaSandikNo);
        }
    }

    /// <summary>
    /// Taşımadan sonra boşalan kaynak saha projesi silinebilir: eski başlık silinir (iz SET NULL), taşınmış kalem ve iz
    /// hedefte kalır; izdeki kaynak proje/sandık bağı boşalır, numara kopyası okunur.
    /// </summary>
    [SandikPostgresFact]
    [Trait("Category", "Postgres")]
    public async Task TasimadanSonraKaynakSahaSilinir_IzVeTasinanKalemKalir()
    {
        await using var kurgu = await Kurgu.CreateAsync();
        Assert.True((await kurgu.TasiAsync()).IsSuccess);

        await using (var db = kurgu.Db())
        {
            var silme = new ProjeSilmeService(
                db,
                new SahaTamamlamaService(db),
                new SahaAktarimSilmeKorumaService(db),
                new SessizBildirim(),
                NullLogger<ProjeSilmeService>.Instance);
            Assert.True(await silme.SilAsync(kurgu.KaynakSahaId));
        }

        await using (var db = kurgu.Db())
        {
            Assert.False(await db.Projeler.AnyAsync(p => p.Id == kurgu.KaynakSahaId));
            var iz = await db.SahaTasimaKalemleri.AsNoTracking().SingleAsync();
            Assert.Equal(((int?)null, (int?)null, "PA723-01-SAHA", "1"), (iz.KaynakSahaProjeId, iz.KaynakSahaSandikId, iz.KaynakSahaProjeNo, iz.KaynakSahaSandikNo));
            var kalem = await db.SahaAktarimKalemleri.AsNoTracking().SingleAsync();
            Assert.Equal((kurgu.HedefSahaId, kurgu.SahaSatirId), (kalem.SahaProjeId, kalem.SahaCekiSatiriId!.Value));
            Assert.Null((await db.SahaAktarimlari.AsNoTracking().SingleAsync(a => a.Id == kalem.SahaAktarimId)).KaynakSahaProjeId);
        }
    }

    /// <summary>
    /// A→B→C zinciri: ürün PA723-01-SAHA'dan PA573-01-SAHA'ya (kapalı sandık 3 yeniden açılır), oradan ürünün bütün
    /// sandığıyla çekisi ve sandığı olmayan üçüncü sahaya taşınır (yeni çeki, kutu bilgisi kopyalı yeni sandık, Tamamlandı
    /// hedef proje hazırlanıyor). Ara saha B boşalınca silinir; ürünün A→B adımı başlık bağı boşalarak izde kalır.
    /// </summary>
    [SandikPostgresFact]
    [Trait("Category", "Postgres")]
    public async Task Zincir_AraSahaSilinir_IlkAdimIzdeKalir_YeniCekiVeSandikAcilir()
    {
        await using var kurgu = await Kurgu.CreateAsync();
        await using (var db = kurgu.Db())
        {
            await db.Sandiklar.Where(s => s.Id == kurgu.HedefSandikId)
                .ExecuteUpdateAsync(x => x.SetProperty(s => s.DurumId, (int)SandikDurum.Kapandi).SetProperty(s => s.Boy, 900m));
            await db.Projeler.Where(p => p.Id == kurgu.UcuncuSahaId)
                .ExecuteUpdateAsync(x => x.SetProperty(p => p.DurumId, (int)ProjeDurum.Tamamlandi));
        }

        var ilk = await kurgu.TasiAsync();
        Assert.True(ilk.IsSuccess, ilk.Error?.Message);
        await using (var db = kurgu.Db())
        {
            Assert.Equal((int)SandikDurum.Hazirlaniyor, (await db.Sandiklar.SingleAsync(s => s.Id == kurgu.HedefSandikId)).DurumId);
            Assert.True(await db.HareketGecmisleri.AnyAsync(h => h.ProjeId == kurgu.HedefSahaId && h.Islem == "Sandık Geri Açıldı"));
        }

        var ikinci = await kurgu.TasiAsync(kaynak: kurgu.HedefSahaId, hedef: kurgu.UcuncuSahaId, kaynakSandik: kurgu.HedefSandikId);
        Assert.True(ikinci.IsSuccess, ikinci.Error?.Message);
        await using (var db = kurgu.Db())
        {
            var ceki = await db.Cekiler.SingleAsync(c => c.ProjeId == kurgu.UcuncuSahaId);
            Assert.Equal((int)CekiTipi.EksikTamamlama, ceki.CekiTipiId);
            var yeniSandik = await db.Sandiklar.SingleAsync(s => s.Id == ikinci.Value!.HedefSandikId);
            Assert.Equal((kurgu.UcuncuSahaId, "3", 900m), (yeniSandik.ProjeId, yeniSandik.SandikNo, yeniSandik.Boy!.Value));
            Assert.Equal((int)SandikDurum.Bos, (await db.Sandiklar.SingleAsync(s => s.Id == kurgu.HedefSandikId)).DurumId);
            Assert.Equal((int)ProjeDurum.Hazirlaniyor, (await db.Projeler.SingleAsync(p => p.Id == kurgu.UcuncuSahaId)).DurumId);
            Assert.Equal(ceki.Id, (await db.CekiSatirlari.SingleAsync(cs => cs.Id == kurgu.SahaSatirId)).CekiId);
        }

        await using (var db = kurgu.Db())
        {
            var silme = new ProjeSilmeService(
                db,
                new SahaTamamlamaService(db),
                new SahaAktarimSilmeKorumaService(db),
                new SessizBildirim(),
                NullLogger<ProjeSilmeService>.Instance);
            Assert.True(await silme.SilAsync(kurgu.HedefSahaId));
        }

        await using (var db = kurgu.Db())
        {
            var izler = await db.SahaTasimaKalemleri.AsNoTracking().OrderBy(t => t.Id).ToListAsync();
            Assert.Equal(2, izler.Count);
            // A→B: başlığı B ile silindi, bağ boşaldı; numaralar okunur.
            Assert.Equal(((int?)null, (int?)null, "PA723-01-SAHA", "PA573-01-SAHA"),
                (izler[0].SahaAktarimId, izler[0].HedefSahaProjeId, izler[0].KaynakSahaProjeNo, izler[0].HedefSahaProjeNo));
            Assert.Equal((ikinci.Value!.SahaAktarimId, (int?)null), (izler[1].SahaAktarimId!.Value, izler[1].KaynakSahaProjeId));

            var iz = Assert.Single(await new EksikUrunlerRaporService(db).GetSahaIziSatirlariAsync(kurgu.UcuncuSahaId, default));
            Assert.Equal("PA723-01-SAHA (1) → PA573-01-SAHA (3) → PA900-01-SAHA (3)", iz.Iz);
        }
    }

    [SandikPostgresFact]
    [Trait("Category", "Postgres")]
    public async Task AyniAnahtarlaEsZamanliIkiTasima_TekKayitYazar()
    {
        await using var kurgu = await Kurgu.CreateAsync();
        var anahtar = Guid.NewGuid();

        async Task<bool> Dene()
        {
            try
            {
                return (await kurgu.TasiAsync(anahtar)).IsSuccess;
            }
            catch (Exception hata) when (hata is _3K.Core.Exceptions.ConcurrencyConflictException or _3K.Core.Exceptions.UniqueConstraintViolationException)
            {
                return false;
            }
        }

        var sonuclar = await Task.WhenAll(Dene(), Dene());

        Assert.Contains(true, sonuclar);
        await using var db = kurgu.Db();
        Assert.Equal(1, await db.SahaTasimaKalemleri.CountAsync());
        Assert.Equal(1, await db.SahaAktarimlari.CountAsync(a => a.IslemAnahtari == anahtar));
    }

    private sealed class SessizBildirim : _3K.Core.Interfaces.ISseNotifier
    {
        public Task SubscribeAsync(object context, int kullaniciId) => Task.CompletedTask;
        public Task NotifyUsersAsync(IEnumerable<int> kullaniciIdleri, string eventName, string data = "refresh") => Task.CompletedTask;
        public Task BroadcastApprovalUpdateAsync() => Task.CompletedTask;
    }

    private sealed class Kurgu : IAsyncDisposable
    {
        private const string VeritabaniOneki = "saha_tasima_tests_";
        private readonly string _connectionString;
        private Kurgu(string connectionString) => _connectionString = connectionString;
        public int KaynakSahaId { get; private set; }
        public int HedefSahaId { get; private set; }
        /// <summary>Çekisi ve sandığı olmayan saha projesi (PA900-01-SAHA).</summary>
        public int UcuncuSahaId { get; private set; }
        public int KaynakSandikId { get; private set; }
        public int HedefSandikId { get; private set; }
        public int SahaSatirId { get; private set; }
        public int KokSatirId { get; private set; }

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

        /// <summary>PA723-01 (normal) → PA723-01-SAHA sandık 1'de 3K'ya gelmiş ürün; hedef PA573-01-SAHA sandık 3.</summary>
        private async Task TohumlaAsync()
        {
            await using var db = Db();
            await db.Database.EnsureCreatedAsync();

            var kullanici = new Kullanici { Id = 7, AdSoyad = "Test", Email = "saha-tasima@example.invalid", RolId = 1 };
            var normal = new Proje { ProjeNo = "PA723-01", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Normal, DurumId = (int)ProjeDurum.EksikSevkEdildi };
            var kaynakSaha = new Proje { ProjeNo = "PA723-01-SAHA", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Saha, DurumId = (int)ProjeDurum.Hazirlaniyor };
            var hedefSaha = new Proje { ProjeNo = "PA573-01-SAHA", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Saha, DurumId = (int)ProjeDurum.Hazirlaniyor };
            var ucuncuSaha = new Proje { ProjeNo = "PA900-01-SAHA", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Saha, DurumId = (int)ProjeDurum.Hazirlaniyor };
            var kok = new CekiSatiri
            {
                Ceki = new Ceki { Proje = normal, OrijinalDosyaYolu = "ceki.xlsx", YuklemeTarihi = new DateTime(2026, 10, 1) },
                SiraNo = 7, BarkodNo = "B-7", Aciklama = "Bushing", IstenenAdet = 5, GelenMiktar = 3
            };
            var sahaCeki = new Ceki { Proje = kaynakSaha, OrijinalDosyaYolu = string.Empty, YuklemeTarihi = new DateTime(2026, 10, 2), CekiTipiId = (int)CekiTipi.EksikTamamlama, TamamlamaNo = 1 };
            var kaynakSandik = new Sandik { Proje = kaynakSaha, SandikNo = "1", Ad = "Sevk sonrası eksikler", DurumId = (int)SandikDurum.Hazirlaniyor };
            var hedefSandik = new Sandik { Proje = hedefSaha, SandikNo = "3", DurumId = (int)SandikDurum.Hazirlaniyor };
            var sahaSatir = new CekiSatiri
            {
                Ceki = sahaCeki, KaynakCekiSatiri = kok, SiraNo = 1, BarkodNo = "B-7", Aciklama = "Bushing", IstenenAdet = 2,
                GelenMiktar = 2, GridDurumuId = (int)GridDurum.TamGeldi, UcKDurumuId = (int)UcKDurum.TamGeldi, CekideGecenSandikNo = "1"
            };
            var aktarim = new SahaAktarim { SahaProje = kaynakSaha, KaynakProje = normal, Kullanici = kullanici, Tarih = new DateTime(2026, 10, 3) };
            aktarim.Kalemler.Add(new SahaAktarimKalemi
            {
                KaynakProje = normal, SahaProje = kaynakSaha, KaynakCekiSatiri = kok, SahaCekiSatiri = sahaSatir,
                SahaSandik = kaynakSandik, Miktar = 2, DurumId = (int)SahaAktarimDurum.Tamamlandi
            });
            db.SandikIcerikleri.Add(new SandikIcerik { Sandik = kaynakSandik, CekiSatiri = sahaSatir, TahsisMiktari = 2, KonulanAdet = 2, Miktar = 2 });
            db.Sandiklar.Add(hedefSandik);
            db.Projeler.Add(ucuncuSaha);
            db.SahaAktarimlari.Add(aktarim);
            await db.SaveChangesAsync();

            KaynakSahaId = kaynakSaha.Id;
            HedefSahaId = hedefSaha.Id;
            UcuncuSahaId = ucuncuSaha.Id;
            KaynakSandikId = kaynakSandik.Id;
            HedefSandikId = hedefSandik.Id;
            SahaSatirId = sahaSatir.Id;
            KokSatirId = kok.Id;
        }

        public AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_connectionString).Options);

        /// <summary>Varsayılan: saha satırı kaynak sahadan hedef sahanın sandık 3'üne; <paramref name="kaynakSandik"/> verilirse bütün sandık yeni sandığa.</summary>
        public async Task<Result<SahadanSahayaTasimaSonucDto>> TasiAsync(Guid? anahtar = null, int? kaynak = null, int? hedef = null, int? kaynakSandik = null)
        {
            // Uygulamadaki gibi tek DbContext: depo, iş birimi, hareket ve saha servisleri aynı izleyiciyi paylaşır.
            await using var db = Db();
            using var uow = new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
            return await new SahadanSahayaTasiCommandHandler(uow, new HareketService(uow, db), new OrtakUser(), new SahaTamamlamaService(db))
                .Handle(new SahadanSahayaTasiCommand
                {
                    IslemAnahtari = anahtar ?? Guid.NewGuid(),
                    KaynakSahaProjeId = kaynak ?? KaynakSahaId,
                    SahaCekiSatiriIds = kaynakSandik.HasValue ? [] : [SahaSatirId],
                    KaynakSandikId = kaynakSandik,
                    HedefSahaProjeId = hedef ?? HedefSahaId,
                    HedefSandikId = kaynakSandik.HasValue ? null : HedefSandikId
                }, default);
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
