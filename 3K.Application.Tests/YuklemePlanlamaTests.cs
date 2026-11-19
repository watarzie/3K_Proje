using ClosedXML.Excel;
using _3K.Application.Behaviors;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Application.Features.ProjeIslemleri.Services;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Validators;
using _3K.Application.Features.YuklemePlanlamaIslemleri.Commands;
using _3K.Application.Features.YuklemePlanlamaIslemleri.DTOs;
using _3K.Application.Features.YuklemePlanlamaIslemleri.Queries;
using _3K.Application.Features.YuklemePlanlamaIslemleri.Validators;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Services.YuklemePlanlama;
using _3K.YuklemeMotoru.Ekipman;

namespace _3K.Application.Tests;

/// <summary>
/// Yükleme Simülasyonu Faz 1 (Simülasyon isterleri 3–7, 11–13): yük verisi ve izin kuralları, plan numarası, ekipman
/// kategorisi davranışı, motorun teknik veri denetimi, Excel biçimi, istek doğrulamaları ve yetkiler. Veritabanı
/// davranışı <see cref="YuklemePlanlamaPostgresTests"/>'tedir.
/// </summary>
public sealed class YuklemePlanlamaTests
{
    /// <summary>Kabul 17: ölçüsü veya ağırlığı eksik, sıfır ya da geçersiz yük hesaplanmaz; mesaj satırda gösterilir.</summary>
    /// <summary>
    /// Araç tipi kademesi (saha geri bildirimi 19.11.2026 madde 1, 3): tip seçilmemişse sadelik sırası olan tipler
    /// kullanılır; seçilmişse yalnız seçilenler (sırası boş olan seçilmiş tip de), seçilmeyenler yedektir.
    /// </summary>
    [Theory]
    [InlineData(new int[0], 3, 2, 2, false)]
    [InlineData(new int[0], 12, null, null, false)]
    [InlineData(new[] { 3 }, 3, 2, 2, false)]
    [InlineData(new[] { 12 }, 12, null, 1, false)]
    [InlineData(new[] { 3 }, 7, 4, 4, true)]
    [InlineData(new[] { 3 }, 12, null, null, true)]
    public void AracTipiKademesi_SecimeGore(int[] secili, int tipId, int? sira, int? beklenenKademe, bool beklenenYedek) =>
        Assert.Equal((beklenenKademe, beklenenYedek), YuklemePlaniKurali.AracTipiKademesi(tipId, sira, secili));

    [Fact]
    public void TipSecimi_TekrarsizVeSinirli()
    {
        Assert.True(YuklemePlaniKurali.TipIdleriGecerli([]));
        Assert.True(YuklemePlaniKurali.TipIdleriGecerli([3, 7]));
        Assert.False(YuklemePlaniKurali.TipIdleriGecerli([3, 3]));
        Assert.False(YuklemePlaniKurali.TipIdleriGecerli([0]));
        Assert.False(YuklemePlaniKurali.TipIdleriGecerli(Enumerable.Range(1, YuklemePlaniKurali.TipSecimiEnFazla + 1).ToList()));
    }

    /// <summary>Tohum: lowbed ve ağır yük tipleri kendi ailesinde; Reefer, Pallet Wide ve frigo yalnız seçilince gelir.</summary>
    [Fact]
    public void Tohum_LowbedAyriAilede_OzelKonteynerlerOtomatikGelmez()
    {
        var tipler = EkipmanTohumu.Tipler.ToDictionary(t => t.Kod);
        Assert.All(["lowbed", "low-loader", "uzamali-agir-yuk", "havuzlu", "hidrolik-dumenli", "moduler"],
            kod => Assert.Equal(EkipmanTohumu.LowbedAilesiId, tipler[kod].AileId));
        Assert.DoesNotContain(EkipmanTohumu.Tipler, t => t.AileId == 2 && EkipmanKategoriKurali.OzelYukEkipmaniMi(t.Kategori));
        Assert.All(["k20-rf", "k40-rf", "k45-pw", "frigo"], kod => Assert.Null(tipler[kod].OtomatikSecimSirasi));
        Assert.True(tipler["k40-dv"].OtomatikSecimSirasi < tipler["k40-hc"].OtomatikSecimSirasi);
        Assert.True(tipler["k40-hc"].OtomatikSecimSirasi < tipler["k40-ht"].OtomatikSecimSirasi);
        Assert.True(tipler["k40-ht"].OtomatikSecimSirasi < tipler["k40-ot"].OtomatikSecimSirasi);
        Assert.True(tipler["k40-ot"].OtomatikSecimSirasi < tipler["k40-fr"].OtomatikSecimSirasi);
        Assert.True(tipler["k40-fr"].OtomatikSecimSirasi < tipler["k40-pl"].OtomatikSecimSirasi);
    }

