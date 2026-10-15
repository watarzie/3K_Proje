using Microsoft.EntityFrameworkCore;
using Npgsql;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Infrastructure.Data;

namespace _3K.Application.Tests;

/// <summary>
/// Canlıya alım provası: bu sürümün veritabanı betikleri (README sıra 9–18) önceki sürümün şemasında ve eski Finans yetki
/// ağacında ad sırasıyla çalıştırılır. Sonuç şeması EF modeliyle aynıdır, Finans yalnız sekme/alt sekme düğümlerinde
/// kalır. Betiklerin hepsi (onay ayarı oturumda açık kalsa da) tekrar çalıştırılınca hiçbir şey değişmez: yöneticinin
/// sonradan kaldırdığı veya düşürdüğü izinler geri verilmez, yeni düğümler silinmez. Salt okunur durum sorgusu
/// (<c>kontrol/Betik_Durumu.sql</c>) klasördeki her betiği listeler ve öncesini/sonrasını doğru gösterir.
/// </summary>
public sealed class CanliyaAlimBetikleriTests
{
    /// <summary>Canlıda çalıştırma sırası (scripts/database/README.md, sıra 9–18).</summary>
    private static readonly string[] Betikler =
    [
        "20261008_01_SandikIcerik_UrunBilgisi.sql",
        "20261008_02_Finans_Ahsap_Kontra_Hacim.sql",
        "20261008_03_Finans_Sekme_Yetkileri.sql",
        "20261008_04_Sevk_Seferleri.sql",
        "20261008_05_Sevkiyatlar_Yetkisi.sql",
        "20261013_01_Finans_Malzemeli_Tarife.sql",
        "20261013_02_Finans_Sekme_Yetkisine_Gecis.sql",
        "20261013_03_Finans_Alt_Sekme_Yetkileri.sql",
        "20261014_01_Proje_Fotograf_Baglantisi.sql",
        "20261014_02_Proje_Fotograf_Baglantisi_Projeler_Altina.sql",
    ];
    private const string YetkiGecisi = "20261013_02_Finans_Sekme_Yetkisine_Gecis.sql";

    /// <summary>Önceki sürümün (master) şeması: bu sürümün eklediği tablo, kolon, kısıt ve indeksler yoktur.</summary>
    private const string OncekiSema = """
        ALTER TABLE "Sevkiyatlar" DROP COLUMN "SevkSeferiId";
        DROP TABLE "SevkSeferleri";
        ALTER TABLE "SandikIcerikleri" DROP COLUMN "UrunBilgisi";
        ALTER TABLE "Projeler" DROP COLUMN "FotografBaglantisi";
        ALTER TABLE "FinansUrunEslesmeleri" DROP COLUMN "Malzeme";
        ALTER TABLE "FinansIsKayitlari" DROP CONSTRAINT "CK_FinansIsKayitlari_AhsapFiyat";
        ALTER TABLE "FinansIsKayitlari" DROP CONSTRAINT "CK_FinansIsKayitlari_Miktarlar";
        ALTER TABLE "FinansIsKayitlari" DROP COLUMN "AhsapM3", DROP COLUMN "KontraM3", DROP COLUMN "HacimManuel",
            DROP COLUMN "AhsapBirimFiyatSnapshot", DROP COLUMN "AhsapFinansUrunId", DROP COLUMN "AhsapTarifeIdSnapshot";
        ALTER TABLE "FinansIsKayitlari" ADD CONSTRAINT "CK_FinansIsKayitlari_Miktarlar"
            CHECK ("Adet" > 0 AND "BirimM3" >= 0 AND "ToplamM3" >= 0);
        """;

