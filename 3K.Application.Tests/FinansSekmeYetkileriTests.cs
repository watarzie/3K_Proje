using System.Reflection;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using _3K.Application.Behaviors;
using _3K.Application.Common;
using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Application.Features.FinansIslemleri.Queries;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

/// <summary>
/// Finans yetkisi yalnız sekme düzeyindedir: her finans isteği ait olduğu sekme kümesinden herhangi birinin iznini
/// (sorgu R, işlem ve işlem önizlemesi W) ister; alt işlem/alan izni yoktur. Vade uyarısı ayrı bir R iznidir. Sekme
/// düğümlerini ekleyen eski göç betiği izinleri azaltmadan düğümleri ekleyip kodları taşır.
/// </summary>
public sealed class FinansSekmeYetkileriTests
{
    private const int Kok = 47, SekmePoFaturaId = 5240;
    private const string Panel = YetkiKodlari.Finans.SekmePanel, IsTakibi = YetkiKodlari.Finans.SekmeIsTakibi,
        PoFatura = YetkiKodlari.Finans.SekmePoFatura, Giderler = YetkiKodlari.Finans.SekmeGiderler,
        GiderKategorileri = YetkiKodlari.Finans.SekmeGiderKategorileri, Tanimlar = YetkiKodlari.Finans.SekmeTanimlar,
        Raporlar = YetkiKodlari.Finans.SekmeRaporlar, Munferit = YetkiKodlari.Finans.SekmeMunferitIsler,
        Modul = YetkiKodlari.Finans.Modul, Vade = YetkiKodlari.Finans.VadeUyarisi;

    // Ortak sekme kümeleri bilerek açık yazılır: FinansSekmeleri'ndeki bir değişiklik bu tabloyu da değiştirmeyi gerektirir.
    private static readonly string[] Kayit = [IsTakibi, Panel, PoFatura, Munferit, Raporlar];
    private static readonly string[] KayitVeGider = [.. Kayit, Giderler];
    private static readonly string[] IsDuzenleme = [IsTakibi, Munferit];
    private static readonly string[] SiparisAcma = [PoFatura, IsTakibi, Munferit];
    private static readonly string[] GiderTanimlari = [Giderler, GiderKategorileri, Raporlar];