    [Fact]
    public void VeriKurali_EksikSifirVeGecersizHesaplanmaz_MesajlarIsterlerdekiGibi()
    {
        Assert.Equal(["Ölçü giriniz."], YukVeriKurali.Sorunlar(null, 1200, 1000, 500));
        Assert.Equal(["Ölçü giriniz."], YukVeriKurali.Sorunlar(1400, 0, 1000, 500));
        Assert.Equal(["Geçerli bir ölçü giriniz."], YukVeriKurali.Sorunlar(1400, 1200, -5, 500));
        Assert.Equal(["Geçerli bir ölçü giriniz."], YukVeriKurali.Sorunlar(60_000, 1200, 1000, 500));
        Assert.Equal(["Ağırlık giriniz."], YukVeriKurali.Sorunlar(1400, 1200, 1000, null));
        Assert.Equal(["Ağırlık giriniz."], YukVeriKurali.Sorunlar(1400, 1200, 1000, 0));
        Assert.Equal(["Geçerli bir ağırlık giriniz."], YukVeriKurali.Sorunlar(1400, 1200, 1000, -1));
        Assert.Equal(["Ölçü giriniz.", "Ağırlık giriniz."], YukVeriKurali.Sorunlar(null, null, null, null));
        Assert.Empty(YukVeriKurali.Sorunlar(1400, 1200, 1375, 500));
        // En büyük gerçek yük (351 t, 14,2 m) sınırın içindedir.
        Assert.Empty(YukVeriKurali.Sorunlar(14_200, 5_250, 4_800, 351_000));

        Assert.Equal(PlanYukDurumu.HaricTutuldu, YuklemePlaniKurali.Durum(false, ["Ölçü giriniz."]));
        Assert.Equal(PlanYukDurumu.EksikVeri, YuklemePlaniKurali.Durum(true, ["Ölçü giriniz."]));
        Assert.Equal(PlanYukDurumu.Bekliyor, YuklemePlaniKurali.Durum(true, []));
        Assert.True(YukVeriKurali.ElleOlcuGecerli(null));
        Assert.False(YukVeriKurali.ElleOlcuGecerli(0));
        Assert.False(YukVeriKurali.ElleAgirlikGecerli(-1));
    }

    /// <summary>
    /// Kabul 2 ve isterler 6: sandıkta döndürme pasif, üste konma ve altta kalma aktif, üst yük sınırı kendi brütü; özel
    /// yükte sandık varsayılanı uygulanmaz.
    /// </summary>
    [Fact]
    public void IzinVarsayilani_SandiktaIsterler6_OzelYuktaKapali()
    {
        Assert.Equal(new PlanYukIzinleri(false, true, true, 500m), YukIzinKurali.Varsayilan((int)YukTuru.Sandik, 500m));
        Assert.Equal(new PlanYukIzinleri(false, true, true, null), YukIzinKurali.Varsayilan((int)YukTuru.Sandik, null));
        Assert.Equal(new PlanYukIzinleri(false, false, false, null), YukIzinKurali.Varsayilan((int)YukTuru.Trafo, 110_000m));
        Assert.Equal(new PlanYukIzinleri(false, false, false, null), YukIzinKurali.Varsayilan((int)YukTuru.DigerOzelYuk, 100m));
    }

    /// <summary>İsterler 6: kullanıcının değiştirdiği izin yeniden hesaplamada korunur, öbürleri yeni kaynaktan gelir.</summary>
    [Fact]
    public void IzinYenile_ElleDegisenKorunur_DigerleriYeniVarsayilandanGelir()
    {
        var mevcut = new PlanYukIzinleri(true, false, true, 300m);
        Assert.Equal(new PlanYukIzinleri(true, true, true, 300m),
            YukIzinKurali.Yenile(mevcut, PlanYukIzni.Dondurme | PlanYukIzni.UstYukSiniri, (int)YukTuru.Sandik, 800m));
        Assert.Equal(new PlanYukIzinleri(false, true, true, 800m),
            YukIzinKurali.Yenile(mevcut, PlanYukIzni.Yok, (int)YukTuru.Sandik, 800m));
        // Sandık trafo olarak işaretlenince elle değiştirilmemiş izinler kapanır.
        Assert.Equal(new PlanYukIzinleri(false, false, false, 300m),
            YukIzinKurali.Yenile(mevcut, PlanYukIzni.UstYukSiniri, (int)YukTuru.Trafo, 800m));
        Assert.True(YukIzinKurali.UstYukSiniriGecerli(0));
        Assert.False(YukIzinKurali.UstYukSiniriGecerli(-1));
    }

    [Fact]
    public void PlanNo_YilVeSiraIle_SandikKoduProjeVeSandikNumarasi()
    {
        Assert.Equal("YP-2026-000123", YuklemePlaniKurali.PlanNo(2026, 123));
        Assert.Equal("YP-2026-000001", YuklemePlaniKurali.SonrakiPlanNo(2026, null));
        Assert.Equal("YP-2026-000042", YuklemePlaniKurali.SonrakiPlanNo(2026, "YP-2026-000041"));
        Assert.Equal("YP-2027-000001", YuklemePlaniKurali.SonrakiPlanNo(2027, "YP-2026-000041"));
        Assert.Equal("PA600-35-01", YuklemePlaniKurali.SandikKodu(" PA600-35 ", "01"));
    }

    /// <summary>Tek adımlı hesabın girdisi: proje ve aile zorunlu, var olan planda revizyon zorunlu, paylar ve tip seçimi sınırlı.</summary>
    [Fact]
    public void PlanHesapla_Dogrulama()
    {
        var dogrulayici = new YuklemePlaniPlanHesaplaCommandValidator();
        YuklemePlaniPlanHesaplaCommand Istek() => new() { ProjeIdleri = [1], TasimaAilesiId = 1 };
        Assert.True(dogrulayici.Validate(Istek()).IsValid);
        Assert.False(dogrulayici.Validate(new YuklemePlaniPlanHesaplaCommand { TasimaAilesiId = 1 }).IsValid);
        Assert.False(dogrulayici.Validate(new YuklemePlaniPlanHesaplaCommand { ProjeIdleri = [1] }).IsValid);
        var mevcut = Istek();
        mevcut.PlanId = 5;
        Assert.Equal(["Plan revizyonu eksik; sayfayı yenileyin."], dogrulayici.Validate(mevcut).Errors.Select(e => e.ErrorMessage));
        mevcut.BeklenenRevizyonNo = 3;
        Assert.True(dogrulayici.Validate(mevcut).IsValid);
        var pay = Istek();
        pay.Ayarlar = new YuklemePlanAyarlari { YukseklikPayiMm = YuklemePlaniKurali.GuvenlikPayiEnFazlaMm + 1 };
        Assert.False(dogrulayici.Validate(pay).IsValid);
        var tip = Istek();
        tip.Ayarlar = new YuklemePlanAyarlari { TipIdleri = [3, 3] };
        Assert.False(dogrulayici.Validate(tip).IsValid);
    }

