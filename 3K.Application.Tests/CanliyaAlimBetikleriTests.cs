using Microsoft.EntityFrameworkCore;
using Npgsql;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Infrastructure.Data;

namespace _3K.Application.Tests;

/// <summary>
/// Canlıya alım provası: bu sürümün veritabanı betikleri (README sıra 22 <c>20261021_02_Sahadan_Sahaya_Tasima.sql</c>,
/// 23 <c>20261023_01_Grid_Saha_Gorunumu_Yetkisi.sql</c>, 24 <c>20261023_02_Sevk_Sonrasi_Duzeltme.sql</c>) önceki sürümün
/// şemasında çalıştırılır. Sonuç şeması EF modeliyle
/// aynıdır (kolon, kısıt, ON DELETE kuralı, indeks). Betiklerin tekrarı hiçbir şeyi değiştirmez: var olan taşıma izi ve
/// verilmiş izinler korunur. Salt okunur durum sorgusu (<c>kontrol/Betik_Durumu.sql</c>) klasördeki her betiği listeler
/// ve öncesini/sonrasını doğru gösterir. 1–20 numaralı betikler canlıya uygulanıp depodan kaldırıldığı için burada yoktur.
/// </summary>
public sealed class CanliyaAlimBetikleriTests
{
    /// <summary>Canlıda çalıştırma sırası (scripts/database/README.md, sıra 22–24).</summary>
    private static readonly string[] Betikler =
        ["20261021_02_Sahadan_Sahaya_Tasima.sql", "20261023_01_Grid_Saha_Gorunumu_Yetkisi.sql",
            "20261023_02_Sevk_Sonrasi_Duzeltme.sql"];

    /// <summary>Önceki sürümün (master) şeması: bu sürümün eklediği tablo, kolon, kısıt ve indeksler yoktur.</summary>
    private const string OncekiSema = """
        DROP TABLE "SahaTasimaKalemleri";
        DROP TABLE "SandikDuzeltmeKayitlari";
        ALTER TABLE "SahaAktarimlari" DROP CONSTRAINT "FK_SahaAktarimlari_Projeler_KaynakSahaProjeId";
        DROP INDEX "IX_SahaAktarimlari_KaynakSahaProjeId";
        DROP INDEX "IX_SahaAktarimlari_IslemAnahtari";
        ALTER TABLE "SahaAktarimlari" DROP COLUMN "KaynakSahaProjeId", DROP COLUMN "IslemAnahtari";
        """;

    /// <summary>Kolon (tip, uzunluk, hassasiyet, boşluk, kimlik), kısıt ve indeks tanımları.</summary>
    private const string SemaSorgusu = """
        SELECT 'S ' || table_name || '.' || column_name || ' ' || data_type || ' ' || coalesce(character_maximum_length::text, '-')
               || ' ' || coalesce(numeric_precision::text, '-') || ',' || coalesce(numeric_scale::text, '-')
               || ' ' || is_nullable || ' ' || is_identity AS "Value"
        FROM information_schema.columns WHERE table_schema = 'public'
        UNION ALL
        SELECT 'K ' || conrelid::regclass::text || ' ' || conname || ' ' || pg_get_constraintdef(oid)
        FROM pg_constraint WHERE connamespace = 'public'::regnamespace
        UNION ALL
        SELECT 'I ' || indexdef FROM pg_indexes WHERE schemaname = 'public'
        """;

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_Betik_SemayiModeleGetirir_TekrarindaTasimaIziniKorur()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            await db.Database.OpenConnectionAsync();
            var modelSemasi = await SemaAsync(db);
            await db.Database.ExecuteSqlRawAsync(OncekiSema);
            await GridIsListesiRolleriniKurAsync(db);

            // Durum sorgusu: klasördeki her betik bir satırdır; önceki sürümde uygulanmamıştır.
            var dosyalar = Directory.GetFiles(Path.GetDirectoryName(Betik(Betikler[0]))!, "*.sql")
                .Select(Path.GetFileNameWithoutExtension).Order(StringComparer.Ordinal).ToList();
            var durum = await BetikDurumuAsync(db);
            Assert.Equal(dosyalar, durum.Keys.Order(StringComparer.Ordinal));
            Assert.All(durum, x => Assert.Equal("UYGULANMAMIŞ", x.Value));

