using _3K.Application.Features.YuklemePlanlamaIslemleri.Commands;
using _3K.Application.Features.YuklemePlanlamaIslemleri.DTOs;
using _3K.Application.Features.YuklemePlanlamaIslemleri.Validators;
using _3K.YuklemeMotoru.Yerlesim;
using Xunit;
using static _3K.Application.Tests.YerlesimMotoruTests;

namespace _3K.Application.Tests;

/// <summary>
/// Elle yerleşim denetimi (Simülasyon isterleri 20–22, kabul 602, 607, 608): elle konumlar kabul edilir ama kontroller
/// atlanmaz; sorun yük kodu, yön ve miktarla yazılır.
/// </summary>
public sealed class ElleYerlesimTests
{
    private static YerlesimGirdisi Girdi(IReadOnlyList<YerlesimYuku> yukler, bool projeAyri = false) =>
        new(yukler, [K40, Lowbed], Ayar, projeAyri, false, null, null, "Konteyner");

    private static YerlesimSonucu Denetle(IReadOnlyList<YerlesimYuku> yukler, params ElleArac[] araclar) =>
        ElleYerlesimDenetimi.Denetle(Girdi(yukler), araclar);

    private static ElleKonum K(string anahtar, int x, int y, int z = 0, bool dondu = false) => new(anahtar, x, y, z, dondu);

    private static IEnumerable<string> Sorunlar(YerlesimSonucu s, string anahtar) =>
        s.Araclar.SelectMany(a => a.YukSorunlari).Where(x => x.YukAnahtari == anahtar).Select(x => x.Mesaj);

    /// <summary>Otomatik yerleşimin konumları elle verildiğinde sorun çıkmaz ve konum korunur (ortalanmaz).</summary>
    [Fact]
    public void OtomatikKonumlar_ElleDenetimdeSorunsuz()
    {
        var yukler = new[] { Sandik("A", 2_000, 2_000, 1_000, 400), Sandik("B", 2_000, 2_000, 1_000, 300), Sandik("C", 1_200, 1_000, 900, 200) };
        var oto = YerlesimMotoru.Hesapla(Girdi(yukler));
        var elle = oto.Araclar.Select(a => new ElleArac(a.KonfigurasyonId, a.Konumlar.Select(k => K(k.YukAnahtari, k.XMm, k.YMm, k.ZMm, k.Donduruldu)).ToList())).ToArray();
        var sonuc = ElleYerlesimDenetimi.Denetle(Girdi(yukler), elle);
        Assert.True(sonuc.ElleDuzenlendi);
        Assert.Empty(sonuc.Araclar.SelectMany(a => a.YukSorunlari));
        Assert.Equal(oto.Araclar[0].Konumlar.Select(k => (k.YukAnahtari, k.XMm, k.YMm, k.ZMm, k.Kat)).Order(),
            sonuc.Araclar[0].Konumlar.Select(k => (k.YukAnahtari, k.XMm, k.YMm, k.ZMm, k.Kat)).Order());
        Assert.NotEqual(KontrolSonucu.Uygunsuz, sonuc.Sonuc);
    }

    /// <summary>Çakışan yükler ilgili kodla bildirilir; boşluk payının altındaki yakınlık teyit ister.</summary>
    [Fact]
    public void Cakisma_IlgiliKodlarlaBildirilir()
    {
        var yukler = new[] { Sandik("A", 1_000, 1_000, 1_000, 100), Sandik("B", 1_000, 1_000, 1_000, 100), Sandik("C", 1_000, 1_000, 1_000, 100) };
        var sonuc = Denetle(yukler, new ElleArac(40, [K("A", 100, 100), K("B", 600, 100), K("C", 1_610, 100)]));
        Assert.Contains("Çakışma: B", Sorunlar(sonuc, "A"));
        Assert.Contains(Sorunlar(sonuc, "C"), m => m.StartsWith("Yerleşim boşluğu (30 mm) altında: B", StringComparison.Ordinal));
        Assert.Equal(KontrolSonucu.Uygunsuz, sonuc.Araclar[0].Sonuc);
    }