    /// <summary>Tek adımlı hesapta plan adı seçilen proje numaralarıdır; sınırı aşarsa kısaltılır.</summary>
    [Fact]
    public void OtomatikPlanAdi_ProjeNumaralari()
    {
        Assert.Equal("PA600-23, PA600-24", YuklemePlaniKurali.OtomatikPlanAdi(["PA600-23", "PA600-24"]));
        var uzun = YuklemePlaniKurali.OtomatikPlanAdi(Enumerable.Range(1, 40).Select(i => $"PA600-{i:00}"));
        Assert.Equal(YuklemePlaniKurali.AdEnFazla, uzun.Length);
        Assert.EndsWith("…", uzun);
    }

    /// <summary>İsterler 17: yerleşim boşluğu başlangıçta 3 cm; güvenlik payları işletmeyle kesinleşene kadar boş.</summary>
    [Fact]
    public void Ayarlar_VarsayilanUcSantim_PaylarBos_SinirDisiGecersiz()
    {
        var ayarlar = new YuklemePlanAyarlari();
        Assert.Equal(30, ayarlar.YerlesimBosluguMm);
        Assert.Null(ayarlar.YukseklikPayiMm);
        Assert.True(YuklemePlaniKurali.AyarlarGecerli(ayarlar));
        Assert.False(YuklemePlaniKurali.AyarlarGecerli(ayarlar with { YerlesimBosluguMm = -1 }));
        Assert.False(YuklemePlaniKurali.AyarlarGecerli(ayarlar with { YukseklikPayiMm = 2000 }));
        Assert.True(YuklemePlaniKurali.AyarlarGecerli(ayarlar with { AgirlikPayiKg = 0, GenislikPayiMm = 50 }));
    }

    /// <summary>
    /// İsterler 13 ve 11: kapalı gövde yalnız arkadan, Open Top arkadan veya üstten, açık ekipman ve tenteli yandan; sandık
    /// taşımaya uygun olmayan ekipman aday değildir; her kategorinin Türkçe adı vardır.
    /// </summary>
    [Fact]
    public void EkipmanKategorisi_YuklemeYontemiVeUygunluk()
    {
        Assert.Equal(YuklemeYontemi.Arkadan, EkipmanKategoriKurali.YuklemeYontemleri(EkipmanKategorisi.KapaliKonteyner));
        Assert.Equal(YuklemeYontemi.Arkadan, EkipmanKategoriKurali.YuklemeYontemleri(EkipmanKategorisi.KapaliKasa));
        Assert.Equal(YuklemeYontemi.Arkadan, EkipmanKategoriKurali.YuklemeYontemleri(EkipmanKategorisi.Frigo));
        Assert.Equal(YuklemeYontemi.Arkadan | YuklemeYontemi.Ustten, EkipmanKategoriKurali.YuklemeYontemleri(EkipmanKategorisi.OpenTopKonteyner));
        foreach (var kategori in new[] { EkipmanKategorisi.FlatRack, EkipmanKategorisi.AcikKasa, EkipmanKategorisi.Platform,
                     EkipmanKategorisi.Lowbed, EkipmanKategorisi.Tenteli, EkipmanKategorisi.ModulerAgirYuk })
            Assert.Equal(YuklemeYontemi.Yandan, EkipmanKategoriKurali.YuklemeYontemleri(kategori));
        foreach (var kategori in new[] { EkipmanKategorisi.Damper, EkipmanKategorisi.Tanker, EkipmanKategorisi.Silobas,
                     EkipmanKategorisi.HareketliTaban, EkipmanKategorisi.KonteynerSasisi })
        {
            Assert.False(EkipmanKategoriKurali.SandikTasimayaUygunMu(kategori));
            Assert.Equal(YuklemeYontemi.Yok, EkipmanKategoriKurali.YuklemeYontemleri(kategori));
        }
        // Tanımlanmamış yöntem varsayılmaz.
        Assert.Equal(YuklemeYontemi.Yok, EkipmanKategoriKurali.YuklemeYontemleri(EkipmanKategorisi.Diger));
        Assert.Equal("Arkadan, üstten", EkipmanKategoriKurali.YontemMetni(YuklemeYontemi.Arkadan | YuklemeYontemi.Ustten));
        Assert.Equal("Tanımsız", EkipmanKategoriKurali.YontemMetni(YuklemeYontemi.Yok));

        Assert.Equal([EkipmanKategoriKurali.Arka], EkipmanKategoriKurali.GerekenAcikliklar(EkipmanKategorisi.KapaliKonteyner));
        Assert.Equal([EkipmanKategoriKurali.Arka, EkipmanKategoriKurali.Ust], EkipmanKategoriKurali.GerekenAcikliklar(EkipmanKategorisi.OpenTopKonteyner));
        Assert.Equal([EkipmanKategoriKurali.Sol, EkipmanKategoriKurali.Sag], EkipmanKategoriKurali.GerekenAcikliklar(EkipmanKategorisi.Tenteli));
        Assert.Empty(EkipmanKategoriKurali.GerekenAcikliklar(EkipmanKategorisi.FlatRack));
        var adlar = Enum.GetValues<EkipmanKategorisi>().Select(EkipmanKategoriKurali.Ad).ToList();
        Assert.Equal(adlar.Count, adlar.Distinct().Count());
        Assert.Equal(("Kapalı konteyner", "Lowbed / low-loader", "Diğer"), (EkipmanKategoriKurali.Ad(EkipmanKategorisi.KapaliKonteyner),
            EkipmanKategoriKurali.Ad(EkipmanKategorisi.Lowbed), EkipmanKategoriKurali.Ad(EkipmanKategorisi.Diger)));
        Assert.False(EkipmanKategoriKurali.DingilliMi(EkipmanKategorisi.FlatRack));
        Assert.True(EkipmanKategoriKurali.DingilliMi(EkipmanKategorisi.Lowbed));
    }

