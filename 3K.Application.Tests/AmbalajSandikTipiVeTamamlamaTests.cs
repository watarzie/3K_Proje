using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri;
using _3K.Application.Features.AmbalajIslemleri.Commands;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Application.Features.AmbalajIslemleri.Queries;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Validators;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Tests;

/// <summary>
/// Ambalaj düzeltmeleri: kaynak sandık tipinin korunması (A7), özel sandık tekilliği (A1), grubun tek
/// doğruluk kaynağı olarak Tur (RR Q2), proje bazlı tamamlama ve PO'lu üretimin geri alınamaması (A2/A3),
/// sandık tipinin zorunluluğu (G2).
/// </summary>
public sealed class AmbalajSandikTipiVeTamamlamaTests
{
    private static readonly DateTime PlanBaslangici = new(2026, 9, 1, 10, 0, 0);

    [Theory]
    [InlineData(AmbalajSandikTuru.Normal, null, true, AmbalajSandikTuru.Ilave)]
    [InlineData(AmbalajSandikTuru.Normal, null, false, AmbalajSandikTuru.Normal)]
    [InlineData(AmbalajSandikTuru.Normal, AmbalajSandikTuru.Normal, true, AmbalajSandikTuru.Normal)]
    [InlineData(AmbalajSandikTuru.Normal, AmbalajSandikTuru.Ilave, false, AmbalajSandikTuru.Ilave)]
    [InlineData(AmbalajSandikTuru.Saha, null, true, AmbalajSandikTuru.Saha)]
    [InlineData(AmbalajSandikTuru.Yedek, AmbalajSandikTuru.Ilave, true, AmbalajSandikTuru.Yedek)]
    public void RRQ2_Grup_kayitta_bir_kez_belirlenir_ve_korunur(
        AmbalajSandikTuru projeGrubu, AmbalajSandikTuru? kayitli, bool cekiDisi, AmbalajSandikTuru beklenen)
    {
        Assert.Equal(beklenen, AmbalajUretimPolitikasi.KaynakGrubu(projeGrubu, kayitli, cekiDisi));
        if (kayitli is { } grup)
            Assert.Equal(beklenen, AmbalajUretimPolitikasi.KaynakGrubu(projeGrubu, grup));
    }

    /// <summary>
    /// Çeki dışından eklenen sandık: numarası projenin çeki satırlarında geçmez. Elle eklenen ürün satırı çeki verisi
    /// değildir; projenin hiç çeki satırı yoksa plan çekiyle başlamamıştır ve sandık ilave sayılmaz.
    /// </summary>
    [Theory]
    [InlineData("5", false)]
    [InlineData(" 2 ", false)]
    [InlineData("05", false)]
    [InlineData("1 2", false)]
    [InlineData("10\u20131", false)]
    [InlineData("9", true)]
    [InlineData("ILV-1", true)]
    [InlineData("12345678901", true)]
    public void Ceki_disi_eklenen_sandik_ceki_satirlarindaki_numaraya_gore_belirlenir(string sandikNo, bool beklenen)
    {
        // Çeki yüklemesi ile aynı koli no biçimi (KoliNoKurali): boşluk/tire farkı ve "1.0" gibi sayı yazımı eşleşir.
        var cekiNolari = AmbalajUretimPolitikasi.CekiSandikNolari(["1.0", "2 ", "5", "12", "10-1", null, " "]);

        Assert.Equal(beklenen, AmbalajUretimPolitikasi.CekiDisiEklenenSandikMi(sandikNo, cekiNolari));
        Assert.False(AmbalajUretimPolitikasi.CekiDisiEklenenSandikMi(sandikNo, AmbalajUretimPolitikasi.CekiSandikNolari([])));
    }

    /// <summary>Koli no çeki yüklemesindeki biçime gelir; int'e sığmayan uzun sayısal numara taşma hatası vermez.</summary>
    [Theory]
    [InlineData("05", "5")]
    [InlineData("1.0", "1")]
    [InlineData(" 1 2 ", "12")]
    [InlineData("12345678901", "12345678901")]
    [InlineData("5010-01", "5010-01")]
    [InlineData(null, "")]
    public void Koli_no_ceki_yuklemesindeki_bicime_gelir(string? deger, string beklenen) =>
        Assert.Equal(beklenen, KoliNoKurali.Normalize(deger));

    /// <summary>İlave sandık ana plana girmez: ilk ambalaj kararı "Yapılmaz"dır; transformatör/bushing kuralı sürer.</summary>
    [Theory]
    [InlineData(AmbalajSandikTuru.Ilave, "Radyatör", false)]
    [InlineData(AmbalajSandikTuru.Normal, "Radyatör", true)]
    [InlineData(AmbalajSandikTuru.Normal, "Transformatör", false)]
    [InlineData(AmbalajSandikTuru.Saha, "Radyatör", true)]
    public void Ilave_sandigin_ilk_ambalaj_karari_yapilmazdir(AmbalajSandikTuru grup, string ad, bool beklenen) =>
        Assert.Equal(beklenen, AmbalajUretimPolitikasi.VarsayilanAmbalajaDahilMi(grup, ad, null));

    /// <summary>RR Q2: çeki dışından eklenen sandık senkronda İlave olarak kaydedilir; sonraki senkron grubu ezmez.</summary>
    [Fact]
    public async Task RRQ2_Ceki_disindan_eklenen_sandik_senkronda_ilave_olur_ve_oyle_kalir()
    {
        var f = new Fixture(OrtakRoller.TumIzinler());
        f.CekiSatiri("1");
        f.KaynakSandik(101, "1", SandikTipi.AhsapKapali, PlanBaslangici.AddDays(-1));
        var ilk = f.SenkronKaydi(101, PlanBaslangici);
        f.KaynakSandik(102, "2", SandikTipi.AhsapKapali, PlanBaslangici.AddDays(3));
        var sync = new AmbalajKaynakSenkronizasyonService(f.Uow, f.Finans);

        Assert.True((await sync.SenkronizeEtAsync(10, f.User, default)).IsSuccess);
        var yeni = Assert.Single(f.Kayitlar, k => k.KaynakKayitId == 102);
        Assert.Equal(AmbalajSandikTuru.Ilave, yeni.Tur);
        Assert.False(yeni.AmbalajaDahil);
        Assert.Equal(AmbalajSandikTuru.Normal, ilk.Tur);

        // Çeki satırı sonradan aynı numarayı alsa da kayıtlı grup ve kullanıcının ambalaj kararı korunur.
        f.CekiSatiri("2");
        Assert.True((await sync.SenkronizeEtAsync(10, f.User, default)).IsSuccess);
        Assert.Equal(AmbalajSandikTuru.Ilave, yeni.Tur);
        Assert.False(yeni.AmbalajaDahil);
    }