            foreach (var betik in Betikler)
                await CalistirAsync(db, betik);

            Assert.All(await BetikDurumuAsync(db), x => Assert.Equal("uygulanmış", x.Value));
            var gridSaha = await db.MenuTanimlari.AsNoTracking().SingleAsync(m => m.Kod == YetkiKodlari.Sandik.GridSahaGorunumu);
            Assert.Equal((5258, "/grid-saha", "MENU.GRID_SAHA_GORUNUMU", 5, 10), (gridSaha.Id, gridSaha.Route, gridSaha.LabelKey, gridSaha.ParentId!.Value, gridSaha.Sira));
            var izinler = await GridSahaIzinleriAsync(db);
            // Admin W; Grid iş listesini gören PersonelGrid R; Grid iş listesi izni olmayan Personel3K izin almaz.
            Assert.Equal([(SistemRolleri.AdminId, (int)YetkiTipi.W), (3, (int)YetkiTipi.R)], izinler);
            var duzeltme = await db.MenuTanimlari.AsNoTracking().SingleAsync(m => m.Kod == YetkiKodlari.Sandik.SevkSonrasiDuzeltme);
            Assert.Equal((5259, (string?)null, "MENU.SEVK_SONRASI_DUZELTME", 5), (duzeltme.Id, duzeltme.Route, duzeltme.LabelKey, duzeltme.ParentId!.Value));
            var duzeltmeIzinleri = await IzinlerAsync(db, YetkiKodlari.Sandik.SevkSonrasiDuzeltme);
            // Yalnız Admin W; Grid iş listesini gören PersonelGrid dahil başka rol almaz.
            Assert.Equal([(SistemRolleri.AdminId, (int)YetkiTipi.W)], duzeltmeIzinleri);
            var sema = await SemaAsync(db);
            var fark = modelSemasi.Except(sema).Select(x => "eksik: " + x).Concat(sema.Except(modelSemasi).Select(x => "fazla: " + x)).ToList();
            Assert.True(fark.Count == 0, string.Join(Environment.NewLine, fark));

            // Betik sonrası taşıma izi yazılır; tekrar çalıştırma şemayı ve izi değiştirmez.
            var izId = await TasimaIziYazAsync(db);
            foreach (var betik in Betikler)
                await CalistirAsync(db, betik);