    /// <summary>Tohum tipleri: her ailenin en az bir tipi var, kodlar tekil; konteyner ailesinde isterler 11'deki tipler.</summary>
    [Fact]
    public void EkipmanTohumu_IsterlerdekiAileVeTipler()
    {
        Assert.Equal(EkipmanTohumu.Tipler.Length, EkipmanTohumu.Tipler.Select(t => t.Kod).Distinct().Count());
        Assert.All(EkipmanTohumu.Aileler, a => Assert.Contains(EkipmanTohumu.Tipler, t => t.AileId == a.Id));
        foreach (var kod in new[] { "k20-dv", "k40-dv", "k40-hc", "k45-hc", "k20-ot", "k40-fr", "k20-rf", "k40-ht", "k45-pw" })
            Assert.Contains(EkipmanTohumu.Tipler, t => t.Kod == kod && t.AileId == 1);
        foreach (var kod in new[] { "lowbed", "low-loader", "havuzlu", "hidrolik-dumenli", "moduler", "tenteli-mega", "frigo", "tanker" })
            Assert.Contains(EkipmanTohumu.Tipler, t => t.Kod == kod);
    }

    private static readonly EkipmanDenetimBaglami KapaliGovde = new([AciklikTarafi.Arka], IcYukseklikGerekli: true, DingilliMi: false);

    private static EkipmanTeknikVerisi KapaliKonteyner() => new()
    {
        DisOlcu = new Olcu(12_192, 2_438, 2_896),
        DaraKg = 3_900,
        TeknikKapasiteKg = 26_580,
        AzamiBrutKg = 30_480,
        Yuzeyler = [new YukYuzeyi("ANA", "Taban", 0, 12_000, 2_350, 0, 2_690, null, null)],
        Acikliklar = [new Aciklik(AciklikTarafi.Arka, null, null, 2_340, 2_580)]
    };

    /// <summary>Motor denetimi: geçerli veri hatasız; yapısal bozukluk hatadır; eksik veri yalnız uyarıdır (tahmin yok).</summary>
    [Fact]
    public void TeknikVeriDenetimi_HataVeEksikVeriAyri()
    {
        var gecerli = EkipmanTeknikVeriDenetimi.Denetle(KapaliKonteyner(), KapaliGovde);
        Assert.True(gecerli.GecerliMi);
        Assert.Empty(gecerli.Uyarilar);

        Assert.Contains("En az bir yükleme yüzeyi", Assert.Single(
            EkipmanTeknikVeriDenetimi.Denetle(KapaliKonteyner() with { Yuzeyler = [] }, KapaliGovde).Hatalar));
        Assert.Contains(EkipmanTeknikVeriDenetimi.Denetle(KapaliKonteyner() with
        {
            Yuzeyler = [new("A", null, 0, 6_000, 2_350, 0, 2_690, null, null), new("B", null, 5_000, 6_000, 2_350, 0, 2_690, null, null)]
        }, KapaliGovde).Hatalar, h => h.Contains("çakışıyor"));
        Assert.Contains(EkipmanTeknikVeriDenetimi.Denetle(KapaliKonteyner() with
        {
            Yuzeyler = [new("A", null, 0, 13_000, 2_350, 0, 2_690, null, null)]
        }, KapaliGovde).Hatalar, h => h.Contains("dış ölçülerin dışına"));
        Assert.Contains(EkipmanTeknikVeriDenetimi.Denetle(KapaliKonteyner() with
        {
            Acikliklar = [new Aciklik(AciklikTarafi.Arka, null, null, 2_340, null)]
        }, KapaliGovde).Hatalar, h => h.StartsWith("Arka açıklık", StringComparison.Ordinal));
        Assert.Contains(EkipmanTeknikVeriDenetimi.Denetle(KapaliKonteyner() with { DaraKg = 31_000 }, KapaliGovde).Hatalar,
            h => h.Contains("Boş ağırlık azami brüt"));

        // Kapı açıklığı, iç yükseklik ve kapasite bilinmiyorsa kayıt geçerlidir ama ilgili kontrol teyit edilemez.
        var eksik = EkipmanTeknikVeriDenetimi.Denetle(KapaliKonteyner() with
        {
            Acikliklar = [],
            TeknikKapasiteKg = null,
            Yuzeyler = [new("ANA", null, 0, 12_000, 2_350, 0, null, null, null)]
        }, KapaliGovde);
        Assert.True(eksik.GecerliMi);
        Assert.Equal(3, eksik.Uyarilar.Count);
        Assert.Contains(eksik.Uyarilar, u => u.StartsWith("Arka açıklık tanımlı değil", StringComparison.Ordinal));

        var dingilsiz = EkipmanTeknikVeriDenetimi.Denetle(KapaliKonteyner(), KapaliGovde with { DingilliMi = true });
        Assert.Contains(dingilsiz.Uyarilar, u => u.StartsWith("Dingil verisi yok", StringComparison.Ordinal));
    }

