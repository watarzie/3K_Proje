using Microsoft.EntityFrameworkCore;
using Npgsql;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Infrastructure.Data;

namespace _3K.Application.Tests;

/// <summary>
/// Canlıya alım provası: bu sürümün veritabanı betikleri (README sıra 22 <c>20261021_02_Sahadan_Sahaya_Tasima.sql</c>,
/// 23 <c>20261023_01_Grid_Saha_Gorunumu_Yetkisi.sql</c>, 24 <c>20261023_02_Sevk_Sonrasi_Duzeltme.sql</c>,
/// 25 <c>20261024_01_Proje_Baglanti_Turu.sql</c>, 26 <c>20261024_02_Barkod_Urun_Arama_Yetkisi.sql</c>,
/// 27 <c>20261024_03_Sandik_Acilis_Kapanis_Gecmisi.sql</c>, 28 <c>20261025_01_Giris_Kilidi.sql</c>,
/// 29 <c>20261026_01_Barkod_Etiket_Yetkisi.sql</c>; hepsi <c>uygulandi/2026-10/</c> altında; 34
/// <c>bekleyen/20261112_01_Saha_Iptal_Proje_Sevk_Durumu.sql</c>) önceki sürümün şemasında çalıştırılır. Sonuç şeması EF modeliyle
/// aynıdır (kolon, kısıt, ON DELETE kuralı, indeks). Betiklerin tekrarı hiçbir şeyi değiştirmez: var olan taşıma izi ve
/// verilmiş izinler korunur. Salt okunur durum sorgusu (<c>kontrol/Betik_Durumu.sql</c>) klasördeki her betiği listeler
/// ve öncesini/sonrasını doğru gösterir. 1–20 numaralı betikler canlıya uygulanıp depodan kaldırıldığı için burada yoktur.
/// </summary>
public sealed class CanliyaAlimBetikleriTests
{
    /// <summary>Canlıda çalıştırma sırası (scripts/database/README.md, sıra 22–29 ve 34; 30–33 yükleme dalında).</summary>
    private static readonly string[] Betikler =
        [.. new[]
        {
            "20261021_02_Sahadan_Sahaya_Tasima", "20261023_01_Grid_Saha_Gorunumu_Yetkisi",
            "20261023_02_Sevk_Sonrasi_Duzeltme", "20261024_01_Proje_Baglanti_Turu",
            "20261024_02_Barkod_Urun_Arama_Yetkisi", "20261024_03_Sandik_Acilis_Kapanis_Gecmisi",
            "20261025_01_Giris_Kilidi", "20261026_01_Barkod_Etiket_Yetkisi"
        }.Select(ad => $"uygulandi/2026-10/{ad}.sql"), $"bekleyen/{VeriOnarimBetigi}.sql"];

    /// <summary>
    /// 34 numaralı veri onarımı iz bırakmaz: onarılacak proje yoksa uygulanmış görünür. Seçimi ve C# kuralıyla eşliği
    /// <see cref="SahaIptalSevkKapanisPostgresTests"/> sınar.
    /// </summary>
    private const string VeriOnarimBetigi = "20261112_01_Saha_Iptal_Proje_Sevk_Durumu";

    /// <summary>Önceki sürümün (master) şeması: bu sürümün eklediği tablo, kolon, kısıt ve indeksler yoktur.</summary>
    private const string OncekiSema = """
        DROP TABLE "SahaTasimaKalemleri";
        DROP TABLE "SandikDuzeltmeKayitlari";
        ALTER TABLE "SahaAktarimlari" DROP CONSTRAINT "FK_SahaAktarimlari_Projeler_KaynakSahaProjeId";
        DROP INDEX "IX_SahaAktarimlari_KaynakSahaProjeId";
        DROP INDEX "IX_SahaAktarimlari_IslemAnahtari";
        ALTER TABLE "SahaAktarimlari" DROP COLUMN "KaynakSahaProjeId", DROP COLUMN "IslemAnahtari";
        ALTER TABLE "ProjeFotografBaglantilari" DROP COLUMN "Tur";
        DROP TABLE "SandikDurumOlaylari";
        DROP TABLE "LookupSandikAcilmaSebepleri";
        ALTER TABLE "Sandiklar" DROP COLUMN "KapanmaTarihi";
        """;

