using _3K.YuklemeMotoru.Ekipman;
using _3K.YuklemeMotoru.Yerlesim;
using Xunit;

namespace _3K.Application.Tests;

/// <summary>
/// Yerleşim motoru: Simülasyon isterleri 30'daki kabul senaryoları (kapıdan geçiş, içeride istif, Open Top, enine
/// ortalama, üst yük, aile kuralı, trafo/lowbed, özel yük, taşma) ve en az araç / alt tip karışımı. Ekipman ölçüleri test
/// verisidir; kitaplıktaki gerçek veri yerine geçmez.
/// </summary>
public sealed class YerlesimMotoruTests
{
    internal static readonly YerlesimAyarlari Ayar = new(30, 50, 0, 0, 0, 50);

    private static EkipmanAdayi Konteyner(int id, string kod, int uzunluk, int kapasite = 26_500, bool ustten = false,
        int icYukseklik = 2_393, int kapiYukseklik = 2_280) => new(
        id, 1, kod, kod, "Konteyner", ustten ? "Open Top" : "Kapalı", AileIci: true, OzelYukEkipmani: false,
        ustten ? GirisYonu.Arkadan | GirisYonu.Ustten : GirisYonu.Arkadan, Duvarli: true, Dingilli: false, Dogrulandi: true,
        new EkipmanTeknikVerisi
        {
            DisOlcu = new Olcu(uzunluk + 160, 2_438, 2_591),
            TeknikKapasiteKg = kapasite,
            Yuzeyler = [new YukYuzeyi("ANA", "Taban", 0, uzunluk, 2_352, 0, icYukseklik, null, null)],
            Acikliklar = ustten
                ? [new Aciklik(AciklikTarafi.Arka, null, null, 2_340, kapiYukseklik), new Aciklik(AciklikTarafi.Ust, 100, uzunluk - 200, 2_200, null)]
                : [new Aciklik(AciklikTarafi.Arka, null, null, 2_340, kapiYukseklik)]
        });

    internal static readonly EkipmanAdayi K40 = Konteyner(40, "K40", 12_032);
    private static readonly EkipmanAdayi K20 = Konteyner(20, "K20", 5_898, 28_000);

    internal static readonly EkipmanAdayi Lowbed = new(
        90, 1, "LB-3", "3 dingil lowbed", "Açık kasa / platform", "Lowbed", AileIci: false, OzelYukEkipmani: true,
        GirisYonu.Yandan, Duvarli: false, Dingilli: true, Dogrulandi: false,
        new EkipmanTeknikVerisi
        {
            DisOlcu = new Olcu(16_000, 2_550, 1_600),
            TeknikKapasiteKg = 60_000,
            KingPin = new KingPin(800, 18_000),
            Yuzeyler = [new YukYuzeyi("BOYUN", "Boyun", 0, 3_000, 2_550, 1_600, null, null, null),
                        new YukYuzeyi("HAVUZ", "Havuz", 3_200, 9_000, 2_550, 900, null, null, null)],
            Dingiller = [new Dingil(12_600, DingilTuru.Tahriksiz, 1, 12_000), new Dingil(13_960, DingilTuru.Tahriksiz, 1, 12_000),
                         new Dingil(15_320, DingilTuru.Tahriksiz, 1, 12_000)]
        });

    internal static YerlesimYuku Sandik(string kod, int boy, int en, int yuk, decimal kg, int? proje = 1, decimal? ustYuk = null,
        bool ust = true, bool alt = true) =>
        new(kod, kod, proje, $"P{proje}", null, boy, en, yuk, kg, false, ust, alt, ustYuk ?? kg, false, OzelEkipmanZorunlulugu.Yok, "Sandık");

    internal static YerlesimYuku Trafo(string kod, int boy, int en, int yuk, decimal kg) =>
        new(kod, kod, 1, "P1", null, boy, en, yuk, kg, false, false, false, null, true, OzelEkipmanZorunlulugu.Her, "Trafo");

    private static YerlesimSonucu Hesapla(IReadOnlyList<YerlesimYuku> yukler, IReadOnlyList<EkipmanAdayi> adaylar,
        bool projeAyri = false, IReadOnlyList<MevcutArac>? mevcut = null, YerlesimAyarlari? ayar = null) =>
        YerlesimMotoru.Hesapla(new YerlesimGirdisi(yukler, adaylar, ayar ?? Ayar, projeAyri, false, mevcut, "Konteyner"));

    /// <summary>Senaryo: yük içeri sığar ama kapıdan geçmez → kapalı ekipman uygun gösterilmez.</summary>
    [Fact]
    public void KapidanGecmeyenYuk_KapaliEkipmanaYerlesmez()
    {
        var sonuc = Hesapla([Sandik("S1", 2_000, 1_200, 2_330, 500)], [K40]);
        Assert.Empty(sonuc.Araclar);
        Assert.Contains("arka kapıdan geçmiyor", Assert.Single(sonuc.Yerlesemeyenler).Neden);
    }

    /// <summary>Senaryo: kapalı araçta istif kapıdan geçmezse içeride istif yapılacağı varsayılmaz.</summary>
    [Fact]
    public void KapidanGecmeyenIstif_KurulmazYanYanaKonur()
    {
        // 2 × 1.150 = 2.300 mm iç yüksekliğe (2.393 − 50 pay) sığar ama kapıdan (2.280 − 50) geçmez.
        var sonuc = Hesapla([Sandik("A", 2_000, 2_000, 1_150, 400), Sandik("B", 2_000, 2_000, 1_150, 300)], [K40]);
        var arac = Assert.Single(sonuc.Araclar);
        Assert.All(arac.Konumlar, k => Assert.Equal(1, k.Kat));
    }