    /// <summary>JSON biçimi kararlıdır (aynı veri aynı metin), enum adları camelCase; okunamayan JSON null döner.</summary>
    [Fact]
    public void TeknikVeriJsonu_KararliVeOkunabilir()
    {
        var json = EkipmanTeknikVeriSerilestirici.Yaz(KapaliKonteyner());
        Assert.Contains("\"taraf\":\"arka\"", json);
        Assert.DoesNotContain("kingPin", json);
        var okunan = EkipmanTeknikVeriSerilestirici.Oku(json);
        Assert.NotNull(okunan);
        Assert.Equal(json, EkipmanTeknikVeriSerilestirici.Yaz(okunan));
        Assert.Null(EkipmanTeknikVeriSerilestirici.Oku("{\"acikliklar\":[{\"taraf\":\"yok\"}]}"));
    }

    private static readonly EkipmanExcelTipi[] Tipler =
    [
        new("k40-hc", "40' High Cube", "Konteyner", EkipmanKategorisi.KapaliKonteyner),
        new("lowbed", "Lowbed", "Açık kasa / açık platform / lowbed", EkipmanKategorisi.Lowbed)
    ];

    internal static EkipmanExcelKaydi LowbedKaydi() => new(0, "LB-3D", "3 dingil lowbed", "lowbed", "Üretici A", "LB 3", EkipmanKaynakTuru.UreticiFoyu,
        "Föy 2025/4 s.2", new DateTime(2026, 10, 1), "Uzama yok", new EkipmanTeknikVerisi
        {
            DisOlcu = new Olcu(15_000, 2_550, 1_600),
            DaraKg = 12_000,
            TeknikKapasiteKg = 40_000,
            KingPin = new KingPin(800, 18_000),
            Yuzeyler = [new("BOYUN", "Boyun", 0, 3_000, 2_550, 1_600, null, null, null), new("HAVUZ", "Havuz", 3_200, 9_000, 2_550, 900, null, 2_000, null)],
            Engeller = [new(EngelTuru.TekerHavuzu, 12_400, 0, 900, 2_500, 2_550, 300, false, "Arka dingil üstü")],
            Dingiller = [new(12_600, DingilTuru.Tahriksiz, 1, 12_000), new(13_960, DingilTuru.Pandul, 1, 12_000)],
            BaglamaNoktalari = [new(3_300, 0, 900, 2_000)]
        });

    /// <summary>Şablon/dışa aktarım biçimi içe aktarımla aynıdır: yazılan kayıt aynen okunur.</summary>
    [Fact]
    public void Excel_YazilanKayitAynenOkunur()
    {
        var bayt = EkipmanExcelDosyasi.Yaz(Tipler, [LowbedKaydi()]);
        var okuma = EkipmanExcelDosyasi.Oku(new MemoryStream(bayt));
        Assert.Empty(okuma.Sorunlar);
        var kayit = Assert.Single(okuma.Kayitlar);
        var beklenen = LowbedKaydi();
        Assert.Equal((beklenen.Kod, beklenen.Ad, beklenen.TipKodu, beklenen.Uretici, beklenen.ModelAdi, beklenen.KaynakTuru,
                beklenen.KaynakAciklama, beklenen.KaynakTarihi, beklenen.Aciklama),
            (kayit.Kod, kayit.Ad, kayit.TipKodu, kayit.Uretici, kayit.ModelAdi, kayit.KaynakTuru, kayit.KaynakAciklama,
                kayit.KaynakTarihi, kayit.Aciklama));
        Assert.Equal(EkipmanTeknikVeriSerilestirici.Yaz(beklenen.TeknikVeri), EkipmanTeknikVeriSerilestirici.Yaz(kayit.TeknikVeri));

        // Boş şablon: başlıklar ve tip sayfası var, kayıt yok.
        var sablon = EkipmanExcelDosyasi.Oku(new MemoryStream(EkipmanExcelDosyasi.Yaz(Tipler, [])));
        Assert.Empty(sablon.Kayitlar);
        Assert.Empty(sablon.Sorunlar);
    }

    /// <summary>Hatalı satırlar sayfa ve satır numarasıyla bildirilir; ondalıklı mm, eksik dış ölçü, yinelenen kod ve
    /// bilinmeyen konfigürasyona bağlı çocuk satır kabul edilmez.</summary>
    [Fact]
    public void Excel_SorunluSatirlarSayfaVeSatirIleBildirilir()
    {
        using var wb = new XLWorkbook(new MemoryStream(EkipmanExcelDosyasi.Yaz(Tipler, [LowbedKaydi()])));
        var konf = wb.Worksheet(EkipmanExcelDosyasi.KonfigurasyonSayfasi);
        konf.Row(3).CopyTo(konf.Row(4));                                // aynı kod iki kez
        konf.Cell(5, 1).Value = "K2"; konf.Cell(5, 2).Value = "Ad"; konf.Cell(5, 3).Value = "k40-hc";
        konf.Cell(5, 4).Value = "Ü"; konf.Cell(5, 5).Value = "M"; konf.Cell(5, 6).Value = "Sahada olculdu";
        konf.Cell(5, 9).Value = 1400.5;                               // ondalıklı mm; dış ölçünün diğer iki değeri yok
        wb.Worksheet(EkipmanExcelDosyasi.DingilSayfasi).Cell(10, 1).Value = "YOK";
        using var akis = new MemoryStream();
        wb.SaveAs(akis);
        akis.Position = 0;

        var okuma = EkipmanExcelDosyasi.Oku(akis);
        Assert.Contains(okuma.Sorunlar, s => s is { Sayfa: EkipmanExcelDosyasi.KonfigurasyonSayfasi, Satir: 4 } && s.Mesaj.Contains("birden fazla"));
        Assert.Contains(okuma.Sorunlar, s => s.Satir == 5 && s.Mesaj.StartsWith("DisUzunlukMm tamsayı", StringComparison.Ordinal));
        Assert.Contains(okuma.Sorunlar, s => s is { Sayfa: EkipmanExcelDosyasi.DingilSayfasi, Satir: 10 } && s.Mesaj.Contains("'YOK'"));
        // "Sahada olculdu" aksansız yazılsa da tanınır; satır 5'in kaynak türü sorunu yoktur.
        Assert.DoesNotContain(okuma.Sorunlar, s => s.Satir == 5 && s.Mesaj.StartsWith("KaynakTuru", StringComparison.Ordinal));

        var bozuk = EkipmanExcelDosyasi.Oku(new MemoryStream([1, 2, 3]));
        Assert.Contains("okunamadı", Assert.Single(bozuk.Sorunlar).Mesaj);
    }