    /// <summary>
    /// Kolon (tip, uzunluk, hassasiyet, boşluk, kimlik), kısıt ve indeks tanımları. Varsayılan değer karşılaştırılmaz:
    /// betik NOT NULL "HacimManuel" kolonunu dolu tabloya DEFAULT FALSE ile ekler, EF modeli varsayılan tanımlamaz.
    /// </summary>
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
    public async Task Postgres_Betikler_CanliSirasiylaSemayiModeleGetirir_TekrarindaYoneticiKararlariniKorur()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            // Tek oturum: onay ayarı (SET) pgAdmin penceresindeki gibi sonraki betiklerde de açık kalır.
            await db.Database.OpenConnectionAsync();
            var modelSemasi = await SemaAsync(db);

            await db.Database.ExecuteSqlRawAsync(OncekiSema);
            await FinansSekmeYetkileriTests.CanliAgaciKurAsync(db);
            var projeler = await db.MenuTanimlari.AsNoTracking().SingleAsync(x => x.Kod == YetkiKodlari.Proje.Kok);
            var sevkEdilen = await db.MenuTanimlari.AsNoTracking().SingleAsync(x => x.Kod == "sevk-edilen");
            db.RolYetkileri.AddRange(
                new RolYetki { RolId = SistemRolleri.AdminId, MenuTanimiId = projeler.Id, YetkiTipiId = (int)YetkiTipi.W },
                new RolYetki { RolId = 14, MenuTanimiId = projeler.Id, YetkiTipiId = (int)YetkiTipi.R },
                new RolYetki { RolId = 14, MenuTanimiId = sevkEdilen.Id, YetkiTipiId = (int)YetkiTipi.R });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            // Durum sorgusu: klasördeki her betik bir satırdır; önceki sürümde 1–8 uygulanmış (4 iz bırakmaz), 9–18 değil.
            var dosyalar = Directory.GetFiles(Path.GetDirectoryName(FinansSekmeAgaci.BetikYolu())!, "*.sql")
                .Select(Path.GetFileNameWithoutExtension).Order(StringComparer.Ordinal).ToList();
            var durum = await BetikDurumuAsync(db);
            Assert.Equal(dosyalar, durum.Keys.Order(StringComparer.Ordinal));
            var dalBetikleri = Betikler.Select(Path.GetFileNameWithoutExtension).ToHashSet();
            Assert.All(durum, x => Assert.Equal(
                dalBetikleri.Contains(x.Key) ? "UYGULANMAMIŞ" : x.Key.StartsWith("20260928_04") ? "tespit edilemez" : "uygulanmış", x.Value));

            foreach (var betik in Betikler)
            {
                if (betik == YetkiGecisi)
                {
                    // Önce onaysız: silinecekleri listeler, hiçbir şeyi değiştirmeden durur (transaction geri alınır).
                    var onceki = await DurumAsync(db);
                    var hata = await Assert.ThrowsAsync<PostgresException>(() => CalistirAsync(db, betik));
                    Assert.Contains("Onay yok", hata.MessageText);
                    await db.Database.ExecuteSqlRawAsync("ROLLBACK");
                    AyniDurum(onceki, await DurumAsync(db));
                    await db.Database.ExecuteSqlRawAsync("SELECT set_config('uc_k.finans_sekme_yetkisi', {0}, false)",
                        db.Database.GetDbConnection().Database);
                }
                await CalistirAsync(db, betik);
            }

            Assert.All(await BetikDurumuAsync(db), x =>
                Assert.Equal(x.Key.StartsWith("20260928_04") ? "tespit edilemez" : "uygulanmış", x.Value));

            var sema = await SemaAsync(db);
            var fark = modelSemasi.Except(sema).Select(x => "eksik: " + x).Concat(sema.Except(modelSemasi).Select(x => "fazla: " + x)).ToList();
            Assert.True(fark.Count == 0, string.Join(Environment.NewLine, fark));

