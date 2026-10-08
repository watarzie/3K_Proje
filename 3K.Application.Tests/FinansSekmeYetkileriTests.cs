using Microsoft.EntityFrameworkCore;
using Npgsql;
using _3K.Application.Common;
using _3K.Application.Features.FinansIslemleri.Queries;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Application.Tests;

/// <summary>
/// Finans sekme yetki düğümleri: sekme düğümü altındaki işlem kodlarının tavanıdır; sekmeye özel uçlar sekme
/// kodunu mevcut kodlarla birlikte (AND) ister; göç betiği izinleri azaltmadan düğümleri ekleyip kodları taşır.
/// </summary>
public sealed class FinansSekmeYetkileriTests
{
    private const int Kok = 47, SekmePoFatura = 5240, PoGir = 5211;

    [Fact]
    public void TestFiksturu_GocBetigindekiSekmeAltlariylaAyni()
    {
        Assert.Equal(
            FinansSekmeAgaci.SekmeAltlari.OrderBy(x => x.Key, StringComparer.Ordinal),
            FinansSekmeAgaci.BetiktekiAltKodlar().OrderBy(x => x.Key, StringComparer.Ordinal));
        var betik = File.ReadAllText(FinansSekmeAgaci.BetikYolu());
        Assert.All(FinansSekmeAgaci.Sekmeler, sekme => Assert.Contains($"'{sekme}'", betik));
    }

    [Theory]
    [InlineData(1, 3, 1)]
    [InlineData(2, 3, 2)]
    [InlineData(3, 3, 3)]
    [InlineData(3, 2, 2)]
    public void SekmeDugumu_AltindakiIslemKodununTavanidir(int sekme, int modul, int beklenen)
    {
        var agac = new YetkiAgaci([
            new YetkiDugumu(Kok, YetkiKodlari.Finans.Modul, null),
            new YetkiDugumu(SekmePoFatura, YetkiKodlari.Finans.SekmePoFatura, Kok),
            new YetkiDugumu(PoGir, YetkiKodlari.Finans.PoGir, SekmePoFatura)
        ]);
        var seviyeler = agac.RolSeviyeleri(new Dictionary<int, int> { [Kok] = modul, [SekmePoFatura] = sekme, [PoGir] = 3 });
        Assert.Equal((YetkiTipi)beklenen, seviyeler[PoGir]);
    }

    [Theory]
    [InlineData(typeof(FinansPanelQuery), YetkiKodlari.Finans.SekmePanel, YetkiKodlari.Finans.Modul)]
    [InlineData(typeof(FinansAylikIslerQuery), YetkiKodlari.Finans.SekmeIsTakibi, YetkiKodlari.Finans.KayitGoruntule)]
    [InlineData(typeof(FinansProjeSandikKirilimiQuery), YetkiKodlari.Finans.SekmeIsTakibi, YetkiKodlari.Finans.KayitGoruntule)]
    [InlineData(typeof(FinansAylikRaporDosyaQuery), YetkiKodlari.Finans.SekmeIsTakibi, YetkiKodlari.Finans.RaporGoruntule)]
    [InlineData(typeof(FinansSiparisDurumRaporuDosyaQuery), YetkiKodlari.Finans.SekmeIsTakibi, YetkiKodlari.Finans.RaporGoruntule)]
    [InlineData(typeof(FinansGiderlerQuery), YetkiKodlari.Finans.SekmeGiderler, YetkiKodlari.Finans.GiderGoruntule)]
    [InlineData(typeof(FinansIsRaporuDosyaQuery), YetkiKodlari.Finans.SekmeRaporlar, YetkiKodlari.Finans.RaporGoruntule)]
    [InlineData(typeof(FinansOzetRaporuDosyaQuery), YetkiKodlari.Finans.SekmeRaporlar, YetkiKodlari.Finans.RaporGoruntule)]
    [InlineData(typeof(FinansAlacakRaporuDosyaQuery), YetkiKodlari.Finans.SekmeRaporlar, YetkiKodlari.Finans.RaporGoruntule)]
    [InlineData(typeof(FinansRaporOnizlemeQuery), YetkiKodlari.Finans.SekmeRaporlar, YetkiKodlari.Finans.RaporGoruntule)]
    public void SekmeyeOzelUc_SekmeKodunuMevcutKodlaBirlikteIster(Type istekTipi, string sekme, string mevcut)
    {
        var istek = Assert.IsAssignableFrom<IRequiresMenuPermissions>(Activator.CreateInstance(istekTipi));
        Assert.Equal(MenuPermissionMatch.All, istek.PermissionMatch);
        Assert.Contains(MenuPermissionRequirement.Read(sekme), istek.RequiredMenuPermissions);
        Assert.Contains(MenuPermissionRequirement.Read(mevcut), istek.RequiredMenuPermissions);
    }

