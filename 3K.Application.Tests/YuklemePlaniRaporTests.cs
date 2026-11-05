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
/// uygunsuzlukta basılır; SVG çizimleri yön işaretlerini ve listedeki numaraları taşır. THREEK_RAPOR_ORNEK_KLASORU
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
    internal static YuklemePlaniDetayi OrnekPlan(int projeSayisi, int projeBasinaYuk, bool uygun)
    {
        var yukler = new List<YerlesimYuku>();
        for (var i = 1; i <= 10; i++)
            yukler.Add(Sandik($"PA100-35-{i}", 1, 1_400, 1_100, 1_000, 300 + i * 20, lokasyon: i % 3 == 0 ? null : 1));
        for (var i = 1; i <= 4; i++)
            yukler.Add(Sandik($"PA200-35-{i}", 2, 2_200, 1_200, 1_400, 600));
        for (var p = 3; p <= projeSayisi; p++)
            for (var i = 1; i <= projeBasinaYuk; i++)
                yukler.Add(Sandik($"PA{p}00-35-{i}", p, 1_200 + i % 3 * 100, 1_000, 900, 250));
        if (!uygun)
        {
            yukler.Add(new("s:TR", "PA100-35-1T", 1, "PA100-35", 1, 6_000, 3_000, 3_800, 45_000, false, false, false, null, true, OzelEkipmanZorunlulugu.Her, "Trafo"));
            yukler.Add(Sandik("PA200-35-UZUN", 2, 13_000, 1_000, 1_000, 900));
        }
        var girdi = new YerlesimGirdisi(yukler, [YerlesimMotoruTests.K40, YerlesimMotoruTests.Lowbed], Ayar, false, false, null,
            new YasalSinirlar(2_550, 4_000, 0, "Onaylı TR"), "Konteyner");
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
            Sonuc = (YerlesimKontrolSonucu)(int)sonuc.Sonuc,
            Araclar = sonuc.Araclar.Select(a => new PlanAraci
            {
                AracNo = a.AracNo, KonfigurasyonId = a.KonfigurasyonId, RevizyonNo = a.RevizyonNo, EkipmanKodu = a.EkipmanKodu, EkipmanAdi = a.EkipmanAdi,
                AileAdi = a.AileAdi, TipAdi = a.TipAdi, AileDisi = a.AileDisi, ZorunluOzelEkipman = a.ZorunluOzelEkipman, AileDisiGerekce = a.AileDisiGerekce,
                ProjeNolari = a.Konumlar.Select(k => yukSozluk[k.YukAnahtari].ProjeNo!).Distinct().ToList(),
                Lokasyonlar = a.Konumlar.Select(k => yukSozluk[k.YukAnahtari].LokasyonId == null ? YuklemeEtiketleri.BelirsizLokasyon : "3K").Distinct().ToList(),
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
            TasimaAilesiAdi = "Konteyner", ProjeKarisimi = YuklemeProjeKarisimi.Karisik, LokasyonTercihi = YuklemeLokasyonTercihi.Karisik,
            YuklemeTarihi = new DateTime(2026, 11, 12), RevizyonNo = 4, Kaydeden = "Planlama Kullanıcısı",
            Projeler = Enumerable.Range(1, Math.Max(2, projeSayisi))
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
        Assert.True(YuklemePlaniPdfOlusturucu.BelirsizLokasyonVar(plan));
        Assert.Single(plan.Yerlesim.Araclar, a => a.ZorunluOzelEkipman);

        if (Environment.GetEnvironmentVariable("THREEK_RAPOR_ORNEK_KLASORU") is { Length: > 0 } klasor)
        {
            Directory.CreateDirectory(klasor);
            File.WriteAllBytes(Path.Combine(klasor, "arac-talebi.pdf"), talep);
            File.WriteAllBytes(Path.Combine(klasor, "yukleme-plani.pdf"), yukleme);
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