    private static YuklemePlaniKaydetCommand GecerliKayit() => new()
    {
        PlanId = 1,
        BeklenenRevizyonNo = 1,
        Ad = "Wasit sevki",
        CalismaBicimi = YuklemeCalismaBicimi.IhtiyacaGoreArac,
        TasimaAilesiId = 1,
        ProjeKarisimi = YuklemeProjeKarisimi.Karisik,
        ProjeIdleri = [10],
        SandikYukleri = [new PlanSandikYukuDto(5, true, false, true, true, 500m, PlanYukIzni.Yok)],
        ManuelYukler = [new PlanManuelYukDto("M-1", null, (int)YukTuru.Sandik, 1200, 800, 600, 300, 2, 10, null, null,
            [new PlanManuelYukBirimiDto(2, false, false, true, true, 100m, PlanYukIzni.UstYukSiniri)])]
    };

    [Fact]
    public void KaydetDogrulamasi_GecerliKabul_YinelenenSandikVeHataliElleYukRet()
    {
        var dogrulayici = new YuklemePlaniKaydetCommandValidator();
        Assert.True(dogrulayici.Validate(GecerliKayit()).IsValid);

        var yinelenen = GecerliKayit();
        yinelenen.SandikYukleri.Add(yinelenen.SandikYukleri[0]);
        Assert.Contains(dogrulayici.Validate(yinelenen).Errors, e => e.ErrorMessage.Contains("birden fazla"));

        PlanManuelYukDto Manuel(int adet = 2, decimal? boy = 1200, int birimNo = 1) =>
            new("M-1", null, (int)YukTuru.Sandik, boy, 800, 600, 300, adet, null, null, null,
                [new PlanManuelYukBirimiDto(birimNo, true, false, true, true, null, PlanYukIzni.Yok)]);
        foreach (var hatali in new[] { Manuel(adet: 0), Manuel(boy: -1), Manuel(boy: 0), Manuel(birimNo: 3) })
        {
            var istek = GecerliKayit();
            istek.ManuelYukler = [hatali];
            Assert.False(dogrulayici.Validate(istek).IsValid);
        }

        var payli = GecerliKayit();
        payli.Ayarlar = new YuklemePlanAyarlari { YerlesimBosluguMm = 600 };
        Assert.False(dogrulayici.Validate(payli).IsValid);

        var projesiz = new YuklemePlaniOlusturCommand { Ad = "P", TasimaAilesiId = 1, CalismaBicimi = YuklemeCalismaBicimi.IhtiyacaGoreArac,
            ProjeKarisimi = YuklemeProjeKarisimi.Karisik };
        Assert.Contains(new YuklemePlaniOlusturCommandValidator().Validate(projesiz).Errors, e => e.ErrorMessage == "En az bir proje seçilmelidir.");
    }

    /// <summary>Gövdede null gelen listeler 500 değil doğrulama hatası (400) olur.</summary>
    [Fact]
    public void KaydetDogrulamasi_NullListeler_IstisnaYerineHata()
    {
        var dogrulayici = new YuklemePlaniKaydetCommandValidator();
        var istek = GecerliKayit();
        istek.ProjeIdleri = null!;
        istek.Ayarlar = null!;
        istek.SandikYukleri = null!;
        istek.ManuelYukler = null!;

        var sonuc = dogrulayici.Validate(istek);
        Assert.False(sonuc.IsValid);
        Assert.Contains(sonuc.Errors, e => e.ErrorMessage == "En az bir proje seçilmelidir.");
        Assert.Contains(sonuc.Errors, e => e.ErrorMessage == "Plan ayarları eksik.");
        Assert.Contains(sonuc.Errors, e => e.ErrorMessage == "Sandık yük listesi eksik.");
        Assert.Contains(sonuc.Errors, e => e.ErrorMessage == "Elle eklenen yük listesi eksik.");

        // Mevcut araç listesi: null ve aynı konfigürasyonun iki kez geçmesi reddedilir.
        var nullAraclar = GecerliKayit();
        nullAraclar.Ayarlar = new YuklemePlanAyarlari { MevcutAraclar = null! };
        var yinelenenArac = GecerliKayit();
        yinelenenArac.Ayarlar = new YuklemePlanAyarlari { MevcutAraclar = [new PlanMevcutArac(5, 1), new PlanMevcutArac(5, 2)] };
        foreach (var istek2 in new[] { nullAraclar, yinelenenArac })
            Assert.Contains(dogrulayici.Validate(istek2).Errors, e => e.ErrorMessage == YuklemePlaniKurali.MevcutAraclarMesaji);
        var gecerliArac = GecerliKayit();
        gecerliArac.Ayarlar = new YuklemePlanAyarlari { MevcutAraclar = [new PlanMevcutArac(5, 2)] };
        Assert.True(dogrulayici.Validate(gecerliArac).IsValid);
    }