    /// <summary>İstif kapıdan geçiyorsa dışarıda hazırlanmış grup olarak kurulur (tek yükleme sırası).</summary>
    [Fact]
    public void KapidanGecenIstif_GrupOlarakKurulur()
    {
        var sonuc = Hesapla([Sandik("A", 2_000, 2_000, 1_000, 400), Sandik("B", 2_000, 2_000, 1_000, 300)], [K40]);
        var konumlar = Assert.Single(sonuc.Araclar).Konumlar;
        Assert.Equal([1, 2], konumlar.Select(k => k.Kat).Order());
        Assert.Single(konumlar.Select(k => k.YuklemeSirasi).Distinct());
        Assert.Equal(1_000, konumlar.Single(k => k.Kat == 2).ZMm);
    }

    /// <summary>Senaryo: Open Top üst açıklığı uygunsa kapıdan geçmeyen yük üstten yüklenir.</summary>
    [Fact]
    public void OpenTop_KapidanGecmeyenYukUsttenYuklenir()
    {
        var openTop = Konteyner(41, "OT40", 12_032, ustten: true);
        var sonuc = Hesapla([Sandik("S1", 2_000, 1_200, 2_330, 500)], [openTop]);
        var konum = Assert.Single(Assert.Single(sonuc.Araclar).Konumlar);
        Assert.Equal(GirisYonu.Ustten, konum.YuklemeYontemi);
    }

    /// <summary>Senaryo: yanına başka yük sığmayan tek yük enine ortalanır.</summary>
    [Fact]
    public void TekYuk_EnineOrtalanir()
    {
        var konum = Assert.Single(Assert.Single(Hesapla([Sandik("S1", 2_000, 1_000, 1_000, 500)], [K40]).Araclar).Konumlar);
        Assert.Equal(2_438 / 2, konum.YMm + konum.EnMm / 2);
        Assert.Equal(30, konum.XMm);
    }

    /// <summary>Senaryo: üst yük kapasitesi aşılıyorsa otomatik çözüm o istifi kurmaz; sınır içindeyse kurar.</summary>
    [Fact]
    public void UstYukSiniri_AsilirsaIstifKurulmaz()
    {
        // Alttaki daha ağırdır (ağır yük alta sıralanır) ama üstüne en fazla 500 kg alır.
        YerlesimYuku Alt() => Sandik("ALT", 1_200, 1_200, 1_000, 800, ustYuk: 500);
        var agir = Assert.Single(Hesapla([Alt(), Sandik("UST", 1_200, 1_200, 1_000, 600)], [K40]).Araclar);
        Assert.All(agir.Konumlar, k => Assert.Equal(1, k.Kat));

        var hafif = Assert.Single(Hesapla([Alt(), Sandik("UST", 1_200, 1_200, 1_000, 400)], [K40]).Araclar);
        Assert.Equal(2, hafif.Konumlar.Single(k => k.YukAnahtari == "UST").Kat);

        var izinsiz = Assert.Single(Hesapla([Alt(), Sandik("UST", 1_200, 1_200, 1_000, 400, ust: false)], [K40]).Araclar);
        Assert.All(izinsiz.Konumlar, k => Assert.Equal(1, k.Kat));
    }

    /// <summary>
    /// Saha geri bildirimi (19.11.2026) madde 4: seçilen aileye sığmayan yük, türü "Sandık" olsa da bırakılmaz; gerekçesiyle
    /// lowbed'e gider ve yanına başka yük konmaz. Diğer sandıklar ailede kalır.
    /// </summary>
    [Fact]
    public void AileyeSigmayanSandik_GerekceliLowbede()
    {
        var genis = Sandik("01", 6_000, 2_500, 2_400, 15_000, ust: false, alt: false);
        var sonuc = Hesapla([genis, Sandik("02", 2_000, 1_200, 1_000, 500)], [K40, Lowbed]);
        Assert.Empty(sonuc.Yerlesemeyenler);
        var lowbed = sonuc.Araclar.Single(a => a.EkipmanKodu == "LB-3");
        Assert.Equal("01", Assert.Single(lowbed.Konumlar).YukAnahtari);
        Assert.Contains("01 Konteyner ailesinde taşınamıyor", lowbed.AileDisiGerekce);
        Assert.Equal("02", Assert.Single(sonuc.Araclar.Single(a => a.EkipmanKodu == "K40").Konumlar).YukAnahtari);
    }

    /// <summary>Aileye de lowbed'e de sığmayan yük yerleşemeyenlerde, iki nedeniyle birlikte kalır.</summary>
    [Fact]
    public void AileyeVeLowbedeSigmayan_YerlesemeyenNedenli()
    {
        var sonuc = Hesapla([Sandik("UZUN", 13_000, 2_000, 1_000, 3_000)], [K40, Lowbed]);
        Assert.Empty(sonuc.Araclar);
        var neden = Assert.Single(sonuc.Yerlesemeyenler).Neden;
        Assert.Contains("Konteyner ailesindeki hiçbir ekipmana sığmıyor", neden);
        Assert.Contains("Uygun özel yük ekipmanı bulunamadı", neden);
    }

