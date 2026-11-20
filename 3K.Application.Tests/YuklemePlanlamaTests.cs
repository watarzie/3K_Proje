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
        pay.Ayarlar = new YuklemePlanAyarlari { YerlesimBosluguMm = YuklemePlaniKurali.YerlesimBosluguEnFazlaMm + 1 };
        Assert.False(dogrulayici.Validate(pay).IsValid);
        var tip = Istek();
        tip.Ayarlar = new YuklemePlanAyarlari { TipIdleri = [3, 3] };
        Assert.False(dogrulayici.Validate(tip).IsValid);
    }

    /// <summary>
    /// Tek adımlı hesapta yükleme yeri tercihi ve sandık seçimleri (saha isteği, 20 Kasım 2026): ikisi de isteğe bağlıdır;
    /// tanımsız tercih, yinelenen sandık, geçersiz üst yük sınırı ya da "elle değişti" bayrağı ve boş öge 400 olur. Sandık
    /// kuralları kaydetme isteğiyle aynıdır.
    /// </summary>
    [Fact]
    public void PlanHesapla_LokasyonTercihiVeSandikSecimleriDogrulamasi()
    {
        var dogrulayici = new YuklemePlaniPlanHesaplaCommandValidator();
        YuklemePlaniPlanHesaplaCommand Istek() => new() { ProjeIdleri = [1], TasimaAilesiId = 1 };
        PlanSandikYukuDto Secim(int sandikId, decimal? ustYuk = 500m, PlanYukIzni elle = PlanYukIzni.Yok) =>
            new(sandikId, true, false, true, true, ustYuk, elle);

        foreach (var tercih in new YuklemeLokasyonTercihi?[] { null, YuklemeLokasyonTercihi.HenuzSecilmedi, YuklemeLokasyonTercihi.LokasyonaGoreAyri, YuklemeLokasyonTercihi.Karisik })
        {
            var istek = Istek();
            istek.LokasyonTercihi = tercih;
            Assert.True(dogrulayici.Validate(istek).IsValid);
        }
        foreach (var tanimsiz in new[] { 0, 4, -1 })
        {
            var istek = Istek();
            istek.LokasyonTercihi = (YuklemeLokasyonTercihi)tanimsiz;
            Assert.Equal([YuklemePlaniKurali.LokasyonTercihiMesaji], dogrulayici.Validate(istek).Errors.Select(e => e.ErrorMessage));
        }

        // Gönderilmeyen ve boş liste geçerlidir (bütün sandıklar dahil, varsayılan izinli).
        var bos = Istek();
        bos.SandikYukleri = [];
        Assert.True(dogrulayici.Validate(bos).IsValid);
        var secimli = Istek();
        secimli.SandikYukleri = [Secim(5), Secim(6, null, PlanYukIzni.UstYukSiniri)];
        Assert.True(dogrulayici.Validate(secimli).IsValid);
        Assert.Equal([5, 6], secimli.Model().SandikYukleri!.Select(y => y.SandikId));
        Assert.Null(Istek().Model().SandikYukleri);

        var yinelenen = Istek();
        yinelenen.SandikYukleri = [Secim(5), Secim(6), Secim(5)];
        Assert.Contains(dogrulayici.Validate(yinelenen).Errors, e => e.ErrorMessage == YuklemePlaniKurali.SandikTekrariMesaji);

        (PlanSandikYukuDto Secim, string Mesaj)[] hatalilar =
        [
            (Secim(0), "Sandık kimliği geçersiz."),
            (Secim(5, -1), YukIzinKurali.UstYukSiniriMesaji),
            (Secim(5, 100m, (PlanYukIzni)16), YukIzinKurali.ElleDegisenMesaji)
        ];
        foreach (var (secim, mesaj) in hatalilar)
        {
            var istek = Istek();
            istek.SandikYukleri = [secim];
            Assert.Equal([mesaj], dogrulayici.Validate(istek).Errors.Select(e => e.ErrorMessage));
            // Aynı kural kaydetme isteğinde de aynı mesajı verir (tek doğrulayıcı).
            var kayit = GecerliKayit();
            kayit.SandikYukleri = [secim];
            Assert.Contains(new YuklemePlaniKaydetCommandValidator().Validate(kayit).Errors, e => e.ErrorMessage == mesaj);
        }

        // Gövdede null öge 500 değil doğrulama hatasıdır.
        var bosOge = Istek();
        bosOge.SandikYukleri = [null!];
        Assert.False(dogrulayici.Validate(bosOge).IsValid);
    }

    /// <summary>Gönderilmeyen ya da "henüz seçilmedi" tercih tek yerden (karışık) hesaplanır; yalnız açık seçim araçları lokasyona böler.</summary>
    [Theory]
    [InlineData(null, YuklemeLokasyonTercihi.Karisik)]
    [InlineData(YuklemeLokasyonTercihi.HenuzSecilmedi, YuklemeLokasyonTercihi.Karisik)]
    [InlineData(YuklemeLokasyonTercihi.Karisik, YuklemeLokasyonTercihi.Karisik)]
    [InlineData(YuklemeLokasyonTercihi.LokasyonaGoreAyri, YuklemeLokasyonTercihi.LokasyonaGoreAyri)]
    public void HesapLokasyonTercihi_BosVeSecilmemisTekYerden(YuklemeLokasyonTercihi? gelen, YuklemeLokasyonTercihi uygulanan) =>
        Assert.Equal(uygulanan, YuklemePlaniKurali.HesapLokasyonTercihi(gelen));

    /// <summary>
    /// Farklı lokasyon sayısı (plan detayı ve yük önizlemesi aynı hesap): yalnız dahil ve verisi tam yükler sayılır;
    /// lokasyonsuz ve depo lokasyonu "Belirsiz" olan yükler tek "belirsiz" lokasyondur.
    /// </summary>
    [Fact]
    public void FarkliLokasyonSayisi_DahilVeVerisiTamYukler_BelirsizlerTekLokasyon()
    {
        static PlanYukSatiri Satir(int? lokasyon, bool dahil = true, bool eksik = false) =>
            new() { LokasyonId = lokasyon, Dahil = dahil, VeriSorunlari = eksik ? ["Ölçü giriniz."] : [] };
        Assert.Equal(0, YuklemePlaniKurali.FarkliLokasyonSayisi([]));
        Assert.Equal(1, YuklemePlaniKurali.FarkliLokasyonSayisi([Satir((int)DepoLokasyon.UcK), Satir((int)DepoLokasyon.UcK)]));
        Assert.Equal(2, YuklemePlaniKurali.FarkliLokasyonSayisi([Satir((int)DepoLokasyon.UcK), Satir((int)DepoLokasyon.Seymen)]));
        Assert.Equal(2, YuklemePlaniKurali.FarkliLokasyonSayisi([Satir((int)DepoLokasyon.UcK), Satir(null), Satir((int)DepoLokasyon.Belirsiz)]));
        // Hariç tutulan ve verisi eksik yük lokasyon sayısını artırmaz.
        Assert.Equal(1, YuklemePlaniKurali.FarkliLokasyonSayisi(
            [Satir((int)DepoLokasyon.UcK), Satir((int)DepoLokasyon.Seymen, dahil: false), Satir(null, eksik: true)]));
    }

    /// <summary>Yük önizlemesi: proje seçimi tek adımlı hesapla aynı kuraldır (boş, sınır üstü ve geçersiz kimlik 400).</summary>
    [Fact]
    public void YukOnizleme_ProjeSecimiHesaplaIleAyniKural()
    {
        var onizleme = new PlanYukOnizlemeQueryValidator();
        var hesap = new YuklemePlaniPlanHesaplaCommandValidator();
        List<int>?[] secimler = [[1, 2], [], null, [0], Enumerable.Range(1, YuklemePlaniKurali.EnFazlaProje + 1).ToList(), [3, 3]];
        foreach (var secim in secimler)
        {
            var onizlemeHatalari = onizleme.Validate(new PlanYukOnizlemeQuery { ProjeIdleri = secim! }).Errors.Select(e => e.ErrorMessage).ToList();
            var hesapHatalari = hesap.Validate(new YuklemePlaniPlanHesaplaCommand { ProjeIdleri = secim!, TasimaAilesiId = 1 }).Errors.Select(e => e.ErrorMessage).ToList();
            Assert.Equal(hesapHatalari, onizlemeHatalari);
        }
        Assert.True(onizleme.Validate(new PlanYukOnizlemeQuery { ProjeIdleri = [1, 2] }).IsValid);
        Assert.Equal(["En az bir proje seçilmelidir."], onizleme.Validate(new PlanYukOnizlemeQuery()).Errors.Select(e => e.ErrorMessage));
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

    /// <summary>Yükler arası boşluk isteğe bağlıdır: girilmezse 3 cm uygulanır; 0–500 mm geçerlidir (güvenlik payları kaldırıldı).</summary>
    [Fact]
    public void Ayarlar_VarsayilanUcSantim_SinirDisiGecersiz()
    {
        var ayarlar = new YuklemePlanAyarlari();
        Assert.Equal((null, 30), (ayarlar.YerlesimBosluguMm, YuklemePlaniKurali.UygulananYerlesimBoslugu(ayarlar)));
        Assert.Equal(0, YuklemePlaniKurali.UygulananYerlesimBoslugu(ayarlar with { YerlesimBosluguMm = 0 }));
        Assert.True(YuklemePlaniKurali.AyarlarGecerli(ayarlar));
        Assert.True(YuklemePlaniKurali.AyarlarGecerli(ayarlar with { YerlesimBosluguMm = 0 }));
        Assert.True(YuklemePlaniKurali.AyarlarGecerli(ayarlar with { YerlesimBosluguMm = YuklemePlaniKurali.YerlesimBosluguEnFazlaMm }));
        Assert.False(YuklemePlaniKurali.AyarlarGecerli(ayarlar with { YerlesimBosluguMm = -1 }));
        Assert.False(YuklemePlaniKurali.AyarlarGecerli(ayarlar with { YerlesimBosluguMm = YuklemePlaniKurali.YerlesimBosluguEnFazlaMm + 1 }));
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

    /// <summary>
    /// Ekrandan gelen teknik veri Excel'in alt sayfalarıyla aynı sonucu verir: listeler kanonik sıraya dizilir, metin
    /// kırpılır ve boş metin "yok" olur; böylece aynı bilgi iki yoldan aynı JSON'u (aynı revizyon kararını) üretir.
    /// </summary>
    [Fact]
    public void EkranTeknikVerisi_ExcelIleAyniKanonikVeri()
    {
        var ornek = LowbedKaydi().TeknikVeri;
        var karisik = ornek with
        {
            Yuzeyler = [ornek.Yuzeyler[1] with { Kod = " HAVUZ " }, ornek.Yuzeyler[0] with { Ad = " Boyun " }],
            Dingiller = ornek.Dingiller.Reverse().ToList(),
            Engeller = [ornek.Engeller[0] with { Aciklama = "  Arka dingil üstü " }]
        };

        var girdi = EkipmanTeknikVeriGirdisi.Oku(EkipmanTeknikVeriSerilestirici.Yaz(karisik));
        Assert.Empty(girdi.Sorunlar);
        Assert.Equal(EkipmanTeknikVeriSerilestirici.Yaz(ornek), EkipmanTeknikVeriSerilestirici.Yaz(girdi.Veri!));

        // Boş metin "yok" olur; "sokulebilir" verilmezse hayır sayılır (Excel'de boş hücre); kök alanların hepsi isteğe bağlıdır.
        var bos = EkipmanTeknikVeriGirdisi.Oku(
            "{\"yuzeyler\":[{\"kod\":\"ANA\",\"ad\":\"  \",\"baslangicXMm\":0,\"uzunlukMm\":6000,\"genislikMm\":2400,\"yerdenYukseklikMm\":1200}]," +
            "\"engeller\":[{\"tur\":\"dikme\",\"xMm\":0,\"yMm\":0,\"zMm\":0,\"uzunlukMm\":100,\"genislikMm\":100,\"yukseklikMm\":100,\"aciklama\":\"\"}]}");
        Assert.Empty(bos.Sorunlar);
        Assert.Null(Assert.Single(bos.Veri!.Yuzeyler).Ad);
        Assert.Equal((false, null), (Assert.Single(bos.Veri.Engeller).Sokulebilir, bos.Veri.Engeller[0].Aciklama));
        Assert.NotNull(EkipmanTeknikVeriGirdisi.Oku("{}").Veri);

        // Yazılan her enum adı katı okumada da tanınır (ayrıntıdaki JSON aynen geri gönderilebilir).
        var tumEnumlar = KapaliKonteyner() with
        {
            Acikliklar = Enum.GetValues<AciklikTarafi>().Select(t => new Aciklik(t, 0, 1_000, 1_000, 1_000)).ToList(),
            Engeller = Enum.GetValues<EngelTuru>().Select((t, i) => new Engel(t, i, 0, 0, 10, 10, 10, false, null)).ToList(),
            Dingiller = Enum.GetValues<DingilTuru>().Select((t, i) => new Dingil(i, t, 1, null)).ToList()
        };
        var enumlu = EkipmanTeknikVeriGirdisi.Oku(EkipmanTeknikVeriSerilestirici.Yaz(tumEnumlar));
        Assert.Empty(enumlu.Sorunlar);
        Assert.Equal(EkipmanTeknikVeriSerilestirici.Yaz(EkipmanTeknikVeriSerilestirici.KanonikSira(tumEnumlar)),
            EkipmanTeknikVeriSerilestirici.Yaz(enumlu.Veri!));

        // Metin sınırları Excel sütunlarıyla aynıdır.
        var uzun = EkipmanTeknikVeriGirdisi.Oku(EkipmanTeknikVeriSerilestirici.Yaz(ornek with
        {
            Yuzeyler = [ornek.Yuzeyler[0] with { Kod = new string('K', EkipmanKitapligiKurali.KodEnFazla + 1) },
                ornek.Yuzeyler[1] with { Ad = new string('A', EkipmanKitapligiKurali.AdEnFazla + 1) }],
            Engeller = [ornek.Engeller[0] with { Aciklama = new string('E', EkipmanKitapligiKurali.AciklamaEnFazla + 1) }]
        }));
        Assert.Equal(3, uzun.Sorunlar.Count);
        Assert.Contains(uzun.Sorunlar, s => s.StartsWith("Yüzey kodu en fazla", StringComparison.Ordinal));
        Assert.Contains(uzun.Sorunlar, s => s.StartsWith("Yüzey 'HAVUZ': ad en fazla", StringComparison.Ordinal));
        Assert.Contains(uzun.Sorunlar, s => s.StartsWith("Engel açıklaması en fazla", StringComparison.Ordinal));
    }

    /// <summary>
    /// Ekrandan gelen teknik veri katı okunur: bilinmeyen ya da yinelenen alan, yanlış tür ve alt kayıtta eksik zorunlu
    /// alan reddedilir (eksik sayı sıfır sayılmaz); mesaj alanın yolunu söyler, iç ayrıntı taşımaz.
    /// </summary>
    [Theory]
    [InlineData("{\"bilinmeyen\":1}", "bilinmeyen")]
    [InlineData("{\"daraKg\":1,\"daraKg\":2}", "daraKg")]
    [InlineData("{\"daraKg\":12.5}", "daraKg")]
    [InlineData("{\"yuzeyler\":null}", "yuzeyler")]
    [InlineData("{\"yuzeyler\":[{\"kod\":\"A\",\"uzunlukMm\":6000,\"genislikMm\":2400,\"yerdenYukseklikMm\":0}]}", "yuzeyler[0]")]
    [InlineData("{\"yuzeyler\":[{\"baslangicXMm\":0,\"uzunlukMm\":6000,\"genislikMm\":2400,\"yerdenYukseklikMm\":0}]}", "yuzeyler[0]")]
    [InlineData("{\"yuzeyler\":[{\"kod\":null,\"baslangicXMm\":0,\"uzunlukMm\":6000,\"genislikMm\":2400,\"yerdenYukseklikMm\":0}]}", "yuzeyler[0]")]
    [InlineData("{\"disOlcu\":{\"uzunlukMm\":12000}}", "disOlcu")]
    [InlineData("{\"kingPin\":{\"azamiBaglantiYukuKg\":18000}}", "kingPin")]
    [InlineData("{\"acikliklar\":[{\"taraf\":1}]}", "acikliklar[0]")]
    [InlineData("{\"acikliklar\":[{\"taraf\":\"yok\"}]}", "acikliklar[0]")]
    [InlineData("{\"dingiller\":[{\"xMm\":100,\"tur\":\"tahriksiz\"}]}", "dingiller[0]")]
    [InlineData("{\"baglamaNoktalari\":[{\"xMm\":1,\"yMm\":1}]}", "baglamaNoktalari[0]")]
    // Enum yalnız tanımlı adıyla kabul edilir: virgüllü birleşim ve sayı (metin ya da sayı olarak, tanımlı olsa da)
    // reddedilir; etiketi olmayan değer Excel'e yazılırken 500 üretmesin.
    [InlineData("{\"engeller\":[{\"tur\":\"travers, kapak\",\"xMm\":0,\"yMm\":0,\"zMm\":0,\"uzunlukMm\":100,\"genislikMm\":100,\"yukseklikMm\":100}]}", "engeller[0].tur")]
    [InlineData("{\"engeller\":[{\"tur\":\"7\",\"xMm\":0,\"yMm\":0,\"zMm\":0,\"uzunlukMm\":100,\"genislikMm\":100,\"yukseklikMm\":100}]}", "engeller[0].tur")]
    [InlineData("{\"engeller\":[{\"tur\":7,\"xMm\":0,\"yMm\":0,\"zMm\":0,\"uzunlukMm\":100,\"genislikMm\":100,\"yukseklikMm\":100}]}", "engeller[0].tur")]
    [InlineData("{\"dingiller\":[{\"xMm\":100,\"tur\":\"2\",\"grupNo\":1}]}", "dingiller[0].tur")]
    [InlineData("{\"acikliklar\":[{\"taraf\":\"arka, sol\",\"genislikMm\":2340,\"yukseklikMm\":2580}]}", "acikliklar[0].taraf")]
    public void EkranTeknikVerisi_SemaDisiJsonReddedilir(string json, string yol)
    {
        var girdi = EkipmanTeknikVeriGirdisi.Oku(json);
        Assert.Null(girdi.Veri);
        var sorun = Assert.Single(girdi.Sorunlar);
        Assert.StartsWith($"Teknik veri okunamadı: '{yol}", sorun, StringComparison.Ordinal);
        Assert.DoesNotContain("_3K", sorun, StringComparison.Ordinal);
        Assert.DoesNotContain("System.", sorun, StringComparison.Ordinal);
    }

    /// <summary>JSON nesnesi olmayan girdi ve liste içindeki boş kayıt da okunmaz; hiçbir durumda istisna çıkmaz.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{bozuk")]
    [InlineData("{\"yuzeyler\":[null]}")]
    public void EkranTeknikVerisi_NesneOlmayanGirdiReddedilir(string? json)
    {
        var girdi = EkipmanTeknikVeriGirdisi.Oku(json);
        Assert.Null(girdi.Veri);
        Assert.Contains(Assert.Single(girdi.Sorunlar), new[] { EkipmanTeknikVeriGirdisi.OkunamadiMesaji, EkipmanTeknikVeriGirdisi.BosKayitMesaji });
    }

    private static EkipmanKonfigurasyonGuncelleCommand GecerliEkipmanKaydi() => new()
    {
        KonfigurasyonId = 5,
        BeklenenRevizyonNo = 1,
        Kod = "LB-3D",
        Ad = "3 dingil lowbed",
        TipId = 21,
        Uretici = "Üretici A",
        ModelAdi = "LB 3",
        KaynakTuru = EkipmanKaynakTuru.UreticiFoyu,
        TeknikVeriJson = "{}"
    };

    /// <summary>
    /// Ekrandan kaydın alan kuralları Excel'deki konfigürasyon satırıyla aynıdır: kod, ad, tip, üretici, model ve kaynak
    /// türü zorunlu; sınırlar <see cref="EkipmanKitapligiKurali"/>'ndan ve kırpılmış metne göre.
    /// </summary>
    [Fact]
    public void EkipmanKaydiDogrulamasi_ZorunluAlanlarVeSinirlar()
    {
        var dogrulayici = new EkipmanKonfigurasyonGuncelleCommandValidator();
        Assert.True(dogrulayici.Validate(GecerliEkipmanKaydi()).IsValid);

        // Sınır kırpılmış metne uygulanır: baştaki/sondaki boşluk sınırı aşırmaz.
        var sinirda = GecerliEkipmanKaydi();
        sinirda.Kod = "  " + new string('K', EkipmanKitapligiKurali.KodEnFazla) + "  ";
        sinirda.Ad = new string('A', EkipmanKitapligiKurali.AdEnFazla);
        sinirda.Uretici = new string('U', EkipmanKitapligiKurali.UreticiEnFazla);
        sinirda.ModelAdi = new string('M', EkipmanKitapligiKurali.ModelEnFazla);
        sinirda.KaynakAciklama = new string('B', EkipmanKitapligiKurali.KaynakAciklamaEnFazla);
        sinirda.Aciklama = new string('C', EkipmanKitapligiKurali.AciklamaEnFazla);
        sinirda.KaynakTarihi = new DateTime(2026, 10, 1);
        Assert.True(dogrulayici.Validate(sinirda).IsValid);

        (Action<EkipmanKonfigurasyonGuncelleCommand> Boz, string Mesaj)[] hatalilar =
        [
            (k => k.Kod = "   ", "Ekipman kodu zorunludur"),
            (k => k.Kod = null!, "Ekipman kodu zorunludur"),
            (k => k.Kod = new string('K', EkipmanKitapligiKurali.KodEnFazla + 1), "Ekipman kodu zorunludur"),
            (k => k.Ad = "", "Ekipman adı zorunludur"),
            (k => k.Ad = new string('A', EkipmanKitapligiKurali.AdEnFazla + 1), "Ekipman adı zorunludur"),
            (k => k.TipId = 0, "Ekipman tipi seçilmelidir."),
            (k => k.Uretici = " ", "Üretici zorunludur"),
            (k => k.Uretici = new string('U', EkipmanKitapligiKurali.UreticiEnFazla + 1), "Üretici zorunludur"),
            (k => k.ModelAdi = null!, "Model adı zorunludur"),
            (k => k.ModelAdi = new string('M', EkipmanKitapligiKurali.ModelEnFazla + 1), "Model adı zorunludur"),
            (k => k.KaynakTuru = 0, "Kaynak türü geçersiz."),
            (k => k.KaynakTuru = (EkipmanKaynakTuru)6, "Kaynak türü geçersiz."),
            (k => k.KaynakAciklama = new string('B', EkipmanKitapligiKurali.KaynakAciklamaEnFazla + 1), "Kaynak açıklaması en fazla"),
            (k => k.Aciklama = new string('C', EkipmanKitapligiKurali.AciklamaEnFazla + 1), "Açıklama en fazla"),
            (k => k.KaynakTarihi = new DateTime(2026, 10, 1, 10, 30, 0), EkipmanKitapligiKurali.KaynakTarihiMesaji),
            (k => k.KaynakTarihi = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), EkipmanKitapligiKurali.KaynakTarihiMesaji),
            (k => k.KaynakTarihi = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Local), EkipmanKitapligiKurali.KaynakTarihiMesaji),
            (k => k.TeknikVeriJson = null!, "Teknik veri zorunludur."),
            (k => k.TeknikVeriJson = " ", "Teknik veri zorunludur."),
            (k => k.TeknikVeriJson = new string(' ', EkipmanKitapligiKurali.TeknikVeriJsonEnFazla) + "{}", "Teknik veri çok büyük."),
            (k => k.BeklenenRevizyonNo = 0, "Ekipmanın revizyon numarası eksik"),
            (k => k.KonfigurasyonId = 0, "Konfigürasyon seçilmelidir.")
        ];
        foreach (var (boz, mesaj) in hatalilar)
        {
            var istek = GecerliEkipmanKaydi();
            boz(istek);
            Assert.StartsWith(mesaj, Assert.Single(dogrulayici.Validate(istek).Errors).ErrorMessage, StringComparison.Ordinal);
        }

        // Ekleme aynı alan kurallarını kullanır; revizyon numarası istemez.
        var ekleme = new EkipmanKonfigurasyonEkleCommandValidator();
        EkipmanKonfigurasyonEkleCommand Ekle(string kod) => new()
        {
            Kod = kod, Ad = "Ad", TipId = 21, Uretici = "Ü", ModelAdi = "M", KaynakTuru = EkipmanKaynakTuru.Diger, TeknikVeriJson = "{}"
        };
        Assert.True(ekleme.Validate(Ekle("LB-3D")).IsValid);
        Assert.StartsWith("Ekipman kodu zorunludur", Assert.Single(ekleme.Validate(Ekle(" ")).Errors).ErrorMessage, StringComparison.Ordinal);

        // İstek servise olduğu gibi aktarılır (kırpma ve kod normalizasyonu servisindir, içe aktarımla aynı yerde).
        var kayit = GecerliEkipmanKaydi().Kayit();
        Assert.Equal(("LB-3D", "3 dingil lowbed", 21, "Üretici A", "LB 3", EkipmanKaynakTuru.UreticiFoyu, "{}"),
            (kayit.Kod, kayit.Ad, kayit.TipId, kayit.Uretici, kayit.ModelAdi, kayit.KaynakTuru, kayit.TeknikVeriJson));
    }

    /// <summary>
    /// Kaynak tarihi saatsiz gündür: gövdede yalnız gün (ya da ayrıntıdaki gibi dilimsiz gece yarısı) kabul edilir ve aynı
    /// gün olarak okunur. Saat ya da saat dilimi taşıyan değer (ör. tarayıcının UTC'ye çevirdiği "bir önceki gün 21:00Z")
    /// günü kaydırabileceği için reddedilir.
    /// </summary>
    [Theory]
    [InlineData("null", true)]
    [InlineData("\"2026-10-01\"", true)]
    [InlineData("\"2026-10-01T00:00:00\"", true)]
    [InlineData("\"2026-10-01T10:30:00\"", false)]
    [InlineData("\"2026-09-30T21:00:00Z\"", false)]
    [InlineData("\"2026-10-01T00:00:00Z\"", false)]
    [InlineData("\"2026-10-01T00:00:00+03:00\"", false)]
    public void EkipmanKaydi_KaynakTarihiSaatsizGun_GunKaymaz(string deger, bool gecerli)
    {
        // API gövdeyi ASP.NET'in varsayılan (Web) JSON ayarlarıyla bağlar.
        var istek = System.Text.Json.JsonSerializer.Deserialize<EkipmanKonfigurasyonEkleCommand>(
            "{\"kaynakTarihi\":" + deger + "}", new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))!;
        Assert.Equal(gecerli, EkipmanKitapligiKurali.KaynakTarihiGecerli(istek.KaynakTarihi));
        if (gecerli && deger != "null")
            Assert.Equal(new DateTime(2026, 10, 1), istek.KaynakTarihi);

        var dogrulama = new EkipmanKonfigurasyonKaydiValidator().Validate(istek);
        Assert.Equal(!gecerli, dogrulama.Errors.Any(e => e.ErrorMessage == EkipmanKitapligiKurali.KaynakTarihiMesaji));
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
            (new PlanYukOnizlemeQuery(), YetkiKodlari.YuklemePlanlama.Planlar, YetkiTipi.R),
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
            (new EkipmanKonfigurasyonEkleCommand(), YetkiKodlari.YuklemePlanlama.EkipmanKitapligi, YetkiTipi.W),
            (new EkipmanKonfigurasyonGuncelleCommand(), YetkiKodlari.YuklemePlanlama.EkipmanKitapligi, YetkiTipi.W),
            (new EkipmanDogrulaCommand(), YetkiKodlari.YuklemePlanlama.EkipmanDogrulama, YetkiTipi.W)
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
        // Ekipman kitaplığı izinleri plan kaydetmeye yetmez.
        Assert.False(await GecerMi(OrtakRoller.Haric(YetkiKodlari.YuklemePlanlama.Planlar)));
    }
}
