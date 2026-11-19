using _3K.YuklemeMotoru.Ekipman;
using _3K.YuklemeMotoru.Yerlesim;
using Xunit;
using static _3K.Application.Tests.YerlesimMotoruTests;

namespace _3K.Application.Tests;

/// <summary>
/// Yerleşim motoru iyileştirmeleri: kısa yükün arkasındaki boşluğun kullanılması (uç nokta yerleşimi), dingilli araçta
/// yük bloğunun dingil/king-pin dengesine kaydırılması, ağırlık dağılımı kontrolü ve "kalanı otomatik diz" (elle
/// konulan yük sabit kalır, kalanlar boşluklara ve gerekirse yeni araca yerleşir).
/// </summary>
public sealed class YerlesimIyilestirmeTests
{
    /// <summary>13,6 m açık dorse: 3 dingil (9 t) ve king-pin (15 t); sökülemeyen engel yok.</summary>
    private static readonly EkipmanAdayi Dorse = new(
        70, 1, "DRS", "Açık dorse", "Konteyner", "Açık kasa", AileIci: true, OzelYukEkipmani: false,
        GirisYonu.Yandan, Duvarli: false, Dingilli: true, Dogrulandi: true,
        new EkipmanTeknikVerisi
        {
            DisOlcu = new Olcu(13_600, 2_550, 1_850),
            TeknikKapasiteKg = 30_000,
            KingPin = new KingPin(1_200, 15_000),
            Yuzeyler = [new YukYuzeyi("ANA", "Taban", 0, 13_600, 2_480, 1_250, null, null, null)],
            Dingiller = [new Dingil(9_700, DingilTuru.Tahriksiz, 1, 9_000), new Dingil(11_010, DingilTuru.Tahriksiz, 1, 9_000),
                         new Dingil(12_320, DingilTuru.Tahriksiz, 1, 9_000)]
        });

    private static YerlesimGirdisi Girdi(IReadOnlyList<YerlesimYuku> yukler, IReadOnlyList<EkipmanAdayi> adaylar, YerlesimAyarlari? ayar = null) =>
        new(yukler, adaylar, ayar ?? Ayar, false, false, null, "Konteyner");

    /// <summary>
    /// Sıra modelinde kısa yükün arkası boş kalırdı: 3.000 mm yükün yanına konan 1.400 mm yükün arkasına (30 mm boşlukla) ikinci 1.400 mm
    /// yük de girer; araçta kullanılan boy uzun yükün boyunu aşmaz.
    /// </summary>
    [Fact]
    public void KisaYukunArkasindakiBosluk_Kullanilir()
    {
        var yukler = new[] { Sandik("UZUN", 3_000, 1_100, 1_000, 900), Sandik("K1", 1_400, 1_100, 1_000, 300), Sandik("K2", 1_400, 1_100, 1_000, 300) };
        var arac = Assert.Single(YerlesimMotoru.Hesapla(Girdi(yukler, [K40])).Araclar);
        Assert.Equal(3, arac.Konumlar.Count);
        Assert.All(arac.Konumlar, k => Assert.Equal(1, k.Kat));
        Assert.True(arac.Konumlar.Max(k => k.XMm + k.BoyMm) <= 30 + 3_000, $"Kullanılan boy {arac.Konumlar.Max(k => k.XMm + k.BoyMm)} mm");
        // Hiçbir iki yük çakışmaz ve yerleşim boşluğu korunur.
        foreach (var a in arac.Konumlar)
            foreach (var b in arac.Konumlar.Where(b => b != a))
                Assert.False(a.XMm < b.XMm + b.BoyMm + 30 && b.XMm < a.XMm + a.BoyMm + 30 && a.YMm < b.YMm + b.EnMm + 30 && b.YMm < a.YMm + a.EnMm + 30,
                    $"{a.YukAnahtari} ile {b.YukAnahtari} çakışıyor");
    }

