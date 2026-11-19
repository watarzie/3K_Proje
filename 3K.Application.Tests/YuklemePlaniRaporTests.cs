using _3K.Core.Enums;
using _3K.Core.Models;
using _3K.Infrastructure.Services.YuklemePlanlama;
using _3K.YuklemeMotoru.Cizim;
using _3K.YuklemeMotoru.Ekipman;
using _3K.YuklemeMotoru.Yerlesim;
using Xunit;

namespace _3K.Application.Tests;

/// <summary>
/// Yükleme planı raporları (Simülasyon isterleri 28–29): iki PDF aynı plan/revizyonla üretilir, taslak ibaresi
/// uygunsuzlukta basılır; SVG çizimleri yön işaretlerini ve sandık numaralarını taşır. THREEK_RAPOR_ORNEK_KLASORU
/// verilirse örnek çıktılar (PDF ve sayfa PNG'leri) o klasöre yazılır (görsel kontrol için).
/// </summary>
public sealed class YuklemePlaniRaporTests
{
    private static readonly YerlesimAyarlari Ayar = new(30, 50, 0, 0, 0, 50);

    private static YerlesimYuku Sandik(string kod, int proje, int boy, int en, int yuk, decimal kg, int? lokasyon = 1) =>
        new($"s:{kod}", kod, proje, $"PA{proje}00-35", lokasyon, boy, en, yuk, kg, false, true, true, kg, false, OzelEkipmanZorunlulugu.Yok, "Sandık");

    /// <summary>Motorun gerçek çıktısını ekran modeline çevirir (servisin eşlemesiyle aynı alanlar).</summary>
    internal static YuklemePlaniDetayi OrnekPlan() => OrnekPlan(projeSayisi: 2, projeBasinaYuk: 0, uygun: false);

