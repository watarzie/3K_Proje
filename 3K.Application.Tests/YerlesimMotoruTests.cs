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
    private static readonly YerlesimAyarlari Ayar = new(30, 50, 0, 0, 0, 50);

    private static EkipmanAdayi Konteyner(int id, string kod, int uzunluk, int kapasite = 26_500, bool ustten = false) => new(
        id, 1, kod, kod, "Konteyner", ustten ? "Open Top" : "Kapalı", AileIci: true, OzelYukEkipmani: false,
        ustten ? GirisYonu.Arkadan | GirisYonu.Ustten : GirisYonu.Arkadan, Duvarli: true, Dingilli: false, Dogrulandi: true,
        new EkipmanTeknikVerisi
        {
            DisOlcu = new Olcu(uzunluk + 160, 2_438, 2_591),
            TeknikKapasiteKg = kapasite,
            Yuzeyler = [new YukYuzeyi("ANA", "Taban", 0, uzunluk, 2_352, 0, 2_393, null, null)],
            Acikliklar = ustten
                ? [new Aciklik(AciklikTarafi.Arka, null, null, 2_340, 2_280), new Aciklik(AciklikTarafi.Ust, 100, uzunluk - 200, 2_200, null)]
                : [new Aciklik(AciklikTarafi.Arka, null, null, 2_340, 2_280)]
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

    private static YerlesimYuku Sandik(string kod, int boy, int en, int yuk, decimal kg, int? proje = 1, decimal? ustYuk = null,
        bool ust = true, bool alt = true) =>
        new(kod, kod, proje, $"P{proje}", null, boy, en, yuk, kg, false, ust, alt, ustYuk ?? kg, false, OzelEkipmanZorunlulugu.Yok, "Sandık");

    private static YerlesimYuku Trafo(string kod, int boy, int en, int yuk, decimal kg) =>
        new(kod, kod, 1, "P1", null, boy, en, yuk, kg, false, false, false, null, true, OzelEkipmanZorunlulugu.Her, "Trafo");

    private static YerlesimSonucu Hesapla(IReadOnlyList<YerlesimYuku> yukler, IReadOnlyList<EkipmanAdayi> adaylar,
        bool projeAyri = false, IReadOnlyList<MevcutArac>? mevcut = null, YerlesimAyarlari? ayar = null) =>
        YerlesimMotoru.Hesapla(new YerlesimGirdisi(yukler, adaylar, ayar ?? Ayar, projeAyri, false, mevcut, null, "Konteyner"));

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

    /// <summary>Senaryo: normal sandıklarda konteyner seçilmişse sistem keyfî olarak açık kasa / lowbed ekleyemez.</summary>
    [Fact]
    public void NormalSandik_AileDisiEkipmanaGitmez()
    {
        var sonuc = Hesapla([Sandik("UZUN", 13_000, 2_000, 1_000, 3_000)], [K40, Lowbed]);
        Assert.Empty(sonuc.Araclar);
        Assert.Contains("Konteyner ailesindeki hiçbir ekipmana sığmıyor", Assert.Single(sonuc.Yerlesemeyenler).Neden);
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
        var tasma = arac.Kontroller.Single(k => k.Tur == KontrolTuru.Tasma);
        Assert.Equal(KontrolSonucu.TeyitEdilmedi, tasma.Sonuc);
        Assert.Contains("sol 1.350 mm", tasma.Mesaj);
        Assert.Equal(KontrolSonucu.TeyitEdilmedi, arac.Kontroller.Single(k => k.Tur == KontrolTuru.EkipmanVerisi).Sonuc);
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

    /// <summary>Tanımsız güvenlik payı ve onaylı mevzuat yokluğu "Teyit edilmedi" üretir; sıfır payla uygunluk yoktur.</summary>
    [Fact]
    public void TanimsizPay_TeyitEdilmedi()
    {
        var paysiz = new YerlesimAyarlari(30, null, null, null, null, null);
        var sonuc = Hesapla([Sandik("S1", 2_000, 1_200, 1_000, 500)], [K40], ayar: paysiz);
        var arac = Assert.Single(sonuc.Araclar);
        Assert.Equal(KontrolSonucu.TeyitEdilmedi, arac.Kontroller.Single(k => k.Tur == KontrolTuru.Agirlik).Sonuc);
        Assert.Contains(sonuc.Uyarilar, u => u.StartsWith("Tanımlanmamış güvenlik payları: yükseklik", StringComparison.Ordinal));
        Assert.Contains(sonuc.Uyarilar, u => u.StartsWith("Onaylı mevzuat kural seti yok", StringComparison.Ordinal));

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

    /// <summary>Tentelide yan açıklık ölçüsü yoksa yandan geçiş teyit edilemez (uygunluk üretilmez).</summary>
    [Fact]
    public void Tenteli_YanAciklikYoksaTeyitEdilmedi()
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
        var gecis = arac.Kontroller.Single(k => k.Tur == KontrolTuru.KapidanGecis);
        Assert.Equal(KontrolSonucu.TeyitEdilmedi, gecis.Sonuc);
        Assert.Contains("Yan açıklık", gecis.Mesaj);
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

    /// <summary>Birden çok dingil grubunda grup yükü tek mesnetle hesaplanmaz; kesin sonuç verilmez.</summary>
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
        Assert.Equal(KontrolSonucu.TeyitEdilmedi, arac.Kontroller.Single(k => k.Tur == KontrolTuru.Dingil).Sonuc);
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