    /// <summary>
    /// Saha geri bildirimi madde 1: araç tipi yük başına seçilir. Kapıdan geçmeyen tek yük için Open Top gelir ve ona
    /// sığan sandıklar da konur; kalan sandıklar standart konteynere gider (bütün araçlar Open Top olmaz).
    /// </summary>
    [Fact]
    public void OzelTip_YalnizGerekenYukIcinGelir()
    {
        var openTop = Konteyner(41, "OT40", 12_032, ustten: true) with { Kademe = 4 };
        var yukler = Enumerable.Range(1, 30).Select(i => Sandik($"S{i:00}", 2_000, 2_000, 1_200, 400, ust: false))
            .Append(Sandik("YUKSEK", 2_000, 2_000, 2_330, 500, ust: false)).ToList();
        var sonuc = Hesapla(yukler, [K40, openTop]);
        Assert.Empty(sonuc.Yerlesemeyenler);
        var ot = Assert.Single(sonuc.Araclar, a => a.EkipmanKodu == "OT40");
        Assert.Contains(ot.Konumlar, k => k.YukAnahtari == "YUKSEK");
        Assert.True(ot.Konumlar.Count > 1, "Open Top'a sığan sandıklar da konur.");
        Assert.All(sonuc.Araclar.Where(a => a != ot), a => Assert.Equal("K40", a.EkipmanKodu));
        Assert.Equal(31, sonuc.Araclar.Sum(a => a.Konumlar.Count));
        // Sade araçlar önce numaralanır.
        Assert.Equal("OT40", sonuc.Araclar[^1].EkipmanKodu);
        Assert.Null(ot.AileDisiGerekce);
    }

    /// <summary>Kademesi olmayan tip (ör. Reefer, açıkça seçilmedikçe) otomatik yerleşimde kullanılmaz.</summary>
    [Fact]
    public void KademesizTip_OtomatikGelmez()
    {
        var reefer = Konteyner(45, "RF40", 12_032) with { Kademe = null };
        var sonuc = Hesapla([Sandik("S1", 2_000, 1_200, 1_000, 500)], [reefer, K40]);
        Assert.Equal("K40", Assert.Single(sonuc.Araclar).EkipmanKodu);

        var yalnizReefer = Hesapla([Sandik("S1", 2_000, 1_200, 1_000, 500)], [reefer]);
        Assert.Empty(yalnizReefer.Araclar);
        Assert.Contains("otomatik kullanılan araç tipi yok", Assert.Single(yalnizReefer.Yerlesemeyenler).Neden);
    }

    /// <summary>
    /// Saha geri bildirimi madde 3: tip seçilince normal yükler yalnız seçilen tipe gider (daha sade tip seçilmediyse
    /// kullanılmaz); seçilen tipe sığmayan yük için ailenin yedek tipi gerekçesiyle gelir.
    /// </summary>
    [Fact]
    public void SeciliTip_YalnizOKullanilir_SigmayanaYedekGerekceli()
    {
        var hc = Konteyner(46, "HC40", 12_032, icYukseklik: 2_698, kapiYukseklik: 2_585) with { Kademe = 2 };
        var dv = K40 with { Yedek = true };
        var openTop = Konteyner(41, "OT40", 12_032, ustten: true) with { Kademe = 4, Yedek = true };
        var sandiklar = Enumerable.Range(1, 3).Select(i => Sandik($"S{i}", 2_000, 1_200, 1_000, 500)).ToList();
        var yalnizHc = Hesapla(sandiklar, [dv, hc, openTop]);
        Assert.Equal("HC40", Assert.Single(yalnizHc.Araclar).EkipmanKodu);

        // 2.330 mm: kapısı dar (2.280) High Cube'un ve DV'nin kapısından geçmez; yedek Open Top üstten alır. Özel aracın
        // yanına sığan sandıklar da konur (saha geri bildirimi madde 1): seçilen HC'ye ayrıca araç açılmaz.
        var yuksek = Sandik("YUKSEK", 2_000, 2_000, 2_330, 500);
        var hcKapisiDar = Konteyner(46, "HC40", 12_032, icYukseklik: 2_698, kapiYukseklik: 2_280) with { Kademe = 2 };
        var sonuc = Hesapla([.. sandiklar, yuksek], [dv, hcKapisiDar, openTop]);
        var ot = Assert.Single(sonuc.Araclar, a => a.EkipmanKodu == "OT40");
        Assert.StartsWith("Seçilen tiplere sığmadı: YUKSEK. Bu yükler için 1 adet OT40", ot.AileDisiGerekce);
        Assert.False(ot.AileDisi);
        Assert.Equal(4, ot.Konumlar.Count);
        Assert.DoesNotContain(sonuc.Araclar, a => a.EkipmanKodu is "K40" or "HC40");
    }

    /// <summary>Ailede veri var ama otomatik tip yoksa (yalnız Reefer) neden tip seçmeyi söyler.</summary>
    [Fact]
    public void OtomatikTipYoksa_TipSecmeyiSoyler()
    {
        var reefer = Konteyner(45, "RF40", 12_032) with { Kademe = null };
        var sonuc = Hesapla([Sandik("S1", 2_000, 1_200, 1_000, 500)], [reefer, Lowbed]);
        Assert.Empty(sonuc.Araclar);
        Assert.Contains("otomatik kullanılan araç tipi yok; planda araç tipi seçin", Assert.Single(sonuc.Yerlesemeyenler).Neden);
    }

    /// <summary>Mevcut araçlara yerleştirmede kullanıcının filosu esastır: kademesi olmayan aile tipi (Reefer) de kullanılır.</summary>
    [Fact]
    public void MevcutAraclar_KademesizFiloKullanilir()
    {
        var reefer = Konteyner(45, "RF40", 12_032) with { Kademe = null };
        var sonuc = Hesapla([Sandik("S1", 2_000, 1_200, 1_000, 500)], [reefer, Lowbed], mevcut: [new MevcutArac(45, 1)]);
        Assert.Equal("RF40", Assert.Single(sonuc.Araclar).EkipmanKodu);
        Assert.Empty(sonuc.Yerlesemeyenler);
    }

    /// <summary>"Kalanı diz", Hesapla gibi: ailede aday yoksa yük lowbed'e çıkmaz, bekleyenlerde kalır (isterler 10.2).</summary>
    [Fact]
    public void ElleTamamla_AiledeVeriYoksa_AileDisinaCikmaz()
    {
        var girdi = new YerlesimGirdisi([Sandik("S1", 2_000, 1_200, 1_000, 500)], [Lowbed], Ayar, false, false, null, "Tenteli");
        Assert.Empty(YerlesimMotoru.ElleTamamla(girdi, []));
    }