            var menuler = await db.MenuTanimlari.AsNoTracking().ToListAsync();
            var sekmeIdleri = menuler.Where(x => FinansSekmeAgaci.Sekmeler.Contains(x.Kod)).Select(x => x.Id).ToHashSet();
            Assert.Equal([.. FinansSekmeAgaci.Sekmeler, YetkiKodlari.Finans.VadeUyarisi],
                menuler.Where(x => x.ParentId == FinansSekmeAgaci.KokId).OrderBy(x => x.Id).Select(x => x.Kod));
            Assert.Equal(FinansSekmeAgaci.AltSekmeler.Select(x => x.Kod),
                menuler.Where(x => sekmeIdleri.Contains(x.ParentId ?? 0)).OrderBy(x => x.Id).Select(x => x.Kod));
            Assert.Equal([(5246, "sevkiyatlar"), (5256, YetkiKodlari.Proje.FotografBaglantisi)],
                menuler.Where(x => x.ParentId == projeler.Id && x.Id > 5000).OrderBy(x => x.Id).Select(x => (x.Id, x.Kod)));
            var izinler = await IzinlerAsync(db);
            Assert.Contains($"14 5246 {(int)YetkiTipi.R}", izinler);
            Assert.Contains($"{SistemRolleri.AdminId} 5256 {(int)YetkiTipi.W}", izinler);
            Assert.Contains($"14 {FinansSekmeAgaci.SekmeId(YetkiKodlari.Finans.SekmePoFatura)} {(int)YetkiTipi.W}", izinler);

            // Yönetici rol ekranında izin kaldırdı (N = satır yok) veya düşürdü; betiklerin tekrarı bunları geri vermez.
            int Id(string kod) => menuler.Single(x => x.Kod == kod).Id;
            int faturalar = Id(YetkiKodlari.Finans.SekmePoFaturaFatura), munferit = Id(YetkiKodlari.Finans.SekmeMunferitIsler),
                duzenli = Id(YetkiKodlari.Finans.SekmeTanimlarDuzenli), panel = Id(YetkiKodlari.Finans.SekmePanel);
            await db.RolYetkileri.Where(x =>
                    (x.RolId == 14 && x.MenuTanimiId == 5246) ||
                    (x.RolId == SistemRolleri.AdminId && x.MenuTanimiId == 5256) ||
                    (x.RolId == 14 && x.MenuTanimiId == faturalar) ||
                    (x.RolId == SistemRolleri.AdminId && x.MenuTanimiId == munferit) ||
                    (x.RolId == SistemRolleri.AdminId && x.MenuTanimiId == duzenli) ||
                    (x.RolId == SistemRolleri.AdminId && x.MenuTanimiId == FinansSekmeAgaci.VadeUyarisiId))
                .ExecuteDeleteAsync();
            await db.RolYetkileri.Where(x => x.RolId == SistemRolleri.AdminId && x.MenuTanimiId == panel)
                .ExecuteUpdateAsync(x => x.SetProperty(r => r.YetkiTipiId, (int)YetkiTipi.R));
            var kararlar = await DurumAsync(db);

            foreach (var betik in Betikler)
                await CalistirAsync(db, betik);

