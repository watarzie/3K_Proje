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
/// uygunsuzlukta basılır; SVG çizimleri yön işaretlerini, sandık numaralarını ve ağırlık merkezi işaretini taşır.
/// Yükleme planı ayrıntılı düzendedir (20 Kasım 2026): özet sayıları, araç ölçüleri, kullanım, yüklü toplam, ağırlık
/// merkezi ve sandık renkleri saf hesaplardır ve burada sabitlenir. THREEK_RAPOR_ORNEK_KLASORU verilirse örnek çıktılar
/// (PDF) o klasöre yazılır (görsel kontrol için).
/// </summary>
public sealed class YuklemePlaniRaporTests
{
    private static readonly YerlesimAyarlari Ayar = new(30, 50, 0, 0, 0, 50);

    private static YerlesimYuku Sandik(string kod, int proje, int boy, int en, int yuk, decimal kg, int? lokasyon = 1, bool usteKonabilir = true) =>
        new($"s:{kod}", kod, proje, $"PA{proje}00-35", lokasyon, boy, en, yuk, kg, false, usteKonabilir, true, kg, false, OzelEkipmanZorunlulugu.Yok, "Sandık");

    /// <summary>Motorun gerçek çıktısını ekran modeline çevirir (servisin eşlemesiyle aynı alanlar).</summary>
    internal static YuklemePlaniDetayi OrnekPlan() => OrnekPlan(projeSayisi: 2, projeBasinaYuk: 0, uygun: false);