    /// <summary>
    /// Motorun gerçek çıktısından ekran modeli. <paramref name="uygun"/> ise trafo ve sığmayan sandık yoktur (taslak
    /// ibaresi basılmaz); <paramref name="projeBasinaYuk"/> çok projeli/çok yüklü düzen denemesi içindir.
    /// </summary>
    internal static YuklemePlaniDetayi OrnekPlan(int projeSayisi, int projeBasinaYuk, bool uygun, bool elle = false, bool tekProje = false, int kucukYuk = 0,
        bool lokasyonaGoreAyri = false)
    {
        var yukler = new List<YerlesimYuku>();
        for (var i = 1; i <= 10; i++)
            yukler.Add(Sandik($"PA100-35-{i}", 1, 1_400, 1_100, 1_000, 300 + i * 20, lokasyon: i % 3 == 0 ? null : 1));
        // Küçük sandıklar tek araçta uzun yük listesi içindir (tablo sayfaya sığmazsa düzen bozulmamalı).
        for (var i = 1; i <= kucukYuk; i++)
            yukler.Add(Sandik($"PA100-35-K{i}", 1, 600, 400, 450, 40));
        if (!tekProje)
            for (var i = 1; i <= 4; i++)
                // Lokasyona göre ayrı örnekte ikinci proje başka lokasyondadır (2 = Seymen); araçlar lokasyona bölünür.
                yukler.Add(Sandik($"PA200-35-{i}", 2, 2_200, 1_200, 1_400, 600, lokasyon: lokasyonaGoreAyri ? 2 : 1));
        for (var p = 3; p <= projeSayisi; p++)
            for (var i = 1; i <= projeBasinaYuk; i++)
                yukler.Add(Sandik($"PA{p}00-35-{i}", p, 1_200 + i % 3 * 100, 1_000, 900, 250));
        if (!uygun)
        {
            yukler.Add(new("s:TR", "PA100-35-1T", 1, "PA100-35", 1, 6_000, 3_000, 3_800, 45_000, false, false, false, null, true, OzelEkipmanZorunlulugu.Her, "Trafo"));
            yukler.Add(Sandik("PA200-35-UZUN", 2, 13_000, 1_000, 1_000, 900));
        }
        var girdi = new YerlesimGirdisi(yukler, [YerlesimMotoruTests.K40, YerlesimMotoruTests.Lowbed], Ayar, false, lokasyonaGoreAyri, null,
            "Konteyner");
        var sonuc = YerlesimMotoru.Hesapla(girdi);
        var yukSozluk = yukler.ToDictionary(y => y.Anahtar);
        // Lokasyonu belirsiz satırlar: biri lokasyonsuz, öbürü depo lokasyonu "Belirsiz" (Id 1) ile.
        int? SatirLokasyonu(YerlesimYuku y) => y.LokasyonId == null ? (y.Kod.EndsWith('3') ? null : 1) : 2;
        var geometri = new Dictionary<int, string>
        {
            [40] = EkipmanTeknikVeriSerilestirici.Yaz(YerlesimMotoruTests.K40.TeknikVeri),
            [90] = EkipmanTeknikVeriSerilestirici.Yaz(YerlesimMotoruTests.Lowbed.TeknikVeri)
        };
        var yerlesim = new PlanYerlesimi
        {
            HesapTarihi = new DateTime(2026, 11, 5, 14, 30, 0),
            ElleDuzenlendi = elle,
            LokasyonaGoreAyri = lokasyonaGoreAyri,
            Sonuc = (YerlesimKontrolSonucu)(int)sonuc.Sonuc,
            Araclar = sonuc.Araclar.Select(a => new PlanAraci
            {
                AracNo = a.AracNo, KonfigurasyonId = a.KonfigurasyonId, RevizyonNo = a.RevizyonNo, EkipmanKodu = a.EkipmanKodu, EkipmanAdi = a.EkipmanAdi,
                AileAdi = a.AileAdi, TipAdi = a.TipAdi, AileDisi = a.AileDisi, ZorunluOzelEkipman = a.ZorunluOzelEkipman, AileDisiGerekce = a.AileDisiGerekce,
                ProjeNolari = a.Konumlar.Select(k => yukSozluk[k.YukAnahtari].ProjeNo!).Distinct().ToList(),
                Lokasyonlar = a.Konumlar.Select(k => yukSozluk[k.YukAnahtari].LokasyonId switch { null => YuklemeEtiketleri.BelirsizLokasyon, 2 => "Seymen", _ => "3K" })
                    .Distinct().Order(StringComparer.Ordinal).ToList(),
                Konumlar = a.Konumlar.Select(k => new PlanYukKonumu
                {
                    YukAnahtari = k.YukAnahtari, Kod = yukSozluk[k.YukAnahtari].Kod, ProjeNo = yukSozluk[k.YukAnahtari].ProjeNo, BrutKg = yukSozluk[k.YukAnahtari].BrutKg,
                    XMm = k.XMm, YMm = k.YMm, ZMm = k.ZMm, BoyMm = k.BoyMm, EnMm = k.EnMm, YukseklikMm = k.YukseklikMm, Donduruldu = k.Donduruldu,
                    YuzeyKodu = k.YuzeyKodu, IstifNo = k.IstifNo, Kat = k.Kat, YuklemeSirasi = k.YuklemeSirasi, YuklemeYontemi = (YerlesimGirisYonu)(int)k.YuklemeYontemi
                }).ToList(),
                ToplamKg = a.ToplamKg, KapasiteKg = a.KapasiteKg, TabanDolulukYuzde = a.TabanDolulukYuzde, AgirlikMerkeziXMm = a.AgirlikMerkeziXMm,
                YukluYukseklikMm = a.YukluYukseklikMm, YukluGenislikMm = a.YukluGenislikMm, YukluUzunlukMm = a.YukluUzunlukMm,
                SolTasmaMm = a.SolTasmaMm, SagTasmaMm = a.SagTasmaMm, OnTasmaMm = a.OnTasmaMm, ArkaTasmaMm = a.ArkaTasmaMm,
                Kontroller = a.Kontroller.Select(k => new PlanAracKontrolu { Tur = (YerlesimKontrolTuru)(int)k.Tur, Sonuc = (YerlesimKontrolSonucu)(int)k.Sonuc, Mesaj = k.Mesaj }).ToList(),
                Sonuc = (YerlesimKontrolSonucu)(int)a.Sonuc,
                // Elle örnekte ilk aracın ilk yükü çakışmalı sayılır (yük sorunu tablosu ve kırmızı kutu için).
                YukSorunlari = elle && a.AracNo == 1
                    ? [new PlanYukSorunu { YukAnahtari = a.Konumlar[0].YukAnahtari, Sonuc = YerlesimKontrolSonucu.Uygunsuz, Mesaj = "Çakışma: PA100-35-2" }]
                    : [],
                TeknikVeriJson = geometri[a.KonfigurasyonId]
            }).ToList(),
            Yerlesemeyenler = sonuc.Yerlesemeyenler.Select(y => new PlanYerlesemeyenYuk { YukAnahtari = y.YukAnahtari, Kod = y.Kod, Neden = y.Neden }).ToList(),
            Uyarilar = sonuc.Uyarilar
        };
        var satirlar = yukler.Select((y, i) => new PlanYukSatiri
        {
            SiraNo = i + 1, Kaynak = PlanYukKaynagi.Sandik, SandikId = i + 1, Kod = y.Kod, ProjeNo = y.ProjeNo, LokasyonId = SatirLokasyonu(y),
            Dahil = true, Durum = PlanYukDurumu.Yerlesti
        }).Append(new PlanYukSatiri { SiraNo = 100_000, Kaynak = PlanYukKaynagi.Sandik, SandikId = 100_000, Kod = "PA100-35-99", ProjeNo = "PA100-35", Dahil = true,
            Durum = PlanYukDurumu.EksikVeri, VeriSorunlari = ["Ölçü giriniz."] }).ToList();
        return new YuklemePlaniDetayi
        {
            Id = 1, PlanNo = "YP-2026-000007", Ad = "Wasit sevki", CalismaBicimi = YuklemeCalismaBicimi.IhtiyacaGoreArac, TasimaAilesiId = 1,
            TasimaAilesiAdi = "Konteyner", ProjeKarisimi = YuklemeProjeKarisimi.Karisik,
            LokasyonTercihi = lokasyonaGoreAyri ? YuklemeLokasyonTercihi.LokasyonaGoreAyri : YuklemeLokasyonTercihi.Karisik,
            YuklemeTarihi = new DateTime(2026, 11, 12), RevizyonNo = 4, Kaydeden = "Planlama Kullanıcısı",
            Projeler = Enumerable.Range(1, tekProje ? 1 : Math.Max(2, projeSayisi))
                .Select(p => new PlanProjeBilgisi { ProjeId = p, ProjeNo = $"PA{p}00-35", Musteri = $"MÜŞTERİ {p}" }).ToList(),
            Yukler = satirlar,
            Sayaclar = new PlanYukSayaclari { Secilen = yukler.Count + 1, Hesaplanabilir = yukler.Count, VeriEksik = 1 },
            Yerlesim = yerlesim
        };
    }