    /// <summary>
    /// Dingilli araçta öne konan ağır yük king-pin'i aşırı yükler; yük bloğu dingil ve king-pin oranları dengelenecek
    /// şekilde arkaya kaydırılır ve iki kontrol de aşım göstermez.
    /// </summary>
    [Fact]
    public void DingilliAracta_AgirYukDengeyeKaydirilir()
    {
        var arac = Assert.Single(YerlesimMotoru.Hesapla(Girdi([Sandik("AGIR", 2_000, 2_000, 1_000, 20_000)], [Dorse])).Araclar);
        var konum = Assert.Single(arac.Konumlar);
        // Dengeli merkez ≈ king-pin + (dingil payı / yük) × açıklık = 1.200 + 12.857/20.000 × 9.810 ≈ 7.507 mm.
        Assert.InRange(konum.XMm + konum.BoyMm / 2, 7_300, 7_700);
        // Aşım yoksa kesin sonuç verilmez (dara ve çekici dağılımı bilinmiyor); kontrol teyitsiz kalır ve gösterilmez.
        Assert.DoesNotContain(arac.Kontroller, k => k.Tur is KontrolTuru.Dingil or KontrolTuru.BaglantiYuku && k.Sonuc == KontrolSonucu.Uygunsuz);
    }

    /// <summary>Ağırlık dağılımı: sınır tanımlı değilse teyit edilmez ve gösterilmez; sınır aşılırsa uygunsuz, içindeyse uygun.</summary>
    [Fact]
    public void AgirlikDagilimi_SinirTanimliysaDegerlendirilir()
    {
        var yukler = new[] { Sandik("A", 1_200, 1_000, 1_000, 2_000) };
        YerlesimAraci Arac(int? sinir) => Assert.Single(YerlesimMotoru.Hesapla(Girdi(yukler, [K40], Ayar with { AgirlikMerkeziSapmaYuzde = sinir })).Araclar);
        AracKontrolu Kontrol(int? sinir) => Arac(sinir).Kontroller.Single(k => k.Tur == KontrolTuru.AgirlikDagilimi);

        Assert.DoesNotContain(Arac(null).Kontroller, k => k.Tur == KontrolTuru.AgirlikDagilimi);
        Assert.Contains("önünde", Kontrol(50).Mesaj);
        // Konteynerde yük öne yaslanır: tek kısa yükün merkezi ortadan ~5,4 m önde (%44) → %10 sınırı aşılır, %50 aşılmaz.
        Assert.Equal(KontrolSonucu.Uygunsuz, Kontrol(10).Sonuc);
        Assert.Equal(KontrolSonucu.Uygun, Kontrol(50).Sonuc);
    }

    /// <summary>
    /// Kalanı otomatik diz: kullanıcının koyduğu yük yerinde kalır, kalanlar aynı araçtaki boşluğa yerleşir; araca
    /// sığmayanlar için yeni araç açılır ve denetimde bekleyen kalmaz.
    /// </summary>
    [Fact]
    public void ElleTamamla_SabitYukKorunurKalanYerlesir()
    {
        var yukler = Enumerable.Range(1, 12).Select(i => Sandik($"S{i:00}", 2_000, 1_100, 1_000, 500, ust: false)).ToList();
        var girdi = Girdi(yukler, [K40]);
        // Yüzey enine 43 mm'den başlar; duvarlı gövdede 30 mm kenar boşluğu: geçerli en sol konum Y = 73.
        var sabit = new ElleKonum("S01", 6_000, 73, 0, false);

        var sonuc = YerlesimMotoru.ElleTamamla(girdi, [new ElleArac(40, [sabit])]);

        Assert.Equal(sabit, sonuc[0].Konumlar.Single(k => k.YukAnahtari == "S01"));
        Assert.True(sonuc[0].Konumlar.Count > 1, "Kalan yükler önce var olan araca yerleşmeli");
        Assert.Equal(12, sonuc.Sum(a => a.Konumlar.Count));
        Assert.True(sonuc.Count >= 2, "K40'a sığmayanlar için yeni araç açılmalı");
        var denetim = ElleYerlesimDenetimi.Denetle(girdi, sonuc);
        Assert.Empty(denetim.Yerlesemeyenler);
        Assert.All(denetim.Araclar, a => Assert.True(!a.YukSorunlari.Any(s => s.Sonuc == KontrolSonucu.Uygunsuz),
            string.Join(" | ", a.YukSorunlari.Select(s => $"{s.YukAnahtari}: {s.Mesaj}")) + " / " + string.Join(", ", a.Konumlar.Select(k => $"{k.YukAnahtari}@{k.XMm},{k.YMm},{k.ZMm}"))));
    }