    /// <summary>Lowbed ailesi seçiliyse "kalanı diz" kullanıcının boş lowbed'ine yük koyar; trafo taşıyan lowbed'e koymaz.</summary>
    [Fact]
    public void ElleTamamla_LowbedAilesindeVarOlanAracaEkler()
    {
        var lowbed = Lowbed with { AileIci = true };
        var trafo = Trafo("TR-01", 6_000, 2_400, 3_000, 30_000);
        var girdi = new YerlesimGirdisi([Sandik("S1", 2_000, 1_200, 1_000, 500), trafo], [lowbed], Ayar, false, false, null, "Lowbed / ağır yük");
        var trafoKonumu = Assert.Single(YerlesimMotoru.ElleTamamla(new YerlesimGirdisi([trafo], [lowbed], Ayar, false, false, null, "Lowbed / ağır yük"), [])).Konumlar;
        var araclar = YerlesimMotoru.ElleTamamla(girdi, [new ElleArac(90, trafoKonumu), new ElleArac(90, [])]);
        Assert.Equal(2, araclar.Count);
        Assert.Equal(["TR-01"], araclar[0].Konumlar.Select(k => k.YukAnahtari));
        Assert.Equal(["S1"], araclar[1].Konumlar.Select(k => k.YukAnahtari));
    }

    /// <summary>Elle denetim, aileye sığmadığı için lowbed'e giden sandığın ve yedek tipin gerekçesini korur.</summary>
    [Fact]
    public void ElleDenetim_AileyeSigmayanVeYedekGerekcesiKorunur()
    {
        var genis = Sandik("01", 6_000, 2_500, 2_400, 15_000, ust: false, alt: false);
        var girdi = new YerlesimGirdisi([genis, Sandik("02", 2_000, 1_200, 1_000, 500)], [K40, Lowbed], Ayar, false, false, null, "Konteyner");
        var denetim = ElleYerlesimDenetimi.Denetle(girdi, YerlesimMotoru.ElleTamamla(girdi, []));
        var lowbed = denetim.Araclar.Single(a => a.EkipmanKodu == "LB-3");
        Assert.Equal("Zorunlu özel ekipman: 01 Konteyner ailesinde taşınamıyor. 1 adet LB-3 plana eklenmiştir.", lowbed.AileDisiGerekce);
        Assert.True(lowbed.ZorunluOzelEkipman);

        var hc = Konteyner(46, "HC40", 12_032, icYukseklik: 2_698, kapiYukseklik: 2_280) with { Kademe = 2 };
        var openTop = Konteyner(41, "OT40", 12_032, ustten: true) with { Kademe = 4, Yedek = true };
        var yedekGirdi = new YerlesimGirdisi([Sandik("YUKSEK", 2_000, 2_000, 2_330, 500)], [hc, openTop], Ayar, false, false, null, "Konteyner");
        var ot = Assert.Single(ElleYerlesimDenetimi.Denetle(yedekGirdi, YerlesimMotoru.ElleTamamla(yedekGirdi, [])).Araclar);
        Assert.StartsWith("Seçilen tiplere sığmadı: YUKSEK.", ot.AileDisiGerekce);
    }

    /// <summary>Saha geri bildirimi madde 2: lowbed tonaja göre seçilir (yükü taşıyan en küçük kapasite).</summary>
    [Fact]
    public void Lowbed_TonajaGoreSecilir()
    {
        var lowbed45 = Lowbed with { KonfigurasyonId = 91, Kod = "LB-45", TeknikVeri = Lowbed.TeknikVeri with { TeknikKapasiteKg = 45_000 } };
        var hafif = Hesapla([Trafo("TR-01", 6_000, 2_400, 3_000, 30_000)], [K40, Lowbed, lowbed45]);
        Assert.Equal("LB-45", Assert.Single(hafif.Araclar).EkipmanKodu);
        var agir = Hesapla([Trafo("TR-02", 6_000, 2_400, 3_000, 50_000)], [K40, Lowbed, lowbed45]);
        Assert.Equal("LB-3", Assert.Single(agir.Araclar).EkipmanKodu);
    }

    /// <summary>"Kalanı diz" de yük başına tip seçer: Open Top yalnız gereken yük için açılır.</summary>
    [Fact]
    public void ElleTamamla_OzelTipYalnizGerekenYukIcin()
    {
        var openTop = Konteyner(41, "OT40", 12_032, ustten: true) with { Kademe = 4 };
        var yukler = Enumerable.Range(1, 30).Select(i => Sandik($"S{i:00}", 2_000, 2_000, 1_200, 400, ust: false))
            .Append(Sandik("YUKSEK", 2_000, 2_000, 2_330, 500, ust: false)).ToList();
        var araclar = YerlesimMotoru.ElleTamamla(new YerlesimGirdisi(yukler, [K40, openTop], Ayar, false, false, null, "Konteyner"), []);
        Assert.Equal(1, araclar.Count(a => a.KonfigurasyonId == 41));
        Assert.Equal(31, araclar.Sum(a => a.Konumlar.Count));
    }