    /// <summary>
    /// Canlı bildirim (PA727-03): çekiden 4 sandık oluşmuş, projenin hiç ambalaj kaydı yokken 5. sandık elle eklenmiş.
    /// İlk senkron hepsini aynı anda kaydeder; eski kural (plan başlangıcı = ilk ambalaj kaydı) elle ekleneni de Normal
    /// ve ambalaja dahil oluşturuyordu. Grup sandığın kaynağından gelir: elle eklenen İlave ve "Yapılmaz" olur, finansa
    /// aktarılmaz; çeki ve revizyon sandıkları ile elle eklenen ürün satırı bu kararı değiştirmez.
    /// </summary>
    [Fact]
    public async Task Ceki_sonrasi_elle_eklenen_sandik_ilk_senkronda_ilave_ve_ambalaj_disi_olusur()
    {
        var f = new Fixture(OrtakRoller.TumIzinler());
        foreach (var no in new[] { "1", "2", "3", "4" })
        {
            f.CekiSatiri(no);
            f.KaynakSandik(100 + int.Parse(no), no, SandikTipi.AhsapKapali, PlanBaslangici);
        }
        // Revizyon yeni sandığı çeki satırındaki numarayla açar; elle eklenen sandığa elle ürün de eklenmiş olabilir.
        f.CekiSatiri("6");
        f.KaynakSandik(106, "6", SandikTipi.AhsapKapali, PlanBaslangici.AddDays(2));
        f.KaynakSandik(105, "5", SandikTipi.AhsapKapali, PlanBaslangici.AddDays(27));
        f.CekiSatiri("5", manuelEklenen: true);

        var sonuc = await new AmbalajKaynakSenkronizasyonService(f.Uow, f.Finans).SenkronizeEtAsync(10, f.User, default);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(6, sonuc.Value!.Eklenen);
        var elle = Assert.Single(f.Kayitlar, k => k.KaynakKayitId == 105);
        Assert.Equal(AmbalajSandikTuru.Ilave, elle.Tur);
        Assert.False(elle.AmbalajaDahil);
        Assert.False(elle.UretimeAlindi);
        Assert.False(elle.BagimsizKayitMi);
        Assert.All(f.Kayitlar.Where(k => k.KaynakKayitId != 105), k =>
        {
            Assert.Equal(AmbalajSandikTuru.Normal, k.Tur);
            Assert.True(k.AmbalajaDahil);
        });
        Assert.Empty(f.Finans.Modeller);
        var hareket = Assert.Single(f.Uow.Repo<AmbalajUretimHareketi>().Rows,
            h => h.AmbalajUretimKaydiId == elle.Id && h.AlanAdi == nameof(AmbalajUretimKaydi.AmbalajaDahil));
        Assert.Equal(bool.FalseString, hareket.YeniDeger);
    }

    /// <summary>
    /// Plan ekranı ve Özel Sandıklar aday listesi kaydı henüz oluşmamış sandığı senkronla aynı kuralla okur: elle eklenen
    /// sandık plan çekmecesinde hiç listelenmez, tamamlama sayacına girmez ve İlave sandık formunda kaynak olarak seçilir.
    /// </summary>
    [Fact]
    public async Task Senkronize_edilmemis_elle_eklenen_sandik_planda_gorunmez_ve_ozel_sandik_adayidir()
    {
        var f = new Fixture(OrtakRoller.TumIzinler());
        f.CekiSatiri("1");
        f.CekiSatiri("2");
        f.KaynakSandik(101, "1", SandikTipi.AhsapKapali, PlanBaslangici);
        f.KaynakSandik(102, "2", SandikTipi.AhsapKapali, PlanBaslangici);
        f.KaynakSandik(105, "5", SandikTipi.AhsapKapali, PlanBaslangici.AddDays(27));

        var plan = await f.Plan();
        var adaylar = await f.IlaveAdaylariHandler().Handle(new GetAmbalajIlaveSandikAdaylariQuery { ProjeId = 10 }, default);

        Assert.Equal([101, 102], plan.Kalemler.Select(k => k.KaynakSandikId!.Value));
        Assert.All(plan.Kalemler, k => Assert.Equal(1, k.Tur));
        Assert.Equal(2, plan.GerekliSandikAdedi);
        Assert.True(adaylar.IsSuccess, adaylar.Error?.Message);
        Assert.Equal(105, Assert.Single(adaylar.Value!).Id);
        Assert.Empty(f.Kayitlar);
    }

    /// <summary>
    /// Senkronlanmış ilave kaynak kaydı da plan çekmecesinde listelenmez; kullanıcı ambalaja dahil ettiyse (eski kayıt
    /// veya bilinçli karar) tamamlama için gerekli olduğundan İlave rozetiyle görünür.
    /// </summary>
    [Fact]
    public async Task Senkronlanmis_ilave_kaynak_ambalaj_disindayken_planda_gorunmez_dahilse_gorunur()
    {
        var f = new Fixture(OrtakRoller.TumIzinler());
        f.CekiSatiri("1");
        f.KaynakSandik(101, "1", SandikTipi.AhsapKapali, PlanBaslangici);
        f.KaynakSandik(105, "5", SandikTipi.AhsapKapali, PlanBaslangici.AddDays(27));
        Assert.True((await new AmbalajKaynakSenkronizasyonService(f.Uow, f.Finans).SenkronizeEtAsync(10, f.User, default)).IsSuccess);
        var ilave = Assert.Single(f.Kayitlar, k => k.KaynakKayitId == 105);

        Assert.Equal(101, Assert.Single((await f.Plan()).Kalemler).KaynakSandikId);

        ilave.AmbalajKarariniUygula(true);
        var dahil = Assert.Single((await f.Plan()).Kalemler, k => k.KaynakSandikId == 105);
        Assert.Equal((2, true), (dahil.Tur, dahil.AmbalajaDahilMi));
    }

    /// <summary>
    /// Elle eklenen sandık Özel Sandıklar'dan İlave'ye dönüştürülür; kaydı henüz yoksa komut önce senkronize eder.
    /// Çekiden gelen (ambalaja dahil) sandık senkron içinde kaydı oluşsa bile kapsam dışına alınmadan dönüştürülemez.
    /// Dönüştürülen sandık planda yeniden satır olarak görünmez ve plandan ambalaj kararı değiştirilemez.
    /// </summary>
    [Fact]
    public async Task Elle_eklenen_sandik_ozel_ilave_sandiga_donusur_cekiden_gelen_donusmez()
    {
        var f = new Fixture(OrtakRoller.TumIzinler());
        f.CekiSatiri("1");
        f.KaynakSandik(101, "1", SandikTipi.AhsapKapali, PlanBaslangici);
        f.KaynakSandik(105, "5", SandikTipi.AhsapKapali, PlanBaslangici.AddDays(27));

        var cekiSandigi = await f.OzelSandikHandler().Handle(OzelSandik(tur: 2, sandikNo: "", kaynakSandikId: 101), default);
        var elle = await f.OzelSandikHandler().Handle(OzelSandik(tur: 2, sandikNo: "", kaynakSandikId: 105), default);

        Assert.False(cekiSandigi.IsSuccess);
        Assert.Equal(400, cekiSandigi.StatusCode);
        Assert.Contains("kapsamı dışında", cekiSandigi.Error!.Message);
        // Bellek içi UoW geri alma yapmaz; reddedilen dönüşüm çeki sandığının senkron kaydını özel kayda çevirmemiştir.
        var cekiKaydi = Assert.Single(f.Kayitlar, k => k.KaynakKayitId == 101);
        Assert.Equal((false, AmbalajSandikTuru.Normal, true),
            (cekiKaydi.BagimsizKayitMi, cekiKaydi.Tur, cekiKaydi.AmbalajaDahil));
        Assert.True(elle.IsSuccess, elle.Error?.Message);
        var kayit = Assert.Single(f.Kayitlar, k => k.KaynakKayitId == 105);
        Assert.True(kayit.BagimsizKayitMi);
        Assert.Equal(AmbalajSandikTuru.Ilave, kayit.Tur);
        Assert.False(kayit.UretimeAlindi);

        Assert.Equal(101, Assert.Single((await f.Plan()).Kalemler).KaynakSandikId);
        var karar = await f.KararHandler().Handle(new AmbalajKarariKaydetCommand { SandikId = 105, AmbalajaDahilMi = false }, default);
        Assert.Equal(409, karar.StatusCode);
        Assert.Equal((true, true), (kayit.BagimsizKayitMi, kayit.AmbalajaDahil));
    }

