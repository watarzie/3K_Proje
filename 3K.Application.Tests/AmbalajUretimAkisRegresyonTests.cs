using System.Linq.Expressions;
using System.Text.Json;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri;
using _3K.Application.Features.AmbalajIslemleri.Commands;
using _3K.Application.Features.AmbalajIslemleri.Queries;
using _3K.Application.Features.AmbalajIslemleri.Validators;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Constants;

namespace _3K.Application.Tests;

public sealed class AmbalajUretimAkisRegresyonTests
{
    [Fact]
    public void RaporDosyasi_PdfYetkisiniRaporIzniyleBirlikteIster()
    {
        var query = new GetAmbalajRaporDosyasiQuery { Tur = AmbalajSandikTuru.Ilave };
        var kodlar = query.RequiredMenuPermissions.Select(x => x.MenuKod).ToArray();

        Assert.Equal(new[] { YetkiKodlari.Ambalaj.RaporGoruntule, YetkiKodlari.Ambalaj.PdfIndir }, kodlar);
        // Alan izinleri rapora giriş şartı değildir; izin verilmeyen kolonlar null kalır.
        Assert.DoesNotContain(YetkiKodlari.Ambalaj.M3Goruntule, kodlar);
        Assert.All(query.RequiredMenuPermissions, x => Assert.Equal(YetkiTipi.R, x.YetkiTipi));
    }