    /// <summary>Senaryo: konteyner seçili projede trafo varsa sandıklar konteynerde, trafo gerekçeli lowbed'de planlanır.</summary>
    [Fact]
    public void Trafo_LowbeddeSandiklarKonteynerde()
    {
        var sonuc = Hesapla([Sandik("S1", 2_000, 1_200, 1_000, 800), Trafo("TR-01", 6_000, 3_000, 3_800, 45_000)], [K40, K20, Lowbed]);
        Assert.Equal(2, sonuc.Araclar.Count);
        var lowbed = sonuc.Araclar.Single(a => a.EkipmanKodu == "LB-3");
        Assert.True(lowbed.AileDisi);
        Assert.StartsWith("Zorunlu özel ekipman: TR-01 trafo için Lowbed gereklidir. Genel Konteyner tercihinin dışında 1 adet LB-3", lowbed.AileDisiGerekce);
        Assert.Equal("HAVUZ", Assert.Single(lowbed.Konumlar).YuzeyKodu);
        Assert.False(sonuc.Araclar.Single(a => a.EkipmanKodu != "LB-3").AileDisi);
    }

    /// <summary>Senaryo: diğer özel yük ailede taşınamıyorsa gerekçeli zorunlu ekipman otomatik eklenir.</summary>
    [Fact]
    public void OzelYuk_AiledeTasinamazsaGerekceliLowbed()
    {
        var yuk = Sandik("OZ-1", 8_000, 2_400, 2_500, 20_000) with { OzelYuk = true, YukTuruAdi = "Diğer özel yük", UsteKonabilir = false, AltaKalabilir = false };
        var arac = Assert.Single(Hesapla([yuk], [K40, Lowbed]).Araclar);
        Assert.Equal("LB-3", arac.EkipmanKodu);
        Assert.Contains("Konteyner ailesinde taşınamıyor", arac.AileDisiGerekce);
    }

    /// <summary>Senaryo: uygun lowbed verisi yoksa uygunluk üretilmez, neden gösterilir.</summary>
    [Fact]
    public void Trafo_LowbedYoksaUygunlukUretilmez()
    {
        var sonuc = Hesapla([Trafo("TR-01", 6_000, 3_000, 3_800, 45_000)], [K40]);
        Assert.Empty(sonuc.Araclar);
        Assert.Contains("lowbed", Assert.Single(sonuc.Yerlesemeyenler).Neden);
        // Araç yokken ya da yük yerleşemediyse genel sonuç "Uygun" olamaz.
        Assert.Equal(KontrolSonucu.Uygunsuz, sonuc.Sonuc);
        Assert.StartsWith("1 yük yerleşemedi", sonuc.Uyarilar[0], StringComparison.Ordinal);

        var agir = Hesapla([Trafo("TR-02", 6_000, 3_000, 3_800, 70_000)], [Lowbed]);
        Assert.Contains("teknik kapasite", Assert.Single(agir.Yerlesemeyenler).Neden);
    }

    /// <summary>Senaryo: kasa dışına taşmada yön ve miktar gösterilir; lowbed'de konum dingil/king-pin dengesine göre seçilir.</summary>
    [Fact]
    public void Tasma_YonVeMiktarGosterilir()
    {
        var arac = Assert.Single(Hesapla([Trafo("TR-01", 6_000, 5_250, 3_800, 45_000)], [Lowbed]).Araclar);
        Assert.Equal(1_350, arac.SolTasmaMm);
        Assert.Equal(1_350, arac.SagTasmaMm);
        Assert.Equal(5_250, arac.YukluGenislikMm);
        // Taşma izni teyit gerektirir; kontrol listesinde gösterilmez, miktar taşma alanlarındadır. Doğrulanmamış
        // ekipman verisi de ayrı kontrol olarak yazılmaz.
        Assert.DoesNotContain(arac.Kontroller, k => k.Tur is KontrolTuru.Tasma or KontrolTuru.EkipmanVerisi);
        // 45 t yükün dingil ve king-pin payı kapasitelere göre dengelenir; konum havuzun önüne yapışık değildir.
        Assert.True(Assert.Single(arac.Konumlar).XMm > 3_200);
        Assert.DoesNotContain(arac.Kontroller, k => k.Sonuc == KontrolSonucu.Uygunsuz);
    }

    /// <summary>En az araç: 25 yük 2 × 40' yerine 40' + 20' ile taşınır (aynı aile içinde alt tipler karışır).</summary>
    [Fact]
    public void EnAzArac_AltTiplerKarisir()
    {
        var yukler = Enumerable.Range(1, 25).Select(i => Sandik($"S{i:00}", 1_100, 1_100, 1_000, 300, ust: false)).ToList();
        var sonuc = Hesapla(yukler, [K40, K20]);
        Assert.Equal(["K40", "K20"], sonuc.Araclar.Select(a => a.EkipmanKodu));
        Assert.Equal(25, sonuc.Araclar.Sum(a => a.Konumlar.Count));
        Assert.Empty(sonuc.Yerlesemeyenler);
    }

    /// <summary>40' High Cube (kademe 2): iç yükseklik 2.700 mm; 1.200 mm'lik iki sandık üst üste yalnız burada sığar.</summary>
    private static readonly EkipmanAdayi K40Hc =
        Konteyner(41, "K40HC", 12_032, icYukseklik: 2_700, kapiYukseklik: 2_585) with { Kademe = 2, TipAdi = "40' High Cube" };

    private static List<YerlesimYuku> IstiflenenSandiklar(int adet) =>
        Enumerable.Range(1, adet).Select(i => Sandik($"I{i:00}", 5_800, 2_000, 1_200, 1_000)).ToList();