    /// <summary>A7: projedeki Ahşap Kapalı sandık İlave özel sandığa dönüşürken formdan gelen "Kontrplak" tipi kaynağı ezemez.</summary>
    [Fact]
    public async Task A7_Kaynak_sandiktan_ozel_sandik_kaynak_tipini_korur()
    {
        var f = new Fixture(OrtakRoller.TumIzinler());
        f.KaynakSandik(101, "6", SandikTipi.AhsapKapali, PlanBaslangici.AddDays(-1));
        var kaynakKaydi = f.SenkronKaydi(101, PlanBaslangici);
        kaynakKaydi.AmbalajKarariniUygula(false);

        var sonuc = await f.OzelSandikHandler().Handle(OzelSandik(tur: 2, sandikNo: "", kaynakSandikId: 101), default);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        var kayit = Assert.Single(f.Kayitlar, k => k.KaynakKayitId == 101);
        Assert.True(kayit.BagimsizKayitMi);
        Assert.Equal(AmbalajSandikCinsi.AhsapKapali, kayit.SandikCinsi);
        Assert.NotEqual("uygulanmaz-v1", kayit.M3HesaplamaVersiyonu);
        // Özel kayda dönüşen çeki sandığı plan çekmecesine kaydı yokmuş gibi (ambalaja dahil) geri dönmez.
        Assert.Empty((await f.Plan()).Kalemler);
    }

    /// <summary>
    /// İlave aday listesi kaydı oluşmamış sandığı da senkronun ilk kararıyla okur: transformatör sandığı ve tamamlanmış
    /// projeye sonradan eklenen sandık senkronda "Yapılmaz" başlayacağı için adaydır; çekiden gelen sandık aday değildir.
    /// </summary>
    [Fact]
    public async Task Ilave_aday_listesi_kaydi_olmayan_sandigi_senkronun_ilk_karariyla_okur()
    {
        var f = new Fixture(OrtakRoller.TumIzinler());
        f.CekiSatiri("1");
        f.CekiSatiri("2");
        f.KaynakSandik(101, "1", SandikTipi.AhsapKapali, PlanBaslangici).Ad = "Transformatör";
        f.KaynakSandik(102, "2", SandikTipi.AhsapKapali, PlanBaslangici);

        var adaylar = await f.IlaveAdaylariHandler().Handle(new GetAmbalajIlaveSandikAdaylariQuery { ProjeId = 10 }, default);
        Assert.Equal(101, Assert.Single(adaylar.Value!).Id);

        f.FormluKayit(1, AmbalajUretimDurumu.Tamamlandi);
        var tamamlanmis = await f.IlaveAdaylariHandler().Handle(new GetAmbalajIlaveSandikAdaylariQuery { ProjeId = 10 }, default);
        Assert.Equal([101, 102], tamamlanmis.Value!.Select(a => a.Id));
    }

    /// <summary>Saha/yedek projesinde grup proje türüdür: çeki dışından eklenen sandık İlave olmaz, ambalaja dahil başlar.</summary>
    [Fact]
    public async Task Saha_projesinde_ceki_disindan_eklenen_sandik_saha_grubunda_ve_dahil_olusur()
    {
        var f = new Fixture(OrtakRoller.TumIzinler(), ProjeTipi.Saha);
        f.CekiSatiri("S1");
        f.KaynakSandik(101, "S1", SandikTipi.AhsapKapali, PlanBaslangici);
        f.KaynakSandik(105, "S5", SandikTipi.AhsapKapali, PlanBaslangici.AddDays(3));

        Assert.True((await new AmbalajKaynakSenkronizasyonService(f.Uow, f.Finans).SenkronizeEtAsync(10, f.User, default)).IsSuccess);

        Assert.All(f.Kayitlar, k => Assert.Equal((AmbalajSandikTuru.Saha, true), (k.Tur, k.AmbalajaDahil)));
        Assert.Equal(2, f.Kayitlar.Count);
    }

    /// <summary>
    /// Özel sandıkta üretim formu yoktur: kaydetmek finansa aktif iş kaydı açmaz (kaynak pasif gider), üretim tarihi
    /// yazılmaz; finans kaydı "Finansa aktar" ile açılır. Daha önce aktarılmış kayıt düzenlenince durumu korunur.
    /// </summary>
    [Fact]
    public async Task Ozel_sandik_kaydedilince_uretime_alinmaz_finans_kaydi_finansa_aktarilinca_acilir()
    {
        var f = new Fixture(OrtakRoller.TumIzinler(), ProjeTipi.Saha);

        var sonuc = await f.OzelSandikHandler().Handle(OzelSandik(tur: 4), default);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        var kayit = Assert.Single(f.Kayitlar);
        Assert.False(kayit.UretimeAlindi);
        Assert.Null(kayit.UretimTarihi);
        Assert.Equal(AmbalajUretimDurumu.Planlandi, kayit.UretimDurumu);
        Assert.False(Assert.Single(f.Finans.Modeller).KaynakAktif);
        Assert.False(AmbalajFinansAktarimPolitikasi.AktarimaHazirMi(kayit));

        // Eski davranışla kaydedilmiş (üretime alınmış) özel sandık düzenlenince finans kaydı pasife düşmez.
        kayit.UretimeAlindi = true;
        kayit.UretimTarihi = PlanBaslangici;
        f.Finans.Modeller.Clear();
        var duzenle = new AmbalajBagimsizSandikKaydetCommand
        {
            SandikId = kayit.Id, ProjeId = 10, Tur = 4, SandikNo = "1", Ad = "Saha sandığı (düzeltildi)", SandikTipi = "Kontrplak Sandık",
            Adet = 1, Boy = 1200, En = 1000, Yukseklik = 900, TalimatVeren = "Planlama"
        };
        Assert.True((await f.OzelSandikHandler().Handle(duzenle, default)).IsSuccess);
        Assert.True(kayit.UretimeAlindi);
        Assert.Equal(PlanBaslangici, kayit.UretimTarihi);
        Assert.True(Assert.Single(f.Finans.Modeller).KaynakAktif);
    }