    /// <summary>
    /// Bütün finans istekleri → sekme kümesi ve izin tipi. Kayda bağlı istekler (belge yükleme, kalıcı silme ve
    /// önizlemesi) burada modelsiz hâliyle (bilinmeyen tür → İş Takibi) yer alır; türe göre eşleme ayrı teoridedir.
    /// </summary>
    public static TheoryData<Type, string[], YetkiTipi> IstekSekmeleri => new()
    {
        // Belge / kalıcı silme
        { typeof(FinansBelgeYukleCommand), [IsTakibi], YetkiTipi.W },
        { typeof(FinansKaliciSilCommand), [IsTakibi], YetkiTipi.W },
        { typeof(FinansKaliciSilOnizlemeQuery), [IsTakibi], YetkiTipi.W },
        { typeof(FinansBelgelerQuery), KayitVeGider, YetkiTipi.R },
        { typeof(FinansBelgeIndirQuery), KayitVeGider, YetkiTipi.R },
        // Giderler
        { typeof(FinansGiderlerQuery), [Giderler], YetkiTipi.R },
        { typeof(FinansGiderOlusturCommand), [Giderler], YetkiTipi.W },
        { typeof(FinansGiderGuncelleCommand), [Giderler], YetkiTipi.W },
        { typeof(FinansGiderIptalCommand), [Giderler], YetkiTipi.W },
        // İş kayıtları
        { typeof(FinansIsKaydiGetirQuery), Kayit, YetkiTipi.R },
        { typeof(FinansIsKayitlariSecimQuery), Kayit, YetkiTipi.R },
        { typeof(FinansAylikIslerQuery), [IsTakibi], YetkiTipi.R },
        { typeof(FinansProjeSandikKirilimiQuery), [IsTakibi], YetkiTipi.R },
        { typeof(FinansProjeSecenekleriQuery), [Modul], YetkiTipi.R },
        { typeof(FinansIsKaydiOlusturCommand), [IsTakibi], YetkiTipi.W },
        { typeof(FinansMunferitIsOlusturCommand), [Munferit], YetkiTipi.W },
        { typeof(FinansIsKaydiIptalCommand), IsDuzenleme, YetkiTipi.W },
        { typeof(FinansIsKaydiGeriAlCommand), IsDuzenleme, YetkiTipi.W },
        { typeof(FinansOzelIsAylikDegerGuncelleCommand), [IsTakibi], YetkiTipi.W },
        { typeof(FinansTarihiDegistirCommand), IsDuzenleme, YetkiTipi.W },
        { typeof(FinansHacimGirCommand), IsDuzenleme, YetkiTipi.W },
        { typeof(FinansFiyatlandirCommand), IsDuzenleme, YetkiTipi.W },
        // Panel ve ortak listeler
        { typeof(FinansPanelQuery), [Panel], YetkiTipi.R },
        { typeof(FinansHareketlerQuery), Kayit, YetkiTipi.R },
        { typeof(FinansGenelAramaQuery), Kayit, YetkiTipi.R },
        { typeof(FinansMunferitIslerQuery), [Munferit], YetkiTipi.R },
        { typeof(FinansYaslandirmaQuery), [Panel, IsTakibi, Raporlar], YetkiTipi.R },
        { typeof(FinansDegisiklikGecmisiQuery), KayitVeGider, YetkiTipi.R },
        // Raporlar
        { typeof(FinansIsRaporuDosyaQuery), [Raporlar], YetkiTipi.R },
        { typeof(FinansGiderRaporuDosyaQuery), [Giderler, Raporlar], YetkiTipi.R },
        { typeof(FinansAylikRaporDosyaQuery), [IsTakibi, Raporlar], YetkiTipi.R },
        { typeof(FinansSiparisDurumRaporuDosyaQuery), [IsTakibi, Raporlar], YetkiTipi.R },
        { typeof(FinansOzetRaporuDosyaQuery), [Raporlar], YetkiTipi.R },
        { typeof(FinansAlacakRaporuDosyaQuery), [Raporlar], YetkiTipi.R },
        { typeof(FinansRaporOnizlemeQuery), [Raporlar], YetkiTipi.R },
        // PO / Fatura
        { typeof(FinansSiparislerQuery), [PoFatura], YetkiTipi.R },
        { typeof(FinansSiparisGruplariQuery), SiparisAcma, YetkiTipi.R },
        { typeof(FinansSiparisGetirQuery), Kayit, YetkiTipi.R },
        { typeof(FinansFaturalarQuery), [PoFatura], YetkiTipi.R },
        { typeof(FinansFaturaGetirQuery), Kayit, YetkiTipi.R },
        { typeof(FinansVadeUyarilariQuery), [Vade], YetkiTipi.R },
        { typeof(FinansSiparisOlusturCommand), SiparisAcma, YetkiTipi.W },
        { typeof(FinansSiparisKalemEkleCommand), SiparisAcma, YetkiTipi.W },
        { typeof(FinansSiparisGuncelleCommand), [PoFatura], YetkiTipi.W },
        { typeof(FinansSiparisIptalCommand), [PoFatura], YetkiTipi.W },
        { typeof(FinansFaturaOlusturCommand), [PoFatura], YetkiTipi.W },
        { typeof(FinansFaturaGuncelleCommand), [PoFatura], YetkiTipi.W },
        { typeof(FinansFaturaIptalCommand), [PoFatura], YetkiTipi.W },
        { typeof(FinansFaturaOdemeCommand), [PoFatura], YetkiTipi.W },
        // Tanımlar ve gider kategorileri
        { typeof(FinansDuzenliIslerQuery), [Tanimlar], YetkiTipi.R },
        { typeof(FinansGiderKategorileriQuery), GiderTanimlari, YetkiTipi.R },
        { typeof(FinansGiderKalemleriQuery), GiderTanimlari, YetkiTipi.R },
        { typeof(FinansGiderKutuphaneKategorileriQuery), [GiderKategorileri], YetkiTipi.R },
        { typeof(FinansGiderKutuphaneKalemleriQuery), [GiderKategorileri], YetkiTipi.R },
        { typeof(FinansTarifeDuzeltmeOnizlemeQuery), [Tanimlar], YetkiTipi.W },
        { typeof(FinansUrunSecenekleriQuery), [Munferit], YetkiTipi.R },
        { typeof(FinansUrunlerQuery), [Tanimlar], YetkiTipi.R },
        { typeof(FinansParaBirimleriQuery), [Modul], YetkiTipi.R },
        { typeof(FinansParametrelerQuery), [Tanimlar, PoFatura], YetkiTipi.R },
        { typeof(FinansDuzenliIsOlusturCommand), [Tanimlar], YetkiTipi.W },
        { typeof(FinansDuzenliIsGuncelleCommand), [Tanimlar], YetkiTipi.W },
        { typeof(FinansDuzenliIsDonemUretCommand), [Tanimlar], YetkiTipi.W },
        { typeof(FinansGiderKategoriOlusturCommand), [GiderKategorileri], YetkiTipi.W },
        { typeof(FinansGiderKategoriGuncelleCommand), [GiderKategorileri], YetkiTipi.W },
        { typeof(FinansGiderKalemiOlusturCommand), [GiderKategorileri], YetkiTipi.W },
        { typeof(FinansGiderKalemiGuncelleCommand), [GiderKategorileri], YetkiTipi.W },
        { typeof(FinansUrunOlusturCommand), [Tanimlar], YetkiTipi.W },
        { typeof(FinansUrunGuncelleCommand), [Tanimlar], YetkiTipi.W },
        { typeof(FinansUrunPasiflestirCommand), [Tanimlar], YetkiTipi.W },
        { typeof(FinansUrunYenidenFiyatlaCommand), [Tanimlar], YetkiTipi.W },
        { typeof(FinansTarifeDuzeltCommand), [Tanimlar], YetkiTipi.W },
        { typeof(FinansParaBirimiOlusturCommand), [Tanimlar], YetkiTipi.W },
        { typeof(FinansParaBirimiGuncelleCommand), [Tanimlar], YetkiTipi.W },
        { typeof(FinansParametreleriKaydetCommand), [Tanimlar], YetkiTipi.W }
    };

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
    [InlineData(3, 1, 1)]
    public void ModulKoku_SekmeDugumununTavanidir(int sekme, int modul, int beklenen)
    {
        var agac = new YetkiAgaci([
            new YetkiDugumu(Kok, Modul, null),
            new YetkiDugumu(SekmePoFaturaId, PoFatura, Kok)
        ]);
        var seviyeler = agac.RolSeviyeleri(new Dictionary<int, int> { [Kok] = modul, [SekmePoFaturaId] = sekme });
        Assert.Equal((YetkiTipi)beklenen, seviyeler[SekmePoFaturaId]);
    }