    /// <summary>Birden çok sekmenin kullandığı uçlar sekme koduna bağlanmaz (kayıt detayı, genel arama, gider raporu…).</summary>
    [Theory]
    [InlineData(typeof(FinansGenelAramaQuery))]
    [InlineData(typeof(FinansIsKaydiGetirQuery))]
    [InlineData(typeof(FinansYaslandirmaQuery))]
    [InlineData(typeof(FinansGiderRaporuDosyaQuery))]
    [InlineData(typeof(FinansParametrelerQuery))]
    public void PaylasilanUc_SekmeKoduIstemez(Type istekTipi)
    {
        var istek = Activator.CreateInstance(istekTipi)!;
        var kodlar = ((IRequiresMenuPermissions)istek).RequiredMenuPermissions.Select(x => x.MenuKod)
            .Append(((IRequiresMenuPermission)istek).RequiredMenuKod);
        Assert.DoesNotContain(kodlar, x => x.StartsWith("finans-sekme-", StringComparison.Ordinal));
    }

    /// <summary>
    /// Canlı ağacın kopyası (kök 47 + 33 yaprak, 08.10.2026 itibarıyla) üzerinde göç: sekme düğümleri eklenir, rollere
    /// sekme seviyesi kodlar taşınmadan önce verilir, bütün mevcut kodların etkin seviyesi değişmez; ikinci çalıştırma
    /// sonradan değiştirilmiş sekme iznine dokunmaz; beklenen kod eksikse hiçbir şey yapmadan durur.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_GocBetigi_SekmeleriEkler_EtkinIzinleriKorur_TekrarCalisir()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            await CanliAgaciKurAsync(db);
            int[] roller = [SistemRolleri.AdminId, 14, 15, 16, 30];
            var once = await EtkinSeviyelerAsync(db, roller);
            var betik = await File.ReadAllTextAsync(FinansSekmeAgaci.BetikYolu());

            await db.Database.ExecuteSqlRawAsync(betik);
            db.ChangeTracker.Clear();

            var menuler = await db.MenuTanimlari.AsNoTracking().ToDictionaryAsync(x => x.Kod);
            foreach (var sekme in FinansSekmeAgaci.Sekmeler)
            {
                var dugum = menuler[sekme];
                Assert.Equal((Kok, (string?)null), (dugum.ParentId, dugum.Route));
                Assert.InRange(dugum.Sira, 1, 8);
            }
            foreach (var (alt, sekme) in FinansSekmeAgaci.SekmeAltlari)
                Assert.Equal(menuler[sekme].Id, menuler[alt].ParentId);
            Assert.Equal(Kok, menuler[YetkiKodlari.Finans.KayitGoruntule].ParentId);
            Assert.Equal(Kok, menuler[YetkiKodlari.Finans.ManuelIsEkle].ParentId);

            var sonra = await EtkinSeviyelerAsync(db, roller);
            Assert.All(once, x => Assert.True(sonra[x.Key] == x.Value, $"{x.Key}: {x.Value} → {sonra[x.Key]}"));

            Assert.All(FinansSekmeAgaci.Sekmeler, s => Assert.Equal(YetkiTipi.W, sonra[(SistemRolleri.AdminId, s)]));
            Assert.Equal(
                [YetkiTipi.W, YetkiTipi.W, YetkiTipi.W, YetkiTipi.W, YetkiTipi.W, YetkiTipi.W, YetkiTipi.R, YetkiTipi.W],
                FinansSekmeAgaci.Sekmeler.Select(s => sonra[(14, s)]));
            Assert.Equal(
                [YetkiTipi.W, YetkiTipi.N, YetkiTipi.N, YetkiTipi.N, YetkiTipi.N, YetkiTipi.N, YetkiTipi.N, YetkiTipi.N],
                FinansSekmeAgaci.Sekmeler.Select(s => sonra[(15, s)]));
            Assert.Equal(
                [YetkiTipi.R, YetkiTipi.R, YetkiTipi.R, YetkiTipi.N, YetkiTipi.N, YetkiTipi.N, YetkiTipi.N, YetkiTipi.N],
                FinansSekmeAgaci.Sekmeler.Select(s => sonra[(30, s)]));

            // Kimlik dizisi en büyük Id'ye çekilir; sonraki elle eklenen menü tanımı çakışmaz.
            var sonDeger = await db.Database.SqlQueryRaw<long?>(
                "SELECT pg_sequence_last_value(pg_get_serial_sequence('\"MenuTanimlari\"', 'Id')::regclass) AS \"Value\"").SingleAsync();
            Assert.True(sonDeger >= 5245, $"Menü kimlik dizisi: {sonDeger}");