    /// <summary>
    /// "Finansa aktar": özel sandık üretime alınmış sayılır, finans tarihiyle aktif iş kaydı açılır; ikinci aktarım 409.
    /// "Finanstan geri al": iş kaydı pasife düşer; PO'ya bağlıysa veya proje tamamlamasıyla tamamlandıysa 409.
    /// </summary>
    [Fact]
    public async Task Ozel_sandik_finansa_aktarilir_ve_po_yoksa_finanstan_geri_alinir()
    {
        var f = new Fixture(OrtakRoller.TumIzinler(), ProjeTipi.Saha);
        Assert.True((await f.OzelSandikHandler().Handle(OzelSandik(tur: 4), default)).IsSuccess);
        var kayit = Assert.Single(f.Kayitlar);
        f.Finans.Modeller.Clear();

        var aktar = await f.OzelFinansHandler().Handle(new() { SandikId = kayit.Id, FinansTarihi = new DateTime(2026, 9, 30) }, default);

        Assert.True(aktar.IsSuccess, aktar.Error?.Message);
        Assert.True(aktar.Value!.UretimeAlindi);
        Assert.Equal((true, AmbalajUretimDurumu.Uretimde), (kayit.UretimeAlindi, kayit.UretimDurumu));
        Assert.NotNull(kayit.UretimTarihi);
        var model = Assert.Single(f.Finans.Modeller);
        Assert.Equal((true, new DateTime(2026, 9, 30)), (model.KaynakAktif, model.FinansTarihi));
        Assert.Equal(409, (await f.OzelFinansHandler().Handle(new() { SandikId = kayit.Id }, default)).StatusCode);

        f.Finans.Modeller.Clear();
        var geriAl = await f.OzelFinansHandler().Handle(new() { SandikId = kayit.Id, Aktar = false }, default);

        Assert.True(geriAl.IsSuccess, geriAl.Error?.Message);
        Assert.Equal((false, AmbalajUretimDurumu.Planlandi, (DateTime?)null), (kayit.UretimeAlindi, kayit.UretimDurumu, kayit.UretimTarihi));
        Assert.False(Assert.Single(f.Finans.Modeller).KaynakAktif);

        // PO'ya bağlanmış finans kaydı geri alınamaz.
        Assert.True((await f.OzelFinansHandler().Handle(new() { SandikId = kayit.Id }, default)).IsSuccess);
        f.PoBagla(kayit);
        var poluGeriAl = await f.OzelFinansHandler().Handle(new() { SandikId = kayit.Id, Aktar = false }, default);
        Assert.Equal(409, poluGeriAl.StatusCode);
        Assert.Contains("PO'ya bağlı", poluGeriAl.Error!.Message);
        Assert.True(kayit.UretimeAlindi);
    }

    /// <summary>
    /// Özel Sandıklar listesi finans durumuna göre süzülür (bekleyen / aktarılan) ve iki sekmenin sayısını taşır; satır
    /// oluşturan ve finansa aktaran kullanıcıyla tarihleri, geçmiş ise her işlemi kullanıcı ve okunur alan değerleriyle verir.
    /// </summary>
    [Fact]
    public async Task Ozel_sandik_listesi_finans_durumuna_gore_suzulur_denetim_ve_gecmis_gosterilir()
    {
        var f = new Fixture(OrtakRoller.TumIzinler(), ProjeTipi.Saha);
        f.Uow.Repo<Kullanici>().Rows.Add(new Kullanici { Id = 7, AdSoyad = "Ayşe Planlama" });
        Assert.True((await f.OzelSandikHandler().Handle(OzelSandik(tur: 4, sandikNo: "1"), default)).IsSuccess);
        Assert.True((await f.OzelSandikHandler().Handle(OzelSandik(tur: 4, sandikNo: "2"), default)).IsSuccess);
        var aktarilan = f.Kayitlar.Single(k => k.SandikNo == "2");
        Assert.True((await f.OzelFinansHandler().Handle(new() { SandikId = aktarilan.Id }, default)).IsSuccess);
        var liste = new GetAmbalajBagimsizSandiklarQueryHandler(f.Uow, f.Roller, f.User);

        var bekleyen = (await liste.Handle(new() { FinansDurumu = "bekleyen" }, default)).Value!;
        var aktarilanlar = (await liste.Handle(new() { FinansDurumu = "aktarilan" }, default)).Value!;

        Assert.Equal(["1"], bekleyen.Items.Select(x => x.SandikNo));
        Assert.Equal((1, 1), (bekleyen.FilteredSummary!.FinansaAktarilmayanKayitSayisi, bekleyen.FilteredSummary.FinansaAktarilanKayitSayisi));
        var satir = Assert.Single(aktarilanlar.Items);
        Assert.Equal(("2", "Ayşe Planlama", "Ayşe Planlama"), (satir.SandikNo, satir.OlusturanAdi, satir.FinansaAktaranAdi));
        Assert.NotNull(satir.OlusturmaTarihi);
        Assert.NotNull(satir.FinansaAktarimTarihi);
        Assert.Null(Assert.Single(bekleyen.Items).FinansaAktarimTarihi);
        Assert.False((await liste.Handle(new() { FinansDurumu = "yanlis" }, default)).IsSuccess);

        var gecmisHandler = new GetAmbalajKayitGecmisiQueryHandler(f.Uow, f.Roller, f.User);
        var gecmis = (await gecmisHandler.Handle(new() { KayitId = aktarilan.Id }, default)).Value!;
        Assert.Equal((AmbalajOzelSandikFinansCommandHandler.AktarimIslemi, "Ayşe Planlama"), (gecmis[0].Islem, gecmis[0].Kullanici));
        Assert.Contains(gecmis[0].Degisiklikler, d => d.Alan == "Üretime alındı / finansa aktarıldı" && d.EskiDeger == "Hayır" && d.YeniDeger == "Evet");
        Assert.Contains(gecmis[0].Degisiklikler, d => d.Alan == "Üretim durumu" && d.EskiDeger == "Planlandı" && d.YeniDeger == "Üretimde");
        var olusturma = Assert.Single(gecmis, o => o.Islem == "Bağımsız ambalaj sandığı eklendi");
        Assert.Contains(olusturma.Degisiklikler, d => d.Alan == "Yapı" && d.YeniDeger == "Kontrplak Sandık");
        Assert.DoesNotContain(olusturma.Degisiklikler, d => d.Alan == "M3HesaplamaVersiyonu");
        Assert.Equal(404, (await gecmisHandler.Handle(new() { KayitId = 999 }, default)).StatusCode);
    }