    /// <summary>
    /// En az araç (kullanıcı kararı, 20 Kasım 2026): standart konteynerde istiflenemeyen sandıklar High Cube'da üst üste
    /// gelir ve araç sayısı düşer; motor High Cube'u kendiliğinden seçer ve nedenini uyarıda yazar.
    /// </summary>
    [Fact]
    public void EnAzArac_UstKademeAracSayisiniDusururse_Secilir()
    {
        var yukler = IstiflenenSandiklar(4);
        Assert.Equal(["K40", "K40"], Hesapla(yukler, [K40]).Araclar.Select(a => a.EkipmanKodu));

        var sonuc = Hesapla(yukler, [K40, K40Hc]);
        var arac = Assert.Single(sonuc.Araclar);
        Assert.Equal("K40HC", arac.EkipmanKodu);
        Assert.Equal(4, arac.Konumlar.Count);
        Assert.Empty(sonuc.Yerlesemeyenler);
        Assert.Contains(sonuc.Uyarilar, u => u == "En az araç için 40' High Cube kullanıldı: yalnız en sade tiplerle 2 araç gerekirdi, bu düzenle 1 araç.");
        // Aynı girdi aynı sonucu verir; "kalanı diz" de aynı kuralla tek High Cube açar.
        Assert.Equal(YerlesimSerilestirici.Yaz(sonuc), YerlesimSerilestirici.Yaz(Hesapla(yukler, [K40, K40Hc])));
        var girdi = new YerlesimGirdisi(yukler, [K40, K40Hc], Ayar, false, false, null, "Konteyner");
        Assert.Equal(41, Assert.Single(YerlesimMotoru.ElleTamamla(girdi, [])).KonfigurasyonId);
    }

    /// <summary>
    /// Üst kademe yalnız ona ihtiyacı olan araçta kalır: istif gerektirmeyen yükleri taşıyan araç yine standart
    /// konteynere indirilir (en büyük ihtiyacın tipi bütün araçlara uygulanmaz).
    /// </summary>
    [Fact]
    public void EnAzArac_IhtiyaciOlmayanAracSadeTipeIner()
    {
        var yukler = IstiflenenSandiklar(4);
        yukler.AddRange([Sandik("Y1", 5_800, 2_000, 2_000, 3_000, ust: false, alt: false), Sandik("Y2", 5_800, 2_000, 2_000, 3_000, ust: false, alt: false)]);
        Assert.Equal(3, Hesapla(yukler, [K40]).Araclar.Count);

        var sonuc = Hesapla(yukler, [K40, K40Hc]);
        Assert.Equal(["K40", "K40HC"], sonuc.Araclar.Select(a => a.EkipmanKodu));
        Assert.Equal(["Y1", "Y2"], sonuc.Araclar[0].Konumlar.Select(k => k.YukAnahtari).Order());
        // Her yük tam bir kez yerleşir.
        Assert.Equal(yukler.Select(y => y.Anahtar).Order(), sonuc.Araclar.SelectMany(a => a.Konumlar).Select(k => k.YukAnahtari).Order());
        Assert.Empty(sonuc.Yerlesemeyenler);
    }

    /// <summary>
    /// Yükseltme yalnız duvarlı (kapalı) tiplere yapılır: geniş platform aynı yükleri daha az araçla alsa da sıradan sandık
    /// araç sayısı için açık tipe taşınmaz.
    /// </summary>
    [Fact]
    public void EnAzArac_AcikTipeYukseltilmez()
    {
        var platform = new EkipmanAdayi(55, 1, "PLT", "40' Platform", "Konteyner", "40' Platform", AileIci: true, OzelYukEkipmani: false,
            GirisYonu.Ustten | GirisYonu.Yandan, Duvarli: false, Dingilli: false, Dogrulandi: true,
            new EkipmanTeknikVerisi
            {
                DisOlcu = new Olcu(12_192, 2_600, 648),
                TeknikKapasiteKg = 39_000,
                Yuzeyler = [new YukYuzeyi("ANA", "Taban", 0, 12_192, 2_600, 648, null, null, null)]
            },
            Kademe: 6);
        var yukler = Enumerable.Range(1, 4).Select(i => Sandik($"D{i}", 5_800, 1_200, 1_000, 1_000, ust: false, alt: false)).ToList();
        // Platformda yan yana ikişer sığar (tek araç); standart konteynerde tek sıra, iki araç.
        Assert.Single(Hesapla(yukler, [platform]).Araclar);
        var sonuc = Hesapla(yukler, [K40, platform]);
        Assert.Equal(["K40", "K40"], sonuc.Araclar.Select(a => a.EkipmanKodu));
        Assert.DoesNotContain(sonuc.Uyarilar, u => u.StartsWith("En az araç", StringComparison.Ordinal));
    }

    /// <summary>Proje bazında ayrı planda her grup kendi içinde yükseltilir; uyarı planın toplam araç sayısıyla tek satır yazılır.</summary>
    [Fact]
    public void EnAzArac_ProjeBazindaAyri_TekUyari()
    {
        var yukler = Enumerable.Range(1, 8).Select(i => Sandik($"P{i}", 5_800, 2_000, 1_200, 1_000, proje: i <= 4 ? 1 : 2)).ToList();
        var sonuc = Hesapla(yukler, [K40, K40Hc], projeAyri: true);
        Assert.Equal(["K40HC", "K40HC"], sonuc.Araclar.Select(a => a.EkipmanKodu));
        Assert.All(sonuc.Araclar, a => Assert.Single(a.ProjeIdleri));
        Assert.Equal(["En az araç için 40' High Cube kullanıldı: yalnız en sade tiplerle 4 araç gerekirdi, bu düzenle 2 araç."],
            sonuc.Uyarilar.Where(u => u.StartsWith("En az araç", StringComparison.Ordinal)));
    }