    /// <summary>
    /// Önceki sürümün kullanıcı tablosu (28 numaralı betikten önce). Hazırlık verisi EF ile kullanıcı yazdığı için
    /// veriden sonra uygulanır.
    /// </summary>
    private const string OncekiKullaniciSemasi = """
        ALTER TABLE "Kullanicilar" DROP CONSTRAINT "CK_Kullanicilar_BasarisizGirisSayisi";
        ALTER TABLE "Kullanicilar" DROP COLUMN "BasarisizGirisSayisi", DROP COLUMN "GirisKilitBitisTarihi";
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
            // Önceki sürümde türü olmayan bağlantı: betik sonrası fotoğraf (1) sayılır.
            var projeId = await ProjeVeTursuzBaglantiYazAsync(db);
            // Önceki sürümde sandık olayları yalnız hareket geçmişindedir (biri arşivde): betik bir kez aktarır.
            var gecmis = await SandikHareketGecmisiYazAsync(db);
            await db.Database.ExecuteSqlRawAsync(OncekiKullaniciSemasi);

            // Durum sorgusu: klasördeki her betik bir satırdır; önceki sürümde uygulanmamıştır.
            // Durum sorgusu bekleyen/ ve uygulandi/ altındaki her betiği listeler (geri-alma/ ve kontrol/ hariç).
            var kok = Path.GetDirectoryName(Betik(Path.Combine("kontrol", "Betik_Durumu.sql")))! + "/..";
            var dosyalar = new[] { "bekleyen", "uygulandi" }
                .SelectMany(k => Directory.GetFiles(Path.Combine(kok, k), "*.sql", SearchOption.AllDirectories))
                .Select(Path.GetFileNameWithoutExtension).Order(StringComparer.Ordinal).ToList();
            Assert.Equal(Betikler.Length, dosyalar.Count);
            var durum = await BetikDurumuAsync(db);
            Assert.Equal(dosyalar, durum.Keys.Order(StringComparer.Ordinal));
            Assert.All(durum, x => Assert.Equal(x.Key == VeriOnarimBetigi ? "uygulanmış" : "UYGULANMAMIŞ", x.Value));

            foreach (var betik in Betikler)
                await CalistirAsync(db, betik);

            Assert.All(await BetikDurumuAsync(db), x => Assert.Equal("uygulanmış", x.Value));
            // Betik kaydı: 34 kendi satırını yazar (kaynak "kayıt"); kayıt bloğundan önceki 22–29 izden tanınır.
            var kayitlar = await BetikKayitlariAsync(db);
            Assert.Equal(new[] { VeriOnarimBetigi }, kayitlar.Keys);
            Assert.All(await BetikKaynagiAsync(db), x => Assert.Equal(x.Key == VeriOnarimBetigi ? "kayıt" : "iz", x.Value));
            var gridSaha = await db.MenuTanimlari.AsNoTracking().SingleAsync(m => m.Kod == YetkiKodlari.Sandik.GridSahaGorunumu);
            // Rotasız izin düğümü: kenar menüde görünmez, ekran proje listesindeki düğmeyle açılır.
            Assert.Equal((5258, (string?)null, "MENU.GRID_SAHA_GORUNUMU", 5, 10), (gridSaha.Id, gridSaha.Route, gridSaha.LabelKey, gridSaha.ParentId!.Value, gridSaha.Sira));
            var izinler = await GridSahaIzinleriAsync(db);
            // Admin W; Grid iş listesini gören PersonelGrid R; Grid iş listesi izni olmayan Personel3K izin almaz.
            Assert.Equal([(SistemRolleri.AdminId, (int)YetkiTipi.W), (3, (int)YetkiTipi.R)], izinler);
            var duzeltme = await db.MenuTanimlari.AsNoTracking().SingleAsync(m => m.Kod == YetkiKodlari.Sandik.SevkSonrasiDuzeltme);
            Assert.Equal((5259, (string?)null, "MENU.SEVK_SONRASI_DUZELTME", 5), (duzeltme.Id, duzeltme.Route, duzeltme.LabelKey, duzeltme.ParentId!.Value));
            var duzeltmeIzinleri = await IzinlerAsync(db, YetkiKodlari.Sandik.SevkSonrasiDuzeltme);
            // Yalnız Admin W; Grid iş listesini gören PersonelGrid dahil başka rol almaz.
            Assert.Equal([(SistemRolleri.AdminId, (int)YetkiTipi.W)], duzeltmeIzinleri);
            var barkod = await db.MenuTanimlari.AsNoTracking().SingleAsync(m => m.Kod == YetkiKodlari.Sandik.BarkodUrunArama);
            // Rotasız izin düğümü: kenar menüde görünmez, ekran üst çubuktaki genel aramadan açılır.
            Assert.Equal((5260, (string?)null, "MENU.BARKOD_URUN_ARAMA", 5), (barkod.Id, barkod.Route, barkod.LabelKey, barkod.ParentId!.Value));
            // 27 numaralı betiğin düğümü (5261) barkod aramadan sonra eklenir.
            Assert.True(barkod.Sira > (await db.MenuTanimlari.AsNoTracking().Where(m => m.ParentId == 5 && m.Id != 5260 && m.Id != 5261 && m.Id != 5262).MaxAsync(m => m.Sira)));
            var barkodIzinleri = await IzinlerAsync(db, YetkiKodlari.Sandik.BarkodUrunArama);
            // Admin W; Sandık Yönetimi'ni gören roller R (tohumdaki rollerin sandik-yonetimi izni).
            Assert.Equal(await SandikYonetimiRolleriAsync(db), barkodIzinleri);
            var baglanti = await db.Set<ProjeFotografBaglantisi>().AsNoTracking().SingleAsync(b => b.ProjeId == projeId);
            Assert.Equal(ProjeBaglantiTuru.Fotograf, baglanti.Tur);
            var istatistik = await db.MenuTanimlari.AsNoTracking().SingleAsync(m => m.Kod == YetkiKodlari.Sandik.SandikAcilisIstatistigi);
            // Rotasız izin düğümü: ekran Projeler ve 3K sandık listesinin "Diğer" menüsünden açılır.
            Assert.Equal((5261, (string?)null, "MENU.SANDIK_ACILIS_ISTATISTIGI", 5), (istatistik.Id, istatistik.Route, istatistik.LabelKey, istatistik.ParentId!.Value));
            Assert.True(istatistik.Sira > (await db.MenuTanimlari.AsNoTracking().Where(m => m.ParentId == 5 && m.Id != 5261 && m.Id != 5262).MaxAsync(m => m.Sira)));
            var istatistikIzinleri = await IzinlerAsync(db, YetkiKodlari.Sandik.SandikAcilisIstatistigi);
            Assert.Equal(await SandikYonetimiRolleriAsync(db), istatistikIzinleri);
            var etiket = await db.MenuTanimlari.AsNoTracking().SingleAsync(m => m.Kod == YetkiKodlari.Sandik.BarkodEtiket);
            // Rotasız izin düğümü: ekran Projeler ve 3K sandık listesinin "Diğer" menüsünden açılır; Sandık Yönetimi'nin sonuncusu.
            Assert.Equal((5262, (string?)null, "MENU.BARKOD_ETIKET", 5), (etiket.Id, etiket.Route, etiket.LabelKey, etiket.ParentId!.Value));
            Assert.True(etiket.Sira > (await db.MenuTanimlari.AsNoTracking().Where(m => m.ParentId == 5 && m.Id != 5262).MaxAsync(m => m.Sira)));
            var etiketIzinleri = await IzinlerAsync(db, YetkiKodlari.Sandik.BarkodEtiket);
            Assert.Equal(await SandikYonetimiRolleriAsync(db), etiketIzinleri);
            Assert.Equal(["Trafo kurulum", "Tadilat"],
                await db.LookupSandikAcilmaSebepleri.AsNoTracking().OrderBy(s => s.Id).Select(s => s.Deger).ToListAsync());
            await GecmisAktarildiAsync(db, gecmis, gecmis.SonKapanma);
            // 28: var olan kullanıcı sayaç 0, kilitsiz başlar.
            Assert.Equal((0, (DateTime?)null), await GirisKilidiAsync(db, gecmis.KullaniciId));
            var sema = await SemaAsync(db);
            var fark = modelSemasi.Except(sema).Select(x => "eksik: " + x).Concat(sema.Except(modelSemasi).Select(x => "fazla: " + x)).ToList();
            Assert.True(fark.Count == 0, string.Join(Environment.NewLine, fark));

            // Betik sonrası taşıma izi yazılır; tekrar çalıştırma şemayı ve izi değiştirmez.
            var izId = await TasimaIziYazAsync(db);
            // Betiğin ilk sürümü düğümü kenar menü rotasıyla kurmuştu: tekrar çalıştırma yalnız rotayı boşaltır.
            await db.Database.ExecuteSqlRawAsync("""UPDATE "MenuTanimlari" SET "Route" = '/grid-saha' WHERE "Kod" = 'grid-saha-gorunumu'""");
            Assert.Equal("UYGULANMAMIŞ", (await BetikDurumuAsync(db))["20261023_01_Grid_Saha_Gorunumu_Yetkisi"]);
            // 26'nın ilk sürümü de kenar menü rotasıyla kurmuştu: tekrar çalıştırma yalnız rotayı boşaltır.
            await db.Database.ExecuteSqlRawAsync("""UPDATE "MenuTanimlari" SET "Route" = '/barkod-arama' WHERE "Kod" = 'barkod-urun-arama'""");
            Assert.Equal("UYGULANMAMIŞ", (await BetikDurumuAsync(db))["20261024_02_Barkod_Urun_Arama_Yetkisi"]);
            // 27: dolu kapanma tarihi tekrarda değişmez; Hazırlanıyor sandıkta kalmış (bayat) tarih boşaltılır.
            var elleTarih = new DateTime(2026, 9, 30, 12, 0, 0);
            await db.Database.ExecuteSqlAsync(
                $"""UPDATE "Sandiklar" SET "KapanmaTarihi" = {elleTarih} WHERE "Id" IN ({gecmis.KapaliSandikId}, {gecmis.AcikSandikId})""");
            // 28: tekrar çalıştırma var olan sayaç ve kilide dokunmaz.
            var kilit = new DateTime(2026, 10, 25, 9, 30, 0);
            await db.Database.ExecuteSqlAsync(
                $"""UPDATE "Kullanicilar" SET "BasarisizGirisSayisi" = 3, "GirisKilitBitisTarihi" = {kilit} WHERE "Id" = {gecmis.KullaniciId}""");
            foreach (var betik in Betikler)
                await CalistirAsync(db, betik);
            Assert.Equal((3, (DateTime?)kilit), await GirisKilidiAsync(db, gecmis.KullaniciId));
            Assert.Null((await db.MenuTanimlari.AsNoTracking().SingleAsync(m => m.Kod == YetkiKodlari.Sandik.GridSahaGorunumu)).Route);
            Assert.Null((await db.MenuTanimlari.AsNoTracking().SingleAsync(m => m.Kod == YetkiKodlari.Sandik.BarkodUrunArama)).Route);

            Assert.Equal(sema, await SemaAsync(db));
            // Tekrar çalıştırma kaydı çoğaltmaz, ilk tarihi korur.
            Assert.Equal(kayitlar, await BetikKayitlariAsync(db));
            Assert.Equal(izinler, await GridSahaIzinleriAsync(db));
            Assert.Equal(1, await db.MenuTanimlari.CountAsync(m => m.Kod == YetkiKodlari.Sandik.GridSahaGorunumu));
            Assert.Equal(duzeltmeIzinleri, await IzinlerAsync(db, YetkiKodlari.Sandik.SevkSonrasiDuzeltme));
            Assert.Equal(1, await db.MenuTanimlari.CountAsync(m => m.Kod == YetkiKodlari.Sandik.SevkSonrasiDuzeltme));
            Assert.Equal(barkodIzinleri, await IzinlerAsync(db, YetkiKodlari.Sandik.BarkodUrunArama));
            Assert.Equal(1, await db.MenuTanimlari.CountAsync(m => m.Kod == YetkiKodlari.Sandik.BarkodUrunArama));
            Assert.Equal(ProjeBaglantiTuru.Fotograf, (await db.Set<ProjeFotografBaglantisi>().AsNoTracking().SingleAsync(b => b.ProjeId == projeId)).Tur);
            Assert.Equal(istatistikIzinleri, await IzinlerAsync(db, YetkiKodlari.Sandik.SandikAcilisIstatistigi));
            Assert.Equal(1, await db.MenuTanimlari.CountAsync(m => m.Kod == YetkiKodlari.Sandik.SandikAcilisIstatistigi));
            Assert.Equal(etiketIzinleri, await IzinlerAsync(db, YetkiKodlari.Sandik.BarkodEtiket));
            Assert.Equal(1, await db.MenuTanimlari.CountAsync(m => m.Kod == YetkiKodlari.Sandik.BarkodEtiket));
            Assert.Equal(2, await db.LookupSandikAcilmaSebepleri.CountAsync());
            // Geçmiş ikinci kez aktarılmaz; dolu kapanma tarihi korunur, açık sandığınki boşalır.
            await GecmisAktarildiAsync(db, gecmis, elleTarih);
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

    /// <summary>
    /// Türü olmayan (önceki sürüm) bağlantı ham SQL ile yazılır; EF modeli Tur kolonunu beklediği için kullanılamaz.
    /// </summary>
    private static async Task<int> ProjeVeTursuzBaglantiYazAsync(AppDbContext db)
    {
        var proje = new Proje { ProjeNo = "PA-FOTO", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Normal };
        db.Projeler.Add(proje);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        await db.Database.ExecuteSqlAsync(
            $"INSERT INTO \"ProjeFotografBaglantilari\" (\"ProjeId\", \"Adres\", \"CreatedDate\") VALUES ({proje.Id}, 'https://foto.ornek.com/pa', now())");
        return proje.Id;
    }

    private sealed record SandikGecmisi(
        int KapaliSandikId, int AcikSandikId, int SevkliSandikId, int KullaniciId, DateTime SonKapanma, DateTime SevkliKapanma);

    /// <summary>
    /// Önceki sürümün sandık hareketleri: kapalı sandıkta kapatma → geri açma (arşivde) → toplu kapatma; açık sandıkta
    /// kapatma → geri açma ve başka bir işlem; sevkli sandıkta arşivde kullanıcısız (0) otomatik kapanma; silinmiş
    /// sandığın ve sayısal olmayan referansın kapatması. Sandık ham SQL ile yazılır (KapanmaTarihi kolonu henüz yok).
    /// </summary>
    private static async Task<SandikGecmisi> SandikHareketGecmisiYazAsync(AppDbContext db)
    {
        var kullanici = new Kullanici { AdSoyad = "Geçmiş Kullanıcı", BasHarf = "GK", Email = "gecmis@ornek.com", RolId = SistemRolleri.AdminId };
        var proje = new Proje { ProjeNo = "PA-GECMIS", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Normal };
        db.Kullanicilar.Add(kullanici);
        db.Projeler.Add(proje);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        async Task<int> SandikAsync(string no, int durumId) =>
            (await db.Database.SqlQueryRaw<int>(
                "INSERT INTO \"Sandiklar\" (\"ProjeId\", \"SandikNo\", \"TipId\", \"DurumId\", \"DepoLokasyonId\", \"SevkiyatDuzeltmeAcikMi\", \"CreatedDate\") " +
                "VALUES ({0}, {1}, 1, {2}, 1, false, now()) RETURNING \"Id\" AS \"Value\"",
                proje.Id, no, durumId).ToListAsync()).Single();
        var kapali = await SandikAsync("1", (int)SandikDurum.Kapandi);
        var acik = await SandikAsync("2", (int)SandikDurum.Hazirlaniyor);
        var sevkli = await SandikAsync("3", (int)SandikDurum.Sevkedildi);
        var t1 = new DateTime(2026, 9, 1, 8, 0, 0);
        var t2 = new DateTime(2026, 9, 2, 9, 0, 0);
        var t3 = new DateTime(2026, 9, 3, 10, 0, 0);
        async Task HareketAsync(bool arsiv, string referansId, string islem, int tip, DateTime tarih, int? kullaniciId = null)
        {
            var kim = kullaniciId ?? kullanici.Id;
            await (arsiv
                ? db.Database.ExecuteSqlAsync($"""
                    INSERT INTO "HareketGecmisleriArsiv" ("ProjeId", "ReferansTipi", "ReferansId", "Islem", "IslemTipiId", "KullaniciId", "Tarih", "Aciklama", "CreatedDate")
                    VALUES ({proje.Id}, 'Sandik', {referansId}, {islem}, NULLIF({tip}, 0), {kim}, {tarih}, 'eski açıklama', now())
                    """)
                : db.Database.ExecuteSqlAsync($"""
                    INSERT INTO "HareketGecmisleri" ("ProjeId", "ReferansTipi", "ReferansId", "Islem", "IslemTipiId", "KullaniciId", "Tarih", "Aciklama", "CreatedDate")
                    VALUES ({proje.Id}, 'Sandik', {referansId}, {islem}, NULLIF({tip}, 0), {kim}, {tarih}, 'eski açıklama', now())
                    """));
        }
        await HareketAsync(false, kapali.ToString(), "Sandık Manuel Kapatma", (int)IslemTipi.SandikKapatildi, t1);
        await HareketAsync(true, kapali.ToString(), "Sandık Geri Açıldı", 0, t2);
        await HareketAsync(false, kapali.ToString(), "Toplu Sandık Kapatma", (int)IslemTipi.TopluSandikKapatildi, t3);
        await HareketAsync(false, acik.ToString(), "Sandık Manuel Kapatma", (int)IslemTipi.SandikKapatildi, t1);
        await HareketAsync(false, acik.ToString(), "Sandık Geri Açıldı", 0, t2);
        await HareketAsync(false, acik.ToString(), "Sandık Lokasyon Güncellendi", (int)IslemTipi.SandikLokasyonGuncellendi, t3);
        // Arşivde kullanıcısı bilinmeyen (0) otomatik kapanma: olayın kullanıcısı boş kalır.
        await HareketAsync(true, sevkli.ToString(), "Sandık Otomatik Hazır", (int)IslemTipi.SandikOtomatikHazirlandi, t2, kullaniciId: 0);
        // Silinmiş sandığın ve sayısal olmayan referansın hareketi aktarılmaz.
        await HareketAsync(false, "999999", "Sandık Manuel Kapatma", (int)IslemTipi.SandikKapatildi, t1);
        await HareketAsync(false, "S-1", "Sandık Manuel Kapatma", (int)IslemTipi.SandikKapatildi, t1);
        return new SandikGecmisi(kapali, acik, sevkli, kullanici.Id, t3, t2);
    }

    private static async Task GecmisAktarildiAsync(AppDbContext db, SandikGecmisi gecmis, DateTime kapaliKapanma)
    {
        var olaylar = await db.SandikDurumOlaylari.AsNoTracking().OrderBy(o => o.SandikId).ThenBy(o => o.Tarih).ToListAsync();
        Assert.Equal(
            [(gecmis.KapaliSandikId, SandikDurumOlayTuru.Kapandi), (gecmis.KapaliSandikId, SandikDurumOlayTuru.Acildi),
             (gecmis.KapaliSandikId, SandikDurumOlayTuru.Kapandi), (gecmis.AcikSandikId, SandikDurumOlayTuru.Kapandi),
             (gecmis.AcikSandikId, SandikDurumOlayTuru.Acildi), (gecmis.SevkliSandikId, SandikDurumOlayTuru.Kapandi)],
            olaylar.Select(o => (o.SandikId!.Value, o.Tur)));
        Assert.All(olaylar, o =>
        {
            Assert.Equal(SandikDurumOlayKaynagi.GecmistenAktarildi, o.Kaynak);
            Assert.Equal(o.SandikId == gecmis.SevkliSandikId ? null : gecmis.KullaniciId, o.KullaniciId);
            Assert.Null(o.SebepId);
            Assert.StartsWith("Geçmiş kayıt: ", o.Not);
        });
        var sandiklar = await db.Sandiklar.AsNoTracking()
            .Where(s => s.Id == gecmis.KapaliSandikId || s.Id == gecmis.AcikSandikId || s.Id == gecmis.SevkliSandikId)
            .ToDictionaryAsync(s => s.Id, s => s.KapanmaTarihi);
        // Kapalı ve sevkli sandık son kapanmayı alır; açık sandığın kapanma tarihi yoktur.
        Assert.Equal(kapaliKapanma, sandiklar[gecmis.KapaliSandikId]);
        Assert.Equal(gecmis.SevkliKapanma, sandiklar[gecmis.SevkliSandikId]);
        Assert.Null(sandiklar[gecmis.AcikSandikId]);
    }

    /// <summary>
    /// Sandık Yönetimi'nden türeyen salt okuma izinlerinin (barkod arama 26, açılış istatistiği 27, barkod etiketi 29)
    /// beklentisi: Admin W, sandik-yonetimi'nde R/W olan diğer roller R.
    /// </summary>
    private static async Task<List<(int RolId, int YetkiTipiId)>> SandikYonetimiRolleriAsync(AppDbContext db) =>
        (await IzinlerAsync(db, YetkiKodlari.Sandik.Yonetimi))
        .Where(x => x.YetkiTipiId is (int)YetkiTipi.R or (int)YetkiTipi.W)
        .Select(x => (x.RolId, x.RolId == SistemRolleri.AdminId ? (int)YetkiTipi.W : (int)YetkiTipi.R))
        .Union([(SistemRolleri.AdminId, (int)YetkiTipi.W)])
        .OrderBy(x => x.Item1)
        .ToList();

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

    private static async Task<(int Sayac, DateTime? Kilit)> GirisKilidiAsync(AppDbContext db, int kullaniciId)
    {
        var kullanici = await db.Kullanicilar.AsNoTracking().SingleAsync(k => k.Id == kullaniciId);
        return (kullanici.BasarisizGirisSayisi, kullanici.GirisKilitBitisTarihi);
    }

    /// <summary>
    /// Şema (kayıt tablosu hariç). "BetikKayitlari" EF modelinde yoktur: uygulama okumaz, betiklerin kendisi kurar ve yazar
    /// (scripts/database/README.md, "Betik kaydı").
    /// </summary>
    private static async Task<List<string>> SemaAsync(AppDbContext db) =>
        (await db.Database.SqlQueryRaw<string>(SemaSorgusu).ToListAsync())
        .Where(x => !x.Contains("BetikKayitlari", StringComparison.Ordinal))
        .Order(StringComparer.Ordinal).ToList();

    /// <summary>Betik adı → ilk uygulama zamanı ("BetikKayitlari").</summary>
    private static async Task<Dictionary<string, DateTime>> BetikKayitlariAsync(AppDbContext db)
    {
        await using var komut = db.Database.GetDbConnection().CreateCommand();
        komut.CommandText = "SELECT \"BetikAdi\", \"UygulamaTarihi\" FROM \"BetikKayitlari\" ORDER BY \"BetikAdi\"";
        await using var okuyucu = await komut.ExecuteReaderAsync();
        var sonuc = new Dictionary<string, DateTime>();
        while (await okuyucu.ReadAsync())
            sonuc.Add(okuyucu.GetString(0), okuyucu.GetDateTime(1));
        return sonuc;
    }

    /// <summary>Betik adı → durum sorgusunun kaynak sütunu (kayıt / iz / -).</summary>
    private static async Task<Dictionary<string, string>> BetikKaynagiAsync(AppDbContext db)
    {
        await using var komut = db.Database.GetDbConnection().CreateCommand();
        komut.CommandText = await File.ReadAllTextAsync(Betik(Path.Combine("kontrol", "Betik_Durumu.sql")));
        await using var okuyucu = await komut.ExecuteReaderAsync();
        var sonuc = new Dictionary<string, string>();
        while (await okuyucu.ReadAsync())
            sonuc.Add(okuyucu.GetString(1), okuyucu.GetString(3));
        return sonuc;
    }
}