    /// <summary>
    /// Geçmiş, listedeki alan maskesiyle aynı izinleri uygular: ölçü/m³ izni olmayan kullanıcı o alanların eski/yeni
    /// değerlerini geçmişte de göremez; kaydın üretim durumunu göremeyen kullanıcı geçmişi hiç göremez (404).
    /// </summary>
    [Fact]
    public async Task Ozel_sandik_gecmisi_olcu_ve_m3_iznine_gore_alan_gizler_gorulemeyen_durumda_404_doner()
    {
        var f = new Fixture(OrtakRoller.TumIzinler(), ProjeTipi.Saha);
        Assert.True((await f.OzelSandikHandler().Handle(OzelSandik(tur: 4), default)).IsSuccess);
        var kayit = Assert.Single(f.Kayitlar);
        string[] olcuAlanlari = ["Boy (mm)", "En (mm)", "Yükseklik (mm)"];
        static IEnumerable<string> Alanlar(IReadOnlyList<AmbalajKayitGecmisiDto> gecmis) =>
            gecmis.SelectMany(o => o.Degisiklikler).Select(d => d.Alan);

        var tum = (await new GetAmbalajKayitGecmisiQueryHandler(f.Uow, f.Roller, f.User).Handle(new() { KayitId = kayit.Id }, default)).Value!;
        Assert.All(olcuAlanlari, alan => Assert.Contains(alan, Alanlar(tum)));

        var olcusuz = OrtakRoller.Haric(YetkiKodlari.Ambalaj.OlcuGoruntule, YetkiKodlari.Ambalaj.M3Goruntule);
        var kisitli = (await new GetAmbalajKayitGecmisiQueryHandler(f.Uow, olcusuz, f.User).Handle(new() { KayitId = kayit.Id }, default)).Value!;
        Assert.NotEmpty(kisitli);
        Assert.All(olcuAlanlari, alan => Assert.DoesNotContain(alan, Alanlar(kisitli)));
        Assert.DoesNotContain(Alanlar(kisitli), alan => alan.Contains("m³", StringComparison.Ordinal));
        Assert.Contains("Sandık no", Alanlar(kisitli));

        // Kayıt Planlandı: bekleyen kayıtları göremeyen kullanıcı için geçmiş yoktur.
        var bekleyenKapali = OrtakRoller.Haric(YetkiKodlari.Ambalaj.BekleyenGoruntule);
        Assert.Equal(404, (await new GetAmbalajKayitGecmisiQueryHandler(f.Uow, bekleyenKapali, f.User).Handle(new() { KayitId = kayit.Id }, default)).StatusCode);
    }

    [Fact]
    public async Task Ozel_sandik_olculeri_eksikse_veya_yapilmayacaksa_finansa_aktarilamaz_form_olusturulamaz()
    {
        var f = new Fixture(OrtakRoller.TumIzinler(), ProjeTipi.Saha);
        Assert.True((await f.OzelSandikHandler().Handle(OzelSandik(tur: 4), default)).IsSuccess);
        var kayit = Assert.Single(f.Kayitlar);

        kayit.Adet = 0;
        Assert.Equal(409, (await f.OzelFinansHandler().Handle(new() { SandikId = kayit.Id }, default)).StatusCode);
        kayit.Adet = 1;
        kayit.AmbalajKarariniUygula(false);
        Assert.Equal(409, (await f.OzelFinansHandler().Handle(new() { SandikId = kayit.Id }, default)).StatusCode);
        Assert.False(kayit.UretimeAlindi);
        // Normal plan kaydı bu komutla aktarılamaz.
        var plan = f.FormluKayit(50, AmbalajUretimDurumu.Planlandi);
        Assert.Equal(404, (await f.OzelFinansHandler().Handle(new() { SandikId = plan.Id }, default)).StatusCode);
    }

    /// <summary>A1: aynı proje + grup + sandık no ile ikinci özel sandık eklenemez (örnek: PA484-03-SAHA sandık 1).</summary>
    [Fact]
    public async Task A1_Ayni_ozel_sandik_ikinci_kez_eklenemez()
    {
        var f = new Fixture(OrtakRoller.TumIzinler(), ProjeTipi.Saha);
        var handler = f.OzelSandikHandler();

        Assert.True((await handler.Handle(OzelSandik(tur: 4), default)).IsSuccess);
        var tekrar = await handler.Handle(OzelSandik(tur: 4, sandikNo: " 1 "), default);

        Assert.Equal(409, tekrar.StatusCode);
        Assert.Contains("zaten özel sandık olarak kayıtlı", tekrar.Error!.Message);
        Assert.Single(f.Kayitlar, k => k.BagimsizKayitMi && !k.IptalMi);
    }

    [Fact]
    public void A3_Po_baglantili_tamamlanan_uretim_geri_alinamaz()
    {
        var kayit = new AmbalajUretimKaydi { SandikNo = "7", UretimDurumu = AmbalajUretimDurumu.Tamamlandi };
        var gereksinim = new AmbalajIslemGereksinimi();

        AmbalajIslemKurallari.Degerlendir(new(kayit, true, FinansaBagli: true) { HedefDurum = AmbalajUretimDurumu.Uretimde }, gereksinim);

        Assert.Contains("PO veya faturaya bağlı", gereksinim.Engel);
        var baglantisiz = new AmbalajIslemGereksinimi();
        AmbalajIslemKurallari.Degerlendir(new(kayit, true, FinansaBagli: false) { HedefDurum = AmbalajUretimDurumu.Uretimde }, baglantisiz);
        Assert.Null(baglantisiz.Engel);
    }

    [Fact]
    public async Task A3_Durum_komutu_po_baglantili_tamamlanani_geri_almayi_409_ile_reddeder()
    {
        var f = new Fixture(OrtakRoller.TumIzinler());
        var kayit = f.FormluKayit(1, AmbalajUretimDurumu.Tamamlandi);
        f.PoBagla(kayit);
        var handler = new AmbalajUretimDurumuGuncelleCommandHandler(f.Uow, f.User, f.Finans, f.Roller, f.Denetim);

        var sonuc = await handler.Handle(new() { Id = kayit.Id, Durum = AmbalajUretimDurumu.Uretimde, Aciklama = "Yanlış tamamlama" }, default);

        Assert.Equal(409, sonuc.StatusCode);
        Assert.Equal(AmbalajUretimDurumu.Tamamlandi, kayit.UretimDurumu);
        Assert.Empty(f.Finans.Modeller);
    }

    /// <summary>A2: FB listesindeki "Tamamla" projenin ambalaja dahil bütün sandıklarını tek işlemde tamamlar ve finansa aktarır.</summary>
    [Fact]
    public async Task A2_Proje_tamamla_dahil_butun_sandiklari_tamamlar_haric_sandiga_dokunmaz()
    {
        var f = new Fixture(OrtakRoller.TumIzinler());
        var bir = f.FormluKayit(1, AmbalajUretimDurumu.Uretimde);
        var iki = f.FormluKayit(2, AmbalajUretimDurumu.Uretimde);
        var haric = f.FormluKayit(3, AmbalajUretimDurumu.Planlandi);
        haric.AmbalajKarariniUygula(false);

        var sonuc = await f.ProjeHandler().Handle(new() { ProjeId = 10, Tamamla = true }, default);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal((2, AmbalajUretimDurumu.Tamamlandi), (sonuc.Value!.DegisenKayit, sonuc.Value.ProjeDurumu));
        Assert.All([bir, iki], k => Assert.Equal(AmbalajUretimDurumu.Tamamlandi, k.UretimDurumu));
        Assert.All([bir, iki], k => Assert.NotNull(k.TamamlanmaTarihi));
        Assert.Equal(AmbalajUretimDurumu.Planlandi, haric.UretimDurumu);
        Assert.Equal(2, f.Finans.Modeller.Count);
        Assert.Equal(2, f.Uow.Repo<AmbalajUretimGerceklesmesi>().Rows.Count);
    }