            // Yönetici sekme iznini sonradan R'ye indirdi: betiğin tekrarı bu kararı ezmez, yeni satır eklemez.
            var poSekmesi = menuler[YetkiKodlari.Finans.SekmePoFatura].Id;
            var izin = await db.RolYetkileri.SingleAsync(x => x.RolId == 14 && x.MenuTanimiId == poSekmesi);
            izin.YetkiTipiId = (int)YetkiTipi.R;
            await db.SaveChangesAsync();
            var satirSayisi = await db.RolYetkileri.CountAsync();
            await db.Database.ExecuteSqlRawAsync(betik);
            db.ChangeTracker.Clear();
            Assert.Equal(satirSayisi, await db.RolYetkileri.CountAsync());
            Assert.Equal((int)YetkiTipi.R,
                (await db.RolYetkileri.SingleAsync(x => x.RolId == 14 && x.MenuTanimiId == poSekmesi)).YetkiTipiId);
            Assert.Equal(8, await db.MenuTanimlari.CountAsync(x => x.Kod.StartsWith("finans-sekme-")));

            // Beklenen kod eksikse betik hiçbir şey değiştirmeden durur.
            var odeme = menuler[YetkiKodlari.Finans.OdemeKaydet].Id;
            await db.RolYetkileri.Where(x => x.MenuTanimiId == odeme).ExecuteDeleteAsync();
            await db.MenuTanimlari.Where(x => x.Id == odeme).ExecuteDeleteAsync();
            var hata = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync(betik));
            Assert.Contains(YetkiKodlari.Finans.OdemeKaydet, hata.MessageText);
        });
    }

    private static async Task<Dictionary<(int Rol, string Kod), YetkiTipi>> EtkinSeviyelerAsync(AppDbContext db, int[] roller)
    {
        var menuler = await db.MenuTanimlari.AsNoTracking().Select(x => new YetkiDugumu(x.Id, x.Kod, x.ParentId)).ToListAsync();
        var kodlar = menuler.ToDictionary(x => x.Id, x => x.Kod);
        var agac = new YetkiAgaci(menuler);
        var sonuc = new Dictionary<(int, string), YetkiTipi>();
        foreach (var rol in roller)
        {
            var atanmis = await db.RolYetkileri.AsNoTracking().Where(x => x.RolId == rol)
                .ToDictionaryAsync(x => x.MenuTanimiId, x => x.YetkiTipiId);
            foreach (var (id, seviye) in agac.RolSeviyeleri(atanmis))
                if (kodlar[id].StartsWith("finans-", StringComparison.Ordinal)) sonuc[(rol, kodlar[id])] = seviye;
        }
        return sonuc;
    }

    /// <summary>Canlı finans ağacının (MenuTanimlari 47 + 5200–5237) ve rollerinin 08.10.2026 SELECT dökümü.</summary>
    private static async Task CanliAgaciKurAsync(AppDbContext db)
    {
        (int Id, string Kod)[] yapraklar =
        [
            (5200, YetkiKodlari.Finans.GelirGoruntule), (5201, YetkiKodlari.Finans.GiderGoruntule),
            (5203, YetkiKodlari.Finans.SiparisOperasyonGoruntule), (5206, YetkiKodlari.Finans.RaporGoruntule),
            (5207, YetkiKodlari.Finans.ManuelIsEkle), (5209, YetkiKodlari.Finans.IsIptal),
            (5210, YetkiKodlari.Finans.TarihDegistir), (5211, YetkiKodlari.Finans.PoGir),
            (5212, YetkiKodlari.Finans.PoDegistir), (5213, YetkiKodlari.Finans.BirimFiyatGoruntule),
            (5214, YetkiKodlari.Finans.BirimFiyatDegistir), (5215, YetkiKodlari.Finans.KarlilikGoruntule),
            (5216, YetkiKodlari.Finans.ExcelAktar), (5217, YetkiKodlari.Finans.PdfAktar),
            (5218, YetkiKodlari.Finans.GiderEkle), (5219, YetkiKodlari.Finans.GiderDuzenle),
            (5220, YetkiKodlari.Finans.GiderKutuphanesiYonet), (5221, YetkiKodlari.Finans.IsKutuphanesiYonet),
            (5222, YetkiKodlari.Finans.DuzenliIsYonet), (5223, YetkiKodlari.Finans.ParasalVeriGoruntule),
            (5224, YetkiKodlari.Finans.TutarGoruntule), (5225, YetkiKodlari.Finans.KayitGoruntule),
            (5226, YetkiKodlari.Finans.FaturaGir), (5227, YetkiKodlari.Finans.FaturaDegistir),
            (5228, YetkiKodlari.Finans.FaturaIptal), (5229, YetkiKodlari.Finans.PoIptal),
            (5230, YetkiKodlari.Finans.GiderIptal), (5231, YetkiKodlari.Finans.KaliciSil),
            (5232, YetkiKodlari.Finans.BelgeYukle), (5233, YetkiKodlari.Finans.BelgeIndir),
            (5235, YetkiKodlari.Finans.DenetimGor), (5236, YetkiKodlari.Finans.FiyatlandirmaDegistir),
            (5237, YetkiKodlari.Finans.OdemeKaydet)
        ];
        Assert.Equal(33, yapraklar.Length);
        if (!await db.MenuTanimlari.AnyAsync(x => x.Id == Kok))
            db.MenuTanimlari.Add(new MenuTanimi { Id = Kok, Kod = YetkiKodlari.Finans.Modul, LabelKey = "MENU.FINANS_YONETIMI", Icon = "ri-line-chart-line", Route = "/finans-yonetimi", Sira = 9 });
        foreach (var (id, kod) in yapraklar)
            db.MenuTanimlari.Add(new MenuTanimi { Id = id, Kod = kod, LabelKey = "MENU." + kod, Icon = "", Sira = id, ParentId = Kok });
        if (!await db.Roller.AnyAsync(x => x.Id == SistemRolleri.AdminId))
            db.Roller.Add(new Rol { Id = SistemRolleri.AdminId, Ad = SistemRolleri.AdminAdi });
        db.Roller.AddRange(new Rol { Id = 14, Ad = "Sistem Yönetici" }, new Rol { Id = 15, Ad = "Sipariş Operasyon Yetkilisi" },
            new Rol { Id = 16, Ad = "TEST" }, new Rol { Id = 30, Ad = "Salt okuma, PO yazması tavanlı" });
        await db.SaveChangesAsync();
        await db.RolYetkileri.Where(x => x.RolId == SistemRolleri.AdminId).ExecuteDeleteAsync();

        var okuma = new HashSet<string>
        {
            YetkiKodlari.Finans.GelirGoruntule, YetkiKodlari.Finans.GiderGoruntule, YetkiKodlari.Finans.SiparisOperasyonGoruntule,
            YetkiKodlari.Finans.RaporGoruntule, YetkiKodlari.Finans.BirimFiyatGoruntule, YetkiKodlari.Finans.KarlilikGoruntule,
            YetkiKodlari.Finans.ExcelAktar, YetkiKodlari.Finans.PdfAktar, YetkiKodlari.Finans.ParasalVeriGoruntule,
            YetkiKodlari.Finans.TutarGoruntule, YetkiKodlari.Finans.KayitGoruntule, YetkiKodlari.Finans.BelgeIndir,
            YetkiKodlari.Finans.DenetimGor
        };
        string[] test =
        [
            YetkiKodlari.Finans.SiparisOperasyonGoruntule, YetkiKodlari.Finans.RaporGoruntule, YetkiKodlari.Finans.ManuelIsEkle,
            YetkiKodlari.Finans.PoGir, YetkiKodlari.Finans.PoDegistir, YetkiKodlari.Finans.BirimFiyatGoruntule,
            YetkiKodlari.Finans.BirimFiyatDegistir, YetkiKodlari.Finans.GiderDuzenle, YetkiKodlari.Finans.IsKutuphanesiYonet,
            YetkiKodlari.Finans.DuzenliIsYonet, YetkiKodlari.Finans.ParasalVeriGoruntule, YetkiKodlari.Finans.TutarGoruntule,
            YetkiKodlari.Finans.KayitGoruntule, YetkiKodlari.Finans.FaturaGir, YetkiKodlari.Finans.BelgeIndir,
            YetkiKodlari.Finans.OdemeKaydet
        ];
        var izinler = new List<RolYetki>
        {
            new() { RolId = SistemRolleri.AdminId, MenuTanimiId = Kok, YetkiTipiId = 3 },
            new() { RolId = 14, MenuTanimiId = Kok, YetkiTipiId = 3 },
            new() { RolId = 15, MenuTanimiId = Kok, YetkiTipiId = 3 },
            new() { RolId = 16, MenuTanimiId = Kok, YetkiTipiId = 3 },
            new() { RolId = 30, MenuTanimiId = Kok, YetkiTipiId = 2 },
            new() { RolId = 30, MenuTanimiId = 5225, YetkiTipiId = 2 },
            new() { RolId = 30, MenuTanimiId = 5203, YetkiTipiId = 2 },
            new() { RolId = 30, MenuTanimiId = 5211, YetkiTipiId = 3 }
        };
        foreach (var (id, kod) in yapraklar)
        {
            izinler.Add(new() { RolId = SistemRolleri.AdminId, MenuTanimiId = id, YetkiTipiId = 3 });
            izinler.Add(new() { RolId = 14, MenuTanimiId = id, YetkiTipiId = okuma.Contains(kod) ? 2 : 3 });
            if (test.Contains(kod)) izinler.Add(new() { RolId = 16, MenuTanimiId = id, YetkiTipiId = 3 });
        }
        db.RolYetkileri.AddRange(izinler);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }
}