    /// <summary>
    /// Motorun gerçek çıktısından ekran modeli. <paramref name="uygun"/> ise trafo ve sığmayan sandık yoktur (taslak
    /// ibaresi basılmaz); <paramref name="projeBasinaYuk"/> çok projeli/çok yüklü düzen denemesi içindir.
    /// </summary>
    internal static YuklemePlaniDetayi OrnekPlan(int projeSayisi, int projeBasinaYuk, bool uygun, bool elle = false, bool tekProje = false, int kucukYuk = 0,
        bool lokasyonaGoreAyri = false, int yukSorunu = 1, int kaynakFarki = 0)
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
                // Bu sandıkların üstüne yük konmaz (yükseklikleri yüzünden zaten istiflenemezler; yerleşim değişmez).
                yukler.Add(Sandik($"PA200-35-{i}", 2, 2_200, 1_200, 1_400, 600, lokasyon: lokasyonaGoreAyri ? 2 : 1, usteKonabilir: false));
        for (var p = 3; p <= projeSayisi; p++)
            for (var i = 1; i <= projeBasinaYuk; i++)
                yukler.Add(Sandik($"PA{p}00-35-{i}", p, 1_200 + i % 3 * 100, 1_000, 900, 250));
        if (!uygun)
        {
            yukler.Add(new("s:TR", "PA100-35-1T", 1, "PA100-35", 1, 6_000, 3_000, 3_800, 45_000, false, false, false, null, true, OzelEkipmanZorunlulugu.Her, "Trafo"));
            yukler.Add(Sandik("PA200-35-UZUN", 2, 13_000, 1_000, 1_000, 900));
        }
        // Anahtarlar servisteki biçimdedir ("s:{sandıkId}"); plan yük satırı konuma bu anahtarla bağlanır.
        yukler = yukler.Select((y, i) => y with { Anahtar = YuklemePlaniKurali.YukAnahtari(PlanYukKaynagi.Sandik, i + 1, null, 1) }).ToList();
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
                // Elle örnekte ilk aracın ilk yükü (istenirse ilk birkaç yükü) çakışmalı sayılır (yük sorunu satırı ve kırmızı kutu için).
                YukSorunlari = elle && a.AracNo == 1
                    ? a.Konumlar.Take(yukSorunu)
                        .Select(k => new PlanYukSorunu { YukAnahtari = k.YukAnahtari, Sonuc = YerlesimKontrolSonucu.Uygunsuz, Mesaj = "Çakışma: PA100-35-2" }).ToList()
                    : [],
                TeknikVeriJson = geometri[a.KonfigurasyonId]
            }).ToList(),
            Yerlesemeyenler = sonuc.Yerlesemeyenler.Select(y => new PlanYerlesemeyenYuk { YukAnahtari = y.YukAnahtari, Kod = y.Kod, Neden = y.Neden }).ToList(),
            Uyarilar = sonuc.Uyarilar
        };
        var satirlar = yukler.Select((y, i) => new PlanYukSatiri
        {
            SiraNo = i + 1, Kaynak = PlanYukKaynagi.Sandik, SandikId = i + 1, BirimNo = 1, BirimSayisi = 1, Kod = y.Kod, ProjeNo = y.ProjeNo,
            // Sandık adı (sandık kaydının "Ad" alanı); her üçüncü sandığın adı yok (raporda boş kalır).
            Aciklama = i % 3 == 2 ? null : OrnekSandikAdlari[i % OrnekSandikAdlari.Length],
            LokasyonId = SatirLokasyonu(y), BoyMm = y.BoyMm, EnMm = y.EnMm, YukseklikMm = y.YukseklikMm, BrutKg = y.BrutKg,
            DondurulebilirMi = y.Dondurulebilir, UsteKonabilirMi = y.UsteKonabilir, AltaKalabilirMi = y.AltaKalabilir, UstYukSiniriKg = y.UstYukSiniriKg,
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
            KaynakFarklari = Enumerable.Range(1, kaynakFarki)
                .Select(i => new PlanKaynakFarki { Tur = PlanKaynakFarkTuru.OlcuDegisti, SandikId = i, Kod = $"PA100-35-{i}", Onceki = "1.400", Guncel = "1.450" }).ToList(),
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

    /// <summary>
    /// Bütün ibareler (taslak, elle düzenlendi, kaynak farkı) ve çok sayıda uyarı satırı birlikteyken araç sayfası düzen
    /// hatası vermez; yükü olmayan (elle eklenmiş boş) araç da çizimleri ve boş tablosuyla raporlanır.
    /// </summary>
    [Fact]
    public void CokUyariliVeBosArac_DuzenHatasizUretilir()
    {
        var plan = OrnekPlan(projeSayisi: 2, projeBasinaYuk: 0, uygun: false, elle: true, yukSorunu: 9, kaynakFarki: 3);
        Assert.Equal(9, plan.Yerlesim!.Araclar[0].YukSorunlari.Count);
        Assert.Equal(3, plan.KaynakFarklari.Count);
        Assert.True(YuklemePlaniPdfOlusturucu.Taslak(plan));
        var olusturucu = new YuklemePlaniPdfOlusturucu();
        var yukleme = olusturucu.YuklemePlaniRaporu(plan, new DateTime(2026, 11, 20, 15, 0, 0));
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(yukleme, 0, 4));

        var bos = new YuklemePlaniDetayi
        {
            PlanNo = "YP-2026-000008", Ad = "Boş araç", TasimaAilesiAdi = "Konteyner", RevizyonNo = 1, Projeler = plan.Projeler,
            Yerlesim = new PlanYerlesimi
            {
                ElleDuzenlendi = true,
                Araclar =
                [
                    new PlanAraci
                    {
                        AracNo = 1, EkipmanKodu = "K40", TipAdi = "Kapalı", AileAdi = "Konteyner", KapasiteKg = 26_500,
                        YukluUzunlukMm = 12_192, YukluGenislikMm = 2_438, YukluYukseklikMm = 2_591,
                        TeknikVeriJson = EkipmanTeknikVeriSerilestirici.Yaz(YerlesimMotoruTests.K40.TeknikVeri)
                    }
                ]
            }
        };
        Assert.Null(YuklemePlaniRaporHesabi.AgirlikMerkezi(bos.Yerlesim.Araclar[0], TeknikVeri(bos.Yerlesim.Araclar[0])));
        var bosRapor = olusturucu.YuklemePlaniRaporu(bos, new DateTime(2026, 11, 20, 15, 0, 0));
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bosRapor, 0, 4));
        if (Environment.GetEnvironmentVariable("THREEK_RAPOR_ORNEK_KLASORU") is { Length: > 0 } klasor)
        {
            Directory.CreateDirectory(klasor);
            File.WriteAllBytes(Path.Combine(klasor, "yukleme-plani-cok-uyari.pdf"), yukleme);
            File.WriteAllBytes(Path.Combine(klasor, "yukleme-plani-bos-arac.pdf"), bosRapor);
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
        var yukleme = olusturucu.YuklemePlaniRaporu(plan, new DateTime(2026, 11, 5));
        Assert.NotEmpty(yukleme);
        if (Environment.GetEnvironmentVariable("THREEK_RAPOR_ORNEK_KLASORU") is { Length: > 0 } klasor)
        {
            Directory.CreateDirectory(klasor);
            File.WriteAllBytes(Path.Combine(klasor, "yukleme-plani-cok-proje.pdf"), yukleme);
        }
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
            // Üstten ve 3B bakışta alt katın üst yüzü üsttekinin altında kalır: numarası üsttekinin üstüne yazılmaz
            // (raporda üst kat birleşik etiketi taşır). Yandan ve arkadan iki kat da görünür.
            if (gorunum is CizimGorunumu.Ustten or CizimGorunumu.Izometrik)
                Assert.DoesNotContain(">1</text>", svg);
            else
                Assert.Contains(">1</text>", svg);
            Assert.Contains(">2</text>", svg);
            Assert.DoesNotContain(YerlesimCizimi.AgirlikMerkeziKimligi, svg); // işaret yalnız ağırlık merkezi verilince çizilir
            Assert.Contains("#dc2626", svg); // uygunsuz yük kırmızı
            Assert.Contains(gorunum == CizimGorunumu.Arkadan ? ">SOL</text>" : ">ÖN</text>", svg);
        }
        // Yazı kaçışı: kodda < > & bozulmaz.
        Assert.Contains("A&amp;B", YerlesimCizimi.Svg(veri, [new("A&B", 0, 0, 0, 100, 100, 100, "#000000", false)], CizimGorunumu.Ustten));
    }

    // ===== Ayrıntılı düzen (20 Kasım 2026): hesaplar, renkler, çizim işaretleri =====

    private static EkipmanTeknikVerisi TeknikVeri(PlanAraci a) => EkipmanTeknikVeriSerilestirici.Oku(a.TeknikVeriJson)!;

    private static PlanYukKonumu Konum(string anahtar, decimal kg, int x, int y, int z, int boy, int en, int yukseklik, int sira = 1, int kat = 1) => new()
    {
        YukAnahtari = anahtar, Kod = anahtar, BrutKg = kg, XMm = x, YMm = y, ZMm = z, BoyMm = boy, EnMm = en, YukseklikMm = yukseklik,
        YuklemeSirasi = sira, Kat = kat
    };

    /// <summary>
    /// Ağırlık merkezi ağırlıkla tartılı ortalamadır (her yük kendi kutusunun ortasında). Boyuna değer motorun araçta
    /// sakladığı değerle aynı hesaptan gelir: raporda ekrandakinden farklı bir X yazılmaz.
    /// </summary>
    [Fact]
    public void AgirlikMerkezi_TartiliOrtalamaVeMotorlaAyniX()
    {
        var nokta = AgirlikMerkeziHesabi.Hesapla(
            [new AgirlikliKutu(100, 0, 0, 0, 1_000, 1_000, 1_000), new AgirlikliKutu(300, 2_000, 500, 0, 1_000, 1_000, 2_000)]);
        Assert.Equal(new AgirlikMerkeziNoktasi(2_000, 875, 875), nokta);
        Assert.Null(AgirlikMerkeziHesabi.Hesapla([]));
        Assert.Null(AgirlikMerkeziHesabi.Hesapla([new AgirlikliKutu(0, 0, 0, 0, 1_000, 1_000, 1_000)]));

        foreach (var plan in (YuklemePlaniDetayi[])[OrnekPlan(), OrnekPlan(1, 0, uygun: true, tekProje: true, kucukYuk: 90)])
        {
            foreach (var a in plan.Yerlesim!.Araclar)
            {
                var hesap = AgirlikMerkeziHesabi.Hesapla(a.Konumlar.Select(k =>
                    new AgirlikliKutu(k.BrutKg, k.XMm, k.YMm, k.ZMm, k.BoyMm, k.EnMm, k.YukseklikMm)));
                Assert.NotNull(a.AgirlikMerkeziXMm);
                Assert.Equal(a.AgirlikMerkeziXMm, hesap!.Value.XMm);
                var merkez = YuklemePlaniRaporHesabi.AgirlikMerkezi(a, TeknikVeri(a))!;
                Assert.Equal((a.AgirlikMerkeziXMm.Value, hesap.Value.YMm, hesap.Value.ZMm), (merkez.XMm, merkez.YMm, merkez.ZYerdenMm));
                // Ağırlık merkezi yüklerin kapladığı zarfın içindedir.
                Assert.InRange(merkez.YMm, a.Konumlar.Min(k => k.YMm), a.Konumlar.Max(k => k.YMm + k.EnMm));
                Assert.InRange(merkez.ZYerdenMm, a.Konumlar.Min(k => k.ZMm), a.Konumlar.Max(k => k.ZMm + k.YukseklikMm));
            }
        }
    }

    /// <summary>
    /// Ağırlık merkezinin yüksekliği tek yükleme seviyesinde yükleme yüzeyinden, yüzeyler farklı yükseklikteyse yerden
    /// yazılır; çizim işareti her zaman ekipman koordinatındadır (yerden).
    /// </summary>
    [Fact]
    public void AgirlikMerkezi_ZYuzeydenYaDaYerden()
    {
        var tekSeviye = new EkipmanTeknikVerisi { Yuzeyler = [new YukYuzeyi("ANA", "Taban", 0, 13_600, 2_480, 1_200, 2_700, null, null)] };
        var arac = new PlanAraci { Konumlar = [Konum("s:1", 500, 0, 0, 1_200, 2_000, 1_000, 1_000)] };
        var merkez = YuklemePlaniRaporHesabi.AgirlikMerkezi(arac, tekSeviye)!;
        Assert.Equal((1_000, 500, 500, true, 1_700), (merkez.XMm, merkez.YMm, merkez.ZMm, merkez.ZYuzeyden, merkez.ZYerdenMm));

        var lowbed = YerlesimMotoruTests.Lowbed.TeknikVeri;
        var havuzda = new PlanAraci { Konumlar = [Konum("s:1", 500, 3_200, 0, 900, 2_000, 1_000, 1_000)] };
        var yerden = YuklemePlaniRaporHesabi.AgirlikMerkezi(havuzda, lowbed)!;
        Assert.Equal((1_400, false, 1_400), (yerden.ZMm, yerden.ZYuzeyden, yerden.ZYerdenMm));

        // Saklanan boyuna değer varsa o yazılır; yük yoksa ağırlık merkezi yoktur.
        var saklanan = new PlanAraci { AgirlikMerkeziXMm = 4_321, Konumlar = havuzda.Konumlar };
        Assert.Equal(4_321, YuklemePlaniRaporHesabi.AgirlikMerkezi(saklanan, lowbed)!.XMm);
        Assert.Null(YuklemePlaniRaporHesabi.AgirlikMerkezi(new PlanAraci(), lowbed));
    }

    /// <summary>
    /// Araç ölçüleri: tek yüzeyde o yüzeyin ölçüleri; birden çok yüzeyde uzunluk toplam, genişlik ve iç yükseklik ana
    /// yüzeyden (alanı en büyük). Kullanılabilir hacim bütün yüzeylerin iç yüksekliği biliniyorsa vardır.
    /// </summary>
    [Fact]
    public void AracOlculeri_TekVeCokYuzey()
    {
        var k40 = YuklemePlaniRaporHesabi.Olculer(YerlesimMotoruTests.K40.TeknikVeri);
        Assert.Equal((12_032, 2_352, 2_393, 1), (k40.IcUzunlukMm, k40.IcGenislikMm, k40.IcYukseklikMm, k40.YuzeySayisi));
        Assert.Equal(12_032m * 2_352 * 2_393 / 1_000_000_000m, k40.KullanilabilirHacimM3);

        var lowbed = YuklemePlaniRaporHesabi.Olculer(YerlesimMotoruTests.Lowbed.TeknikVeri);
        Assert.Equal((12_000, 2_550, (int?)null, 2, "Havuz"), (lowbed.IcUzunlukMm, lowbed.IcGenislikMm, lowbed.IcYukseklikMm, lowbed.YuzeySayisi, lowbed.AnaYuzeyAdi));
        Assert.Null(lowbed.KullanilabilirHacimM3);

        var bos = YuklemePlaniRaporHesabi.Olculer(new EkipmanTeknikVerisi());
        Assert.Equal((null, null, null, 0), (bos.IcUzunlukMm, bos.IcGenislikMm, bos.IcYukseklikMm, bos.YuzeySayisi));
    }

    /// <summary>
    /// Kullanım: yüklerin kapladığı zarf, ağırlığın kapasiteye ve hacmin kullanılabilir hacme oranı, kalan hacim. Kapasite
    /// ya da iç yükseklik bilinmiyorsa oran yazılmaz; yük hacmi kullanılabilir hacmi aşıyorsa kalan yazılmaz.
    /// </summary>
    [Fact]
    public void AracKullanimi_ZarfHacimVeOranlar()
    {
        var olcu = YuklemePlaniRaporHesabi.Olculer(YerlesimMotoruTests.K40.TeknikVeri);
        var arac = new PlanAraci
        {
            ToplamKg = 5_300, KapasiteKg = 26_500,
            Konumlar = [Konum("s:1", 3_300, 30, 100, 0, 2_000, 1_000, 1_000), Konum("s:2", 2_000, 2_060, 100, 0, 1_000, 2_000, 1_500, sira: 2)]
        };
        var kullanim = YuklemePlaniRaporHesabi.Kullanim(arac, olcu);
        Assert.Equal((3_030, 2_000, 1_500), (kullanim.UzunlukMm, kullanim.GenislikMm, kullanim.YukseklikMm));
        Assert.Equal((5_300m, 20m, 5m), (kullanim.YukKg, kullanim.AgirlikYuzde, kullanim.YukHacmiM3));
        Assert.Equal(5m * 100 / olcu.KullanilabilirHacimM3!.Value, kullanim.HacimYuzde);
        Assert.Equal(olcu.KullanilabilirHacimM3.Value - 5m, kullanim.KalanHacimM3);

        // Kapasitesi ve iç yüksekliği bilinmeyen araç: yüzde ve kalan yok.
        var acik = YuklemePlaniRaporHesabi.Kullanim(new PlanAraci { ToplamKg = 5_300, Konumlar = arac.Konumlar },
            YuklemePlaniRaporHesabi.Olculer(YerlesimMotoruTests.Lowbed.TeknikVeri));
        Assert.Equal((null, null, null, 5m), (acik.AgirlikYuzde, acik.HacimYuzde, acik.KalanHacimM3, acik.YukHacmiM3));

        // Taşan yük kullanılabilir hacmi aşabilir: oran %100'ün üstünde yazılır, eksi kalan yazılmaz.
        var kucuk = new YuklemePlaniRaporHesabi.AracOlculeri(2_000, 1_000, 1_000, 1, "Taban", 2m);
        var tasan = YuklemePlaniRaporHesabi.Kullanim(arac, kucuk);
        Assert.Equal((250m, (decimal?)null), (tasan.HacimYuzde, tasan.KalanHacimM3));

        var bos = YuklemePlaniRaporHesabi.Kullanim(new PlanAraci { KapasiteKg = 26_500 }, olcu);
        Assert.Equal((0, 0, 0, 0m, 0m), (bos.UzunlukMm, bos.GenislikMm, bos.YukseklikMm, bos.YukHacmiM3, bos.AgirlikYuzde));
    }

    /// <summary>
    /// Yüklü toplam: ölçüler motorun sakladığı değerlerdir; brüt ağırlık dara biliniyorsa yazılır. Üstten taşma yükün dış
    /// yükseklikten (uç/yan duvar) yukarı çıkan kısmıdır; dış yüksekliği güvertenin kendisi olan platformda sayılmaz.
    /// </summary>
    [Fact]
    public void AracToplami_BrutVeUsttenTasma()
    {
        var flatRack = new EkipmanTeknikVerisi
        {
            DisOlcu = new Olcu(12_192, 2_438, 2_591), DaraKg = 5_000,
            Yuzeyler = [new YukYuzeyi("ANA", "Taban", 0, 12_048, 2_438, 648, null, null, null)]
        };
        var arac = new PlanAraci
        {
            ToplamKg = 35_500, YukluUzunlukMm = 12_192, YukluGenislikMm = 2_438, YukluYukseklikMm = 3_748,
            Konumlar = [Konum("s:1", 35_500, 3_000, 69, 648, 5_400, 2_300, 3_100)]
        };
        var toplam = YuklemePlaniRaporHesabi.Toplam(arac, flatRack);
        Assert.Equal((12_192, 2_438, 3_748, 40_500m, 1_157), (toplam.UzunlukMm, toplam.GenislikMm, toplam.YukseklikMm, toplam.BrutKg, toplam.UsttenTasmaMm));
        Assert.True(YuklemePlaniRaporHesabi.OlcuDisi(arac, flatRack));

        // Dara tanımlı değilse brüt yazılmaz (tahmin edilmez).
        Assert.Null(YuklemePlaniRaporHesabi.Toplam(arac, flatRack with { DaraKg = null }).BrutKg);

        // Lowbed: dış yükseklik boyun seviyesidir; havuzdaki yük dış yükseklikten yüksek olsa da üstten taşma sayılmaz.
        var lowbed = YerlesimMotoruTests.Lowbed.TeknikVeri;
        var havuzda = new PlanAraci { Konumlar = [Konum("s:1", 500, 3_200, 100, 900, 2_000, 1_000, 2_500)] };
        Assert.Equal(0, YuklemePlaniRaporHesabi.UsttenTasmaMm(havuzda, lowbed));
        Assert.False(YuklemePlaniRaporHesabi.OlcuDisi(havuzda, lowbed));
        Assert.True(YuklemePlaniRaporHesabi.OlcuDisi(new PlanAraci { SagTasmaMm = 225, Konumlar = havuzda.Konumlar }, lowbed));
    }

    /// <summary>
    /// Sandık renkleri belirlenimcidir, paletten gelir (kırmızı yok: uygunsuz yükün vurgusudur) ve komşu yükler aynı
    /// rengi almaz. Çizim kutuları ve tablo satırı aynı rengi taşır (aynı sandık her görünümde ve tabloda aynı renk).
    /// </summary>
    [Fact]
    public void SandikRenkleri_BelirlenimciKomsuFarkliVeTabloylaAyni()
    {
        var plan = OrnekPlan(projeSayisi: 1, projeBasinaYuk: 0, uygun: true, tekProje: true, kucukYuk: 90);
        var konmazlar = YuklemePlaniRaporHesabi.UstuneKonmazlar(plan);
        foreach (var a in plan.Yerlesim!.Araclar)
        {
            var satirlar = YuklemePlaniPdfOlusturucu.AracYukSatirlari(a, tekProje: true, konmazlar);
            Assert.Equal(a.Konumlar.Count, satirlar.Count);
            Assert.Equal(satirlar.Select(s => s.Renk), YuklemePlaniPdfOlusturucu.AracYukSatirlari(a, tekProje: true, konmazlar).Select(s => s.Renk));
            Assert.All(satirlar, s => Assert.Contains(s.Renk, YuklemePlaniRaporHesabi.SandikPaleti));
            for (var i = 0; i < satirlar.Count; i++)
                for (var j = 0; j < i; j++)
                    if (YuklemePlaniRaporHesabi.Komsu(satirlar[i].Konum, satirlar[j].Konum))
                        Assert.NotEqual(satirlar[i].Renk, satirlar[j].Renk);

            // İki etiket düzeninde de (birleşik istif etiketi / kat etiketi) kutu, satırının rengini ve konumunu taşır.
            foreach (var katEtiketi in (bool[])[false, true])
            {
                var kutular = YuklemePlaniPdfOlusturucu.CizimKutulari(satirlar, katEtiketi);
                Assert.Equal(satirlar.Select(s => (s.Renk, s.Sorunlu, s.Konum.XMm, s.Konum.YMm, s.Konum.ZMm)),
                    kutular.Select(k => (k.Renk, k.Sorunlu, k.XMm, k.YMm, k.ZMm)));
                Assert.Equal(satirlar.Select(s => katEtiketi ? s.Etiket : s.CizimEtiketi), kutular.Select(k => k.Etiket));
            }
        }
        // Çok yüklü araçta palet yük sayısından küçüktür; renkler tekrar eder ama birden çok renk kullanılır.
        var kalabalik = plan.Yerlesim.Araclar.MaxBy(a => a.Konumlar.Count)!;
        Assert.True(kalabalik.Konumlar.Count > YuklemePlaniRaporHesabi.SandikPaleti.Count);
        Assert.True(YuklemePlaniPdfOlusturucu.AracYukSatirlari(kalabalik, true, konmazlar).Select(s => s.Renk).Distinct().Count() >= 10);

        Assert.Equal(YuklemePlaniRaporHesabi.SandikPaleti.Count, YuklemePlaniRaporHesabi.SandikPaleti.Distinct().Count());
        Assert.DoesNotContain("#dc2626", YuklemePlaniRaporHesabi.SandikPaleti);
        Assert.Empty(YuklemePlaniRaporHesabi.SandikRenkleri([]));
    }

    private static readonly string[] OrnekSandikAdlari =
        ["Radyatör grubu", "Yağ genleşme tankı ve bağlantı boruları", "Aksesuar sandığı", "Buşing", "Kontrol panosu"];

    /// <summary>
    /// Sandık adı plan yük satırından (sandık kaydının adı) gelir, konuma yük anahtarıyla bağlanır ve tablo satırına taşınır;
    /// adı boş sandık ve satırı kalmamış konum boş kalır, ad kırpılır.
    /// </summary>
    [Fact]
    public void SandikAdi_PlanSatirindanTabloyaTasinir()
    {
        var plan = OrnekPlan();
        var adlar = YuklemePlaniRaporHesabi.YukAdlari(plan);
        var satirlar = plan.Yerlesim!.Araclar
            .SelectMany(a => YuklemePlaniPdfOlusturucu.AracYukSatirlari(a, tekProje: false, YuklemePlaniRaporHesabi.UstuneKonmazlar(plan), adlar)).ToList();
        var planSatirlari = plan.Yukler.Where(y => y.SandikId != null).ToDictionary(y => "s:" + y.SandikId);
        Assert.All(satirlar, s => Assert.Equal(planSatirlari.TryGetValue(s.Konum.YukAnahtari, out var y) ? y.Aciklama : null, s.Ad));
        Assert.Contains(satirlar, s => s.Ad != null);
        Assert.Contains(satirlar, s => s.Ad == null);

        var ozel = new YuklemePlaniDetayi
        {
            Yukler =
            [
                new PlanYukSatiri { Kaynak = PlanYukKaynagi.Sandik, SandikId = 7, BirimNo = 1, Aciklama = "  Trafo kazanı " },
                new PlanYukSatiri { Kaynak = PlanYukKaynagi.Sandik, SandikId = 8, BirimNo = 1, Aciklama = "   " },
                new PlanYukSatiri { Kaynak = PlanYukKaynagi.Sandik, SandikId = null, BirimNo = 1, Aciklama = "Silinmiş" },
                new PlanYukSatiri { Kaynak = PlanYukKaynagi.Manuel, ManuelGrupNo = 2, BirimNo = 1, Aciklama = "Elle yük" }
            ]
        };
        Assert.Equal(new Dictionary<string, string> { ["s:7"] = "Trafo kazanı", ["m:2:1"] = "Elle yük" }, YuklemePlaniRaporHesabi.YukAdlari(ozel));
    }

    /// <summary>
    /// "Üstüne konmaz" işareti plan yük satırındaki izinden gelir (üste konabilir değilse) ve konuma yük anahtarıyla
    /// bağlanır; elle eklenen yükün birimi kendi anahtarıyla eşlenir, sandığı silinmiş satır anahtar üretmez.
    /// </summary>
    [Fact]
    public void UstuneKonmaz_PlanSatirindakiIzindenEslenir()
    {
        var plan = OrnekPlan();
        var konmazlar = YuklemePlaniRaporHesabi.UstuneKonmazlar(plan);
        var satirlar = plan.Yerlesim!.Araclar.SelectMany(a => YuklemePlaniPdfOlusturucu.AracYukSatirlari(a, tekProje: false, konmazlar)).ToList();
        Assert.NotEmpty(satirlar);
        // Örnek planda ikinci projenin sandıklarının ve trafonun üstüne yük konmaz.
        Assert.All(satirlar, s => Assert.Equal(
            s.Konum.Kod == "PA100-35-1T" || s.Konum.Kod.StartsWith("PA200-35-", StringComparison.Ordinal), s.UstuneKonmaz));
        Assert.Contains(satirlar, s => s.UstuneKonmaz);
        Assert.Contains(satirlar, s => !s.UstuneKonmaz);

        var elle = new YuklemePlaniDetayi
        {
            Yukler =
            [
                new PlanYukSatiri { Kaynak = PlanYukKaynagi.Manuel, ManuelGrupNo = 3, BirimNo = 2, UsteKonabilirMi = false },
                new PlanYukSatiri { Kaynak = PlanYukKaynagi.Manuel, ManuelGrupNo = 3, BirimNo = 1, UsteKonabilirMi = true },
                new PlanYukSatiri { Kaynak = PlanYukKaynagi.Sandik, SandikId = 7, BirimNo = 1, UsteKonabilirMi = false },
                new PlanYukSatiri { Kaynak = PlanYukKaynagi.Sandik, SandikId = null, BirimNo = 1, UsteKonabilirMi = false }
            ]
        };
        Assert.Equal(["m:3:2", "s:7"], YuklemePlaniRaporHesabi.UstuneKonmazlar(elle).Order(StringComparer.Ordinal));
    }

    /// <summary>
    /// Özet sayfasının sayıları: araç sayısı, ölçü dışı (kasa dışına taşan) yük taşıyan araç sayısı, araca yüklenen yük
    /// ve sandık sayısı, toplam ağırlık ve hacim. Araca atanamayan yükler sayılmaz.
    /// </summary>
    [Fact]
    public void Ozet_AracYukAgirlikVeHacim()
    {
        var plan = OrnekPlan();
        var araclar = plan.Yerlesim!.Araclar;
        var ozet = YuklemePlaniRaporHesabi.Ozet(plan, TeknikVeri);
        Assert.Equal(araclar.Count, ozet.AracSayisi);
        Assert.Equal(araclar.Sum(a => a.Konumlar.Count), ozet.YukSayisi);
        Assert.Equal(ozet.YukSayisi, ozet.SandikSayisi);
        Assert.Equal(araclar.Sum(a => a.ToplamKg), ozet.ToplamKg);
        Assert.Equal(araclar.SelectMany(a => a.Konumlar).Sum(k => (decimal)k.BoyMm * k.EnMm * k.YukseklikMm) / 1_000_000_000m, ozet.ToplamHacimM3);
        // Trafo (3.000 mm) lowbed'in (2.550 mm) iki yanından taşar: tek ölçü dışı araç odur.
        var lowbed = Assert.Single(araclar, a => a.ZorunluOzelEkipman);
        Assert.True(lowbed.SolTasmaMm > 0 && lowbed.SagTasmaMm > 0);
        Assert.Equal(1, ozet.OlcuDisiAracSayisi);
        // Yerleşemeyen ve verisi eksik yükler özette yüklenmiş sayılmaz.
        Assert.Equal(plan.Yukler.Count - YuklemePlaniPdfOlusturucu.AtanamayanYukler(plan).Count, ozet.YukSayisi);
        // Tip dağılımı talep formuyla aynı kaynaktandır ve adetleri araç sayısını verir.
        Assert.Equal(ozet.AracSayisi, YuklemePlaniPdfOlusturucu.AracTipleri(plan).Sum(t => t.Adet));

        Assert.Equal("15 sandık yüklendi", YuklemePlaniPdfOlusturucu.YuklenenMetni(new(3, 0, 15, 15, 0, 0)));
        Assert.Equal("1.250 yük yüklendi (1.200 sandık)", YuklemePlaniPdfOlusturucu.YuklenenMetni(new(40, 0, 1_250, 1_200, 0, 0)));

        var bos = YuklemePlaniRaporHesabi.Ozet(new YuklemePlaniDetayi(), TeknikVeri);
        Assert.Equal((0, 0, 0, 0m, 0m), (bos.AracSayisi, bos.OlcuDisiAracSayisi, bos.YukSayisi, bos.ToplamKg, bos.ToplamHacimM3));
    }

    /// <summary>Ağırlık merkezi verilince her görünümde dört dilimli daire işareti çizilir.</summary>
    [Fact]
    public void Cizim_AgirlikMerkeziIsareti()
    {
        var veri = YerlesimMotoruTests.K40.TeknikVeri;
        CizimKutusu[] kutular = [new("1", 30, 100, 0, 1_400, 1_100, 1_000, "#1f77b4", false)];
        foreach (var gorunum in Enum.GetValues<CizimGorunumu>())
        {
            var svg = YerlesimCizimi.Svg(veri, kutular, gorunum, new CizimNoktasi(730, 650, 500));
            Assert.Contains($"<g id='{YerlesimCizimi.AgirlikMerkeziKimligi}'><circle ", svg);
            // İşaret kutulardan ve etiketlerden sonra çizilir (üstte kalır).
            Assert.True(svg.IndexOf(YerlesimCizimi.AgirlikMerkeziKimligi, StringComparison.Ordinal) > svg.IndexOf(">1</text>", StringComparison.Ordinal));
        }
        // Yandan görünümde işaret boyuna konumda (X) durur.
        Assert.Contains("<circle cx='730'", YerlesimCizimi.Svg(veri, kutular, CizimGorunumu.Yandan, new CizimNoktasi(730, 650, 500)));
        Assert.Contains(YerlesimCizimi.AgirlikMerkeziKimligi, YerlesimCizimi.AgirlikMerkeziIsareti());
    }

    /// <summary>Etiket yazısı zeminle kontrastlıdır: açık renkli sandıkta koyu, koyu renkli sandıkta ve uygunsuz yükte beyaz.</summary>
    [Fact]
    public void Cizim_EtiketYazisiZemineGoreSecilir()
    {
        Assert.Equal("#ffffff", YerlesimCizimi.YaziRengi("#1f77b4"));
        Assert.Equal("#ffffff", YerlesimCizimi.YaziRengi("#dc2626"));
        Assert.Equal("#0f172a", YerlesimCizimi.YaziRengi("#ffbb78"));
        Assert.Equal("#0f172a", YerlesimCizimi.YaziRengi("#dbdb8d"));
        Assert.Equal("#ffffff", YerlesimCizimi.YaziRengi("mavi"));

        var veri = YerlesimMotoruTests.K40.TeknikVeri;
        var acik = YerlesimCizimi.Svg(veri, [new("7", 0, 0, 0, 1_200, 1_000, 1_000, "#ffbb78", false)], CizimGorunumu.Ustten);
        Assert.Contains("fill='#0f172a' font-weight='bold'>7</text>", acik);
        // Uygunsuz yük kırmızı çizilir; yazısı kendi rengine değil kırmızı zemine göre seçilir.
        var sorunlu = YerlesimCizimi.Svg(veri, [new("7", 0, 0, 0, 1_200, 1_000, 1_000, "#ffbb78", true)], CizimGorunumu.Ustten);
        Assert.Contains("fill='#ffffff' font-weight='bold'>7</text>", sorunlu);
    }

    /// <summary>
    /// 3B çizim sırası: arkadaki (bakana uzak) yük önce çizilir. Uzun bir yükün solunda ve arka ucunda duran kısa yük,
    /// köşe uzaklığına göre sıralansa uzun yükün üstüne çizilirdi.
    /// </summary>
    [Fact]
    public void Cizim_UcBoyutluSiraUzaktakiOnce()
    {
        var uzun = new CizimKutusu("UZUN", 0, 1_200, 0, 6_000, 1_200, 1_000, "#1f77b4", false);
        var kisa = new CizimKutusu("KISA", 5_000, 0, 0, 1_000, 1_000, 2_000, "#2ca02c", false);
        var ust = new CizimKutusu("UST", 0, 1_200, 1_000, 1_000, 1_000, 500, "#9467bd", false);
        foreach (var girdi in (CizimKutusu[][])[[uzun, kisa, ust], [ust, kisa, uzun], [kisa, ust, uzun]])
        {
            var sira = YerlesimCizimi.RessamSirasi(girdi).Select(k => k.Etiket).ToList();
            // Uzun yük bakana daha yakındır (sağda); kısa yük ondan önce, üstündeki yük ondan sonra çizilir.
            Assert.True(sira.IndexOf("KISA") < sira.IndexOf("UZUN"), string.Join(",", sira));
            Assert.True(sira.IndexOf("UZUN") < sira.IndexOf("UST"), string.Join(",", sira));
        }
        // İç içe geçen (elle yerleşimde çakışan) kutular da eksiksiz ve bir kez çizilir.
        var cakisan = YerlesimCizimi.RessamSirasi([uzun, uzun with { Etiket = "CAKISAN", XMm = 500 }, kisa]);
        Assert.Equal(["CAKISAN", "KISA", "UZUN"], cakisan.Select(k => k.Etiket).Order(StringComparer.Ordinal));
    }
}