    [Fact]
    public async Task A2_Formu_olmayan_sandik_varken_proje_tamamlanamaz()
    {
        var f = new Fixture(OrtakRoller.TumIzinler());
        f.FormluKayit(1, AmbalajUretimDurumu.Uretimde);
        var formsuz = f.FormluKayit(2, AmbalajUretimDurumu.Uretimde, formEkle: false);

        var sonuc = await f.ProjeHandler().Handle(new() { ProjeId = 10, Tamamla = true }, default);

        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains("Üretim formu olmayan: 2 (Normal)", sonuc.Error!.Message);
        Assert.Equal(AmbalajUretimDurumu.Uretimde, formsuz.UretimDurumu);
        Assert.Empty(f.Finans.Modeller);
    }

    /// <summary>
    /// 707-02: çeki dışından projeye eklenen sandık senkronda İlave ve "Yapılmaz" olur; kullanıcı plan ekranından
    /// ambalaja dahil ederse formu olmadığı için tamamlamayı durdurur. Red sandık numarasını ve grubunu söyler; ekran
    /// satırı issues listesinden kaynak sandıkla eşler.
    /// </summary>
    [Fact]
    public async Task A2_Ilave_siniflanan_formsuz_kaynak_kayit_tamamlamayi_sandik_no_ve_grupla_engeller()
    {
        var f = new Fixture(OrtakRoller.TumIzinler());
        f.CekiSatiri("1");
        f.KaynakSandik(101, "1", SandikTipi.AhsapKapali, PlanBaslangici.AddDays(-1));
        var normal = f.UretimdeFormlu(f.SenkronKaydi(101, PlanBaslangici));
        f.KaynakSandik(102, "12", SandikTipi.AhsapKapali, PlanBaslangici.AddDays(3));
        Assert.True((await new AmbalajKaynakSenkronizasyonService(f.Uow, f.Finans).SenkronizeEtAsync(10, f.User, default)).IsSuccess);
        var ilave = Assert.Single(f.Kayitlar, k => k.KaynakKayitId == 102);
        Assert.Equal(AmbalajSandikTuru.Ilave, ilave.Tur);
        Assert.False(ilave.AmbalajaDahil);
        ilave.AmbalajKarariniUygula(true);
        f.Finans.Modeller.Clear();

        var sonuc = await f.ProjeHandler().Handle(new() { ProjeId = 10, Tamamla = true }, default);

        Assert.Equal(409, sonuc.StatusCode);
        Assert.StartsWith("Üretim tamamlanamıyor. Üretim formu olmayan: 12 (İlave).", sonuc.Error!.Message);
        var engel = Assert.Single(Engeller(sonuc));
        Assert.Equal((ilave.Id, (int?)102, AmbalajProjeTamamlamaEngelleri.FormYok, false),
            (engel.KayitId, engel.KaynakSandikId, engel.Neden, engel.OzelSandik));
        Assert.Equal(AmbalajUretimDurumu.Uretimde, normal.UretimDurumu);
        Assert.Equal(AmbalajUretimDurumu.Planlandi, ilave.UretimDurumu);
        Assert.Empty(f.Finans.Modeller);
    }

    /// <summary>
    /// Özel sandıkta form yoktur: tamamlamayı yalnız finansa aktarılmamış özel sandık engeller ve "özel sandık" olarak
    /// bildirilir; finansa aktarılmış formsuz özel sandık projeyle birlikte tamamlanır.
    /// </summary>
    [Fact]
    public async Task A2_Finansa_aktarilmamis_ozel_sandik_tamamlamayi_ozel_sandik_olarak_engeller()
    {
        var f = new Fixture(OrtakRoller.TumIzinler());
        f.FormluKayit(1, AmbalajUretimDurumu.Uretimde);
        var ozel = f.FormluKayit(2, AmbalajUretimDurumu.Planlandi, formEkle: false);
        ozel.BagimsizKayitMi = true;
        ozel.Tur = AmbalajSandikTuru.Ilave;
        ozel.SandikNo = "OZEL-1";
        ozel.UretimeAlindi = false;

        var sonuc = await f.ProjeHandler().Handle(new() { ProjeId = 10, Tamamla = true }, default);

        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains("Finansa aktarılmamış özel sandık: OZEL-1 (İlave özel sandık)", sonuc.Error!.Message);
        Assert.EndsWith("Özel sandıkları Özel Sandıklar listesinden finansa aktarın.", sonuc.Error.Message);
        var engel = Assert.Single(Engeller(sonuc));
        Assert.Equal((2, (int?)null, true, AmbalajProjeTamamlamaEngelleri.FinansaAktarilmadi),
            (engel.KayitId, engel.KaynakSandikId, engel.OzelSandik, engel.Neden));
        Assert.Equal("OZEL-1 (İlave özel sandık): finansa aktarılmamış", engel.Mesaj);

        ozel.UretimeAlindi = true;
        ozel.UretimTarihi = PlanBaslangici;
        var tamam = await f.ProjeHandler().Handle(new() { ProjeId = 10, Tamamla = true }, default);
        Assert.True(tamam.IsSuccess, tamam.Error?.Message);
        Assert.Equal(AmbalajUretimDurumu.Tamamlandi, ozel.UretimDurumu);

        // Tamamlanmış özel sandık finanstan geri alınamaz (önce proje tamamlaması geri alınır).
        var geriAl = await f.OzelFinansHandler().Handle(new() { SandikId = ozel.Id, Aktar = false }, default);
        Assert.Equal(409, geriAl.StatusCode);
        Assert.Contains("proje tamamlamasını geri alın", geriAl.Error!.Message);
    }

    /// <summary>Formu olmayan, üretime alınmamış ve ölçüsü eksik sandıklar tek yanıtta, kendi başlıklarıyla bildirilir.</summary>
    [Fact]
    public async Task A2_Butun_tamamlama_engelleri_tek_yanitta_toplanir_ve_hicbir_kayit_degismez()
    {
        var f = new Fixture(OrtakRoller.TumIzinler());
        var hazir = f.FormluKayit(1, AmbalajUretimDurumu.Uretimde);
        f.FormluKayit(3, AmbalajUretimDurumu.Uretimde, formEkle: false);
        var secilmemis = f.FormluKayit(7, AmbalajUretimDurumu.Uretimde);
        secilmemis.UretimeAlindi = false;
        var olcusuz = f.FormluKayit(9, AmbalajUretimDurumu.Uretimde);
        olcusuz.Boy = 0;
        AmbalajUretimYardimcilari.M3DegerleriniHesapla(olcusuz);

        var sonuc = await f.ProjeHandler().Handle(new() { ProjeId = 10, Tamamla = true }, default);

        Assert.Equal(409, sonuc.StatusCode);
        Assert.StartsWith("Üretim tamamlanamıyor. Üretim formu olmayan: 3 (Normal). Üretime alınmamış: 7 (Normal). " +
                          "Ölçüsü veya manuel m³ değeri eksik: 9 (Normal).", sonuc.Error!.Message);
        Assert.Equal([3, 7, 9], Engeller(sonuc).Select(e => e.KayitId));
        Assert.All(f.Kayitlar, k => Assert.Equal(AmbalajUretimDurumu.Uretimde, k.UretimDurumu));
        Assert.Null(hazir.TamamlanmaTarihi);
        Assert.Empty(f.Uow.Repo<AmbalajUretimGerceklesmesi>().Rows);
        Assert.Empty(f.Finans.Modeller);
    }