    /// <summary>
    /// Kalanı otomatik diz sabit istifin üstüne gerçek üst kottan istif kurar; tabanı yüzeyde olmayan (havadaki) sabit
    /// yükün üstüne yük konmaz. Taban boyu iki sabit yükle dolu olduğu için yeni yük istife gider.
    /// </summary>
    [Theory]
    [InlineData(0, "A")]
    [InlineData(500, "C")]
    public void ElleTamamla_SabitIstifUstuneDogruKottanVeDestekliyeIstifler(int aZ, string beklenenAlt)
    {
        var yukler = new[] { Sandik("A", 2_000, 1_100, 1_000, 500), Sandik("C", 2_000, 1_100, 1_000, 500), Sandik("B", 1_000, 1_000, 800, 200) };
        var arac = new ElleArac(20, [new ElleKonum("A", 30, 73, aZ, false), new ElleKonum("C", 30, 1_203, 0, false)]);
        var k20 = Konteyner2000();
        var sonuc = Assert.Single(YerlesimMotoru.ElleTamamla(Girdi(yukler, [k20]), [arac]));

        var b = sonuc.Konumlar.Single(k => k.YukAnahtari == "B");
        var alt = sonuc.Konumlar.Single(k => k.YukAnahtari == beklenenAlt);
        Assert.Equal(alt.ZMm + 1_000, b.ZMm);
        Assert.InRange(b.XMm, alt.XMm, alt.XMm + 2_000 - 1_000);
        Assert.InRange(b.YMm, alt.YMm, alt.YMm + 1_100 - 1_000);
        // Sabit yükler yerinde kalır.
        Assert.Equal(aZ, sonuc.Konumlar.Single(k => k.YukAnahtari == "A").ZMm);
    }

    /// <summary>Tabanı 2.060 mm (iki sabit yükün boyu ve boşluk) olan kısa konteyner: yeni yük ancak istife girer.</summary>
    private static EkipmanAdayi Konteyner2000() => new(
        20, 1, "K2", "K2", "Konteyner", "Kapalı", AileIci: true, OzelYukEkipmani: false, GirisYonu.Arkadan, Duvarli: true, Dingilli: false, Dogrulandi: true,
        new EkipmanTeknikVerisi
        {
            DisOlcu = new Olcu(2_220, 2_438, 2_591),
            TeknikKapasiteKg = 26_500,
            Yuzeyler = [new YukYuzeyi("ANA", "Taban", 0, 2_060, 2_352, 0, 2_393, null, null)],
            Acikliklar = [new Aciklik(AciklikTarafi.Arka, null, null, 2_340, 2_280)]
        });

    /// <summary>Dingil ya da king-pin kapasitelerinden biri bilinmezse dengeleme yapılmaz (yük bilinmeyen tarafa yığılmaz).</summary>
    [Fact]
    public void DingilliAracta_TekKapasiteBilinirseKaydirilmaz()
    {
        var kpsiz = Dorse with { TeknikVeri = Dorse.TeknikVeri with { KingPin = new KingPin(1_200, null) } };
        var arac = Assert.Single(YerlesimMotoru.Hesapla(Girdi([Sandik("AGIR", 2_000, 2_000, 1_000, 20_000)], [kpsiz])).Araclar);
        Assert.Equal(0, Assert.Single(arac.Konumlar).XMm);
    }

    /// <summary>Kalanı otomatik diz araçsız başlarsa (sıfırdan) bütün yükleri otomatik yerleşimle araçlara koyar; trafo lowbed'e gider.</summary>
    [Fact]
    public void ElleTamamla_AracsizBaslayinca_HepsiYerlesir()
    {
        var yukler = new[] { Sandik("S1", 2_000, 1_200, 1_000, 800), Trafo("TR", 6_000, 2_400, 3_000, 40_000) };
        var sonuc = YerlesimMotoru.ElleTamamla(Girdi(yukler, [K40, Lowbed]), []);
        Assert.Equal(2, sonuc.Count);
        Assert.Equal("S1", Assert.Single(sonuc.Single(a => a.KonfigurasyonId == 40).Konumlar).YukAnahtari);
        Assert.Equal("TR", Assert.Single(sonuc.Single(a => a.KonfigurasyonId == 90).Konumlar).YukAnahtari);
    }
}