    /// <summary>"Elle değişti" bayrakları yalnız tanımlı izinlerden oluşur; tanımsız bit reddedilir.</summary>
    [Fact]
    public void ElleDegisenIzinler_TanimsizBitReddedilir()
    {
        Assert.True(YukIzinKurali.ElleDegisenGecerli(PlanYukIzni.Yok));
        Assert.True(YukIzinKurali.ElleDegisenGecerli(YukIzinKurali.TumIzinler));
        Assert.False(YukIzinKurali.ElleDegisenGecerli((PlanYukIzni)255));

        var istek = GecerliKayit();
        istek.SandikYukleri = [new PlanSandikYukuDto(5, true, false, true, true, 500m, (PlanYukIzni)16)];
        Assert.Contains(new YuklemePlaniKaydetCommandValidator().Validate(istek).Errors, e => e.ErrorMessage == YukIzinKurali.ElleDegisenMesaji);
    }

    /// <summary>Konfigürasyon kodu büyük harfe çevrilir; dosyada küçük harfle yazılan kod mevcut kaydın revizyonu olur.</summary>
    [Fact]
    public void Excel_KodBuyukHarfeCevrilir_CocukSatirlarEslesir()
    {
        using var wb = new XLWorkbook(new MemoryStream(EkipmanExcelDosyasi.Yaz(Tipler, [LowbedKaydi()])));
        wb.Worksheet(EkipmanExcelDosyasi.KonfigurasyonSayfasi).Cell(3, 1).Value = " lb-3d ";
        wb.Worksheet(EkipmanExcelDosyasi.DingilSayfasi).Cell(3, 1).Value = "Lb-3D";
        using var akis = new MemoryStream();
        wb.SaveAs(akis);
        akis.Position = 0;

        var okuma = EkipmanExcelDosyasi.Oku(akis);
        Assert.Empty(okuma.Sorunlar);
        var kayit = Assert.Single(okuma.Kayitlar);
        Assert.Equal("LB-3D", kayit.Kod);
        Assert.Equal(LowbedKaydi().TeknikVeri.Dingiller.Count, kayit.TeknikVeri.Dingiller.Count);
        Assert.Equal("LB-3D", EkipmanKitapligiKurali.KodNormalize("lb-3d"));
    }

    /// <summary>Uzaktaki tek dolu hücre bütün sayfanın taranmasına yol açmaz: sınır satırlar okunmadan denetlenir.</summary>
    [Fact]
    public void Excel_SatirSiniriAsilirsaSayfaOkunmaz()
    {
        using var wb = new XLWorkbook(new MemoryStream(EkipmanExcelDosyasi.Yaz(Tipler, [LowbedKaydi()])));
        wb.Worksheet(EkipmanExcelDosyasi.EngelSayfasi).Cell(EkipmanKitapligiKurali.SayfaEnFazlaSatir + 10, 1).Value = "LB-3D";
        using var akis = new MemoryStream();
        wb.SaveAs(akis);
        akis.Position = 0;

        var okuma = EkipmanExcelDosyasi.Oku(akis);
        Assert.Contains(okuma.Sorunlar, s => s.Sayfa == EkipmanExcelDosyasi.EngelSayfasi && s.Mesaj.Contains("en fazla"));
    }

    /// <summary>Üst sınırsız koordinat int taşmasıyla dış ölçü kontrolünü geçemez.</summary>
    [Fact]
    public void TeknikVeriDenetimi_SinirDisiKoordinatHatadir()
    {
        var veri = KapaliKonteyner() with
        {
            Yuzeyler = [new YukYuzeyi("ANA", "Taban", int.MaxValue - 100, 12_000, 2_350, 0, 2_690, null, null)],
            Dingiller = [new Dingil(70_000, DingilTuru.Tahriksiz, 1, null)]
        };
        var sonuc = EkipmanTeknikVeriDenetimi.Denetle(veri, KapaliGovde);
        Assert.Contains(sonuc.Hatalar, h => h.StartsWith("Yüzey 'Taban': başlangıç", StringComparison.Ordinal) || h.StartsWith("Yüzey 'ANA': başlangıç", StringComparison.Ordinal));
        Assert.Contains(sonuc.Hatalar, h => h.Contains("dış ölçülerin dışına"));
        Assert.Contains(sonuc.Hatalar, h => h.StartsWith("Dingil (X=70000", StringComparison.Ordinal));
    }

    /// <summary>Sahaya aktarılan yeni sandık, ürünlerinin hepsi aynı yük türündeki sandıklardan geliyorsa o türü korur.</summary>
    [Fact]
    public void SahaAktarimi_OrtakYukTuruKorunur_KarisikVeyaKaynaksizSandik()
    {
        var turler = new Dictionary<int, int> { [1] = (int)YukTuru.Trafo, [2] = (int)YukTuru.Trafo, [3] = (int)YukTuru.Sandik };
        EksikSahaUrunDto Urun(int? kaynak) => new() { CekiSatiriId = 1, KaynakSandikId = kaynak, Miktar = 1 };

        Assert.Equal((int)YukTuru.Trafo, SahaAktarimIslemi.OrtakYukTuru([Urun(1), Urun(2)], turler));
        Assert.Equal((int)YukTuru.Sandik, SahaAktarimIslemi.OrtakYukTuru([Urun(1), Urun(3)], turler));
        Assert.Equal((int)YukTuru.Sandik, SahaAktarimIslemi.OrtakYukTuru([Urun(1), Urun(null)], turler));
        Assert.Equal((int)YukTuru.Sandik, SahaAktarimIslemi.OrtakYukTuru([], turler));
    }

    /// <summary>Sandık özellik güncellemesinde yük türü isteğe bağlıdır; verilirse tanımlı türlerden biri olmalıdır.</summary>
    [Fact]
    public void SandikOzellikGuncelle_YukTuruIstegeBagliVeTanimliOlmali()
    {
        var dogrulayici = new SandikOzellikGuncelleCommandValidator();
        SandikOzellikGuncelleCommand Istek(int? yukTuru) => new() { SandikId = 1, TipId = (int)SandikTipi.AhsapKapali, YukTuruId = yukTuru };
        Assert.True(dogrulayici.Validate(Istek(null)).IsValid);
        Assert.True(dogrulayici.Validate(Istek((int)YukTuru.Trafo)).IsValid);
        Assert.Equal(YukTuruKurali.Mesaj, Assert.Single(dogrulayici.Validate(Istek(9)).Errors).ErrorMessage);
    }