    /// <summary>Kaynak kayda müdahale izni olmayan kullanıcıya engel listesinden önce 403 döner; kayıt değişmez.</summary>
    [Fact]
    public async Task A2_Kaynak_kayda_mudahale_izni_yoksa_proje_tamamlama_403_doner()
    {
        var f = new Fixture(OrtakRoller.Haric(YetkiKodlari.Ambalaj.KaynakMudahalesi));
        f.KaynakSandik(101, "1", SandikTipi.AhsapKapali, PlanBaslangici.AddDays(-1));
        var kaynakli = f.UretimdeFormlu(f.SenkronKaydi(101, PlanBaslangici));
        f.FormluKayit(5, AmbalajUretimDurumu.Uretimde, formEkle: false);

        var sonuc = await f.ProjeHandler().Handle(new() { ProjeId = 10, Tamamla = true }, default);

        Assert.Equal(403, sonuc.StatusCode);
        Assert.Contains("Kaynak modülden gelen kayda müdahale", sonuc.Error!.Message);
        Assert.Equal(AmbalajUretimDurumu.Uretimde, kaynakli.UretimDurumu);
        Assert.Empty(f.Finans.Modeller);
    }

    /// <summary>
    /// Finans aktarımının iş kuralı reddi (ör. aktif sipariş miktarının altına düşme) 409 olarak yukarı taşınır ve
    /// tamamlama transaction'ı geri alınır; kayıtların değişmediği gerçek PostgreSQL'de
    /// <see cref="AmbalajUretimV2PostgresTests"/> içinde doğrulanır.
    /// </summary>
    [Fact]
    public async Task A2_Finans_reddi_proje_tamamlamayi_409_ile_durdurur()
    {
        var f = new Fixture(OrtakRoller.TumIzinler());
        f.FormluKayit(1, AmbalajUretimDurumu.Uretimde);
        f.Finans.Red = "Sentetik: üretim miktarı aktif sipariş miktarının altına düşürülemez.";

        var hata = await Assert.ThrowsAsync<IsKuraliException>(
            () => f.ProjeHandler().Handle(new() { ProjeId = 10, Tamamla = true }, default));

        Assert.Equal(409, hata.StatusCode);
        Assert.Equal(f.Finans.Red, hata.Message);
        Assert.False(f.Uow.HasActiveTransaction);
    }

    private static IReadOnlyList<AmbalajTamamlamaEngeliDto> Engeller(Result<AmbalajProjeUretimDurumuSonucDto> sonuc) =>
        Assert.IsAssignableFrom<IReadOnlyList<AmbalajTamamlamaEngeliDto>>(sonuc.Error!.Issues);

    [Fact]
    public async Task A3_Proje_geri_alma_gerekce_ister_po_bagliysa_reddedilir()
    {
        var f = new Fixture(OrtakRoller.TumIzinler());
        var kayit = f.FormluKayit(1, AmbalajUretimDurumu.Tamamlandi);

        Assert.Equal(400, (await f.ProjeHandler().Handle(new() { ProjeId = 10, Tamamla = false }, default)).StatusCode);
        f.PoBagla(kayit);
        Assert.Equal(409, (await f.ProjeHandler().Handle(new() { ProjeId = 10, Tamamla = false, Aciklama = "Geri al" }, default)).StatusCode);
        Assert.Equal(AmbalajUretimDurumu.Tamamlandi, kayit.UretimDurumu);
    }

    [Fact]
    public async Task A3_Proje_geri_alma_po_yoksa_tamamlananlari_uretimde_yapar()
    {
        var f = new Fixture(OrtakRoller.TumIzinler());
        var kayit = f.FormluKayit(1, AmbalajUretimDurumu.Tamamlandi);
        kayit.TamamlanmaTarihi = PlanBaslangici;

        var sonuc = await f.ProjeHandler().Handle(new() { ProjeId = 10, Tamamla = false, Aciklama = "Eksik ölçü" }, default);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(AmbalajUretimDurumu.Uretimde, kayit.UretimDurumu);
        Assert.Null(kayit.TamamlanmaTarihi);
    }