            Assert.Equal(sema, await SemaAsync(db));
            Assert.Equal(izinler, await GridSahaIzinleriAsync(db));
            Assert.Equal(1, await db.MenuTanimlari.CountAsync(m => m.Kod == YetkiKodlari.Sandik.GridSahaGorunumu));
            Assert.Equal(duzeltmeIzinleri, await IzinlerAsync(db, YetkiKodlari.Sandik.SevkSonrasiDuzeltme));
            Assert.Equal(1, await db.MenuTanimlari.CountAsync(m => m.Kod == YetkiKodlari.Sandik.SevkSonrasiDuzeltme));
            var iz = await db.SahaTasimaKalemleri.AsNoTracking().SingleAsync();
            Assert.Equal((izId, "PA-SAHA-A", "PA-SAHA-B", 3m), (iz.Id, iz.KaynakSahaProjeNo, iz.HedefSahaProjeNo, iz.Miktar));
            Assert.NotNull((await db.SahaAktarimlari.AsNoTracking().SingleAsync(a => a.Id == iz.SahaAktarimId)).IslemAnahtari);
        });
    }

    /// <summary>
    /// Canlıdaki Grid iş listesi düğümü (EF tohumunda yok) ve onu R ile gören PersonelGrid rolü; 23 numaralı betik bu
    /// rollere Grid Sahası R'si verir, Sira'yı da bu düğümden türetir.
    /// </summary>
    private static async Task GridIsListesiRolleriniKurAsync(AppDbContext db)
    {
        db.MenuTanimlari.Add(new MenuTanimi
        {
            Id = 117, Kod = YetkiKodlari.Sandik.GridIsListesi, LabelKey = "MENU.GRID_IS_LISTESI", Icon = "ri-list-check-3-line",
            Route = null, Sira = 9, ParentId = 5
        });
        db.RolYetkileri.Add(new RolYetki { RolId = 3, MenuTanimiId = 117, YetkiTipiId = (int)YetkiTipi.R });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    private static Task<List<(int RolId, int YetkiTipiId)>> GridSahaIzinleriAsync(AppDbContext db) =>
        IzinlerAsync(db, YetkiKodlari.Sandik.GridSahaGorunumu);

    private static async Task<List<(int RolId, int YetkiTipiId)>> IzinlerAsync(AppDbContext db, string kod) =>
        (await db.RolYetkileri.AsNoTracking()
            .Where(ry => ry.MenuTanimi.Kod == kod)
            .OrderBy(ry => ry.RolId)
            .Select(ry => new { ry.RolId, ry.YetkiTipiId })
            .ToListAsync())
        .Select(x => (x.RolId, x.YetkiTipiId))
        .ToList();

    /// <summary>Kaynak normal satır, iki saha projesi ve hedef sahada tek taşıma izi.</summary>
    private static async Task<int> TasimaIziYazAsync(AppDbContext db)
    {
        var normal = new Proje { ProjeNo = "PA-NORMAL", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Normal };
        var kaynakSaha = new Proje { ProjeNo = "PA-SAHA-A", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Saha };
        var hedefSaha = new Proje { ProjeNo = "PA-SAHA-B", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Saha };
        var kok = new CekiSatiri { Ceki = new Ceki { Proje = normal }, BarkodNo = "B-1", Aciklama = "Ürün", IstenenAdet = 3 };
        var baslik = new SahaAktarim { SahaProje = hedefSaha, KaynakProje = normal, KaynakSahaProje = kaynakSaha, IslemAnahtari = Guid.NewGuid() };
        var iz = new SahaTasimaKalemi
        {
            SahaAktarim = baslik,
            KaynakCekiSatiri = kok,
            KaynakSahaProje = kaynakSaha,
            KaynakSahaProjeNo = kaynakSaha.ProjeNo,
            KaynakSahaSandikNo = "1",
            HedefSahaProje = hedefSaha,
            HedefSahaProjeNo = hedefSaha.ProjeNo,
            HedefSahaSandikNo = "3",
            Miktar = 3
        };
        db.SahaTasimaKalemleri.Add(iz);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return iz.Id;
    }

    private static async Task CalistirAsync(AppDbContext db, string betik)
    {
        var metin = await File.ReadAllTextAsync(Betik(betik));
        try
        {
            await db.Database.ExecuteSqlRawAsync(metin);
        }
        catch (PostgresException hata)
        {
            throw new Xunit.Sdk.XunitException($"{betik}: {hata.MessageText}");
        }
        db.ChangeTracker.Clear();
    }

    private static string Betik(string ad)
    {
        var dizin = new DirectoryInfo(AppContext.BaseDirectory);
        while (dizin != null && !File.Exists(Path.Combine(dizin.FullName, "scripts", "database", ad)))
            dizin = dizin.Parent;
        Assert.NotNull(dizin);
        return Path.Combine(dizin.FullName, "scripts", "database", ad);
    }

    /// <summary>Betik adı → durum (uygulanmış / UYGULANMAMIŞ / tespit edilemez).</summary>
    private static async Task<Dictionary<string, string>> BetikDurumuAsync(AppDbContext db)
    {
        await using var komut = db.Database.GetDbConnection().CreateCommand();
        komut.CommandText = await File.ReadAllTextAsync(Betik(Path.Combine("kontrol", "Betik_Durumu.sql")));
        await using var okuyucu = await komut.ExecuteReaderAsync();
        var sonuc = new Dictionary<string, string>();
        while (await okuyucu.ReadAsync())
            sonuc.Add(okuyucu.GetString(1), okuyucu.GetString(2));
        return sonuc;
    }

    private static async Task<List<string>> SemaAsync(AppDbContext db) =>
        (await db.Database.SqlQueryRaw<string>(SemaSorgusu).ToListAsync()).Order(StringComparer.Ordinal).ToList();
}