    /// <summary>Araç sayısı aynıysa sade tip kalır ve uyarı yazılmaz; kullanıcının seçmediği (yedek) tipe yükseltme yapılmaz.</summary>
    [Fact]
    public void EnAzArac_EsitlikteSadeKalir_YedekTipeYukseltilmez()
    {
        var esit = Hesapla(IstiflenenSandiklar(2), [K40, K40Hc]);
        Assert.Equal("K40", Assert.Single(esit.Araclar).EkipmanKodu);
        Assert.DoesNotContain(esit.Uyarilar, u => u.StartsWith("En az araç", StringComparison.Ordinal));

        // Kullanıcı yalnız standart konteyneri seçti: High Cube yedektir, araç sayısı için kullanılmaz.
        var secili = Hesapla(IstiflenenSandiklar(4), [K40, K40Hc with { Yedek = true }]);
        Assert.Equal(["K40", "K40"], secili.Araclar.Select(a => a.EkipmanKodu));
        Assert.DoesNotContain(secili.Uyarilar, u => u.StartsWith("En az araç", StringComparison.Ordinal));
    }

    /// <summary>Proje bazında ayrı yüklemede bir araçta yalnız bir proje bulunur.</summary>
    [Fact]
    public void ProjeBazindaAyri_AracBasinaTekProje()
    {
        var yukler = new[] { Sandik("A", 1_000, 1_000, 1_000, 100, proje: 1), Sandik("B", 1_000, 1_000, 1_000, 100, proje: 2) };
        Assert.Single(Hesapla(yukler, [K40]).Araclar);
        var ayri = Hesapla(yukler, [K40], projeAyri: true);
        Assert.Equal(2, ayri.Araclar.Count);
        Assert.All(ayri.Araclar, a => Assert.Single(a.ProjeIdleri));
    }

    /// <summary>Mevcut araçlara yerleştirme: sığmayan yükler listelenir ve ek ekipman ihtiyacı önerilir.</summary>
    [Fact]
    public void MevcutAraclar_SigmayanlarVeEkIhtiyac()
    {
        var yukler = Enumerable.Range(1, 14).Select(i => Sandik($"S{i:00}", 1_100, 1_100, 1_000, 300, ust: false)).ToList();
        var sonuc = Hesapla(yukler, [K40, K20], mevcut: [new MevcutArac(20, 1)]);
        Assert.Equal("K20", Assert.Single(sonuc.Araclar).EkipmanKodu);
        Assert.Equal(4, sonuc.Yerlesemeyenler.Count);
        Assert.All(sonuc.Yerlesemeyenler, y => Assert.Equal("Mevcut araçlara sığmadı", y.Neden));
        Assert.Equal(1, Assert.Single(sonuc.EkIhtiyaclar).Adet);
    }

    /// <summary>
    /// Tanımsız güvenlik payı sıfır sayılmaz: o kontrol "Uygun" yazılmaz, teyit edilemediği için gösterilmez; araç sonucu
    /// gösterilen kontrollerden türer. Mevzuat ve tanımsız pay uyarısı yazılmaz (saha isteği, 20 Kasım 2026).
    /// </summary>
    [Fact]
    public void TanimsizPay_KontrolGosterilmez()
    {
        var paysiz = new YerlesimAyarlari(30, null, null, null, null, null);
        var sonuc = Hesapla([Sandik("S1", 2_000, 1_200, 1_000, 500)], [K40], ayar: paysiz);
        var arac = Assert.Single(sonuc.Araclar);
        Assert.DoesNotContain(arac.Kontroller, k => k.Tur == KontrolTuru.Agirlik);
        Assert.DoesNotContain(arac.Kontroller, k => k.Sonuc == KontrolSonucu.TeyitEdilmedi);
        Assert.DoesNotContain(arac.Kontroller, k => k.Tur is KontrolTuru.YasalYukseklik or KontrolTuru.YasalGenislik or KontrolTuru.Mevzuat);
        Assert.Equal(KontrolSonucu.Uygun, arac.Sonuc);
        Assert.DoesNotContain(sonuc.Uyarilar, u => u.Contains("teyit", StringComparison.OrdinalIgnoreCase) || u.Contains("mevzuat", StringComparison.OrdinalIgnoreCase));
        // Paylı ayarda geçen kontrol "Uygun" yazılır.
        Assert.Equal(KontrolSonucu.Uygun, Assert.Single(Hesapla([Sandik("S1", 2_000, 1_200, 1_000, 500)], [K40]).Araclar)
            .Kontroller.Single(k => k.Tur == KontrolTuru.Agirlik).Sonuc);

        var asim = Hesapla([Sandik("S1", 2_000, 1_200, 1_000, 30_000)], [K40]);
        Assert.Contains("teknik kapasite", Assert.Single(asim.Yerlesemeyenler).Neden);
    }

    /// <summary>Son araç küçük alt tiple değiştirilirken o alt tipe girmeyen yük düşmez (araç değiştirilmez).</summary>
    [Fact]
    public void AltTipDegisimi_YukDusurmez()
    {
        var hc40 = Konteyner(45, "HC40", 12_032) with
        {
            TeknikVeri = K40.TeknikVeri with
            {
                Yuzeyler = [new YukYuzeyi("ANA", "Taban", 0, 12_032, 2_352, 0, 2_698, null, null)],
                Acikliklar = [new Aciklik(AciklikTarafi.Arka, null, null, 2_340, 2_585)]
            }
        };
        var yukler = new List<YerlesimYuku> { Sandik("UZUN", 1_000, 1_000, 2_500, 400) };
        yukler.AddRange(Enumerable.Range(1, 3).Select(i => Sandik($"K{i}", 1_000, 1_000, 1_000, 100, ust: false)));
        var sonuc = Hesapla(yukler, [hc40, K20]);
        Assert.Empty(sonuc.Yerlesemeyenler);
        Assert.Equal("HC40", Assert.Single(sonuc.Araclar).EkipmanKodu);
        Assert.Equal(4, sonuc.Araclar[0].Konumlar.Count);
    }