    /// <summary>Havada duran yük ve alt yükü başka yere taşınan üstteki yük desteksiz kalır.</summary>
    [Fact]
    public void AltYukTasininca_UsttekiDesteksiz()
    {
        var yukler = new[] { Sandik("ALT", 1_200, 1_200, 1_000, 500), Sandik("UST", 1_200, 1_200, 1_000, 300) };
        var istif = Denetle(yukler, new ElleArac(40, [K("ALT", 100, 100), K("UST", 100, 100, 1_000)]));
        Assert.Empty(istif.Araclar[0].YukSorunlari);
        Assert.Equal(2, istif.Araclar[0].Konumlar.Single(k => k.YukAnahtari == "UST").Kat);

        var tasindi = Denetle(yukler, new ElleArac(40, [K("ALT", 5_000, 100), K("UST", 100, 100, 1_000)]));
        Assert.Contains(Sorunlar(tasindi, "UST"), m => m.StartsWith("Desteksiz", StringComparison.Ordinal));

        var kisim = Denetle(yukler, new ElleArac(40, [K("ALT", 100, 100), K("UST", 600, 100, 1_000)]));
        Assert.Contains(Sorunlar(kisim, "UST"), m => m.StartsWith("Yetersiz destek", StringComparison.Ordinal));
    }

    /// <summary>Kabul 607: trafo elle uygunsuz araca taşınırsa zorunlu ekipman ihlali gösterilir.</summary>
    [Fact]
    public void Trafo_KonteynereTasininca_YanlisEkipman()
    {
        var trafo = Trafo("TR", 2_000, 2_000, 2_000, 9_000);
        var sonuc = Denetle([trafo], new ElleArac(40, [K("TR", 100, 100)]));
        Assert.Contains(Sorunlar(sonuc, "TR"), m => m.StartsWith("Yanlış ekipman: TR trafo", StringComparison.Ordinal));

        var dogru = Denetle([trafo], new ElleArac(90, [K("TR", 4_000, 275, 900)]));
        Assert.DoesNotContain(Sorunlar(dogru, "TR"), m => m.StartsWith("Yanlış ekipman", StringComparison.Ordinal));
        Assert.True(dogru.Araclar[0].ZorunluOzelEkipman);
    }

    /// <summary>Kasa sınırı aşımı yön ve miktarla; döndürme izni olmadan döndürme reddedilir.</summary>
    [Fact]
    public void KasaSiniri_VeDondurmeIzni()
    {
        var yukler = new[] { Sandik("A", 2_000, 1_000, 1_000, 100) };
        var tasan = Denetle(yukler, new ElleArac(40, [K("A", 11_000, 100)]));
        Assert.Contains("Kasa sınırı aşımı: arka 968 mm", Sorunlar(tasan, "A"));

        var dondu = Denetle(yukler, new ElleArac(40, [K("A", 100, 100, dondu: true)]));
        Assert.Contains("Tabanda 90° döndürme izni yok", Sorunlar(dondu, "A"));
        Assert.Equal((1_000, 2_000), (dondu.Araclar[0].Konumlar[0].BoyMm, dondu.Araclar[0].Konumlar[0].EnMm));
    }

    /// <summary>Kabul 602: elle kurulan istifte üst yük sınırı aşılırsa uyarı oluşur (otomatik çözüm bunu kurmaz).</summary>
    [Fact]
    public void UstYukAsimi_ElleUyari()
    {
        var yukler = new[] { Sandik("ALT", 1_200, 1_200, 1_000, 800, ustYuk: 500), Sandik("UST", 1_200, 1_200, 1_000, 600) };
        var sonuc = Denetle(yukler, new ElleArac(40, [K("ALT", 100, 100), K("UST", 100, 100, 1_000)]));
        Assert.Contains("Üst yük sınırı aşımı: üstündeki 600 kg, sınır 500 kg", Sorunlar(sonuc, "ALT"));
    }