    [Fact]
    public void Raporlar_UretilirVeTaslakIbaresiBasilir()
    {
        var plan = OrnekPlan();
        Assert.Equal(KontrolSonucu.Uygunsuz, (KontrolSonucu)(int)plan.Yerlesim!.Sonuc);
        var olusturucu = new YuklemePlaniPdfOlusturucu();
        var talep = olusturucu.AracTalepRaporu(plan, new DateTime(2026, 11, 5, 15, 0, 0));
        var yukleme = olusturucu.YuklemePlaniRaporu(plan, new DateTime(2026, 11, 5, 15, 0, 0));
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(talep, 0, 4));
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(yukleme, 0, 4));

        Assert.True(YuklemePlaniPdfOlusturucu.Taslak(plan));
        Assert.Single(plan.Yerlesim.Araclar, a => a.ZorunluOzelEkipman);

        if (Environment.GetEnvironmentVariable("THREEK_RAPOR_ORNEK_KLASORU") is { Length: > 0 } klasor)
        {
            Directory.CreateDirectory(klasor);
            File.WriteAllBytes(Path.Combine(klasor, "arac-talebi.pdf"), talep);
            File.WriteAllBytes(Path.Combine(klasor, "yukleme-plani.pdf"), yukleme);
        }
    }

    /// <summary>Elle düzenlenen yerleşim: ibare ve yük sorunu tablosuyla rapor düzen hatasız üretilir.</summary>
    [Fact]
    public void ElleDuzenlenmisPlan_RaporUretilir()
    {
        var plan = OrnekPlan(projeSayisi: 2, projeBasinaYuk: 0, uygun: true, elle: true);
        Assert.True(plan.Yerlesim!.ElleDuzenlendi);
        Assert.Single(plan.Yerlesim.Araclar[0].YukSorunlari);
        var yukleme = new YuklemePlaniPdfOlusturucu().YuklemePlaniRaporu(plan, new DateTime(2026, 11, 5, 15, 0, 0));
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(yukleme, 0, 4));
        if (Environment.GetEnvironmentVariable("THREEK_RAPOR_ORNEK_KLASORU") is { Length: > 0 } klasor)
        {
            Directory.CreateDirectory(klasor);
            File.WriteAllBytes(Path.Combine(klasor, "yukleme-plani-elle.pdf"), yukleme);
        }
    }

    /// <summary>Uygunsuzluk ve yerleşemeyen yük yoksa taslak ibaresi basılmaz.</summary>
    [Fact]
    public void UygunPlan_TaslakDegil()
    {
        var plan = OrnekPlan(projeSayisi: 2, projeBasinaYuk: 0, uygun: true);
        Assert.Empty(plan.Yerlesim!.Yerlesemeyenler);
        Assert.DoesNotContain(plan.Yerlesim.Araclar, a => a.Sonuc == YerlesimKontrolSonucu.Uygunsuz);
        Assert.False(YuklemePlaniPdfOlusturucu.Taslak(plan));
    }

    /// <summary>Lokasyonu yok ya da depo lokasyonu "Belirsiz" olan yük belirsizdir (isterler 26).</summary>
    [Fact]
    public void BelirsizLokasyon_TekKural()
    {
        Assert.True(YuklemePlaniKurali.LokasyonBelirsizMi(null));
        Assert.True(YuklemePlaniKurali.LokasyonBelirsizMi((int)DepoLokasyon.Belirsiz));
        Assert.False(YuklemePlaniKurali.LokasyonBelirsizMi((int)DepoLokasyon.UcK));
    }

    /// <summary>Çok projeli ve çok yüklü plan (15 proje, ~300 yük) düzen hatası vermeden üretilir.</summary>
    [Fact]
    public void CokProjeliPlan_DuzenHatasizUretilir()
    {
        var plan = OrnekPlan(projeSayisi: 15, projeBasinaYuk: 22, uygun: false);
        Assert.True(plan.Yerlesim!.Araclar.Count > 3);
        var olusturucu = new YuklemePlaniPdfOlusturucu();
        Assert.NotEmpty(olusturucu.AracTalepRaporu(plan, new DateTime(2026, 11, 5)));
        Assert.NotEmpty(olusturucu.YuklemePlaniRaporu(plan, new DateTime(2026, 11, 5)));
    }

    /// <summary>Madde 8: tek projede yalnız sandık (koli) no, çok projede tam kod; kodu proje no ile başlamayan yük tam kod.</summary>
    [Theory]
    [InlineData("PA600-23-01", "PA600-23", true, "01")]
    [InlineData("PA600-23-01", "PA600-23", false, "PA600-23-01")]
    [InlineData("PA702-5010-2-2", "PA702-5010", true, "2-2")]
    [InlineData("TRAFO-1", "PA600-23", true, "TRAFO-1")]
    [InlineData("TRAFO-1", null, true, "TRAFO-1")]
    [InlineData("PA600-23-", "PA600-23", true, "PA600-23-")]
    public void SandikEtiketi_TekProjedeKoliNo(string kod, string? projeNo, bool tekProje, string beklenen) =>
        Assert.Equal(beklenen, YuklemePlaniKurali.SandikEtiketi(kod, projeNo, tekProje));

    /// <summary>Madde 7: talep formu araç tipine göre adet verir; özel aracın gerekçesi tipin satırındadır.</summary>
    [Fact]
    public void AracTalepFormu_TipBazindaAdet()
    {
        var plan = OrnekPlan();
        var tipler = YuklemePlaniPdfOlusturucu.AracTipleri(plan);
        Assert.Equal(plan.Yerlesim!.Araclar.Count, tipler.Sum(t => t.Adet));
        Assert.Equal(tipler.Count, tipler.Select(t => (t.AileAdi, t.TipAdi)).Distinct().Count());
        var ozel = Assert.Single(tipler, t => t.Gerekceler.Count > 0);
        Assert.Equal(plan.Yerlesim.Araclar.Single(a => a.ZorunluOzelEkipman).TipAdi, ozel.TipAdi);
        // Yerleşemeyen ve verisi eksik yük, talep formunda "araç atanamayan" olarak listelenir.
        var atanamayan = YuklemePlaniPdfOlusturucu.AtanamayanYukler(plan);
        Assert.Contains(atanamayan, x => x.Kod == "PA100-35-99" && x.Neden.Contains("Ölçü giriniz."));
        Assert.Equal(plan.Yerlesim.Yerlesemeyenler.Count + 1, atanamayan.Count);
    }

    /// <summary>Madde 8: çizim ve tablo sandık numarasını yazar; istifte üst kat bütün katların numaralarını taşır.</summary>
    [Fact]
    public void AracYukleri_SandikNumarasiVeIstifEtiketi()
    {
        var tek = OrnekPlan(projeSayisi: 1, projeBasinaYuk: 0, uygun: true, tekProje: true);
        Assert.True(YuklemePlaniPdfOlusturucu.TekProje(tek));
        var yukler = tek.Yerlesim!.Araclar.SelectMany(a => YuklemePlaniPdfOlusturucu.AracYukleri(a, true)).ToList();
        Assert.Equal(10, yukler.Count);
        Assert.All(yukler, x => Assert.DoesNotContain("PA100", x.Etiket, StringComparison.Ordinal));
        Assert.Equal(Enumerable.Range(1, 10).Select(i => i.ToString()).Order(), yukler.Select(x => x.Etiket).Order());
        // İstif: aynı sıradaki katlar; üst katın çizim etiketi alttan üste numaraları "/" ile birleştirir.
        foreach (var istif in yukler.GroupBy(x => x.Sira).Where(g => g.Count() > 1))
        {
            var katlar = istif.OrderBy(x => x.Konum.ZMm).ToList();
            Assert.Equal(string.Join("/", katlar.Select(x => x.Etiket)), katlar[^1].CizimEtiketi);
            Assert.All(katlar.SkipLast(1), x => Assert.Equal(x.Etiket, x.CizimEtiketi));
        }

        var cok = OrnekPlan(projeSayisi: 2, projeBasinaYuk: 0, uygun: true);
        Assert.False(YuklemePlaniPdfOlusturucu.TekProje(cok));
        var cokYukler = cok.Yerlesim!.Araclar.SelectMany(a => YuklemePlaniPdfOlusturucu.AracYukleri(a, false)).ToList();
        Assert.All(cokYukler, x => Assert.Equal(x.Konum.Kod, x.Etiket));
        // Çok projede aynı projenin istifi: ilk kat tam kod, üstteki katlar yalnız sandık no (PA100-35-10/2).
        var cokIstifler = cok.Yerlesim.Araclar.SelectMany(a => YuklemePlaniPdfOlusturucu.AracYukleri(a, false).GroupBy(x => x.Sira))
            .Where(g => g.Count() > 1 && g.Select(x => x.Konum.ProjeNo).Distinct().Count() == 1).ToList();
        Assert.NotEmpty(cokIstifler); // örnek planda motor aynı projeden istif kurar
        var cokIstif = cokIstifler[0].OrderBy(x => x.Konum.ZMm).ToList();
        Assert.Equal(cokIstif[0].Etiket + "/" + string.Join("/", cokIstif.Skip(1).Select(x => YuklemePlaniKurali.SandikEtiketi(x.Konum.Kod, x.Konum.ProjeNo, true))),
            cokIstif[^1].CizimEtiketi);
        Assert.StartsWith("PA100-35-", cokIstif[^1].CizimEtiketi, StringComparison.Ordinal);
        Assert.Single(cokIstif[^1].CizimEtiketi.Split('/'), p => p.StartsWith("PA", StringComparison.Ordinal));
    }

    /// <summary>
    /// Sıra sahadaki adım numarasıdır (saklanan yükleme sırası boşluklu olsa da ardışık); karışık projeli istifte kat,
    /// altındaki katla aynı projedeyse kısa, değilse tam kodla yazılır.
    /// </summary>
    [Fact]
    public void AracYukleri_AdimNoVeKarisikIstif()
    {
        static PlanYukKonumu K(string kod, string proje, int sira, int kat) => new()
        {
            YukAnahtari = "s:" + kod, Kod = kod, ProjeNo = proje, YuklemeSirasi = sira, Kat = kat, ZMm = (kat - 1) * 1000,
            BoyMm = 1000, EnMm = 1000, YukseklikMm = 1000
        };
        var arac = new PlanAraci
        {
            Konumlar = [K("P1-5", "P1", 3, 1), K("P1-1", "P1", 7, 1), K("P2-1", "P2", 7, 2), K("P2-2", "P2", 7, 3), K("P1-2", "P1", 7, 4)]
        };
        var yukler = YuklemePlaniPdfOlusturucu.AracYukleri(arac, tekProje: false);
        Assert.Equal([1, 2, 2, 2, 2], yukler.Select(x => x.Sira));
        Assert.Equal("P1-1/P2-1/2/P1-2", yukler[^1].CizimEtiketi);
        Assert.Equal(["P1-5", "P1-1", "P2-1", "P2-2", "P1-2"], yukler.Select(x => x.Etiket));
    }

    /// <summary>
    /// Mevzuattan önce saklanmış yerleşim gösterilirken "Teyit edilmedi" hiçbir yerde kalmaz: araç ve plan sonucu
    /// "Uygun" olur (uygunsuz kontrol yoktu), teyitsiz kontrol ve yük sorunu ile kaldırılan kuralların uyarıları düşer.
    /// </summary>
    [Theory]
    [InlineData(YerlesimKontrolSonucu.TeyitEdilmedi, YerlesimKontrolSonucu.Uygun)]
    [InlineData(YerlesimKontrolSonucu.Uygun, YerlesimKontrolSonucu.Uygun)]
    [InlineData(YerlesimKontrolSonucu.Uygunsuz, YerlesimKontrolSonucu.Uygunsuz)]
    public void EskiYerlesim_TeyitEdilmediGosterilmez(YerlesimKontrolSonucu saklanan, YerlesimKontrolSonucu gosterilen)
    {
        Assert.Equal(gosterilen, YerlesimGosterimKurali.Sonuc(saklanan));
        Assert.False(YerlesimGosterimKurali.Gosterilir(YerlesimKontrolSonucu.TeyitEdilmedi));
        Assert.True(YerlesimGosterimKurali.Gosterilir(YerlesimKontrolSonucu.Uygunsuz));
        Assert.False(YerlesimGosterimKurali.UyariGosterilir("Onaylı mevzuat kural seti yok; yasal ölçü ve ağırlık kontrolleri teyit edilmedi."));
        Assert.False(YerlesimGosterimKurali.UyariGosterilir("Tanımlanmamış güvenlik payları: yükseklik. İlgili kontroller teyit edilmedi."));
        // Farklı lokasyonların aynı araca atanması artık uyarı değildir (20 Kasım 2026); eski saklı uyarı da gösterilmez.
        // Metin "Araç N: " ile başladığı için içerikle eşleşir; öbür uyarılar etkilenmez.
        Assert.False(YerlesimGosterimKurali.UyariGosterilir("Araç 2: farklı lokasyonlardaki yükler aynı araca atandı; fiziksel yükleme organizasyonu belirlenmelidir."));
        Assert.False(YerlesimGosterimKurali.UyariGosterilir("Araç 12: farklı lokasyonlardaki yükler aynı araca atandı; fiziksel yükleme organizasyonu belirlenmelidir."));
        Assert.True(YerlesimGosterimKurali.UyariGosterilir("Araç 2 (K40): yükseklik sınırı bilinmediği için istif yapılmadı."));
        Assert.True(YerlesimGosterimKurali.UyariGosterilir("1 yük yerleşemedi; plan eksiktir."));
    }

    /// <summary>
    /// Lokasyona göre ayrı plan (saha isteği, 20 Kasım 2026): talep formu araçları lokasyona, sonra tipe göre toplar; her
    /// lokasyonun ara toplamı araç sayısıdır ve toplamları planın araç sayısını verir. Lokasyon sırası ilk aracın sırasıdır.
    /// </summary>
    [Fact]
    public void AracTalepFormu_LokasyonKirilimi()
    {
        static PlanAraci Arac(int no, string tip, params string[] lokasyonlar) =>
            new() { AracNo = no, AileAdi = "Konteyner", TipAdi = tip, Lokasyonlar = lokasyonlar };
        var plan = new YuklemePlaniDetayi
        {
            Yerlesim = new PlanYerlesimi
            {
                LokasyonaGoreAyri = true,
                Araclar =
                [
                    Arac(1, "40' standart (DV)", "3K"), Arac(2, "40' High Cube", "Seymen"), Arac(3, "40' standart (DV)", "3K"),
                    Arac(4, "40' High Cube", "3K"), Arac(5, "40' standart (DV)", YuklemeEtiketleri.BelirsizLokasyon), Arac(6, "40' standart (DV)")
                ]
            }
        };

        var lokasyonlar = YuklemePlaniPdfOlusturucu.LokasyonAracTipleri(plan);
        Assert.Equal(["3K", "Seymen", YuklemeEtiketleri.BelirsizLokasyon], lokasyonlar.Select(l => l.Lokasyon));
        Assert.Equal([3, 1, 2], lokasyonlar.Select(l => l.Adet));
        Assert.Equal([("40' standart (DV)", 2), ("40' High Cube", 1)], lokasyonlar[0].Tipler.Select(t => (t.TipAdi, t.Adet)));
        Assert.Equal([("40' High Cube", 1)], lokasyonlar[1].Tipler.Select(t => (t.TipAdi, t.Adet)));
        // Lokasyonu belirsiz yükler ve yükü olmayan araç aynı "belirsiz" grubundadır.
        Assert.Equal([("40' standart (DV)", 2)], lokasyonlar[2].Tipler.Select(t => (t.TipAdi, t.Adet)));
        Assert.Equal(plan.Yerlesim.Araclar.Count, lokasyonlar.Sum(l => l.Adet));
        Assert.All(lokasyonlar, l => Assert.Equal(l.Adet, l.Tipler.Sum(t => t.Adet)));
        // Elle düzenlemeyle iki lokasyonun yükünü alan araç iki lokasyonla birlikte yazılır.
        Assert.Equal("3K + Seymen", YuklemePlaniPdfOlusturucu.AracLokasyonu(Arac(7, "40' High Cube", "3K", "Seymen")));
        // Lokasyona göre ayrı planda "tek yerden" notu yazılmaz.
        Assert.Null(YuklemePlaniPdfOlusturucu.KarisikLokasyonNotu(plan));
    }

    /// <summary>
    /// Tek yerden (karışık) planda tablo tip bazındadır; yükler birden çok lokasyondaysa lokasyonları ve lokasyon başına araç
    /// sayısı için ne yapılacağını söyleyen tek not yazılır, tek lokasyonda not yoktur.
    /// </summary>
    [Fact]
    public void AracTalepFormu_KarisikPlandaLokasyonNotu()
    {
        static YuklemePlaniDetayi Plan(params string[][] aracLokasyonlari) => new()
        {
            Yerlesim = new PlanYerlesimi
            {
                Araclar = aracLokasyonlari.Select((l, i) => new PlanAraci { AracNo = i + 1, AileAdi = "Konteyner", TipAdi = "40' standart (DV)", Lokasyonlar = l }).ToList()
            }
        };

        var notu = YuklemePlaniPdfOlusturucu.KarisikLokasyonNotu(Plan(["Seymen", "3K"], ["3K"]));
        Assert.NotNull(notu);
        Assert.StartsWith("Yükler 2 lokasyonda: 3K, Seymen.", notu, StringComparison.Ordinal);
        Assert.Contains("\"Lokasyona göre ayrı\"", notu, StringComparison.Ordinal);
        Assert.Null(YuklemePlaniPdfOlusturucu.KarisikLokasyonNotu(Plan(["3K"], ["3K"])));
        Assert.Null(YuklemePlaniPdfOlusturucu.KarisikLokasyonNotu(Plan()));

        // Örnek plan tek yerden hesaplanır ve yükleri iki lokasyondadır (3K ve belirsiz): not yazılır, tablo tip bazında kalır.
        var ornek = OrnekPlan(projeSayisi: 2, projeBasinaYuk: 0, uygun: true);
        Assert.False(ornek.Yerlesim!.LokasyonaGoreAyri);
        Assert.Contains(YuklemeEtiketleri.BelirsizLokasyon, YuklemePlaniPdfOlusturucu.KarisikLokasyonNotu(ornek)!, StringComparison.Ordinal);
    }

    /// <summary>
    /// Lokasyona göre ayrı hesaplanan plan: motor her araca tek lokasyon verir, iki rapor lokasyon sütunu/başlığıyla düzen
    /// hatası vermeden üretilir (örnek çıktı görsel kontrol içindir).
    /// </summary>
    [Fact]
    public void LokasyonaGoreAyriPlan_RaporlarUretilir()
    {
        var plan = OrnekPlan(projeSayisi: 2, projeBasinaYuk: 0, uygun: true, lokasyonaGoreAyri: true);
        Assert.True(plan.Yerlesim!.LokasyonaGoreAyri);
        Assert.All(plan.Yerlesim.Araclar, a => Assert.Single(a.Lokasyonlar));
        var lokasyonlar = YuklemePlaniPdfOlusturucu.LokasyonAracTipleri(plan);
        Assert.Equal(["3K", YuklemeEtiketleri.BelirsizLokasyon, "Seymen"], lokasyonlar.Select(l => l.Lokasyon).Order(StringComparer.Ordinal));
        Assert.Equal(plan.Yerlesim.Araclar.Count, lokasyonlar.Sum(l => l.Adet));

        var olusturucu = new YuklemePlaniPdfOlusturucu();
        var talep = olusturucu.AracTalepRaporu(plan, new DateTime(2026, 11, 20, 15, 0, 0));
        var yukleme = olusturucu.YuklemePlaniRaporu(plan, new DateTime(2026, 11, 20, 15, 0, 0));
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(talep, 0, 4));
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(yukleme, 0, 4));
        if (Environment.GetEnvironmentVariable("THREEK_RAPOR_ORNEK_KLASORU") is { Length: > 0 } klasor)
        {
            Directory.CreateDirectory(klasor);
            File.WriteAllBytes(Path.Combine(klasor, "arac-talebi-lokasyon.pdf"), talep);
            File.WriteAllBytes(Path.Combine(klasor, "yukleme-plani-lokasyon.pdf"), yukleme);
        }
    }

    /// <summary>
    /// Taşma kontrolü teyit gerektirdiği için kontrol listesinde gösterilmez; taşma yönü ve miktarı raporda araç
    /// alanlarından yazılmaya devam eder.
    /// </summary>
    [Fact]
    public void Tasma_KontrolGizliyseRapordaYazilir()
    {
        var arac = new PlanAraci { SolTasmaMm = 1_350, SagTasmaMm = 1_350 };
        Assert.Equal("sol 1.350, sağ 1.350 mm", YuklemePlaniPdfOlusturucu.TasmaMetni(arac));
        Assert.Equal(string.Empty, YuklemePlaniPdfOlusturucu.TasmaMetni(new PlanAraci()));
    }

    /// <summary>Tek araçta sayfaya sığmayan uzun yük listesi düzen hatası vermeden sonraki sayfaya akar.</summary>
    [Fact]
    public void UzunYukListesi_DuzenHatasizUretilir()
    {
        var plan = OrnekPlan(projeSayisi: 1, projeBasinaYuk: 0, uygun: true, tekProje: true, kucukYuk: 90);
        Assert.Contains(plan.Yerlesim!.Araclar, a => a.Konumlar.Count > 45);
        var olusturucu = new YuklemePlaniPdfOlusturucu();
        var yukleme = olusturucu.YuklemePlaniRaporu(plan, new DateTime(2026, 11, 5, 15, 0, 0));
        var talep = olusturucu.AracTalepRaporu(plan, new DateTime(2026, 11, 5, 15, 0, 0));
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(yukleme, 0, 4));
        if (Environment.GetEnvironmentVariable("THREEK_RAPOR_ORNEK_KLASORU") is { Length: > 0 } klasor)
        {
            Directory.CreateDirectory(klasor);
            File.WriteAllBytes(Path.Combine(klasor, "yukleme-plani-tek-proje.pdf"), yukleme);
            File.WriteAllBytes(Path.Combine(klasor, "arac-talebi-tek-proje.pdf"), talep);
        }
    }

    /// <summary>Uzun etiket (çok projeli sandık kodu) kutuya sığacak kadar küçük yazılır.</summary>
    [Fact]
    public void Cizim_UzunEtiketKucukYazilir()
    {
        var veri = YerlesimMotoruTests.K40.TeknikVeri;
        double Boyut(string etiket)
        {
            var svg = YerlesimCizimi.Svg(veri, [new(etiket, 0, 0, 0, 1_200, 1_000, 1_000, "#0e7490", false)], CizimGorunumu.Ustten);
            var i = svg.IndexOf($">{etiket}</text>", StringComparison.Ordinal);
            var bas = svg.LastIndexOf("font-size='", i, StringComparison.Ordinal) + "font-size='".Length;
            return double.Parse(svg[bas..svg.IndexOf('\'', bas)], System.Globalization.CultureInfo.InvariantCulture);
        }
        var kisa = Boyut("7");
        var uzun = Boyut("PA702-5010-12");
        Assert.True(uzun < kisa, $"{uzun} < {kisa}");
        Assert.True(uzun * 13 * 0.6 <= 1_200 * 0.9 + 1, "Uzun etiket kutu boyuna sığmalı.");
    }

    [Fact]
    public void Cizim_YonIsaretleriVeNumaralarVar()
    {
        var veri = YerlesimMotoruTests.K40.TeknikVeri;
        CizimKutusu[] kutular = [new("1", 30, 100, 0, 1_400, 1_100, 1_000, "#0e7490", false), new("2", 30, 100, 1_000, 1_400, 1_100, 800, "#7c3aed", true)];
        foreach (var gorunum in Enum.GetValues<CizimGorunumu>())
        {
            var svg = YerlesimCizimi.Svg(veri, kutular, gorunum);
            Assert.StartsWith("<svg xmlns='http://www.w3.org/2000/svg' viewBox='", svg, StringComparison.Ordinal);
            Assert.Contains(">1</text>", svg);
            Assert.Contains(">2</text>", svg);
            Assert.Contains("#dc2626", svg); // uygunsuz yük kırmızı
            Assert.Contains(gorunum == CizimGorunumu.Arkadan ? ">SOL</text>" : ">ÖN</text>", svg);
        }
        // Yazı kaçışı: kodda < > & bozulmaz.
        Assert.Contains("A&amp;B", YerlesimCizimi.Svg(veri, [new("A&B", 0, 0, 0, 100, 100, 100, "#000000", false)], CizimGorunumu.Ustten));
    }
}
