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
/// Finans yetkisi yalnız sekme ve alt sekme düzeyindedir: her finans isteği ait olduğu sekme/alt sekme kümesinden
/// herhangi birinin iznini (sorgu R, işlem ve işlem önizlemesi W) ister; alt işlem/alan izni yoktur. İş Takibi, Tanımlar
/// ve PO/Fatura işlemleri alt sekmeye bağlıdır; sekme alt sekmenin tavanıdır ve sekmenin W izni alt sekme iznini
/// kendiliğinden vermez. Vade uyarısı ayrı bir R iznidir. Göç betikleri izinleri azaltmadan düğümleri ekler.
/// </summary>
public sealed class FinansSekmeYetkileriTests
{
    private const int Kok = FinansSekmeAgaci.KokId, SekmePoFaturaId = 5240, SekmePoFaturaPoId = 5254;
    private const string Panel = YetkiKodlari.Finans.SekmePanel, IsTakibi = YetkiKodlari.Finans.SekmeIsTakibi,
        PoFatura = YetkiKodlari.Finans.SekmePoFatura, Giderler = YetkiKodlari.Finans.SekmeGiderler,
        GiderKategorileri = YetkiKodlari.Finans.SekmeGiderKategorileri, Tanimlar = YetkiKodlari.Finans.SekmeTanimlar,
        Raporlar = YetkiKodlari.Finans.SekmeRaporlar, Munferit = YetkiKodlari.Finans.SekmeMunferitIsler,
        Modul = YetkiKodlari.Finans.Modul, Vade = YetkiKodlari.Finans.VadeUyarisi;
    private const string IsTakibiAylik = YetkiKodlari.Finans.SekmeIsTakibiAylik,
        IsTakibiBekleyen = YetkiKodlari.Finans.SekmeIsTakibiBekleyen,
        TanimlarDuzenli = YetkiKodlari.Finans.SekmeTanimlarDuzenli, TanimlarTarifeler = YetkiKodlari.Finans.SekmeTanimlarTarifeler,
        TanimlarParaBirimi = YetkiKodlari.Finans.SekmeTanimlarParaBirimi,
        PoFaturaPo = YetkiKodlari.Finans.SekmePoFaturaPo, PoFaturaFatura = YetkiKodlari.Finans.SekmePoFaturaFatura;

    // Ortak sekme kümeleri bilerek açık yazılır: FinansSekmeleri'ndeki bir değişiklik bu tabloyu da değiştirmeyi gerektirir.
    private static readonly string[] Kayit = [IsTakibi, Panel, PoFatura, Munferit, Raporlar];
    private static readonly string[] KayitVeGider = [.. Kayit, Giderler];
    private static readonly string[] IsDuzenleme = [IsTakibi, Munferit];
    private static readonly string[] SiparisAcma = [PoFaturaPo, IsTakibi, Munferit];
    private static readonly string[] GiderTanimlari = [Giderler, GiderKategorileri, Raporlar];

