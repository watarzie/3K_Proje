using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri;
using _3K.Application.Features.AmbalajIslemleri.Commands;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
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
    [InlineData(AmbalajSandikTuru.Normal, null, "2026-09-02", AmbalajSandikTuru.Ilave)]
    [InlineData(AmbalajSandikTuru.Normal, null, "2026-08-31", AmbalajSandikTuru.Normal)]
    [InlineData(AmbalajSandikTuru.Normal, AmbalajSandikTuru.Normal, "2026-09-02", AmbalajSandikTuru.Normal)]
    [InlineData(AmbalajSandikTuru.Normal, AmbalajSandikTuru.Ilave, "2026-08-31", AmbalajSandikTuru.Ilave)]
    [InlineData(AmbalajSandikTuru.Saha, null, "2026-09-02", AmbalajSandikTuru.Saha)]
    public void RRQ2_Grup_kayitta_bir_kez_belirlenir_ve_korunur(
        AmbalajSandikTuru projeGrubu, AmbalajSandikTuru? kayitli, string olusturma, AmbalajSandikTuru beklenen)
    {
        Assert.Equal(beklenen, AmbalajUretimPolitikasi.KaynakGrubu(projeGrubu, kayitli, PlanBaslangici, DateTime.Parse(olusturma)));
        Assert.Equal(AmbalajSandikTuru.Normal, AmbalajUretimPolitikasi.KaynakGrubu(AmbalajSandikTuru.Normal, null, null, DateTime.Parse(olusturma)));
    }

    /// <summary>RR Q2: plan başladıktan sonra eklenen sandık senkronda İlave olarak kaydedilir; sonraki senkron grubu ezmez.</summary>
    [Fact]
    public async Task RRQ2_Plan_basladiktan_sonra_eklenen_sandik_senkronda_ilave_olur_ve_oyle_kalir()
    {
        var f = new Fixture(OrtakRoller.TumIzinler());
        f.KaynakSandik(101, "1", SandikTipi.AhsapKapali, PlanBaslangici.AddDays(-1));
        var ilk = f.SenkronKaydi(101, PlanBaslangici);
        f.KaynakSandik(102, "2", SandikTipi.AhsapKapali, PlanBaslangici.AddDays(3));
        var sync = new AmbalajKaynakSenkronizasyonService(f.Uow, f.Finans);

        Assert.True((await sync.SenkronizeEtAsync(10, f.User, default)).IsSuccess);
        var yeni = Assert.Single(f.Kayitlar, k => k.KaynakKayitId == 102);
        Assert.Equal(AmbalajSandikTuru.Ilave, yeni.Tur);
        Assert.Equal(AmbalajSandikTuru.Normal, ilk.Tur);

        Assert.True((await sync.SenkronizeEtAsync(10, f.User, default)).IsSuccess);
        Assert.Equal(AmbalajSandikTuru.Ilave, yeni.Tur);
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
    }

    /// <summary>
    /// Özel sandık normal plan sandıkları gibi üretim formu oluşunca üretime alınır: kaydetmek finansa aktif iş kaydı
    /// açmaz (kaynak pasif gider), üretim tarihi yazılmaz. Daha önce üretime alınmış (eski) kayıt düzenlenince durumu korunur.
    /// </summary>
    [Fact]
    public async Task Ozel_sandik_kaydedilince_uretime_alinmaz_finans_kaydi_form_olusunca_acilir()
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
    /// 707-02: plan başladıktan sonra projeye eklenen sandık senkronda İlave olur ve formu olmadığı için tamamlamayı
    /// durdurur. Red sandık numarasını ve grubunu söyler; ekran satırı issues listesinden kaynak sandıkla eşler.
    /// </summary>
    [Fact]
    public async Task A2_Ilave_siniflanan_formsuz_kaynak_kayit_tamamlamayi_sandik_no_ve_grupla_engeller()
    {
        var f = new Fixture(OrtakRoller.TumIzinler());
        f.KaynakSandik(101, "1", SandikTipi.AhsapKapali, PlanBaslangici.AddDays(-1));
        var normal = f.UretimdeFormlu(f.SenkronKaydi(101, PlanBaslangici));
        f.KaynakSandik(102, "12", SandikTipi.AhsapKapali, PlanBaslangici.AddDays(3));
        Assert.True((await new AmbalajKaynakSenkronizasyonService(f.Uow, f.Finans).SenkronizeEtAsync(10, f.User, default)).IsSuccess);
        var ilave = Assert.Single(f.Kayitlar, k => k.KaynakKayitId == 102);
        Assert.Equal(AmbalajSandikTuru.Ilave, ilave.Tur);
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

    /// <summary>Özel Sandıklar listesindeki formsuz bağımsız kayıt da adıyla ve "özel sandık" olarak bildirilir.</summary>
    [Fact]
    public async Task A2_Formsuz_ozel_sandik_tamamlamayi_ozel_sandik_olarak_engeller()
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
        Assert.Contains("Üretim formu olmayan: OZEL-1 (İlave özel sandık)", sonuc.Error!.Message);
        var engel = Assert.Single(Engeller(sonuc));
        Assert.Equal((2, (int?)null, true), (engel.KayitId, engel.KaynakSandikId, engel.OzelSandik));
        Assert.Equal("OZEL-1 (İlave özel sandık): üretim formu yok", engel.Mesaj);
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