    /// <summary>Kapalı araçta kapıdan geçmeyen istif içeride kurulamaz.</summary>
    [Fact]
    public void KapidanGecmeyenIstif_Uygunsuz()
    {
        var yukler = new[] { Sandik("ALT", 2_000, 2_000, 1_150, 400), Sandik("UST", 2_000, 2_000, 1_150, 300) };
        var sonuc = Denetle(yukler, new ElleArac(40, [K("ALT", 100, 100), K("UST", 100, 100, 1_150)]));
        Assert.Contains(Sorunlar(sonuc, "UST"), m => m.StartsWith("Girişten geçemiyor: istif grubu", StringComparison.Ordinal));
        // Araç düzeyindeki kapı kontrolü yük sorunuyla birleşir (raporda "Uygun" basılmaz); yöntem bileşik bayrak değil "Yok".
        var kapi = sonuc.Araclar[0].Kontroller.Single(k => k.Tur == KontrolTuru.KapidanGecis);
        Assert.Equal(KontrolSonucu.Uygunsuz, kapi.Sonuc);
        Assert.StartsWith("Elle yerleşimde 2 yükte sorun (ALT, UST): Girişten geçemiyor", kapi.Mesaj, StringComparison.Ordinal);
        Assert.All(sonuc.Araclar[0].Konumlar, k => Assert.Equal(GirisYonu.Yok, k.YuklemeYontemi));

        // Saklanan JSON'da "Yok" yöntem ve yük sorunu türü korunur (geri okunamayan yerleşim sessizce kaybolmasın).
        var okunan = YerlesimSerilestirici.Oku(YerlesimSerilestirici.Yaz(sonuc))!;
        Assert.True(okunan.ElleDuzenlendi);
        Assert.All(okunan.Araclar[0].Konumlar, k => Assert.Equal(GirisYonu.Yok, k.YuklemeYontemi));
        Assert.Equal(sonuc.Araclar[0].YukSorunlari, okunan.Araclar[0].YukSorunlari);
    }

    /// <summary>Havadaki yük kendi istifinin tabanı sayılsa da destek ve iç yükseklik kontrolü yük sorunuyla uygunsuzdur.</summary>
    [Fact]
    public void HavadakiYuk_DestekVeIcYukseklikKontroluUygunsuz()
    {
        var sonuc = Denetle([Sandik("A", 1_000, 1_000, 1_000, 100)], new ElleArac(40, [K("A", 100, 100, 1_500)]));
        var kontroller = sonuc.Araclar[0].Kontroller;
        Assert.Contains(Sorunlar(sonuc, "A"), m => m.StartsWith("Yükseklik aşımı: üst kot 2.500 mm", StringComparison.Ordinal));
        Assert.Equal(KontrolSonucu.Uygunsuz, kontroller.Single(k => k.Tur == KontrolTuru.Destek).Sonuc);
        Assert.Equal(KontrolSonucu.Uygunsuz, kontroller.Single(k => k.Tur == KontrolTuru.IcYukseklik).Sonuc);
    }

    /// <summary>İstif izni: altta kalamayan yükün üstüne ve üste konamayan yük başka yükün üstüne konmaz.</summary>
    [Fact]
    public void IstifIzni_Ihlali()
    {
        var yukler = new[] { Sandik("ALT", 1_200, 1_200, 1_000, 500, alt: false), Sandik("UST", 1_000, 1_000, 800, 100, ust: false) };
        var sonuc = Denetle(yukler, new ElleArac(40, [K("ALT", 100, 100), K("UST", 200, 200, 1_000)]));
        Assert.Contains("İstif izni ihlali: bu yükün üstüne yük konamaz", Sorunlar(sonuc, "ALT"));
        Assert.Contains("İstif izni ihlali: bu yük başka yükün üstüne konamaz", Sorunlar(sonuc, "UST"));
    }

    /// <summary>Bir yükün üstüne ikinci yük oturmaz (köprüleme/yan yana üst yük yok); neden açıkça yazılır.</summary>
    [Fact]
    public void AyniAltYukeIkinciUstYuk_Reddedilir()
    {
        var yukler = new[] { Sandik("ALT", 2_000, 2_000, 500, 500), Sandik("U1", 900, 900, 500, 50), Sandik("U2", 900, 900, 500, 50) };
        var sonuc = Denetle(yukler, new ElleArac(40, [K("ALT", 100, 100), K("U1", 150, 150, 500), K("U2", 1_150, 1_150, 500)]));
        Assert.Empty(Sorunlar(sonuc, "U1"));
        Assert.Contains("Yetersiz destek: ALT üstünde zaten başka yük var; bir yükün üstüne tek yük oturur", Sorunlar(sonuc, "U2"));
    }

    /// <summary>Açık ekipmanda yüzeyden taşma teyit ister; ağırlık merkezi yüzey dışındaysa kısmi oturma uygunsuzdur.</summary>
    [Fact]
    public void AcikEkipman_KismiOturma()
    {
        var yukler = new[] { Sandik("A", 2_000, 1_000, 1_000, 100) };
        var tasan = Denetle(yukler, new ElleArac(90, [K("A", 10_500, 100, 900)]));
        Assert.Contains(tasan.Araclar[0].YukSorunlari, s => s.Sonuc == KontrolSonucu.TeyitEdilmedi && s.Mesaj == "Yükleme yüzeyinden taşıyor: arka 300 mm");
        var devrilir = Denetle(yukler, new ElleArac(90, [K("A", 11_500, 100, 900)]));
        Assert.Contains(devrilir.Araclar[0].YukSorunlari, s => s.Sonuc == KontrolSonucu.Uygunsuz && s.Mesaj.StartsWith("Kısmi oturma: ağırlık merkezi", StringComparison.Ordinal));
        Assert.Equal(KontrolSonucu.Uygunsuz, devrilir.Araclar[0].Kontroller.Single(k => k.Tur == KontrolTuru.Tasma).Sonuc);
    }