    /// <summary>Her finans isteği sekme kümesindeki her sekme için tek gereksinim üretir; herhangi biri yeter (Any).</summary>
    [Theory]
    [MemberData(nameof(IstekSekmeleri))]
    public void FinansIstegi_SekmeKumesindenHerhangiBirininIzniniIster(Type istekTipi, string[] sekmeler, YetkiTipi tip)
    {
        var istek = Activator.CreateInstance(istekTipi)!;
        Assert.IsAssignableFrom<ISecuredRequest>(istek);
        var tekli = Assert.IsAssignableFrom<IRequiresMenuPermission>(istek);
        var coklu = Assert.IsAssignableFrom<IRequiresMenuPermissions>(istek);
        Assert.Equal(sekmeler[0], tekli.RequiredMenuKod);
        Assert.Equal(tip, tekli.RequiredYetkiTipi);
        Assert.Equal(MenuPermissionMatch.Any, coklu.PermissionMatch);
        Assert.Equal(sekmeler.Select(kod => new MenuPermissionRequirement(kod, tip)), coklu.RequiredMenuPermissions);
    }

    /// <summary>Yeni bir finans isteği eklendiğinde sekme eşlemesi tabloya yazılmadan test geçmez.</summary>
    [Fact]
    public void TumFinansIstekleri_SekmeTablosundadir()
    {
        var tablo = IstekSekmeleri.Select(x => (Type)x[0]).ToList();
        var istekler = typeof(FinansCommand).Assembly.GetTypes().Where(x => !x.IsAbstract && FinansIstegiMi(x)).ToList();
        Assert.Equal(tablo.Count, tablo.Distinct().Count());
        Assert.Empty(istekler.Except(tablo).Select(x => x.Name));
        Assert.Empty(tablo.Except(istekler).Select(x => x.Name));
    }