    /// <summary>Her istek kendi izin kodunu ve seviyesini bildirir; sunucuda tanımlıdır.</summary>
    [Fact]
    public async Task Yetki_HerIstekDogruKodVeSeviye_IzinsizKullanici403()
    {
        (object Istek, string Kod, YetkiTipi Tip)[] beklenen =
        [
            (new YuklemePlaniTanimlariQuery(), YetkiKodlari.YuklemePlanlama.Planlar, YetkiTipi.R),
            (new PlanProjeAdaylariQuery(), YetkiKodlari.YuklemePlanlama.Planlar, YetkiTipi.R),
            (new YuklemePlanlariQuery(), YetkiKodlari.YuklemePlanlama.Planlar, YetkiTipi.R),
            (new YuklemePlaniGetirQuery(), YetkiKodlari.YuklemePlanlama.Planlar, YetkiTipi.R),
            (new YuklemePlaniOlusturCommand(), YetkiKodlari.YuklemePlanlama.Planlar, YetkiTipi.W),
            (new YuklemePlaniKaydetCommand(), YetkiKodlari.YuklemePlanlama.Planlar, YetkiTipi.W),
            (new YuklemePlaniKaynaktanGuncelleCommand(), YetkiKodlari.YuklemePlanlama.Planlar, YetkiTipi.W),
            (new YuklemePlaniHesaplaCommand(), YetkiKodlari.YuklemePlanlama.Planlar, YetkiTipi.W),
            (new YuklemePlaniPlanHesaplaCommand(), YetkiKodlari.YuklemePlanlama.Planlar, YetkiTipi.W),
            (new YuklemePlaniAracTalepRaporuQuery(), YetkiKodlari.YuklemePlanlama.Planlar, YetkiTipi.R),
            (new YuklemePlaniElleDenetleQuery(), YetkiKodlari.YuklemePlanlama.Planlar, YetkiTipi.W),
            (new YuklemePlaniElleKaydetCommand(), YetkiKodlari.YuklemePlanlama.Planlar, YetkiTipi.W),
            (new YuklemePlaniElleTamamlaQuery(), YetkiKodlari.YuklemePlanlama.Planlar, YetkiTipi.W),
            (new YuklemePlaniElleEkipmanlariQuery(), YetkiKodlari.YuklemePlanlama.Planlar, YetkiTipi.W),
            (new YuklemePlaniYuklemeRaporuQuery(), YetkiKodlari.YuklemePlanlama.Planlar, YetkiTipi.R),
            (new EkipmanAileleriQuery(), YetkiKodlari.YuklemePlanlama.EkipmanKitapligi, YetkiTipi.R),
            (new EkipmanKonfigurasyonlariQuery(), YetkiKodlari.YuklemePlanlama.EkipmanKitapligi, YetkiTipi.R),
            (new EkipmanKonfigurasyonGetirQuery(), YetkiKodlari.YuklemePlanlama.EkipmanKitapligi, YetkiTipi.R),
            (new EkipmanSablonuQuery(), YetkiKodlari.YuklemePlanlama.EkipmanKitapligi, YetkiTipi.R),
            (new EkipmanDisaAktarQuery(), YetkiKodlari.YuklemePlanlama.EkipmanKitapligi, YetkiTipi.R),
            (new EkipmanIceAktarOnizleQuery(), YetkiKodlari.YuklemePlanlama.EkipmanKitapligi, YetkiTipi.W),
            (new EkipmanIceAktarCommand(), YetkiKodlari.YuklemePlanlama.EkipmanKitapligi, YetkiTipi.W),
            (new EkipmanDogrulaCommand(), YetkiKodlari.YuklemePlanlama.EkipmanDogrulama, YetkiTipi.W),
            (new MevzuatKuralSetleriQuery(), YetkiKodlari.YuklemePlanlama.MevzuatKurallari, YetkiTipi.R),
            (new MevzuatKuralSetiGetirQuery(), YetkiKodlari.YuklemePlanlama.MevzuatKurallari, YetkiTipi.R)
        ];
        foreach (var (istek, kod, tip) in beklenen)
        {
            var izin = Assert.IsAssignableFrom<IRequiresMenuPermission>(istek);
            Assert.Equal((kod, tip), (izin.RequiredMenuKod, izin.RequiredYetkiTipi));
            Assert.True(RequestMenuPermissionResolver.HasServerDefinition(istek));
        }

        async Task<bool> GecerMi(OrtakRoller roller)
        {
            var gecti = false;
            var sonuc = await new AuthorizationBehavior<YuklemePlaniKaydetCommand, Result<YuklemePlaniDetayi>>(new OrtakUser(), roller)
                .Handle(GecerliKayit(), () => { gecti = true; return Task.FromResult(Result<YuklemePlaniDetayi>.Success(new YuklemePlaniDetayi())); }, CancellationToken.None);
            if (!gecti) Assert.Equal(403, sonuc.StatusCode);
            return gecti;
        }
        Assert.True(await GecerMi(OrtakRoller.Yalniz(YetkiKodlari.YuklemePlanlama.Planlar)));
        // Ekipman kitaplığı ve mevzuat izinleri plan kaydetmeye yetmez.
        Assert.False(await GecerMi(OrtakRoller.Haric(YetkiKodlari.YuklemePlanlama.Planlar)));
    }
}