    /// <summary>
    /// Bütün finans istekleri → sekme/alt sekme kümesi ve izin tipi. Kayda bağlı istekler (belge yükleme, kalıcı silme
    /// ve önizlemesi) burada modelsiz hâliyle (bilinmeyen tür → İş Takibi) yer alır; türe göre eşleme ayrı teoridedir.
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
        { typeof(FinansAylikIslerQuery), [IsTakibiAylik], YetkiTipi.R },
        { typeof(FinansProjeSandikKirilimiQuery), [IsTakibiAylik], YetkiTipi.R },
        { typeof(FinansProjeSecenekleriQuery), [Modul], YetkiTipi.R },
        { typeof(FinansIsKaydiOlusturCommand), [IsTakibiAylik], YetkiTipi.W },
        { typeof(FinansMunferitIsOlusturCommand), [Munferit], YetkiTipi.W },
        { typeof(FinansIsKaydiIptalCommand), IsDuzenleme, YetkiTipi.W },
        { typeof(FinansIsKaydiGeriAlCommand), IsDuzenleme, YetkiTipi.W },
        { typeof(FinansOzelIsAylikDegerGuncelleCommand), [IsTakibiAylik], YetkiTipi.W },
        { typeof(FinansTarihiDegistirCommand), IsDuzenleme, YetkiTipi.W },
        { typeof(FinansHacimGirCommand), IsDuzenleme, YetkiTipi.W },
        { typeof(FinansFiyatlandirCommand), IsDuzenleme, YetkiTipi.W },
        // Panel ve ortak listeler
        { typeof(FinansPanelQuery), [Panel], YetkiTipi.R },
        { typeof(FinansHareketlerQuery), Kayit, YetkiTipi.R },
        { typeof(FinansGenelAramaQuery), Kayit, YetkiTipi.R },
        { typeof(FinansMunferitIslerQuery), [Munferit], YetkiTipi.R },
        { typeof(FinansYaslandirmaQuery), [Panel, IsTakibiBekleyen, Raporlar], YetkiTipi.R },
        { typeof(FinansDegisiklikGecmisiQuery), KayitVeGider, YetkiTipi.R },
        // Raporlar
        { typeof(FinansIsRaporuDosyaQuery), [Raporlar], YetkiTipi.R },
        { typeof(FinansGiderRaporuDosyaQuery), [Giderler, Raporlar], YetkiTipi.R },
        { typeof(FinansAylikRaporDosyaQuery), [IsTakibiAylik, Raporlar], YetkiTipi.R },
        { typeof(FinansSiparisDurumRaporuDosyaQuery), [IsTakibiAylik, Raporlar], YetkiTipi.R },
        { typeof(FinansOzetRaporuDosyaQuery), [Raporlar], YetkiTipi.R },
        { typeof(FinansAlacakRaporuDosyaQuery), [Raporlar], YetkiTipi.R },
        { typeof(FinansFinansalDurumRaporuDosyaQuery), [Raporlar], YetkiTipi.R },
        { typeof(FinansRaporOnizlemeQuery), [Raporlar], YetkiTipi.R },
        // PO / Fatura
        { typeof(FinansSiparislerQuery), [PoFaturaPo, PoFaturaFatura], YetkiTipi.R },
        { typeof(FinansSiparisGruplariQuery), SiparisAcma, YetkiTipi.R },
        { typeof(FinansSiparisGetirQuery), Kayit, YetkiTipi.R },
        { typeof(FinansFaturalarQuery), [PoFaturaFatura], YetkiTipi.R },
        { typeof(FinansFaturaGetirQuery), Kayit, YetkiTipi.R },
        { typeof(FinansVadeUyarilariQuery), [Vade], YetkiTipi.R },
        { typeof(FinansSiparisOlusturCommand), SiparisAcma, YetkiTipi.W },
        { typeof(FinansSiparisKalemEkleCommand), SiparisAcma, YetkiTipi.W },
        { typeof(FinansPoOnayGonderCommand), SiparisAcma, YetkiTipi.W },
        { typeof(FinansSiparisGuncelleCommand), [PoFaturaPo], YetkiTipi.W },
        { typeof(FinansSiparisIptalCommand), [PoFaturaPo], YetkiTipi.W },
        { typeof(FinansFaturaOlusturCommand), [PoFaturaFatura], YetkiTipi.W },
        { typeof(FinansFaturaGuncelleCommand), [PoFaturaFatura], YetkiTipi.W },
        { typeof(FinansFaturaIptalCommand), [PoFaturaFatura], YetkiTipi.W },
        { typeof(FinansFaturaOdemeCommand), [PoFaturaFatura], YetkiTipi.W },
        // Tanımlar ve gider kategorileri
        { typeof(FinansDuzenliIslerQuery), [TanimlarDuzenli], YetkiTipi.R },
        { typeof(FinansGiderKategorileriQuery), GiderTanimlari, YetkiTipi.R },
        { typeof(FinansGiderKalemleriQuery), GiderTanimlari, YetkiTipi.R },
        { typeof(FinansGiderKutuphaneKategorileriQuery), [GiderKategorileri], YetkiTipi.R },
        { typeof(FinansGiderKutuphaneKalemleriQuery), [GiderKategorileri], YetkiTipi.R },
        { typeof(FinansTarifeDuzeltmeOnizlemeQuery), [TanimlarTarifeler], YetkiTipi.W },
        { typeof(FinansUrunSecenekleriQuery), [Munferit], YetkiTipi.R },
        { typeof(FinansUrunlerQuery), [TanimlarTarifeler], YetkiTipi.R },
        { typeof(FinansParaBirimleriQuery), [Modul], YetkiTipi.R },
        { typeof(FinansParametrelerQuery), [TanimlarParaBirimi, PoFaturaFatura], YetkiTipi.R },
        { typeof(FinansDuzenliIsOlusturCommand), [TanimlarDuzenli], YetkiTipi.W },
        { typeof(FinansDuzenliIsGuncelleCommand), [TanimlarDuzenli], YetkiTipi.W },
        { typeof(FinansDuzenliIsDonemUretCommand), [TanimlarDuzenli], YetkiTipi.W },
        { typeof(FinansGiderKategoriOlusturCommand), [GiderKategorileri], YetkiTipi.W },
        { typeof(FinansGiderKategoriGuncelleCommand), [GiderKategorileri], YetkiTipi.W },
        { typeof(FinansGiderKalemiOlusturCommand), [GiderKategorileri], YetkiTipi.W },
        { typeof(FinansGiderKalemiGuncelleCommand), [GiderKategorileri], YetkiTipi.W },
        { typeof(FinansUrunOlusturCommand), [TanimlarTarifeler], YetkiTipi.W },
        { typeof(FinansUrunGuncelleCommand), [TanimlarTarifeler], YetkiTipi.W },
        { typeof(FinansUrunPasiflestirCommand), [TanimlarTarifeler], YetkiTipi.W },
        { typeof(FinansUrunYenidenFiyatlaCommand), [TanimlarTarifeler], YetkiTipi.W },
        { typeof(FinansTarifeDuzeltCommand), [TanimlarTarifeler], YetkiTipi.W },
        { typeof(FinansParaBirimiOlusturCommand), [TanimlarParaBirimi], YetkiTipi.W },
        { typeof(FinansParaBirimiGuncelleCommand), [TanimlarParaBirimi], YetkiTipi.W },
        { typeof(FinansParametreleriKaydetCommand), [TanimlarParaBirimi], YetkiTipi.W }
    };

    [Fact]
    public void TestFiksturu_GocBetigindekiSekmeAltlariylaAyni()
    {
        Assert.Equal(
            FinansSekmeAgaci.SekmeAltlari.OrderBy(x => x.Key, StringComparer.Ordinal),
            FinansSekmeAgaci.BetiktekiAltKodlar().OrderBy(x => x.Key, StringComparer.Ordinal));
        var betik = File.ReadAllText(FinansSekmeAgaci.BetikYolu());
        Assert.All(FinansSekmeAgaci.Sekmeler, sekme => Assert.Contains($"'{sekme}'", betik));
        Assert.All(FinansSekmeAgaci.Sekmeler, sekme => Assert.Contains($"({FinansSekmeAgaci.SekmeId(sekme)}, '{sekme}'", betik));
    }

    /// <summary>Fikstürdeki alt sekmeler (Id, kod, sekme) alt sekme betiğiyle ve YetkiKodlari'ndaki bütün alt sekme kodlarıyla aynıdır.</summary>
    [Fact]
    public void TestFiksturu_AltSekmeBetigiVeYetkiKodlariylaAyni()
    {
        Assert.Equal(FinansSekmeAgaci.AltSekmeler, FinansSekmeAgaci.BetiktekiAltSekmeler());
        var altSekmeKodlari = typeof(YetkiKodlari.Finans).GetFields()
            .Where(f => f.IsLiteral).Select(f => (string)f.GetRawConstantValue()!)
            .Where(kod => kod.StartsWith("finans-sekme-", StringComparison.Ordinal) && !FinansSekmeAgaci.Sekmeler.Contains(kod))
            .Order(StringComparer.Ordinal);
        Assert.Equal(altSekmeKodlari, FinansSekmeAgaci.AltSekmeler.Select(x => x.Kod).Order(StringComparer.Ordinal));
        Assert.All(FinansSekmeAgaci.AltSekmeler, x => Assert.StartsWith(x.Sekme + "-", x.Kod, StringComparison.Ordinal));
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

    /// <summary>Sekme alt sekmenin tavanıdır; sekmenin W'si alt sekme izni vermez (alt sekme kaydı yoksa N).</summary>
    [Theory]
    [InlineData(3, 3, 3)]
    [InlineData(3, 2, 2)]
    [InlineData(2, 3, 2)]
    [InlineData(1, 3, 1)]
    [InlineData(3, 1, 1)]
    [InlineData(3, null, 1)]
    public void SekmeDugumu_AltSekmeninTavanidir(int sekme, int? altSekme, int beklenen)
    {
        var agac = new YetkiAgaci([
            new YetkiDugumu(Kok, Modul, null),
            new YetkiDugumu(SekmePoFaturaId, PoFatura, Kok),
            new YetkiDugumu(SekmePoFaturaPoId, PoFaturaPo, SekmePoFaturaId)
        ]);
        var atanmis = new Dictionary<int, int> { [Kok] = 3, [SekmePoFaturaId] = sekme };
        if (altSekme is int seviye) atanmis[SekmePoFaturaPoId] = seviye;
        Assert.Equal((YetkiTipi)beklenen, agac.RolSeviyeleri(atanmis)[SekmePoFaturaPoId]);
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

    /// <summary>
    /// Belge yükleme, kalıcı silme ve önizlemesi hedef kaydın sekmesinin W iznini ister: PO → PO'lar, fatura → Faturalar
    /// alt sekmesi (birbirinin yerine geçmez), gider → Giderler, iş kaydı → İş Takibi/Münferit İşler.
    /// </summary>
    [Theory]
    [InlineData("IsKaydi", new[] { IsTakibi, Munferit })]
    [InlineData("Siparis", new[] { PoFaturaPo })]
    [InlineData("Fatura", new[] { PoFaturaFatura })]
    [InlineData("Gider", new[] { Giderler })]
    [InlineData("Bilinmeyen", new[] { IsTakibi })]
    public void KayitBagliIslem_HedefKaydinSekmesininYazmaIzniniIster(string tur, string[] sekmeler)
    {
        Assert.Equal(sekmeler, FinansSekmeleri.Varlik(tur));
        var beklenen = sekmeler.Select(MenuPermissionRequirement.Write).ToArray();
        Assert.Equal(beklenen, new FinansKaliciSilCommand { Model = new(tur, 1, "1", true, "Hatalı kayıt") }.RequiredMenuPermissions);
        Assert.Equal(beklenen, new FinansKaliciSilOnizlemeQuery { VarlikTuru = tur, Id = 1 }.RequiredMenuPermissions);
        Assert.Equal(beklenen, new FinansBelgeYukleCommand { Model = new(tur, 1, "belge.pdf", []) }.RequiredMenuPermissions);
    }

    /// <summary>
    /// Gerçek yetki değerlendiricisiyle, göç betiği gibi alt sekmeleri sekmeyle aynı seviyede alan rol: sekmenin R izni
    /// sorguları açar, işlemleri açmaz (403); W izni işlemleri ve sekmenin kaydı için kalıcı silmeyi açar; başka sekmenin
    /// kaydı ve sorgusu açılmaz.
    /// </summary>
    [Theory]
    [InlineData(Giderler, typeof(FinansGiderlerQuery), typeof(FinansGiderOlusturCommand), "Gider", "Siparis")]
    [InlineData(PoFatura, typeof(FinansFaturalarQuery), typeof(FinansFaturaOlusturCommand), "Fatura", "Gider")]
    [InlineData(PoFatura, typeof(FinansSiparislerQuery), typeof(FinansSiparisGuncelleCommand), "Siparis", "IsKaydi")]
    [InlineData(IsTakibi, typeof(FinansAylikIslerQuery), typeof(FinansFiyatlandirCommand), "IsKaydi", "Fatura")]
    [InlineData(IsTakibi, typeof(FinansProjeSandikKirilimiQuery), typeof(FinansIsKaydiOlusturCommand), "IsKaydi", "Gider")]
    [InlineData(Munferit, typeof(FinansMunferitIslerQuery), typeof(FinansMunferitIsOlusturCommand), "IsKaydi", "Gider")]
    public async Task SekmeOkumasi_SorguyuAcarIslemiAcmaz_SekmeYazmasiIslemiVeKaliciSilmeyiAcar(
        string sekme, Type sorguTipi, Type islemTipi, string varlikTuru, string baskaVarlikTuru)
    {
        var sorgu = Activator.CreateInstance(sorguTipi)!;
        var islem = Activator.CreateInstance(islemTipi)!;
        FinansKaliciSilCommand Sil(string tur) => new() { Model = new(tur, 1, "1", true, "Hatalı kayıt") };

        var okuma = Roller(AltSekmeleriyle(sekme, YetkiTipi.R));
        Assert.True(await GecerMiAsync(sorgu, okuma));
        Assert.False(await GecerMiAsync(islem, okuma));
        Assert.False(await GecerMiAsync(Sil(varlikTuru), okuma));
        Assert.False(await GecerMiAsync(new FinansKaliciSilOnizlemeQuery { VarlikTuru = varlikTuru, Id = 1 }, okuma));

        var yazma = Roller(AltSekmeleriyle(sekme, YetkiTipi.W));
        Assert.True(await GecerMiAsync(sorgu, yazma));
        Assert.True(await GecerMiAsync(islem, yazma));
        Assert.True(await GecerMiAsync(Sil(varlikTuru), yazma));
        Assert.True(await GecerMiAsync(new FinansKaliciSilOnizlemeQuery { VarlikTuru = varlikTuru, Id = 1 }, yazma));
        Assert.False(await GecerMiAsync(Sil(baskaVarlikTuru), yazma));
        Assert.False(await GecerMiAsync(new FinansUrunlerQuery(), yazma));

        Assert.False(await GecerMiAsync(sorgu, Roller()));
    }

    /// <summary>
    /// Alt sekmeye bağlı uç: sekmenin W'si alt sekme kaydı olmadan sorguyu da işlemi de açmaz (403); sekme R altında alt
    /// sekme W etkin R'dir (tavan): sorgu açılır, işlem 403; sekme ve alt sekme W ise ikisi de açılır, kardeş alt sekmenin
    /// işlemi açılmaz; sekme izni olmadan alt sekme izni de bir şey açmaz.
    /// </summary>
    [Theory]
    [InlineData(IsTakibi, IsTakibiAylik, typeof(FinansAylikIslerQuery), typeof(FinansIsKaydiOlusturCommand), typeof(FinansFiyatlandirCommand))]
    [InlineData(Tanimlar, TanimlarTarifeler, typeof(FinansUrunlerQuery), typeof(FinansUrunGuncelleCommand), typeof(FinansDuzenliIsOlusturCommand))]
    [InlineData(Tanimlar, TanimlarDuzenli, typeof(FinansDuzenliIslerQuery), typeof(FinansDuzenliIsOlusturCommand), typeof(FinansParaBirimiOlusturCommand))]
    [InlineData(Tanimlar, TanimlarParaBirimi, typeof(FinansParametrelerQuery), typeof(FinansParametreleriKaydetCommand), typeof(FinansTarifeDuzeltCommand))]
    [InlineData(PoFatura, PoFaturaPo, typeof(FinansSiparislerQuery), typeof(FinansSiparisIptalCommand), typeof(FinansFaturaIptalCommand))]
    [InlineData(PoFatura, PoFaturaFatura, typeof(FinansFaturalarQuery), typeof(FinansFaturaOdemeCommand), typeof(FinansSiparisGuncelleCommand))]
    public async Task AltSekme_SekmeYazmasiAltSekmeIzniVermez_SekmeOkumasiAltSekmeYazmasininTavanidir(
        string sekme, string altSekme, Type sorguTipi, Type islemTipi, Type kardesIslemTipi)
    {
        var sorgu = Activator.CreateInstance(sorguTipi)!;
        var islem = Activator.CreateInstance(islemTipi)!;
        var kardesIslem = Activator.CreateInstance(kardesIslemTipi)!;

        var yalnizSekme = Roller((sekme, YetkiTipi.W));
        Assert.False(await GecerMiAsync(sorgu, yalnizSekme));
        Assert.False(await GecerMiAsync(islem, yalnizSekme));

        var tavanli = Roller((sekme, YetkiTipi.R), (altSekme, YetkiTipi.W));
        Assert.True(await GecerMiAsync(sorgu, tavanli));
        Assert.False(await GecerMiAsync(islem, tavanli));

        var tam = Roller((sekme, YetkiTipi.W), (altSekme, YetkiTipi.W));
        Assert.True(await GecerMiAsync(sorgu, tam));
        Assert.True(await GecerMiAsync(islem, tam));
        // İş Takibi'nde kardeş işlem (fiyatlandırma) sekme düzeyindedir; diğerlerinde kardeş alt sekmeye bağlıdır.
        Assert.Equal(sekme == IsTakibi, await GecerMiAsync(kardesIslem, tam));

        var sekmesiz = Roller((altSekme, YetkiTipi.W));
        Assert.False(await GecerMiAsync(sorgu, sekmesiz));
        Assert.False(await GecerMiAsync(islem, sekmesiz));
    }

    /// <summary>PO'lar alt sekmesinin W'si fatura işlemlerini, Faturalar alt sekmesinin W'si PO işlemlerini açmaz.</summary>
    [Fact]
    public async Task PoVeFaturaAltSekmeleri_BirbirininIsleminiAcmaz()
    {
        object[] poIslemleri =
        [
            new FinansSiparisOlusturCommand(), new FinansSiparisKalemEkleCommand(), new FinansSiparisGuncelleCommand(),
            new FinansSiparisIptalCommand(), new FinansKaliciSilCommand { Model = new("Siparis", 1, "1", true, "Hatalı kayıt") },
            new FinansKaliciSilOnizlemeQuery { VarlikTuru = "Siparis", Id = 1 },
            new FinansBelgeYukleCommand { Model = new("Siparis", 1, "belge.pdf", []) }
        ];
        object[] faturaIslemleri =
        [
            new FinansFaturaOlusturCommand(), new FinansFaturaGuncelleCommand(), new FinansFaturaIptalCommand(),
            new FinansFaturaOdemeCommand(), new FinansKaliciSilCommand { Model = new("Fatura", 1, "1", true, "Hatalı kayıt") },
            new FinansKaliciSilOnizlemeQuery { VarlikTuru = "Fatura", Id = 1 },
            new FinansBelgeYukleCommand { Model = new("Fatura", 1, "belge.pdf", []) }, new FinansFaturalarQuery(),
            new FinansParametrelerQuery()
        ];

        var po = Roller((PoFatura, YetkiTipi.W), (PoFaturaPo, YetkiTipi.W));
        foreach (var istek in poIslemleri) Assert.True(await GecerMiAsync(istek, po), istek.GetType().Name);
        foreach (var istek in faturaIslemleri) Assert.False(await GecerMiAsync(istek, po), istek.GetType().Name);

        var fatura = Roller((PoFatura, YetkiTipi.W), (PoFaturaFatura, YetkiTipi.W));
        foreach (var istek in faturaIslemleri) Assert.True(await GecerMiAsync(istek, fatura), istek.GetType().Name);
        foreach (var istek in poIslemleri) Assert.False(await GecerMiAsync(istek, fatura), istek.GetType().Name);

        // Kayıt detayı sekme düzeyindedir; PO listesi fatura girişinde PO seçimi için Faturalar alt sekmesine de açıktır.
        foreach (var rol in new[] { po, fatura })
        {
            Assert.True(await GecerMiAsync(new FinansSiparislerQuery(), rol));
            Assert.True(await GecerMiAsync(new FinansSiparisGetirQuery(), rol));
            Assert.True(await GecerMiAsync(new FinansFaturaGetirQuery(), rol));
        }
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
        var disarida = Roller([(Giderler, YetkiTipi.W), (GiderKategorileri, YetkiTipi.W), .. AltSekmeleriyle(Tanimlar, YetkiTipi.W), (Vade, YetkiTipi.R)]);
        Assert.False(await GecerMiAsync(new FinansIsKaydiGetirQuery { Id = 1 }, disarida));
        Assert.False(await GecerMiAsync(new FinansGenelAramaQuery(), disarida));
        Assert.False(await GecerMiAsync(new FinansSiparisGetirQuery(), disarida));
        Assert.True(await GecerMiAsync(new FinansDegisiklikGecmisiQuery(), disarida));
        Assert.True(await GecerMiAsync(new FinansBelgeIndirQuery { Id = 1 }, disarida));
    }

    /// <summary>Vade uyarısı yalnız kendi R iznini ister: bütün sekme ve alt sekmelerin W izni yetmez; vade izni de sekme açmaz.</summary>
    [Fact]
    public async Task VadeUyarisi_KendiOkumaIzniniIster_SekmeIzniYetmez()
    {
        var tumSekmeler = Roller(FinansSekmeAgaci.Sekmeler.SelectMany(x => AltSekmeleriyle(x, YetkiTipi.W)).ToArray());
        Assert.False(await GecerMiAsync(new FinansVadeUyarilariQuery(), tumSekmeler));
        Assert.True(await GecerMiAsync(new FinansFaturalarQuery(), tumSekmeler));

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

            // Sekme düğümleri varken eksik eski kod beklenen durumdur (20261013_02 siler): betik durmaz, hiçbir şey değiştirmez.
            var odeme = menuler[FinansSekmeAgaci.EskiKodlar.OdemeKaydet].Id;
            await db.RolYetkileri.Where(x => x.MenuTanimiId == odeme).ExecuteDeleteAsync();
            await db.MenuTanimlari.Where(x => x.Id == odeme).ExecuteDeleteAsync();
            satirSayisi = await db.RolYetkileri.CountAsync();
            await db.Database.ExecuteSqlRawAsync(betik);
            Assert.Equal(satirSayisi, await db.RolYetkileri.CountAsync());

            // Eklenecek sekme varken beklenen kod eksikse betik hiçbir şey değiştirmeden durur.
            var munferit = menuler[YetkiKodlari.Finans.SekmeMunferitIsler].Id;
            await db.RolYetkileri.Where(x => x.MenuTanimiId == munferit).ExecuteDeleteAsync();
            await db.MenuTanimlari.Where(x => x.Id == munferit).ExecuteDeleteAsync();
            var hata = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync(betik));
            Assert.Contains(FinansSekmeAgaci.EskiKodlar.OdemeKaydet, hata.MessageText);
            Assert.False(await db.MenuTanimlari.AnyAsync(x => x.Kod == YetkiKodlari.Finans.SekmeMunferitIsler));
        });
    }

    /// <summary>
    /// Alt sekme göçü (20261013_03) sekme düğümleri olan ağaçta: 8 alt sekme doğru sekmenin altına eklenir; her rol alt
    /// sekmede sekmesindeki seviyeyi alır (sekme N veya kaydı yoksa alt sekme kaydı eklenmez, etkin seviye yine eşittir);
    /// mevcut kodların etkin seviyesi değişmez; ikinci çalıştırma hiçbir şeyi değiştirmez ve sonradan değiştirilmiş alt
    /// sekme iznine dokunmaz; sekme düğümü eksikse hiçbir şey yapmadan durur.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_AltSekmeBetigi_DugumleriSekmeAltinaEkler_RolSeviyesiniKopyalar_TekrarCalisir()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var roller = await SekmeAgaciniKurAsync(db);
            var once = await EtkinSeviyelerAsync(db, roller);
            var onceSatirlar = await RolSatirlariAsync(db);
            var betik = await File.ReadAllTextAsync(FinansSekmeAgaci.BetikYolu(FinansSekmeAgaci.AltSekmeBetikAdi));

            await db.Database.ExecuteSqlRawAsync(betik);
            db.ChangeTracker.Clear();

            var menuler = await db.MenuTanimlari.AsNoTracking().ToDictionaryAsync(x => x.Kod);
            foreach (var (id, kod, sekme) in FinansSekmeAgaci.AltSekmeler)
                Assert.Equal((id, (int?)menuler[sekme].Id, (string?)null), (menuler[kod].Id, menuler[kod].ParentId, menuler[kod].Route));

            var sonra = await EtkinSeviyelerAsync(db, roller);
            Assert.All(once, x => Assert.True(sonra[x.Key] == x.Value, $"{x.Key}: {x.Value} → {sonra[x.Key]}"));
            foreach (var rol in roller)
                foreach (var (_, kod, sekme) in FinansSekmeAgaci.AltSekmeler)
                    Assert.True(sonra[(rol, kod)] == sonra[(rol, sekme)], $"Rol {rol}, {kod}: {sonra[(rol, kod)]} ≠ {sekme}: {sonra[(rol, sekme)]}");

            // Alt sekme sırası: İş Takibi × 3, Tanımlar × 3, PO/Fatura × 2.
            const YetkiTipi N = YetkiTipi.N, R = YetkiTipi.R, W = YetkiTipi.W;
            Assert.All(FinansSekmeAgaci.AltSekmeler, x => Assert.Equal(W, sonra[(SistemRolleri.AdminId, x.Kod)]));
            Assert.Equal([W, W, W, W, W, W, R, R], FinansSekmeAgaci.AltSekmeler.Select(x => sonra[(14, x.Kod)]));
            Assert.Equal([N, N, N, R, R, R, W, W], FinansSekmeAgaci.AltSekmeler.Select(x => sonra[(15, x.Kod)]));
            Assert.All(FinansSekmeAgaci.AltSekmeler, x => Assert.Equal(N, sonra[(16, x.Kod)]));
            Assert.Equal([R, R, R, N, N, N, R, R], FinansSekmeAgaci.AltSekmeler.Select(x => sonra[(30, x.Kod)]));

            // Satır düzeyi: sekmede R/W kaydı olan role alt sekmede aynı kayıt; N kaydı veya kaydı olmayan sekmede alt sekme kaydı yok.
            var satirlar = await RolSatirlariAsync(db);
            foreach (var rol in roller)
                foreach (var (altId, kod, sekme) in FinansSekmeAgaci.AltSekmeler)
                {
                    var sekmeSeviyesi = onceSatirlar.TryGetValue((rol, menuler[sekme].Id), out var s) && s != (int)N ? s : (int?)null;
                    Assert.True(sekmeSeviyesi == (satirlar.TryGetValue((rol, altId), out var a) ? a : null), $"Rol {rol}, {kod}");
                }
            var altSekmeIdleri = FinansSekmeAgaci.AltSekmeler.Select(x => x.Id).ToHashSet();
            Assert.Equal(onceSatirlar.OrderBy(x => x.Key), satirlar.Where(x => !altSekmeIdleri.Contains(x.Key.MenuTanimiId)).OrderBy(x => x.Key));

            // Kimlik dizisi en büyük Id'ye çekilir; sonraki elle eklenen menü tanımı çakışmaz.
            var sonDeger = await db.Database.SqlQueryRaw<long?>(
                "SELECT pg_sequence_last_value(pg_get_serial_sequence('\"MenuTanimlari\"', 'Id')::regclass) AS \"Value\"").SingleAsync();
            Assert.True(sonDeger >= 5255, $"Menü kimlik dizisi: {sonDeger}");

            // İkinci çalıştırma hiçbir şeyi değiştirmez.
            var menuSayisi = await db.MenuTanimlari.CountAsync();
            await db.Database.ExecuteSqlRawAsync(betik);
            db.ChangeTracker.Clear();
            Assert.Equal(menuSayisi, await db.MenuTanimlari.CountAsync());
            Assert.Equal(satirlar.OrderBy(x => x.Key), (await RolSatirlariAsync(db)).OrderBy(x => x.Key));

            // Yönetici alt sekme iznini sonradan R'ye indirdi: betiğin tekrarı bu kararı ezmez, yeni satır eklemez.
            var faturaAltSekmesi = menuler[YetkiKodlari.Finans.SekmePoFaturaFatura].Id;
            var izin = await db.RolYetkileri.SingleAsync(x => x.RolId == 15 && x.MenuTanimiId == faturaAltSekmesi);
            izin.YetkiTipiId = (int)R;
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync(betik);
            db.ChangeTracker.Clear();
            Assert.Equal(satirlar.Count, await db.RolYetkileri.CountAsync());
            Assert.Equal((int)R, (await db.RolYetkileri.SingleAsync(x => x.RolId == 15 && x.MenuTanimiId == faturaAltSekmesi)).YetkiTipiId);

            // Beklenen sekme düğümü yoksa betik hiçbir şey değiştirmeden durur.
            var tanimlar = menuler[YetkiKodlari.Finans.SekmeTanimlar].Id;
            await db.MenuTanimlari.Where(x => x.Id == tanimlar).ExecuteUpdateAsync(x => x.SetProperty(m => m.Kod, "finans-sekme-tanimlar-kaldirildi"));
            await db.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"RolYetkileri\" WHERE \"MenuTanimiId\" BETWEEN 5251 AND 5253; DELETE FROM \"MenuTanimlari\" WHERE \"Id\" BETWEEN 5251 AND 5253;");
            menuSayisi = await db.MenuTanimlari.CountAsync();
            var satirSayisi = await db.RolYetkileri.CountAsync();
            var hata = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync(betik));
            Assert.Contains(YetkiKodlari.Finans.SekmeTanimlar, hata.MessageText);
            Assert.Equal(menuSayisi, await db.MenuTanimlari.CountAsync());
            Assert.Equal(satirSayisi, await db.RolYetkileri.CountAsync());
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
    /// Gerçek <see cref="RolService"/>: sentetik ağaç kök → 8 sekme (+ alt sekmeleri) ve vade uyarısı, canlı Id'lerle;
    /// rol kökte W, verilen kodlarda verilen seviyededir. Alt sekmeye ayrıca izin verilmezse kaydı yoktur (N).
    /// </summary>
    private static RolService Roller(params (string Kod, YetkiTipi Seviye)[] izinler)
    {
        var menuler = new List<MenuTanimi>
        {
            new() { Id = Kok, Kod = Modul },
            new() { Id = FinansSekmeAgaci.VadeUyarisiId, Kod = Vade, ParentId = Kok }
        };
        menuler.AddRange(FinansSekmeAgaci.Sekmeler.Select(kod => new MenuTanimi { Id = FinansSekmeAgaci.SekmeId(kod), Kod = kod, ParentId = Kok }));
        menuler.AddRange(FinansSekmeAgaci.AltSekmeler.Select(x => new MenuTanimi { Id = x.Id, Kod = x.Kod, ParentId = FinansSekmeAgaci.SekmeId(x.Sekme) }));
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

    /// <summary>Sekmeyi ve (varsa) alt sekmelerini aynı seviyede verir; göç betiğinin mevcut rollere verdiği biçim.</summary>
    private static (string Kod, YetkiTipi Seviye)[] AltSekmeleriyle(string sekme, YetkiTipi seviye) =>
        [(sekme, seviye), .. FinansSekmeAgaci.AltSekmeleri(sekme).Select(kod => (kod, seviye))];

    private static async Task<Dictionary<(int RolId, int MenuTanimiId), int>> RolSatirlariAsync(AppDbContext db) =>
        (await db.RolYetkileri.AsNoTracking().Select(x => new { x.RolId, x.MenuTanimiId, x.YetkiTipiId }).ToListAsync())
            .ToDictionary(x => (x.RolId, x.MenuTanimiId), x => x.YetkiTipiId);

    /// <summary>
    /// 20261013_02 sonrası ağaç: kök 47, 8 sekme düğümü (5238–5245) ve vade uyarısı (5247). Roller: Admin bütün sekmelerde W;
    /// 14 karışık (İş Takibi W, PO/Fatura R, Tanımlar W, Panel R); 15 (İş Takibi açık N kaydı, PO/Fatura W, Tanımlar R);
    /// 16 hiç finans izni yok; 30 kökte R, İş Takibi W kaydı (etkin R), PO/Fatura R.
    /// </summary>
    private static async Task<int[]> SekmeAgaciniKurAsync(AppDbContext db)
    {
        if (!await db.MenuTanimlari.AnyAsync(x => x.Id == Kok))
            db.MenuTanimlari.Add(new MenuTanimi { Id = Kok, Kod = YetkiKodlari.Finans.Modul, LabelKey = "MENU.FINANS_YONETIMI", Icon = "ri-line-chart-line", Route = "/finans-yonetimi", Sira = 9 });
        foreach (var (sekme, sira) in FinansSekmeAgaci.Sekmeler.Select((x, i) => (x, i + 1)))
            db.MenuTanimlari.Add(new MenuTanimi { Id = FinansSekmeAgaci.SekmeId(sekme), Kod = sekme, LabelKey = "MENU." + sekme, Icon = "", Sira = sira, ParentId = Kok });
        db.MenuTanimlari.Add(new MenuTanimi { Id = FinansSekmeAgaci.VadeUyarisiId, Kod = Vade, LabelKey = "MENU.FINANS_VADE_UYARISI", Icon = "", Sira = 9, ParentId = Kok });
        if (!await db.Roller.AnyAsync(x => x.Id == SistemRolleri.AdminId))
            db.Roller.Add(new Rol { Id = SistemRolleri.AdminId, Ad = SistemRolleri.AdminAdi });
        db.Roller.AddRange(new Rol { Id = 14, Ad = "Sistem Yönetici" }, new Rol { Id = 15, Ad = "Sipariş Operasyon Yetkilisi" },
            new Rol { Id = 16, Ad = "TEST" }, new Rol { Id = 30, Ad = "Salt okuma, İş Takibi yazması tavanlı" });
        await db.SaveChangesAsync();
        await db.RolYetkileri.Where(x => x.RolId == SistemRolleri.AdminId).ExecuteDeleteAsync();

        RolYetki Izin(int rol, int menu, YetkiTipi seviye) => new() { RolId = rol, MenuTanimiId = menu, YetkiTipiId = (int)seviye };
        int Sekme(string kod) => FinansSekmeAgaci.SekmeId(kod);
        db.RolYetkileri.AddRange(FinansSekmeAgaci.Sekmeler.Select(x => Izin(SistemRolleri.AdminId, Sekme(x), YetkiTipi.W)));
        db.RolYetkileri.AddRange(
            Izin(SistemRolleri.AdminId, Kok, YetkiTipi.W), Izin(SistemRolleri.AdminId, FinansSekmeAgaci.VadeUyarisiId, YetkiTipi.R),
            Izin(14, Kok, YetkiTipi.W), Izin(14, Sekme(IsTakibi), YetkiTipi.W), Izin(14, Sekme(PoFatura), YetkiTipi.R),
            Izin(14, Sekme(Tanimlar), YetkiTipi.W), Izin(14, Sekme(Panel), YetkiTipi.R),
            Izin(15, Kok, YetkiTipi.W), Izin(15, Sekme(IsTakibi), YetkiTipi.N), Izin(15, Sekme(PoFatura), YetkiTipi.W),
            Izin(15, Sekme(Tanimlar), YetkiTipi.R),
            Izin(30, Kok, YetkiTipi.R), Izin(30, Sekme(IsTakibi), YetkiTipi.W), Izin(30, Sekme(PoFatura), YetkiTipi.R));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return [SistemRolleri.AdminId, 14, 15, 16, 30];
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
    internal static async Task CanliAgaciKurAsync(AppDbContext db)
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