    /// <summary>Belge yükleme, kalıcı silme ve önizlemesi hedef kaydın sekmesinin W iznini ister.</summary>
    [Theory]
    [InlineData("IsKaydi", new[] { IsTakibi, Munferit })]
    [InlineData("Siparis", new[] { PoFatura })]
    [InlineData("Fatura", new[] { PoFatura })]
    [InlineData("Gider", new[] { Giderler })]
    [InlineData("Bilinmeyen", new[] { IsTakibi })]
    public void KayitBagliIslem_HedefKaydinSekmesininYazmaIzniniIster(string tur, string[] sekmeler)
    {
        var beklenen = sekmeler.Select(MenuPermissionRequirement.Write).ToArray();
        Assert.Equal(beklenen, new FinansKaliciSilCommand { Model = new(tur, 1, "1", true, "Hatalı kayıt") }.RequiredMenuPermissions);
        Assert.Equal(beklenen, new FinansKaliciSilOnizlemeQuery { VarlikTuru = tur, Id = 1 }.RequiredMenuPermissions);
        Assert.Equal(beklenen, new FinansBelgeYukleCommand { Model = new(tur, 1, "belge.pdf", []) }.RequiredMenuPermissions);
    }

    /// <summary>
    /// Gerçek yetki değerlendiricisiyle: sekmenin R izni sekmenin sorgularını açar, işlemlerini açmaz (403); W izni
    /// işlemleri ve sekmenin kaydı için kalıcı silmeyi açar; başka sekmenin kaydı ve sorgusu açılmaz.
    /// </summary>
    [Theory]
    [InlineData(Giderler, typeof(FinansGiderlerQuery), typeof(FinansGiderOlusturCommand), "Gider", "Siparis")]
    [InlineData(PoFatura, typeof(FinansFaturalarQuery), typeof(FinansFaturaOlusturCommand), "Fatura", "Gider")]
    [InlineData(IsTakibi, typeof(FinansAylikIslerQuery), typeof(FinansFiyatlandirCommand), "IsKaydi", "Fatura")]
    [InlineData(Munferit, typeof(FinansMunferitIslerQuery), typeof(FinansMunferitIsOlusturCommand), "IsKaydi", "Gider")]
    public async Task SekmeOkumasi_SorguyuAcarIslemiAcmaz_SekmeYazmasiIslemiVeKaliciSilmeyiAcar(
        string sekme, Type sorguTipi, Type islemTipi, string varlikTuru, string baskaVarlikTuru)
    {
        var sorgu = Activator.CreateInstance(sorguTipi)!;
        var islem = Activator.CreateInstance(islemTipi)!;
        FinansKaliciSilCommand Sil(string tur) => new() { Model = new(tur, 1, "1", true, "Hatalı kayıt") };

        var okuma = Roller((sekme, YetkiTipi.R));
        Assert.True(await GecerMiAsync(sorgu, okuma));
        Assert.False(await GecerMiAsync(islem, okuma));
        Assert.False(await GecerMiAsync(Sil(varlikTuru), okuma));
        Assert.False(await GecerMiAsync(new FinansKaliciSilOnizlemeQuery { VarlikTuru = varlikTuru, Id = 1 }, okuma));

        var yazma = Roller((sekme, YetkiTipi.W));
        Assert.True(await GecerMiAsync(sorgu, yazma));
        Assert.True(await GecerMiAsync(islem, yazma));
        Assert.True(await GecerMiAsync(Sil(varlikTuru), yazma));
        Assert.True(await GecerMiAsync(new FinansKaliciSilOnizlemeQuery { VarlikTuru = varlikTuru, Id = 1 }, yazma));
        Assert.False(await GecerMiAsync(Sil(baskaVarlikTuru), yazma));
        Assert.False(await GecerMiAsync(new FinansUrunlerQuery(), yazma));

        Assert.False(await GecerMiAsync(sorgu, Roller()));
    }