    /// <summary>A6: formda girilen tarih finans kaleminin varsayılan Finans Tarihi olur; üretim tarihi değişmez.</summary>
    [Fact]
    public void A6_Form_tarihi_finans_aktariminda_finans_tarihidir()
    {
        var kayit = new AmbalajUretimKaydi
        {
            IsAkisKimligi = Guid.NewGuid(), SandikNo = "1", Tur = AmbalajSandikTuru.Normal, Adet = 1,
            SandikCinsi = AmbalajSandikCinsi.AhsapKapali, UretimTarihi = new DateTime(2026, 9, 28), FinansTarihi = new DateTime(2026, 10, 1)
        };

        var model = AmbalajFinansSenkronizasyonu.ModelOlustur(kayit, null);

        Assert.Equal(new DateTime(2026, 10, 1), model.FinansTarihi);
        Assert.Equal(new DateTime(2026, 10, 1), model.FinansDonemi);
        Assert.Equal(new DateTime(2026, 9, 28), model.UretimTarihi);
        kayit.FinansTarihi = null;
        Assert.Equal(new DateTime(2026, 9, 1), AmbalajFinansSenkronizasyonu.ModelOlustur(kayit, null).FinansDonemi);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    public void G2_Sandik_tipi_eklemede_ve_guncellemede_zorunludur(int? tipId, bool gecerli)
    {
        Assert.Equal(gecerli, SandikTipiKurali.Gecerli(tipId));
        Assert.Equal(gecerli, new SandikEkleCommandValidator().Validate(new SandikEkleCommand { ProjeId = 1, SandikNo = "1", TipId = tipId }).IsValid);
        Assert.Equal(gecerli, new SandikOzellikGuncelleCommandValidator().Validate(new SandikOzellikGuncelleCommand { SandikId = 1, TipId = tipId }).IsValid);
    }

    private static AmbalajBagimsizSandikKaydetCommand OzelSandik(int tur, string sandikNo = "1", int? kaynakSandikId = null) => new()
    {
        ProjeId = 10, Tur = tur, SandikNo = sandikNo, KaynakSandikId = kaynakSandikId, Ad = "Saha sandığı", SandikTipi = "Kontrplak Sandık",
        Adet = 1, Boy = 1200, En = 1000, Yukseklik = 900, TalimatVeren = "Planlama"
    };

    private sealed class Fixture
    {
        public Fixture(OrtakRoller roller, ProjeTipi projeTipi = ProjeTipi.Normal)
        {
            Roller = roller;
            Uow.Repo<Proje>().Rows.Add(new Proje { Id = 10, ProjeNo = "PA-10", Musteri = "Müşteri", ProjeTipiId = (int)projeTipi });
            Denetim = new AmbalajKritikIslemDenetimi(Uow, Roller, User);
        }

        public OrtakMemoryUow Uow { get; } = new();
        public OrtakRoller Roller { get; }
        public OrtakUser User { get; } = new();
        public FinansAktarimi Finans { get; } = new();
        public AmbalajKritikIslemDenetimi Denetim { get; }
        public List<AmbalajUretimKaydi> Kayitlar => Uow.Repo<AmbalajUretimKaydi>().Rows;

        public AmbalajBagimsizSandikKaydetCommandHandler OzelSandikHandler() =>
            new(Uow, User, Roller, new AmbalajKaynakSenkronizasyonService(Uow, Finans), Finans, Denetim);

        public AmbalajProjeUretimDurumuCommandHandler ProjeHandler() => new(Uow, User, Finans, Roller, Denetim);

        public AmbalajOzelSandikFinansCommandHandler OzelFinansHandler() => new(Uow, User, Roller, Finans, Denetim);

        public GetAmbalajIlaveSandikAdaylariQueryHandler IlaveAdaylariHandler() => new(Uow, Roller, User);

        public AmbalajKarariKaydetCommandHandler KararHandler() =>
            new(Uow, User, Roller, new AmbalajKaynakSenkronizasyonService(Uow, Finans), Finans, Denetim);

        /// <summary>Plan çekmecesinin gördüğü plan (normal proje 10).</summary>
        public async Task<AmbalajPlanlamaPlanDto> Plan()
        {
            var plan = await new GetAmbalajPlanlamaPlanQueryHandler(Uow, Roller, User)
                .Handle(new GetAmbalajPlanlamaPlanQuery { ProjeId = 10 }, default);
            Assert.True(plan.IsSuccess, plan.Error?.Message);
            return plan.Value!;
        }

        /// <summary>Projenin çekisine satır ekler; çeki yüklemesi ve revizyonu sandığı bu satırdaki numarayla açar.</summary>
        public void CekiSatiri(string sandikNo, bool manuelEklenen = false)
        {
            if (Uow.Repo<Ceki>().Rows.Count == 0)
                Uow.Repo<Ceki>().Rows.Add(new Ceki { Id = 1, ProjeId = 10, CekiTipiId = (int)CekiTipi.Normal });
            var satirlar = Uow.Repo<CekiSatiri>().Rows;
            satirlar.Add(new CekiSatiri
            {
                Id = satirlar.Count + 1, CekiId = 1, CekideGecenSandikNo = sandikNo, FiiliSandikNo = sandikNo,
                IsManuelEklenen = manuelEklenen
            });
        }

        public Sandik KaynakSandik(int id, string no, SandikTipi tip, DateTime olusturma)
        {
            var sandik = new Sandik
            {
                Id = id, ProjeId = 10, SandikNo = no, Ad = "Sandık " + no, TipId = (int)tip,
                Boy = 2000, En = 1000, Yukseklik = 1200, CreatedDate = olusturma
            };
            Uow.Repo<Sandik>().Rows.Add(sandik);
            return sandik;
        }

        public AmbalajUretimKaydi SenkronKaydi(int kaynakId, DateTime olusturma)
        {
            var sandik = Uow.Repo<Sandik>().Rows.Single(x => x.Id == kaynakId);
            var kayit = new AmbalajUretimKaydi
            {
                Id = Kayitlar.Select(x => x.Id).DefaultIfEmpty().Max() + 1, ProjeId = 10, IsAkisKimligi = Guid.NewGuid(),
                Tur = AmbalajSandikTuru.Normal, KaynakModul = AmbalajKaynakModulu.Sandik, KaynakKayitId = kaynakId,
                SandikNo = sandik.SandikNo, Ad = sandik.Ad, SandikCinsi = AmbalajUretimPolitikasi.KaynakCinsi(sandik.TipId),
                Adet = 1, Boy = sandik.Boy ?? 0, En = sandik.En ?? 0, Yukseklik = sandik.Yukseklik ?? 0,
                AmbalajaDahil = true, UretimDurumu = AmbalajUretimDurumu.Planlandi, CreatedDate = olusturma
            };
            AmbalajUretimYardimcilari.M3DegerleriniHesapla(kayit);
            Kayitlar.Add(kayit);
            return kayit;
        }

        public AmbalajUretimKaydi FormluKayit(int id, AmbalajUretimDurumu durum, bool formEkle = true)
        {
            var kayit = new AmbalajUretimKaydi
            {
                Id = id, ProjeId = 10, IsAkisKimligi = Guid.NewGuid(), Tur = AmbalajSandikTuru.Normal,
                KaynakModul = AmbalajKaynakModulu.Manuel, SandikNo = id.ToString(), Ad = "Sandık", Adet = 1,
                SandikCinsi = AmbalajSandikCinsi.AhsapKapali, Boy = 1200, En = 1000, Yukseklik = 900,
                AmbalajaDahil = true, UretimeAlindi = true, UretimDurumu = durum, UretimTarihi = PlanBaslangici
            };
            AmbalajUretimYardimcilari.M3DegerleriniHesapla(kayit);
            Kayitlar.Add(kayit);
            if (formEkle)
                Uow.Repo<AmbalajUretimFormuKaydi>().Rows.Add(new AmbalajUretimFormuKaydi { FormSurumuId = 1, AmbalajUretimKaydiId = id });
            return kayit;
        }

        /// <summary>Kaydı ilk üretim formuyla üretime alınmış hâle getirir (form sonrası senkron kilidi dahil).</summary>
        public AmbalajUretimKaydi UretimdeFormlu(AmbalajUretimKaydi kayit)
        {
            kayit.UretimeAlindi = true;
            kayit.UretimDurumu = AmbalajUretimDurumu.Uretimde;
            kayit.KaynakSenkronizasyonuKilitliMi = true;
            Uow.Repo<AmbalajUretimFormuKaydi>().Rows.Add(new AmbalajUretimFormuKaydi { FormSurumuId = 1, AmbalajUretimKaydiId = kayit.Id });
            return kayit;
        }

        public void PoBagla(AmbalajUretimKaydi kayit)
        {
            var is_ = new FinansIsKaydi
            {
                Id = kayit.Id, KaynakTuru = FinansKaynakTurleri.AmbalajUretim, KaynakKayitId = kayit.IsAkisKimligi.ToString("D")
            };
            is_.SiparisKalemleri.Add(new FinansSiparisKalemi { FinansSiparis = new FinansSiparis { PoNumarasi = "4500123456" } });
            Uow.Repo<FinansIsKaydi>().Rows.Add(is_);
        }
    }

    private sealed class FinansAktarimi : IFinansUretimAktarimService
    {
        public List<FinansUretimAktarimModel> Modeller { get; } = [];
        /// <summary>Doluysa aktarım, finans servisinin iş kuralı reddi gibi 409 ile reddedilir.</summary>
        public string? Red { get; set; }

        public Task<FinansSenkronizasyonSonucModel> UretimKayitlariniAktarAsync(
            IReadOnlyList<FinansUretimAktarimModel> modeller, CancellationToken cancellationToken)
        {
            if (Red != null)
                throw IsKuraliException.Cakisma(Red);
            Modeller.AddRange(modeller);
            return Task.FromResult(new FinansSenkronizasyonSonucModel(0, modeller.Count, 0));
        }
    }
}