    [Fact]
    public async Task OzelSandikRaporu_KokOkumaYetkisiM3SarfVeKaynakAlanlariniAcmaz()
    {
        var kayit = GecerliFormKaydi();
        kayit.Tur = AmbalajSandikTuru.Ilave;
        kayit.KaynakModul = AmbalajKaynakModulu.Sandik;
        kayit.KaynakKayitId = 91;
        kayit.M3Override = 9.5m;
        var dosyaService = new FakeAmbalajDosyaService();
        var handler = new GetAmbalajRaporDosyasiQueryHandler(
            new FakeUnitOfWork().AddRepository(kayit),
            dosyaService,
            FakeRolService.Yalniz(YetkiKodlari.Ambalaj.RaporGoruntule, YetkiKodlari.Ambalaj.PdfIndir),
            new FakeCurrentUserService());

        var sonuc = await handler.Handle(
            new GetAmbalajRaporDosyasiQuery { Tur = AmbalajSandikTuru.Ilave }, CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal("application/pdf", sonuc.Value!.IcerikTuru);
        var satir = Assert.Single(dosyaService.SonRaporSatirlari!);
        Assert.Null(satir.NetM3);
        Assert.Null(satir.SarfOrani);
        Assert.Null(satir.SarfM3);
        Assert.Null(satir.ToplamM3);
        Assert.Equal(AmbalajKaynakModulu.Manuel, satir.KaynakModul);
        Assert.Null(dosyaService.SonRaporOzeti!.NetM3);
    }

    [Fact]
    public async Task UretimFormu_EskiSurumEtiketiyleGuncelParcaFormulunuKullanmaz()
    {
        var kayit = GecerliFormKaydi();
        kayit.M3HesaplamaVersiyonu = "KER-ESKI-01";
        var sonuc = await AmbalajUretimFormuOlusturucu.OlusturAsync(new FakeUnitOfWork().AddRepository(kayit), [kayit.Id]);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains("KER-ESKI-01", sonuc.Error!.Message);
        Assert.Contains(AmbalajHesaplayici.FormulVersiyonu, sonuc.Error.Message);
    }

    [Fact]
    public async Task UretimFormu_M3DegerlerindeKaydedilmisSnapshotiKullanir()
    {
        var proje = new Proje { Id = 1, ProjeNo = "PA-SNAP", Musteri = "Snapshot" };
        var kayit = ProjeKaydi(GecerliFormKaydi(), proje.Id);
        var unitOfWork = new FakeUnitOfWork().AddRepository(proje).AddRepository(kayit);
        await FormuKaydetAsync(unitOfWork, kayit.Id);
        var dosyaService = new FakeAmbalajDosyaService();
        var handler = new GetAmbalajUretimFormuDosyasiQueryHandler(unitOfWork, dosyaService,
            FakeRolService.TumYetkiler(), new FakeCurrentUserService());
        var eskiNet = kayit.HesaplananToplamM3;
        kayit.HesaplananToplamM3 = 999;
        kayit.Boy = 9999;

        var sonuc = await handler.Handle(
            new GetAmbalajUretimFormuDosyasiQuery { ProjeId = proje.Id }, CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        var kalem = Assert.Single(dosyaService.SonUretimFormu!.Kalemler);
        Assert.Equal(eskiNet, kalem.HesaplananNetM3);
        Assert.Equal(eskiNet, kalem.NetM3);
        Assert.Equal(kayit.SarfM3, kalem.SarfM3);
        Assert.Equal(kayit.ToplamM3, kalem.ToplamM3);
        Assert.Equal(0, unitOfWork.SaveChangesSayisi);
    }

    [Fact]
    public async Task UretimFormu_YalnizSeciliKayitlariAlirVeKaynakBrutKgleriTekSorgudaGetirir()
    {
        var proje = new Proje
        {
            Id = 1,
            ProjeNo = "PA-FORM",
            Musteri = "Form Müşterisi",
            FBNo = "FB-42"
        };
        var secili = ProjeKaydi(GecerliFormKaydi(), proje.Id);
        secili.Id = 41;
        secili.KaynakModul = AmbalajKaynakModulu.Sandik;
        secili.KaynakKayitId = 501;
        var secilmeyen = ProjeKaydi(GecerliFormKaydi(), proje.Id);
        secilmeyen.Id = 42;
        secilmeyen.SandikNo = "2";
        secilmeyen.KaynakModul = AmbalajKaynakModulu.Sandik;
        secilmeyen.KaynakKayitId = 502;
        var kaynakSandik = new Sandik
        {
            Id = 501,
            ProjeId = proje.Id,
            SandikNo = "1",
            GrossKg = 875
        };
        var unitOfWork = new FakeUnitOfWork()
            .AddRepository(proje)
            .AddRepository(secili, secilmeyen)
            .AddRepository(kaynakSandik);

        var sonuc = await AmbalajUretimFormuOlusturucu.OlusturAsync(unitOfWork, [secili.Id]);

        Assert.True(sonuc.IsSuccess);
        var kalem = Assert.Single(sonuc.Value!.Kalemler);
        Assert.Equal(secili.Id, kalem.KayitId);
        Assert.Equal(875m, kalem.BrutKg);
        Assert.Equal("FB-42", sonuc.Value.FBNo);
        Assert.Equal(1, unitOfWork.Repository<Sandik>().FindAsyncSayisi);
    }

    [Fact]
    public async Task KaynakSenkronizasyonu_SandikProjeDegistirinceAyniKaydiTasir()
    {
        var hedefProje = new Proje
        {
            Id = 22,
            ProjeNo = "SAHA-022",
            Musteri = "Test Müşteri",
            ProjeTipiId = (int)ProjeTipi.Saha
        };
        var tasinanSandik = new Sandik
        {
            Id = 73,
            ProjeId = hedefProje.Id,
            SandikNo = "3",
            Ad = "Saha Sandığı",
            TipId = (int)SandikTipi.KatlanirSandik,
            Boy = 1200,
            En = 900,
            Yukseklik = 800
        };
        var mevcutKayit = new AmbalajUretimKaydi
        {
            Id = 91,
            ProjeId = 11,
            Tur = AmbalajSandikTuru.Normal,
            KaynakModul = AmbalajKaynakModulu.Sandik,
            KaynakKayitId = tasinanSandik.Id,
            SandikNo = "ESKİ-3",
            Ad = "Eski ad",
            SandikCinsi = AmbalajSandikCinsi.AhsapKapali,
            Adet = 1,
            Boy = 1000,
            En = 700,
            Yukseklik = 600,
            AmbalajaDahil = true,
            UretimeAlindi = false,
            UretimDurumu = AmbalajUretimDurumu.Planlandi
        };
        var unitOfWork = new FakeUnitOfWork()
            .AddRepository(hedefProje)
            .AddRepository(tasinanSandik)
            .AddRepository(mevcutKayit)
            .AddRepository<AmbalajUretimHareketi>();
        var finans = new FakeFinansAktarimService();
        var handler = new AmbalajKaynakSenkronizasyonService(unitOfWork, finans);

        var sonuc = await handler.SenkronizeEtAsync(hedefProje.Id, new FakeCurrentUserService(), CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(0, sonuc.Value!.Eklenen);
        Assert.Equal(1, sonuc.Value.Guncellenen);
        Assert.Single(unitOfWork.Repository<AmbalajUretimKaydi>().Items);
        Assert.Same(mevcutKayit, unitOfWork.Repository<AmbalajUretimKaydi>().Items.Single());
        Assert.Equal(hedefProje.Id, mevcutKayit.ProjeId);
        Assert.Equal(AmbalajKaynakModulu.Saha, mevcutKayit.KaynakModul);
        Assert.Equal(AmbalajSandikTuru.Saha, mevcutKayit.Tur);
        Assert.Equal(tasinanSandik.Id, mevcutKayit.KaynakKayitId);
        Assert.Empty(finans.AktarilanGruplar);
    }

    [Theory]
    [InlineData(ProjeTipi.Normal, AmbalajKaynakModulu.Sandik, AmbalajSandikTuru.Normal)]
    [InlineData(ProjeTipi.Saha, AmbalajKaynakModulu.Saha, AmbalajSandikTuru.Saha)]
    [InlineData(ProjeTipi.Yedek, AmbalajKaynakModulu.Yedek, AmbalajSandikTuru.Yedek)]
    public async Task KaynakSenkronizasyonu_ProjeTipiniDogruKaynakVeSandikTuruneEsler(
        ProjeTipi projeTipi,
        AmbalajKaynakModulu beklenenKaynak,
        AmbalajSandikTuru beklenenTur)
    {
        var proje = new Proje { Id = 81, ProjeNo = "P-81", Musteri = "M", ProjeTipiId = (int)projeTipi };
        var sandik = new Sandik
        {
            Id = 810,
            ProjeId = proje.Id,
            SandikNo = "1",
            Ad = "Kaynak sandık",
            TipId = (int)SandikTipi.AhsapKapali,
            Boy = 1000,
            En = 1000,
            Yukseklik = 1000
        };
        var unitOfWork = new FakeUnitOfWork()
            .AddRepository(proje)
            .AddRepository(sandik)
            .AddRepository<AmbalajUretimKaydi>()
            .AddRepository<AmbalajUretimHareketi>();
        var finans = new FakeFinansAktarimService();
        IAmbalajKaynakSenkronizasyonService service = new AmbalajKaynakSenkronizasyonService(unitOfWork, finans);

        var sonuc = await service.SenkronizeEtAsync(
            proje.Id, new FakeCurrentUserService(), CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        var kayit = Assert.Single(unitOfWork.Repository<AmbalajUretimKaydi>().Items);
        Assert.Equal(beklenenKaynak, kayit.KaynakModul);
        Assert.Equal(beklenenTur, kayit.Tur);
        Assert.Equal(sandik.Id, kayit.KaynakKayitId);
        Assert.False(kayit.UretimeAlindi);
        Assert.Null(kayit.UretimTarihi);
        Assert.Empty(finans.AktarilanGruplar);
        Assert.All(unitOfWork.Repository<AmbalajUretimHareketi>().Items,
            hareket => Assert.Equal(7, hareket.KullaniciId));
    }

    [Fact]
    public void Planlama_SenkronizeEdilmemisKaynakSandigiUretimeSeciliBaslatmaz()
    {
        var proje = new Proje { Id = 82, ProjeNo = "PA-82", Musteri = "M", ProjeTipiId = 1 };
        var sandik = new Sandik
        {
            Id = 820,
            ProjeId = proje.Id,
            SandikNo = "1",
            Ad = "Henüz senkronize edilmemiş kaynak",
            Boy = 1000,
            En = 1000,
            Yukseklik = 1000
        };

        var plan = AmbalajPlanlamaYardimcisi.PlanDtoOlustur(
            proje,
            "Normal",
            [sandik],
            []);

        var kalem = Assert.Single(plan.Kalemler);
        Assert.False(kalem.UretimeAlindi);
        Assert.Equal(0, plan.SeciliSandikAdedi);
        Assert.Equal(0, plan.SeciliHacimM3);
    }

    [Fact]
    public void FinansAktarimi_YalnizAcikcaUretimeAlinmisKaydiAktifSayar()
    {
        var kayit = GecerliFormKaydi();
        kayit.UretimTarihi = null;

        var otomatikSecim = AmbalajFinansSenkronizasyonu.ModelOlustur(kayit, null);

        Assert.False(otomatikSecim.KaynakAktif);
        Assert.False(AmbalajUretimYardimcilari.DtoOlustur(kayit).FinansAktarimaHazirMi);

        kayit.UretimTarihi = new DateTime(2026, 9, 1, 9, 30, 0);

        var bilincliSecim = AmbalajFinansSenkronizasyonu.ModelOlustur(kayit, null);

        Assert.True(bilincliSecim.KaynakAktif);
        Assert.True(AmbalajUretimYardimcilari.DtoOlustur(kayit).FinansAktarimaHazirMi);

        kayit.Boy = 0;
        kayit.En = 0;
        kayit.Yukseklik = 0;
        kayit.M3Override = 1.5m;

        var manuelM3Secimi = AmbalajFinansSenkronizasyonu.ModelOlustur(kayit, null);

        Assert.True(manuelM3Secimi.KaynakAktif);
        Assert.True(AmbalajUretimYardimcilari.DtoOlustur(kayit).FinansAktarimaHazirMi);

        kayit.M3Override = null;

        var gecersizMiktar = AmbalajFinansSenkronizasyonu.ModelOlustur(kayit, null);

        Assert.False(gecersizMiktar.KaynakAktif);
        Assert.False(AmbalajUretimYardimcilari.DtoOlustur(kayit).FinansAktarimaHazirMi);
    }

    [Fact]
    public async Task KaynakSenkronizasyonu_EksileniSistemIptalEder_ReappearOluncaYalnizSistemIptaliniAcar()
    {
        var proje = new Proje { Id = 5, ProjeNo = "PA-5", Musteri = "M", ProjeTipiId = 1 };
        var kayit = GecerliFormKaydi();
        kayit.ProjeId = proje.Id;
        kayit.ManuelProjeNo = null;
        kayit.ManuelProjeAdi = null;
        kayit.KaynakModul = AmbalajKaynakModulu.Sandik;
        kayit.KaynakKayitId = 55;
        kayit.UretimDurumu = AmbalajUretimDurumu.Planlandi;
        var unitOfWork = new FakeUnitOfWork()
            .AddRepository(proje)
            .AddRepository<Sandik>()
            .AddRepository(kayit)
            .AddRepository<AmbalajUretimHareketi>();
        var finans = new FakeFinansAktarimService();
        var handler = new AmbalajKaynakSenkronizasyonService(unitOfWork, finans);
        unitOfWork.ErisimTakibiniBaslat();

        var eksilme = await handler.SenkronizeEtAsync(proje.Id, new FakeCurrentUserService(), CancellationToken.None);

        Assert.True(eksilme.IsSuccess);
        Assert.True(kayit.IptalMi);
        Assert.Null(kayit.IptalEdenKullaniciId);
        Assert.Equal("system-source-missing", kayit.IptalNedeni);
        Assert.False(Assert.Single(finans.AktarilanGruplar).Single().KaynakAktif);
        Assert.True(unitOfWork.TumTakipliErisimlerTransactionIcinde);

        unitOfWork.Repository<Sandik>().Items.Add(new Sandik
        {
            Id = 55,
            ProjeId = proje.Id,
            SandikNo = "55",
            Ad = "Geri gelen",
            TipId = (int)SandikTipi.AhsapKapali,
            Boy = 2500,
            En = 1500,
            Yukseklik = 1800
        });
        var geriDonus = await handler.SenkronizeEtAsync(proje.Id, new FakeCurrentUserService(), CancellationToken.None);

        Assert.True(geriDonus.IsSuccess);
        Assert.False(kayit.IptalMi);
        Assert.Null(kayit.IptalNedeni);
        Assert.True(finans.AktarilanGruplar.Last().Single().KaynakAktif);
    }

    [Fact]
    public async Task KaynakSenkronizasyonu_KullaniciIptaliniAyniNedenMetniOlsaBileGeriAcmaz()
    {
        var proje = new Proje { Id = 6, ProjeNo = "PA-6", Musteri = "M", ProjeTipiId = 1 };
        var sandik = new Sandik
        {
            Id = 66,
            ProjeId = proje.Id,
            SandikNo = "66",
            TipId = (int)SandikTipi.AhsapKapali,
            Boy = 1000,
            En = 1000,
            Yukseklik = 1000
        };
        var kayit = GecerliFormKaydi();
        kayit.ProjeId = proje.Id;
        kayit.ManuelProjeNo = null;
        kayit.ManuelProjeAdi = null;
        kayit.KaynakModul = AmbalajKaynakModulu.Sandik;
        kayit.KaynakKayitId = sandik.Id;
        kayit.UretimDurumu = AmbalajUretimDurumu.Planlandi;
        kayit.IptalMi = true;
        kayit.IptalEdenKullaniciId = 7;
        kayit.IptalNedeni = "system-source-missing";
        var unitOfWork = new FakeUnitOfWork()
            .AddRepository(proje)
            .AddRepository(sandik)
            .AddRepository(kayit)
            .AddRepository<AmbalajUretimHareketi>();
        var handler = new AmbalajKaynakSenkronizasyonService(unitOfWork, new FakeFinansAktarimService());

        var sonuc = await handler.SenkronizeEtAsync(proje.Id, new FakeCurrentUserService(), CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.True(kayit.IptalMi);
        Assert.Equal(1, sonuc.Value!.Degismeyen);
    }

    [Theory]
    [InlineData(true, AmbalajUretimDurumu.Planlandi)]
    [InlineData(false, AmbalajUretimDurumu.Uretimde)]
    [InlineData(false, AmbalajUretimDurumu.Tamamlandi)]
    public async Task KaynakSenkronizasyonu_KilitliVeyaIlerlemişKaydiEzmez(
        bool senkronizasyonKilitli,
        AmbalajUretimDurumu uretimDurumu)
    {
        var proje = new Proje { Id = 67, ProjeNo = "PA-67", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Normal };
        var sandik = new Sandik
        {
            Id = 670,
            ProjeId = proje.Id,
            SandikNo = "KAYNAK-YENI",
            Ad = "Kaynakta değişen ad",
            TipId = (int)SandikTipi.KatlanirSandik,
            Boy = 3000,
            En = 2000,
            Yukseklik = 1500
        };
        var kayit = GecerliFormKaydi();
        kayit.Id = 671;
        kayit.ProjeId = proje.Id;
        kayit.ManuelProjeNo = null;
        kayit.ManuelProjeAdi = null;
        kayit.KaynakModul = AmbalajKaynakModulu.Sandik;
        kayit.KaynakKayitId = sandik.Id;
        kayit.KaynakSenkronizasyonuKilitliMi = senkronizasyonKilitli;
        kayit.UretimDurumu = uretimDurumu;
        kayit.SandikNo = "ADMIN-DUZELTMESI";
        kayit.Ad = "Admin tarafından düzeltilen ad";
        kayit.Boy = 2222;
        var unitOfWork = new FakeUnitOfWork()
            .AddRepository(proje)
            .AddRepository(sandik)
            .AddRepository(kayit)
            .AddRepository<AmbalajUretimHareketi>();
        var handler = new AmbalajKaynakSenkronizasyonService(unitOfWork, new FakeFinansAktarimService());

        var sonuc = await handler.SenkronizeEtAsync(proje.Id, new FakeCurrentUserService(), CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(1, sonuc.Value!.Degismeyen);
        Assert.Equal("ADMIN-DUZELTMESI", kayit.SandikNo);
        Assert.Equal("Admin tarafından düzeltilen ad", kayit.Ad);
        Assert.Equal(2222, kayit.Boy);
        Assert.Empty(unitOfWork.Repository<AmbalajUretimHareketi>().Items);
    }

    [Fact]
    public async Task BagimsizSandikOlusturma_YeniKaydiModifiedDurumunaGecirmez()
    {
        var proje = new Proje
        {
            Id = 68,
            ProjeNo = "PA-S-68",
            Musteri = "Test",
            ProjeTipiId = (int)ProjeTipi.Saha
        };
        var unitOfWork = new FakeUnitOfWork()
            .AddRepository(proje)
            .AddRepository<AmbalajUretimKaydi>()
            .AddRepository<AmbalajUretimHareketi>();
        var handler = new AmbalajBagimsizSandikKaydetCommandHandler(
            unitOfWork,
            new FakeCurrentUserService(),
            new BeklenmeyenAmbalajKaynakSenkronizasyonService(),
            new FakeFinansAktarimService());

        var sonuc = await handler.Handle(new AmbalajBagimsizSandikKaydetCommand
        {
            Tur = 4,
            ProjeId = proje.Id,
            Ad = "Saha özel sandığı",
            SandikTipi = "Ahşap Kapalı",
            Adet = 1,
            Boy = 150,
            En = 150,
            Yukseklik = 150,
            TalimatVeren = "Sinan"
        }, CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        var repository = unitOfWork.Repository<AmbalajUretimKaydi>();
        var kayit = Assert.Single(repository.Items);
        Assert.True(kayit.BagimsizKayitMi);
        Assert.Equal(0, repository.UpdateSayisi);
        Assert.Equal(1, unitOfWork.SaveChangesSayisi);
    }

    [Fact]
    public async Task IlkUretimFormu_UretimeAlinmamisPlanliKaydiKabulEder()
    {
        // Form üretimi başlatır; kayıt formdan önce üretime alınmış olmak zorunda değildir.
        var kayit = GecerliFormKaydi();
        kayit.UretimeAlindi = false;
        kayit.UretimDurumu = AmbalajUretimDurumu.Planlandi;

        var sonuc = await AmbalajUretimFormuOlusturucu.OlusturAsync(
            new FakeUnitOfWork().AddRepository(kayit), [kayit.Id]);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(kayit.Id, Assert.Single(sonuc.Value!.Kalemler).KayitId);
    }

    [Fact]
    public async Task UretimFormuDosyasi_ProjeFormunuPdfIcerikTuruVeGuvenliAdlaDondurur()
    {
        var proje = new Proje { Id = 1, ProjeNo = "PA/42", Musteri = "Dosya" };
        var kayit = ProjeKaydi(GecerliFormKaydi(), proje.Id);
        var unitOfWork = new FakeUnitOfWork().AddRepository(proje).AddRepository(kayit);
        await FormuKaydetAsync(unitOfWork, kayit.Id);
        var handler = new GetAmbalajUretimFormuDosyasiQueryHandler(
            unitOfWork, new FakeAmbalajDosyaService(), FakeRolService.TumYetkiler(), new FakeCurrentUserService());

        var sonuc = await handler.Handle(
            new GetAmbalajUretimFormuDosyasiQuery { ProjeId = proje.Id }, CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.NotEmpty(sonuc.Value!.Icerik);
        Assert.Equal("application/pdf", sonuc.Value.IcerikTuru);
        Assert.StartsWith("ambalaj-uretim-formu-PA-42-", sonuc.Value.DosyaAdi);
        Assert.EndsWith(".pdf", sonuc.Value.DosyaAdi, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SeciliSandiklarUretimFormu_AyniProjedekiIdleriTekillestiripYalnizSecilenleriUretir()
    {
        var proje = new Proje { Id = 71, ProjeNo = "PA699-02", Musteri = "Test" };
        var bir = ProjeKaydi(GecerliFormKaydi(), proje.Id);
        bir.Id = 711;
        bir.SandikNo = "7";
        var iki = ProjeKaydi(GecerliFormKaydi(), proje.Id);
        iki.Id = 712;
        iki.SandikNo = "8";
        var secilmeyen = ProjeKaydi(GecerliFormKaydi(), proje.Id);
        secilmeyen.Id = 713;
        secilmeyen.SandikNo = "9";
        var unitOfWork = new FakeUnitOfWork()
            .AddRepository(proje)
            .AddRepository(bir, iki, secilmeyen);

        var sonuc = await AmbalajUretimFormuOlusturucu.OlusturAsync(unitOfWork, [iki.Id, bir.Id, iki.Id]);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal("PA699-02", sonuc.Value!.ProjeNo);
        Assert.Equal(new[] { bir.Id, iki.Id }, sonuc.Value.Kalemler.Select(x => x.KayitId).ToArray());
        Assert.Equal(1, unitOfWork.Repository<AmbalajUretimKaydi>().FindAsyncSayisi);
    }

    [Fact]
    public async Task SeciliSandiklarUretimFormu_BulunamayanIdVarsaKismiFormUretmez()
    {
        var kayit = GecerliFormKaydi();
        var unitOfWork = new FakeUnitOfWork().AddRepository(kayit);
        var sonuc = await AmbalajUretimFormuOlusturucu.OlusturAsync(unitOfWork, [kayit.Id, 9999]);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(404, sonuc.StatusCode);
        Assert.Contains("9999", sonuc.Error!.Message);
        Assert.Null(sonuc.Value);
        Assert.Equal(0, unitOfWork.SaveChangesSayisi);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, false, true)]
    public async Task SeciliSandiklarUretimFormu_UygunOlmayanTekKayitVarsaTumTalebiReddeder(
        bool iptalMi,
        bool ambalajaDahil,
        bool uretimeAlindi)
    {
        var uygun = GecerliFormKaydi();
        uygun.Id = 801;
        var uygunOlmayan = GecerliFormKaydi();
        uygunOlmayan.Id = 802;
        uygunOlmayan.SandikNo = "2";
        uygunOlmayan.IptalMi = iptalMi;
        uygunOlmayan.AmbalajaDahil = ambalajaDahil;
        uygunOlmayan.UretimeAlindi = uretimeAlindi;
        var unitOfWork = new FakeUnitOfWork().AddRepository(uygun, uygunOlmayan);
        var sonuc = await AmbalajUretimFormuOlusturucu.OlusturAsync(unitOfWork, [uygun.Id, uygunOlmayan.Id]);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains(uygunOlmayan.Id.ToString(), sonuc.Error!.Message);
        Assert.Null(sonuc.Value);
        Assert.Equal(0, unitOfWork.SaveChangesSayisi);
    }

    [Fact]
    public async Task SeciliSandiklarUretimFormu_FarkliSistemProjeleriniKaristirmaz()
    {
        var projeBir = new Proje { Id = 91, ProjeNo = "PA-1", Musteri = "Bir" };
        var projeIki = new Proje { Id = 92, ProjeNo = "PA-2", Musteri = "İki" };
        var bir = GecerliFormKaydi();
        bir.Id = 901;
        bir.ProjeId = projeBir.Id;
        bir.ManuelProjeNo = null;
        var iki = GecerliFormKaydi();
        iki.Id = 902;
        iki.ProjeId = projeIki.Id;
        iki.ManuelProjeNo = null;
        var unitOfWork = new FakeUnitOfWork().AddRepository(projeBir, projeIki).AddRepository(bir, iki);
        var sonuc = await AmbalajUretimFormuOlusturucu.OlusturAsync(unitOfWork, [bir.Id, iki.Id]);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains("farklı projelere", sonuc.Error!.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(sonuc.Value);
        Assert.Equal(0, unitOfWork.SaveChangesSayisi);
    }

    [Fact]
    public async Task SeciliSandiklarUretimFormu_SistemVeManuelProjeKaydiniKaristirmaz()
    {
        var sistem = GecerliFormKaydi();
        sistem.Id = 911;
        sistem.ProjeId = 10;
        sistem.ManuelProjeNo = null;
        var manuel = GecerliFormKaydi();
        manuel.Id = 912;
        manuel.ProjeId = null;
        manuel.ManuelProjeNo = "MAN-10";
        var sonuc = await AmbalajUretimFormuOlusturucu.OlusturAsync(new FakeUnitOfWork().AddRepository(sistem, manuel), [sistem.Id, manuel.Id]);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
    }

    [Fact]
    public async Task SeciliSandiklarUretimFormu_FarkliManuelProjeleriKaristirmaz()
    {
        var bir = GecerliFormKaydi();
        bir.Id = 921;
        bir.ManuelProjeNo = "MAN-1";
        var iki = GecerliFormKaydi();
        iki.Id = 922;
        iki.ManuelProjeNo = "MAN-2";
        var sonuc = await AmbalajUretimFormuOlusturucu.OlusturAsync(new FakeUnitOfWork().AddRepository(bir, iki), [bir.Id, iki.Id]);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains("farklı manuel projelere", sonuc.Error!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UretimFormuDosyasiValidatoru_GecerliProjeVeTurIster()
    {
        var validator = new GetAmbalajUretimFormuDosyasiQueryValidator();

        Assert.False(validator.Validate(new GetAmbalajUretimFormuDosyasiQuery()).IsValid);
        Assert.False(validator.Validate(new GetAmbalajUretimFormuDosyasiQuery { ProjeId = -1 }).IsValid);
        Assert.False(validator.Validate(new GetAmbalajUretimFormuDosyasiQuery
        {
            ProjeId = 1,
            Tur = (AmbalajSandikTuru)99
        }).IsValid);
        Assert.True(validator.Validate(new GetAmbalajUretimFormuDosyasiQuery
        {
            ProjeId = 1,
            Tur = AmbalajSandikTuru.Ilave
        }).IsValid);
    }

    [Fact]
    public async Task SeciliSandiklarUretimFormu_BosSecimdeFormUretmez()
    {
        var unitOfWork = new FakeUnitOfWork().AddRepository(GecerliFormKaydi());

        var sonuc = await AmbalajUretimFormuOlusturucu.OlusturAsync(unitOfWork, []);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(400, sonuc.StatusCode);
        Assert.Equal(0, unitOfWork.Repository<AmbalajUretimKaydi>().FindAsyncSayisi);
    }

    [Theory]
    [InlineData("699")]
    [InlineData("PA 699")]
    [InlineData("pa699-02")]
    [InlineData("PA-699-02")]
    [InlineData("pa 699 02")]
    [InlineData("PA699–02")]
    public void ProjeAramaFiltresi_ProjeNumarasiniOnekBoslukVeTiredenBagimsizArar(string arama)
    {
        var projeler = new[]
        {
            new Proje { Id = 1, ProjeNo = "PA699-02", Musteri = "Birinci Müşteri", FBNo = "FB-100" },
            new Proje { Id = 2, ProjeNo = "PA700-01", Musteri = "İkinci Müşteri", FBNo = "FB-200" }
        };

        var proje = Assert.Single(AmbalajProjeAramaFiltresi.Uygula(projeler.AsQueryable(), arama));

        Assert.Equal("PA699-02", proje.ProjeNo);
    }

    [Theory]
    [InlineData("acme lojistik", 1)]
    [InlineData("fb-2026-700", 2)]
    public void ProjeAramaFiltresi_GelismisProjeNoAramasiMusteriVeFbAramasiniBozmaz(
        string arama,
        int beklenenProjeId)
    {
        var projeler = new[]
        {
            new Proje { Id = 1, ProjeNo = "PA699-02", Musteri = "ACME Lojistik", FBNo = "FB-2026-699" },
            new Proje { Id = 2, ProjeNo = "PA700-01", Musteri = "Başka Müşteri", FBNo = "FB-2026-700" }
        };

        var proje = Assert.Single(AmbalajProjeAramaFiltresi.Uygula(projeler.AsQueryable(), arama));

        Assert.Equal(beklenenProjeId, proje.Id);
    }

    [Fact]
    public void ProjeAramaFiltresi_PostgreSqlSorgusunaCevirilebilir()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=translation_only;Username=test;Password=test")
            .Options;
        using var context = new AppDbContext(options);

        var sql = AmbalajProjeAramaFiltresi
            .Uygula(context.Projeler.AsNoTracking(), "PA 699-02")
            .ToQueryString();

        Assert.Contains("replace", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("lower", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LIKE", sql, StringComparison.OrdinalIgnoreCase);
    }

    private static AmbalajUretimKaydi GecerliFormKaydi() => new()
    {
        Id = 31,
        IsAkisKimligi = Guid.NewGuid(),
        ManuelProjeNo = "MAN-FORM-01",
        ManuelProjeAdi = "Form Projesi",
        SandikNo = "1",
        Ad = "Ana Sandık",
        Adet = 1,
        Boy = 2500,
        En = 1500,
        Yukseklik = 1800,
        AmbalajaDahil = true,
        UretimeAlindi = true,
        UretimDurumu = AmbalajUretimDurumu.Uretimde,
        UretimTarihi = new DateTime(2026, 8, 28, 10, 0, 0),
        M3HesaplamaVersiyonu = AmbalajHesaplayici.FormulVersiyonu,
        HesaplananBirimM3 = 1.25m,
        HesaplananToplamM3 = 1.25m,
        SarfM3 = 0.1375m,
        ToplamM3 = 1.3875m
    };

    private static AmbalajUretimKaydi ProjeKaydi(AmbalajUretimKaydi kayit, int projeId)
    {
        kayit.ProjeId = projeId;
        kayit.ManuelProjeNo = null;
        kayit.ManuelProjeAdi = null;
        return kayit;
    }

    // İndirme uçları hesaplama/oluşturma yapmaz. Kurgu önce gerçek oluşturucu ile değişmez
    // snapshot üretir; indirme testleri yalnız saklanan JSON'u okur.
    private static async Task FormuKaydetAsync(FakeUnitOfWork uow, params int[] kayitIdleri)
    {
        var result = await AmbalajUretimFormuOlusturucu.OlusturAsync(uow, kayitIdleri);
        Assert.True(result.IsSuccess, result.Error?.Message);
        var form = result.Value!;
        var entity = new AmbalajUretimFormuSurumu
        {
            Id = 1, FormKimligi = Guid.NewGuid(), Surum = 1, IdempotencyAnahtari = Guid.NewGuid(),
            KapsamAnahtari = form.ProjeId.HasValue ? "P:" + form.ProjeId : "M:" + form.ProjeNo,
            ProjeId = form.ProjeId, SnapshotJson = JsonSerializer.Serialize(form), OlusturanKullaniciId = 7
        };
        uow.AddRepository(entity);
        uow.AddRepository(form.Kalemler.Select((k, i) => new AmbalajUretimFormuKaydi
        { Id = i + 1, FormSurumuId = 1, AmbalajUretimKaydiId = k.KayitId }).ToArray());
    }

    [Fact]
    public async Task UretimFormuDosyasi_SnapshotYoksaYeniFormUretmezVeDurumDegistirmez()
    {
        var kayit = ProjeKaydi(GecerliFormKaydi(), 5);
        var uow = new FakeUnitOfWork().AddRepository(kayit);
        var before = JsonSerializer.Serialize(kayit);
        var dosyaService = new FakeAmbalajDosyaService();

        var result = await new GetAmbalajUretimFormuDosyasiQueryHandler(uow, dosyaService,
            FakeRolService.TumYetkiler(), new FakeCurrentUserService()).Handle(new() { ProjeId = 5 }, default);

        Assert.Equal(404, result.StatusCode);
        Assert.Equal(before, JsonSerializer.Serialize(kayit));
        Assert.Equal(0, uow.SaveChangesSayisi);
        Assert.Empty(uow.Repository<AmbalajUretimFormuSurumu>().Items);
        Assert.Null(dosyaService.SonUretimFormu);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private readonly Dictionary<Type, object> _repositories = new();

        public FakeUnitOfWork()
        {
            AddRepository<AmbalajUretimFormuSurumu>();
            AddRepository<AmbalajUretimFormuKaydi>();
            AddRepository<AmbalajUretimGerceklesmesi>();
        }

        private bool _transactionIcinde;
        private bool _erisimTakibiAktif;

        public bool HasActiveTransaction => _transactionIcinde;
        public int SaveChangesSayisi { get; private set; }
        public bool TumTakipliErisimlerTransactionIcinde { get; private set; } = true;

        public void ErisimTakibiniBaslat() => _erisimTakibiAktif = true;

        public FakeUnitOfWork AddRepository<T>(params T[] entities) where T : BaseEntity
        {
            _repositories[typeof(T)] = new FakeRepository<T>(entities);
            return this;
        }

        public FakeRepository<T> Repository<T>() where T : BaseEntity =>
            (FakeRepository<T>)_repositories[typeof(T)];

        public IGenericRepository<T> GetRepository<T>() where T : BaseEntity
        {
            if (_erisimTakibiAktif && !_transactionIcinde)
                TumTakipliErisimlerTransactionIcinde = false;
            return Repository<T>();
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesSayisi++;
            return Task.FromResult(1);
        }

        public Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            _transactionIcinde = true;
            return ExecuteAsync();

            async Task<TResult> ExecuteAsync()
            {
                try
                {
                    return await operation(cancellationToken);
                }
                finally
                {
                    _transactionIcinde = false;
                }
            }
        }

        public void RegisterAfterCommit(Func<CancellationToken, Task> callback) =>
            throw new NotSupportedException();

        public void RegisterAfterRollback(Func<CancellationToken, Task> callback) =>
            throw new NotSupportedException();

        public void Dispose()
        {
        }
    }

    private sealed class FakeRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        public FakeRepository(IEnumerable<T> items) => Items = items.ToList();

        public List<T> Items { get; }
        public int FindAsyncSayisi { get; private set; }
        public int UpdateSayisi { get; private set; }

        public Task<T?> GetByIdAsync(int id) =>
            Task.FromResult(Items.SingleOrDefault(x => x.Id == id));

        public Task<IEnumerable<T>> GetAllAsync() =>
            Task.FromResult<IEnumerable<T>>(Items);

        public Task<IEnumerable<T>> GetAllWithIncludeAsync<TProp>(Expression<Func<T, TProp>> include) =>
            Task.FromResult<IEnumerable<T>>(Items);

        public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            FindAsyncSayisi++;
            return Task.FromResult<IEnumerable<T>>(Items.AsQueryable().Where(predicate));
        }

        public IQueryable<T> Queryable() => Items.AsQueryable();

        public Task AddAsync(T entity)
        {
            Items.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(T entity)
        {
            UpdateSayisi++;
        }

        public void Remove(T entity) => Items.Remove(entity);
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public int? UserId => 7;
        public bool IsAuthenticated => true;
    }

    private sealed class BeklenmeyenAmbalajKaynakSenkronizasyonService
        : IAmbalajKaynakSenkronizasyonService
    {
        public Task<Result<AmbalajSenkronizasyonSonucu>> SenkronizeEtAsync(
            int projeId,
            ICurrentUserService islemiYapan,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Manuel saha sandığında kaynak senkronizasyonu çalışmamalıdır.");
    }

    private sealed class FakeFinansAktarimService : IFinansUretimAktarimService
    {
        public List<IReadOnlyList<FinansUretimAktarimModel>> AktarilanGruplar { get; } = new();

        public Task<FinansSenkronizasyonSonucModel> UretimKayitlariniAktarAsync(
            IReadOnlyList<FinansUretimAktarimModel> modeller,
            CancellationToken cancellationToken)
        {
            AktarilanGruplar.Add(modeller.ToList());
            return Task.FromResult(new FinansSenkronizasyonSonucModel(0, modeller.Count, 0));
        }
    }

    private sealed class FakeAmbalajDosyaService : IAmbalajRaporDosyaService
    {
        public AmbalajUretimFormuModel? SonUretimFormu { get; private set; }
        public IReadOnlyList<AmbalajRaporSatiri>? SonRaporSatirlari { get; private set; }
        public AmbalajRaporOzeti? SonRaporOzeti { get; private set; }

        public byte[] ExcelOlustur(IReadOnlyList<AmbalajRaporSatiri> satirlar, AmbalajRaporOzeti ozet) => [1];

        public byte[] PdfOlustur(IReadOnlyList<AmbalajRaporSatiri> satirlar, AmbalajRaporOzeti ozet)
        {
            SonRaporSatirlari = satirlar;
            SonRaporOzeti = ozet;
            return [2];
        }

        public byte[] UretimFormuExcelOlustur(AmbalajUretimFormuModel form)
        {
            SonUretimFormu = form;
            return [3, 4];
        }

        public byte[] UretimFormuPdfOlustur(AmbalajUretimFormuModel form)
        {
            SonUretimFormu = form;
            return [5, 6];
        }
    }

    private sealed class FakeRolService : IRolService
    {
        private readonly HashSet<string> _yetkiler;

        private FakeRolService(IEnumerable<string> yetkiler) =>
            _yetkiler = yetkiler.ToHashSet(StringComparer.OrdinalIgnoreCase);

        public static FakeRolService Yalniz(params string[] yetkiler) => new(yetkiler);

        public static FakeRolService TumYetkiler() => new(typeof(YetkiKodlari.Ambalaj)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!));

        public Task<bool> HasUserPermissionAsync(
            int userId,
            string menuKod,
            YetkiTipi requiredYetkiTipi,
            CancellationToken ct = default) => Task.FromResult(_yetkiler.Contains(menuKod));

        public Task<List<MenuTanimi>> GetMenuAgaciAsync(CancellationToken ct = default) => Task.FromResult(new List<MenuTanimi>());
        public Task<List<RolYetki>> GetRolYetkileriAsync(int rolId, CancellationToken ct = default) => Task.FromResult(new List<RolYetki>());
        public Task YetkileriGuncelleAsync(int rolId, List<RolYetki> yetkiler, CancellationToken ct = default) => Task.CompletedTask;
    }
}