    /// <summary>Kayıt detayı ve genel arama gibi ortak uçlar kümedeki herhangi tek bir sekmenin okumasıyla açılır.</summary>
    [Theory]
    [InlineData(IsTakibi)]
    [InlineData(Panel)]
    [InlineData(PoFatura)]
    [InlineData(Munferit)]
    [InlineData(Raporlar)]
    public async Task OrtakUc_KumedekiHerhangiBirSekmeninOkumasiylaAcilir(string sekme)
    {
        var roller = Roller((sekme, YetkiTipi.R));
        Assert.True(await GecerMiAsync(new FinansIsKaydiGetirQuery { Id = 1 }, roller));
        Assert.True(await GecerMiAsync(new FinansGenelAramaQuery(), roller));
        Assert.True(await GecerMiAsync(new FinansDegisiklikGecmisiQuery(), roller));
        Assert.True(await GecerMiAsync(new FinansBelgelerQuery { HedefTuru = "IsKaydi", HedefId = 1 }, roller));
    }

    [Fact]
    public async Task OrtakUc_KumeDisindakiSekmelerleAcilmaz_GiderlerYalnizBelgeVeGecmisiAcar()
    {
        var disarida = Roller((Giderler, YetkiTipi.W), (GiderKategorileri, YetkiTipi.W), (Tanimlar, YetkiTipi.W), (Vade, YetkiTipi.R));
        Assert.False(await GecerMiAsync(new FinansIsKaydiGetirQuery { Id = 1 }, disarida));
        Assert.False(await GecerMiAsync(new FinansGenelAramaQuery(), disarida));
        Assert.False(await GecerMiAsync(new FinansSiparisGetirQuery(), disarida));
        Assert.True(await GecerMiAsync(new FinansDegisiklikGecmisiQuery(), disarida));
        Assert.True(await GecerMiAsync(new FinansBelgeIndirQuery { Id = 1 }, disarida));
    }