    /// <summary>Bekleyenler listesindeki yük plan eksikliğidir; kabul 608: aynı tipten boş araç eklenebilir.</summary>
    [Fact]
    public void BekleyenVeBosArac()
    {
        var yukler = new[] { Sandik("A", 1_000, 1_000, 1_000, 100), Sandik("B", 1_000, 1_000, 1_000, 100) };
        var sonuc = Denetle(yukler, new ElleArac(40, [K("A", 100, 100)]), new ElleArac(40, []));
        Assert.Equal(2, sonuc.Araclar.Count);
        Assert.Empty(sonuc.Araclar[1].Konumlar);
        Assert.Equal("B", Assert.Single(sonuc.Yerlesemeyenler).YukAnahtari);
        Assert.Equal(KontrolSonucu.Uygunsuz, sonuc.Sonuc);
        Assert.Contains(sonuc.Uyarilar, u => u.StartsWith("1 yük bekleyenler listesinde", StringComparison.Ordinal));
    }

    /// <summary>Proje bazında ayrı planda elle karıştırılan araç organizasyon kontrolünde uygunsuzdur.</summary>
    [Fact]
    public void ProjeAyri_KaristirmaUygunsuz()
    {
        var yukler = new[] { Sandik("A", 1_000, 1_000, 1_000, 100, proje: 1), Sandik("B", 1_000, 1_000, 1_000, 100, proje: 2) };
        var sonuc = ElleYerlesimDenetimi.Denetle(Girdi(yukler, projeAyri: true), [new ElleArac(40, [K("A", 100, 100), K("B", 1_500, 100)])]);
        Assert.Equal(KontrolSonucu.Uygunsuz, sonuc.Araclar[0].Kontroller.Single(k => k.Tur == KontrolTuru.Organizasyon).Sonuc);
    }
}

/// <summary>Elle yerleşim isteklerinin doğrulaması: null eleman 400 (500 değil), koordinat ve revizyon sınırları.</summary>
public sealed class ElleYerlesimValidatorTests
{
    private static YuklemePlaniElleKaydetCommand Komut(params ElleAracDto[] araclar) =>
        new() { PlanId = 1, BeklenenRevizyonNo = 2, Araclar = [.. araclar] };

    [Fact]
    public void GecerliIstek_VeBosArac()
    {
        var v = new YuklemePlaniElleKaydetCommandValidator();
        Assert.True(v.Validate(Komut(new ElleAracDto(40, [new ElleKonumDto("s:1", 0, -100, 0, false)]), new ElleAracDto(40, null))).IsValid);
    }

    [Fact]
    public void NullEleman_VeSinirDisi_Reddedilir()
    {
        var v = new YuklemePlaniElleKaydetCommandValidator();
        Assert.False(v.Validate(Komut((ElleAracDto)null!)).IsValid);
        Assert.False(v.Validate(Komut(new ElleAracDto(40, [null!]))).IsValid);
        Assert.False(v.Validate(Komut(new ElleAracDto(0, []))).IsValid);
        Assert.False(v.Validate(Komut(new ElleAracDto(40, [new ElleKonumDto("s:1", 100_001, 0, 0, false)]))).IsValid);
        Assert.False(v.Validate(Komut(new ElleAracDto(40, [new ElleKonumDto("s:1", 0, 0, -1, false)]))).IsValid);
        Assert.False(v.Validate(Komut(new ElleAracDto(40, [new ElleKonumDto("", 0, 0, 0, false)]))).IsValid);
        Assert.False(v.Validate(new YuklemePlaniElleKaydetCommand { PlanId = 1, BeklenenRevizyonNo = 0 }).IsValid);
        var fazla = Enumerable.Range(0, 201).Select(_ => new ElleAracDto(40, [])).ToArray();
        Assert.False(new YuklemePlaniElleDenetleQueryValidator().Validate(new YuklemePlaniElleDenetleQuery { PlanId = 1, Araclar = [.. fazla] }).IsValid);
    }
}