    /// <summary>Tentelide yan açıklık ölçüsü yoksa yandan geçiş teyit edilemez: uygunluk üretilmez, kontrol gösterilmez.</summary>
    [Fact]
    public void Tenteli_YanAciklikYoksaGecisGosterilmez()
    {
        var tenteli = new EkipmanAdayi(70, 1, "TNT", "Tenteli", "Tenteli", "Tenteli", AileIci: true, OzelYukEkipmani: false,
            GirisYonu.Yandan, Duvarli: true, Dingilli: true, Dogrulandi: true,
            new EkipmanTeknikVerisi
            {
                DisOlcu = new Olcu(13_600, 2_550, 4_000),
                TeknikKapasiteKg = 24_000,
                Yuzeyler = [new YukYuzeyi("ANA", "Taban", 0, 13_600, 2_480, 1_200, 2_700, null, null)]
            });
        var arac = Assert.Single(Hesapla([Sandik("S1", 2_000, 1_200, 1_000, 500)], [tenteli]).Araclar);
        Assert.DoesNotContain(arac.Kontroller, k => k.Tur == KontrolTuru.KapidanGecis);
    }

    /// <summary>Lowbed'de sökülemeyen engelle çakışan konum seçilmez; çakışmasız konum yoksa uygunluk üretilmez.</summary>
    [Fact]
    public void Lowbed_EngelleCakismaz()
    {
        Engel Havuzda(int x, int u) => new(EngelTuru.Diger, x, 0, 900, u, 2_550, 300, false, "Kaynak sacı");
        var engelli = Lowbed with { TeknikVeri = Lowbed.TeknikVeri with { Engeller = [Havuzda(3_200, 1_000)] } };
        var konum = Assert.Single(Assert.Single(Hesapla([Trafo("TR-01", 6_000, 3_000, 3_800, 45_000)], [engelli]).Araclar).Konumlar);
        Assert.True(konum.XMm >= 4_200 + Ayar.YerlesimBosluguMm, $"X={konum.XMm}");

        var dolu = Lowbed with { TeknikVeri = Lowbed.TeknikVeri with { Engeller = [Havuzda(3_200, 4_000)] } };
        var sonuc = Hesapla([Trafo("TR-02", 6_000, 3_000, 3_800, 45_000)], [dolu]);
        Assert.Contains("engellerle çakışmayan konum yok", Assert.Single(sonuc.Yerlesemeyenler).Neden);
    }

    /// <summary>Ailede ekipman verisi yoksa özel yük veri eksikliği yüzünden aile dışına çıkmaz.</summary>
    [Fact]
    public void AiledeVeriYoksa_OzelYukAileDisinaCikmaz()
    {
        var yuk = Sandik("OZ-1", 8_000, 2_400, 2_500, 20_000) with { OzelYuk = true, UsteKonabilir = false, AltaKalabilir = false };
        var sonuc = Hesapla([yuk], [Lowbed]);
        Assert.Empty(sonuc.Araclar);
        Assert.Contains("ekipman verisi yok", Assert.Single(sonuc.Yerlesemeyenler).Neden);
    }

    /// <summary>Mevcut araçlarda kullanıcının lowbed'i trafoya ayrılır; normal yükler önce aile araçlarına gider.</summary>
    [Fact]
    public void MevcutAraclar_LowbedTrafoyaAyrilir()
    {
        var sonuc = Hesapla([Sandik("S1", 2_000, 1_200, 1_000, 500), Trafo("TR-01", 6_000, 3_000, 3_800, 45_000)], [K40, Lowbed],
            mevcut: [new MevcutArac(90, 1), new MevcutArac(40, 1)]);
        Assert.Empty(sonuc.Yerlesemeyenler);
        Assert.Equal(["K40", "LB-3"], sonuc.Araclar.Select(a => a.EkipmanKodu));
        var lowbed = sonuc.Araclar.Single(a => a.EkipmanKodu == "LB-3");
        Assert.True(lowbed.AileDisi);
        Assert.Contains("mevcut araç listesindeki LB-3", lowbed.AileDisiGerekce);
        Assert.True(lowbed.ZorunluOzelEkipman);
        Assert.False(sonuc.Araclar.Single(a => a.EkipmanKodu == "K40").ZorunluOzelEkipman);
    }

    /// <summary>Birden çok dingil grubunda grup yükü tek mesnetle hesaplanmaz; kesin sonuç verilmez (kontrol gösterilmez).</summary>
    [Fact]
    public void CokDingilGrubu_KesinSonucVerilmez()
    {
        var iki = Lowbed with
        {
            TeknikVeri = Lowbed.TeknikVeri with
            {
                Dingiller = [new Dingil(12_600, DingilTuru.Tahriksiz, 1, 1_000), new Dingil(15_320, DingilTuru.Tahriksiz, 2, 1_000)]
            }
        };
        var arac = Assert.Single(Hesapla([Trafo("TR-01", 6_000, 3_000, 3_800, 45_000)], [iki]).Araclar);
        Assert.DoesNotContain(arac.Kontroller, k => k.Tur == KontrolTuru.Dingil);
    }

    /// <summary>Sonuç saklama biçimi geri okunduğunda aynıdır.</summary>
    [Fact]
    public void Serilestirici_GeriOkunur()
    {
        var sonuc = Hesapla([Sandik("S1", 2_000, 1_200, 1_000, 500), Trafo("TR-01", 6_000, 3_000, 3_800, 45_000)], [K40, Lowbed]);
        var json = YerlesimSerilestirici.Yaz(sonuc);
        var okunan = YerlesimSerilestirici.Oku(json);
        Assert.NotNull(okunan);
        Assert.Equal(json, YerlesimSerilestirici.Yaz(okunan));
        Assert.Contains("\"yuklemeYontemi\":\"arkadan\"", json);
        Assert.Null(YerlesimSerilestirici.Oku("{bozuk"));
    }
}