    /// <summary>Vade uyarısı yalnız kendi R iznini ister: bütün sekmelerin W izni yetmez; vade izni de sekme açmaz.</summary>
    [Fact]
    public async Task VadeUyarisi_KendiOkumaIzniniIster_SekmeIzniYetmez()
    {
        var tumSekmeler = Roller(FinansSekmeAgaci.Sekmeler.Select(x => (x, YetkiTipi.W)).ToArray());
        Assert.False(await GecerMiAsync(new FinansVadeUyarilariQuery(), tumSekmeler));

        var vade = Roller((Vade, YetkiTipi.R));
        Assert.True(await GecerMiAsync(new FinansVadeUyarilariQuery(), vade));
        Assert.False(await GecerMiAsync(new FinansFaturalarQuery(), vade));
        Assert.False(await GecerMiAsync(new FinansAylikIslerQuery(), vade));
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
            Assert.Equal(Kok, menuler[FinansSekmeAgaci.EskiKodlar.KayitGoruntule].ParentId);
            Assert.Equal(Kok, menuler[FinansSekmeAgaci.EskiKodlar.ManuelIsEkle].ParentId);

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
            var odeme = menuler[FinansSekmeAgaci.EskiKodlar.OdemeKaydet].Id;
            await db.RolYetkileri.Where(x => x.MenuTanimiId == odeme).ExecuteDeleteAsync();
            await db.MenuTanimlari.Where(x => x.Id == odeme).ExecuteDeleteAsync();
            var hata = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync(betik));
            Assert.Contains(FinansSekmeAgaci.EskiKodlar.OdemeKaydet, hata.MessageText);
        });
    }

    private static bool FinansIstegiMi(Type tip)
    {
        for (var taban = tip.BaseType; taban != null; taban = taban.BaseType)
            if (taban == typeof(FinansCommand) || taban.IsGenericType &&
                (taban.GetGenericTypeDefinition() == typeof(FinansCommand<>) || taban.GetGenericTypeDefinition() == typeof(FinansQuery<>)))
                return true;
        return false;
    }

    /// <summary>
    /// Gerçek <see cref="RolService"/>: sentetik ağaç kök → 8 sekme + vade uyarısı; rol kökte W, verilen kodlarda verilen
    /// seviyededir. Kullanıcıya özel izin yoktur.
    /// </summary>
    private static RolService Roller(params (string Kod, YetkiTipi Seviye)[] izinler)
    {
        var menuler = new List<MenuTanimi> { new() { Id = Kok, Kod = Modul } };
        menuler.AddRange(FinansSekmeAgaci.Sekmeler.Append(Vade).Select((kod, i) => new MenuTanimi { Id = 5238 + i, Kod = kod, ParentId = Kok }));
        var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().Options);
        context.Kullanicilar = new OrtakMemorySet<Kullanici>([new() { Id = 7, RolId = 1 }]);
        context.MenuTanimlari = new OrtakMemorySet<MenuTanimi>(menuler);
        context.RolYetkileri = new OrtakMemorySet<RolYetki>(
        [
            new() { RolId = 1, MenuTanimiId = Kok, YetkiTipiId = (int)YetkiTipi.W },
            .. izinler.Select(x => new RolYetki { RolId = 1, MenuTanimiId = menuler.Single(m => m.Kod == x.Kod).Id, YetkiTipiId = (int)x.Seviye })
        ]);
        return new RolService(context);
    }

    /// <summary>İsteği gerçek <see cref="AuthorizationBehavior{TRequest, TResponse}"/>'dan geçirir; reddedilirse 403 bekler.</summary>
    private static async Task<bool> GecerMiAsync(object istek, RolService roller)
    {
        var yanit = istek.GetType().GetInterfaces()
            .Single(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRequest<>)).GetGenericArguments()[0];
        var yontem = typeof(FinansSekmeYetkileriTests).GetMethod(nameof(DavranistanGecerMiAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(istek.GetType(), yanit);
        return await (Task<bool>)yontem.Invoke(null, [istek, roller])!;
    }

    private static async Task<bool> DavranistanGecerMiAsync<TRequest, TResponse>(TRequest istek, RolService roller) where TRequest : notnull
    {
        var gecti = false;
        var sonuc = await new AuthorizationBehavior<TRequest, TResponse>(new OrtakUser(), roller).Handle(istek, () =>
        {
            gecti = true;
            return Task.FromResult(default(TResponse)!);
        }, CancellationToken.None);
        if (!gecti) Assert.Equal(403, Assert.IsAssignableFrom<Result>(sonuc).StatusCode);
        return gecti;
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

    /// <summary>Canlı finans ağacının (MenuTanimlari 47 + 5200–5237) ve rollerinin 08.10.2026 SELECT dökümü (eski kodlarla).</summary>
    private static async Task CanliAgaciKurAsync(AppDbContext db)
    {
        (int Id, string Kod)[] yapraklar =
        [
            (5200, FinansSekmeAgaci.EskiKodlar.GelirGoruntule), (5201, FinansSekmeAgaci.EskiKodlar.GiderGoruntule),
            (5203, FinansSekmeAgaci.EskiKodlar.SiparisOperasyonGoruntule), (5206, FinansSekmeAgaci.EskiKodlar.RaporGoruntule),
            (5207, FinansSekmeAgaci.EskiKodlar.ManuelIsEkle), (5209, FinansSekmeAgaci.EskiKodlar.IsIptal),
            (5210, FinansSekmeAgaci.EskiKodlar.TarihDegistir), (5211, FinansSekmeAgaci.EskiKodlar.PoGir),
            (5212, FinansSekmeAgaci.EskiKodlar.PoDegistir), (5213, FinansSekmeAgaci.EskiKodlar.BirimFiyatGoruntule),
            (5214, FinansSekmeAgaci.EskiKodlar.BirimFiyatDegistir), (5215, FinansSekmeAgaci.EskiKodlar.KarlilikGoruntule),
            (5216, FinansSekmeAgaci.EskiKodlar.ExcelAktar), (5217, FinansSekmeAgaci.EskiKodlar.PdfAktar),
            (5218, FinansSekmeAgaci.EskiKodlar.GiderEkle), (5219, FinansSekmeAgaci.EskiKodlar.GiderDuzenle),
            (5220, FinansSekmeAgaci.EskiKodlar.GiderKutuphanesiYonet), (5221, FinansSekmeAgaci.EskiKodlar.IsKutuphanesiYonet),
            (5222, FinansSekmeAgaci.EskiKodlar.DuzenliIsYonet), (5223, FinansSekmeAgaci.EskiKodlar.ParasalVeriGoruntule),
            (5224, FinansSekmeAgaci.EskiKodlar.TutarGoruntule), (5225, FinansSekmeAgaci.EskiKodlar.KayitGoruntule),
            (5226, FinansSekmeAgaci.EskiKodlar.FaturaGir), (5227, FinansSekmeAgaci.EskiKodlar.FaturaDegistir),
            (5228, FinansSekmeAgaci.EskiKodlar.FaturaIptal), (5229, FinansSekmeAgaci.EskiKodlar.PoIptal),
            (5230, FinansSekmeAgaci.EskiKodlar.GiderIptal), (5231, FinansSekmeAgaci.EskiKodlar.KaliciSil),
            (5232, FinansSekmeAgaci.EskiKodlar.BelgeYukle), (5233, FinansSekmeAgaci.EskiKodlar.BelgeIndir),
            (5235, FinansSekmeAgaci.EskiKodlar.DenetimGor), (5236, FinansSekmeAgaci.EskiKodlar.FiyatlandirmaDegistir),
            (5237, FinansSekmeAgaci.EskiKodlar.OdemeKaydet)
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
            FinansSekmeAgaci.EskiKodlar.GelirGoruntule, FinansSekmeAgaci.EskiKodlar.GiderGoruntule,
            FinansSekmeAgaci.EskiKodlar.SiparisOperasyonGoruntule, FinansSekmeAgaci.EskiKodlar.RaporGoruntule,
            FinansSekmeAgaci.EskiKodlar.BirimFiyatGoruntule, FinansSekmeAgaci.EskiKodlar.KarlilikGoruntule,
            FinansSekmeAgaci.EskiKodlar.ExcelAktar, FinansSekmeAgaci.EskiKodlar.PdfAktar,
            FinansSekmeAgaci.EskiKodlar.ParasalVeriGoruntule, FinansSekmeAgaci.EskiKodlar.TutarGoruntule,
            FinansSekmeAgaci.EskiKodlar.KayitGoruntule, FinansSekmeAgaci.EskiKodlar.BelgeIndir,
            FinansSekmeAgaci.EskiKodlar.DenetimGor
        };
        string[] test =
        [
            FinansSekmeAgaci.EskiKodlar.SiparisOperasyonGoruntule, FinansSekmeAgaci.EskiKodlar.RaporGoruntule,
            FinansSekmeAgaci.EskiKodlar.ManuelIsEkle, FinansSekmeAgaci.EskiKodlar.PoGir, FinansSekmeAgaci.EskiKodlar.PoDegistir,
            FinansSekmeAgaci.EskiKodlar.BirimFiyatGoruntule, FinansSekmeAgaci.EskiKodlar.BirimFiyatDegistir,
            FinansSekmeAgaci.EskiKodlar.GiderDuzenle, FinansSekmeAgaci.EskiKodlar.IsKutuphanesiYonet,
            FinansSekmeAgaci.EskiKodlar.DuzenliIsYonet, FinansSekmeAgaci.EskiKodlar.ParasalVeriGoruntule,
            FinansSekmeAgaci.EskiKodlar.TutarGoruntule, FinansSekmeAgaci.EskiKodlar.KayitGoruntule,
            FinansSekmeAgaci.EskiKodlar.FaturaGir, FinansSekmeAgaci.EskiKodlar.BelgeIndir, FinansSekmeAgaci.EskiKodlar.OdemeKaydet
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