            AyniDurum(kararlar, await DurumAsync(db));
            Assert.Equal(sema, await SemaAsync(db));
        });
    }

    /// <summary>
    /// Sıfırlama (20260929_01) Finans/Ambalaj tablolarını GÜNCEL EF modeliyle kurar: tek başına çalıştırıldığında şema
    /// modelle aynıdır (canlıda sıfırlamadan sonra eksik kolon hatası olmaz). Sonradan 10 ve 14 numaralı betikler
    /// (20261008_02, 20261013_01) çalıştırılsa da şema değişmez. Finans/Ambalaj'a yeni kolon eklenip sıfırlama betiği
    /// güncellenmezse bu test kırılır. Başka tablodan Finans/Ambalaj'a yabancı anahtar olmadığı için sıfırlama durmaz.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_Sifirlama_TekBasinaSemayiModeleGetirir_SonrakiBetiklerDegistirmez()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            await db.Database.OpenConnectionAsync();
            var modelSemasi = await SemaAsync(db);
            await db.Database.ExecuteSqlRawAsync("SELECT set_config('uc_k.finans_ambalaj_sifirla', {0}, false)",
                db.Database.GetDbConnection().Database);

            await CalistirAsync(db, "20260929_01_Finans_Ambalaj_Sifirlama.sql");
            var sema = await SemaAsync(db);
            var fark = modelSemasi.Except(sema).Select(x => "eksik: " + x).Concat(sema.Except(modelSemasi).Select(x => "fazla: " + x)).ToList();
            Assert.True(fark.Count == 0, string.Join(Environment.NewLine, fark));

            await CalistirAsync(db, "20261008_02_Finans_Ahsap_Kontra_Hacim.sql");
            await CalistirAsync(db, "20261013_01_Finans_Malzemeli_Tarife.sql");
            Assert.Equal(sema, await SemaAsync(db));
        });
    }

    private static async Task CalistirAsync(AppDbContext db, string betik)
    {
        var metin = await File.ReadAllTextAsync(FinansSekmeAgaci.BetikYolu(betik));
        try
        {
            await db.Database.ExecuteSqlRawAsync(metin);
        }
        catch (PostgresException hata) when (!hata.MessageText.Contains("Onay yok"))
        {
            throw new Xunit.Sdk.XunitException($"{betik}: {hata.MessageText}");
        }
        db.ChangeTracker.Clear();
    }

    private static void AyniDurum((List<string> Menuler, List<string> Izinler) beklenen, (List<string> Menuler, List<string> Izinler) gercek)
    {
        Assert.Equal(beklenen.Menuler, gercek.Menuler);
        Assert.Equal(beklenen.Izinler, gercek.Izinler);
    }

    /// <summary>Betik adı → durum (uygulanmış / UYGULANMAMIŞ / tespit edilemez).</summary>
    private static async Task<Dictionary<string, string>> BetikDurumuAsync(AppDbContext db)
    {
        await using var komut = db.Database.GetDbConnection().CreateCommand();
        komut.CommandText = await File.ReadAllTextAsync(FinansSekmeAgaci.BetikYolu(Path.Combine("kontrol", "Betik_Durumu.sql")));
        await using var okuyucu = await komut.ExecuteReaderAsync();
        var sonuc = new Dictionary<string, string>();
        while (await okuyucu.ReadAsync())
            sonuc.Add(okuyucu.GetString(1), okuyucu.GetString(2));
        return sonuc;
    }

    private static async Task<List<string>> SemaAsync(AppDbContext db) =>
        (await db.Database.SqlQueryRaw<string>(SemaSorgusu).ToListAsync()).Order(StringComparer.Ordinal).ToList();

    /// <summary>Menü ağacı ve rol izinleri (rol ekranının kaydettiği biçimde).</summary>
    private static async Task<(List<string> Menuler, List<string> Izinler)> DurumAsync(AppDbContext db) =>
        ((await db.MenuTanimlari.AsNoTracking().OrderBy(x => x.Id)
                .Select(x => new { x.Id, x.Kod, x.ParentId, x.Sira, x.Icon, x.Route, x.LabelKey }).ToListAsync())
            .Select(x => $"{x.Id} {x.Kod} {x.ParentId} {x.Sira} {x.Icon} {x.Route} {x.LabelKey}").ToList(),
         await IzinlerAsync(db));

    private static async Task<List<string>> IzinlerAsync(AppDbContext db) =>
        (await db.RolYetkileri.AsNoTracking().OrderBy(x => x.RolId).ThenBy(x => x.MenuTanimiId)
                .Select(x => new { x.RolId, x.MenuTanimiId, x.YetkiTipiId }).ToListAsync())
            .Select(x => $"{x.RolId} {x.MenuTanimiId} {x.YetkiTipiId}").ToList();
}
