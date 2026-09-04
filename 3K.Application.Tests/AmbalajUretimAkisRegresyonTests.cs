using System.Linq.Expressions;
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

namespace _3K.Application.Tests;

public sealed class AmbalajUretimAkisRegresyonTests
{
    [Fact]
    public void OlusturmaYetkileri_TurVeManuelProjeyiAndMantigiylaIster()
    {
        var command = new AmbalajUretimKaydiOlusturCommand
        {
            ProjeId = null,
            ManuelProjeNo = "MAN-1",
            ManuelProjeAdi = "Manuel",
            Tur = AmbalajSandikTuru.Ilave
        };

        var kodlar = command.RequiredMenuPermissions.Select(x => x.MenuKod).ToArray();

        Assert.Contains(AmbalajMenuKodlari.KayitDuzenle, kodlar);
        Assert.Contains(AmbalajMenuKodlari.IlaveOlustur, kodlar);
        Assert.Contains(AmbalajMenuKodlari.ManuelProje, kodlar);
        Assert.All(command.RequiredMenuPermissions, requirement => Assert.Equal(YetkiTipi.W, requirement.YetkiTipi));
    }

    [Theory]
    [InlineData("pdf", "ambalaj-uretim-listesi")]
    [InlineData("xlsx", "ambalaj-uretim-listesi")]
    public void RaporDosyasi_FormatYetkisiniDigerGereksinimlerleBirlikteIster(
        string format,
        string beklenenFormatKodu)
    {
        var query = new GetAmbalajRaporDosyasiQuery { Format = format };
        var kodlar = query.RequiredMenuPermissions.Select(x => x.MenuKod).ToArray();

        Assert.Contains(AmbalajMenuKodlari.RaporGoruntule, kodlar);
        Assert.Contains(AmbalajMenuKodlari.M3Goruntule, kodlar);
        Assert.Contains(AmbalajMenuKodlari.SarfGoruntule, kodlar);
        Assert.Contains(AmbalajMenuKodlari.KaynakGoruntule, kodlar);
        Assert.Contains(beklenenFormatKodu, kodlar);
        Assert.Contains(query.RequiredMenuPermissions, x => x.MenuKod == beklenenFormatKodu && x.YetkiTipi == YetkiTipi.W);
    }

    [Fact]
    public async Task Liste_RootOkumaYetkisiyleM3SarfVeKaynakAlanlariniGosterir()
    {
        var kayit = GecerliFormKaydi();
        kayit.KaynakModul = AmbalajKaynakModulu.Sandik;
        kayit.KaynakKayitId = 91;
        kayit.M3Override = 9.5m;
        kayit.M3OverrideNedeni = "Gizli";
        var unitOfWork = new FakeUnitOfWork().AddRepository(kayit);
        var handler = new GetAmbalajUretimKayitlariQueryHandler(
            unitOfWork,
            FakeRolService.Yalniz(AmbalajMenuKodlari.Listele),
            new FakeCurrentUserService());

        var sonuc = await handler.Handle(new GetAmbalajUretimKayitlariQuery(), CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        var dto = Assert.Single(sonuc.Value!.Items);
        Assert.True(dto.M3BilgisiGorunurMu);
        Assert.True(dto.SarfBilgisiGorunurMu);
        Assert.True(dto.KaynakBilgisiGorunurMu);
        Assert.Equal(kayit.M3Override, dto.NetM3);
        Assert.Equal(kayit.SarfM3, dto.SarfM3);
        Assert.Equal(kayit.ToplamM3, dto.ToplamM3);
        Assert.Equal(9.5m, dto.M3Override);
        Assert.Equal(91, dto.KaynakKayitId);
    }

    [Fact]
    public async Task Detay_RootOkumaYetkisiyleAuditDegerleriniGosterir()
    {
        var kayit = GecerliFormKaydi();
        var hareket = new AmbalajUretimHareketi
        {
            Id = 2,
            AmbalajUretimKaydiId = kayit.Id,
            KullaniciId = 7,
            Islem = "Kaynak sandık değişiklikleri senkronize edildi",
            AlanAdi = nameof(AmbalajUretimKaydi.HesaplananToplamM3),
            EskiDeger = "1.2",
            YeniDeger = "3.4",
            Aciklama = "Kaynak: gizli"
        };
        var unitOfWork = new FakeUnitOfWork()
            .AddRepository(kayit)
            .AddRepository(hareket);
        var handler = new GetAmbalajUretimKaydiDetayQueryHandler(
            unitOfWork,
            FakeRolService.Yalniz(AmbalajMenuKodlari.Listele),
            new FakeCurrentUserService());

        var sonuc = await handler.Handle(
            new GetAmbalajUretimKaydiDetayQuery { Id = kayit.Id }, CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        var maskeliHareket = Assert.Single(sonuc.Value!.Hareketler);
        Assert.False(maskeliHareket.DegerlerGizliMi);
        Assert.Equal("1.2", maskeliHareket.EskiDeger);
        Assert.Equal("3.4", maskeliHareket.YeniDeger);
        Assert.Equal("Kaynak: gizli", maskeliHareket.Aciklama);
        Assert.Equal(nameof(AmbalajUretimKaydi.HesaplananToplamM3), maskeliHareket.AlanAdi);
    }

    [Fact]
    public async Task SecimHandleri_KokMenuYetkisiyleDahilKaydiHaricTutar()
    {
        var kayit = GecerliFormKaydi();
        kayit.KaynakKayitId = null;
        kayit.KaynakModul = AmbalajKaynakModulu.Manuel;
        var unitOfWork = new FakeUnitOfWork()
            .AddRepository(kayit)
            .AddRepository<AmbalajUretimHareketi>();
        var handler = new AmbalajUretimSecimGuncelleCommandHandler(
            unitOfWork,
            new FakeCurrentUserService(),
            new FakeFinansAktarimService(),
            FakeRolService.Yalniz(AmbalajMenuKodlari.Listele));

        var sonuc = await handler.Handle(new AmbalajUretimSecimGuncelleCommand
        {
            Id = kayit.Id,
            AmbalajaDahil = false,
            UretimeAlindi = false
        }, CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.False(kayit.AmbalajaDahil);
        Assert.False(kayit.UretimeAlindi);
    }

    [Fact]
    public async Task SecimHandleri_DuzenlemeYetkisiyleKaynakKaydaMudahaleEder()
    {
        var kayit = GecerliFormKaydi();
        kayit.KaynakModul = AmbalajKaynakModulu.Sandik;
        kayit.KaynakKayitId = 77;
        var unitOfWork = new FakeUnitOfWork()
            .AddRepository(kayit)
            .AddRepository<AmbalajUretimHareketi>();
        var handler = new AmbalajUretimSecimGuncelleCommandHandler(
            unitOfWork,
            new FakeCurrentUserService(),
            new FakeFinansAktarimService(),
            FakeRolService.Yalniz(
                AmbalajMenuKodlari.Listele,
                AmbalajMenuKodlari.HaricTut,
                AmbalajMenuKodlari.UretimdenCikar));

        var sonuc = await handler.Handle(new AmbalajUretimSecimGuncelleCommand
        {
            Id = kayit.Id,
            AmbalajaDahil = false,
            UretimeAlindi = false
        }, CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.False(kayit.AmbalajaDahil);
    }

    [Fact]
    public async Task SecimHandleri_ManuelM3VarsaEksikOlculuKaydiUretimeAlir()
    {
        var kayit = GecerliFormKaydi();
        kayit.KaynakModul = AmbalajKaynakModulu.Manuel;
        kayit.KaynakKayitId = null;
        kayit.Boy = 0;
        kayit.En = 0;
        kayit.Yukseklik = 0;
        kayit.M3Override = 2.25m;
        kayit.UretimeAlindi = false;
        kayit.UretimTarihi = null;
        var unitOfWork = new FakeUnitOfWork()
            .AddRepository(kayit)
            .AddRepository<AmbalajUretimHareketi>();
        var finans = new FakeFinansAktarimService();
        var handler = new AmbalajUretimSecimGuncelleCommandHandler(
            unitOfWork,
            new FakeCurrentUserService(),
            finans,
            FakeRolService.TumYetkiler());

        var sonuc = await handler.Handle(new AmbalajUretimSecimGuncelleCommand
        {
            Id = kayit.Id,
            AmbalajaDahil = true,
            UretimeAlindi = true
        }, CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.True(kayit.UretimeAlindi);
        Assert.NotNull(kayit.UretimTarihi);
        Assert.True(finans.AktarilanGruplar.Last().Single().KaynakAktif);
    }

    [Fact]
    public async Task GuncellemeHandleri_DuzenlemeYetkisiyleOlcuDiffiniUygular()
    {
        var kayit = GecerliFormKaydi();
        kayit.KaynakModul = AmbalajKaynakModulu.Manuel;
        kayit.KaynakKayitId = null;
        var unitOfWork = new FakeUnitOfWork()
            .AddRepository(kayit)
            .AddRepository<AmbalajUretimHareketi>();
        var handler = new AmbalajUretimKaydiGuncelleCommandHandler(
            unitOfWork,
            new FakeCurrentUserService(),
            new FakeFinansAktarimService(),
            FakeRolService.Yalniz(AmbalajMenuKodlari.KayitDuzenle));
        var command = GuncellemeKomutu(kayit);
        command.Boy += 100;

        var sonuc = await handler.Handle(command, CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(2600, kayit.Boy);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    public async Task TekKayitUretimFormu_UygunOlmayanKaydiReddeder(
        bool iptalMi,
        bool ambalajaDahil,
        bool uretimeAlindi)
    {
        var kayit = GecerliFormKaydi();
        kayit.IptalMi = iptalMi;
        kayit.AmbalajaDahil = ambalajaDahil;
        kayit.UretimeAlindi = uretimeAlindi;
        var handler = new GetAmbalajUretimFormuQueryHandler(
            new FakeUnitOfWork().AddRepository(kayit));

        var sonuc = await handler.Handle(
            new GetAmbalajUretimFormuQuery { KayitId = kayit.Id },
            CancellationToken.None);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
    }

    [Fact]
    public async Task UretimFormu_EskiSurumEtiketiyleGuncelParcaFormulunuKullanmaz()
    {
        var kayit = GecerliFormKaydi();
        kayit.M3HesaplamaVersiyonu = "KER-ESKI-01";
        var handler = new GetAmbalajUretimFormuQueryHandler(
            new FakeUnitOfWork().AddRepository(kayit));

        var sonuc = await handler.Handle(
            new GetAmbalajUretimFormuQuery { KayitId = kayit.Id },
            CancellationToken.None);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains("KER-ESKI-01", sonuc.Error!.Message);
        Assert.Contains(AmbalajHesaplayici.FormulVersiyonu, sonuc.Error.Message);
    }

    [Fact]
    public async Task UretimFormu_M3DegerlerindeKaydedilmisSnapshotiKullanir()
    {
        var kayit = GecerliFormKaydi();
        var handler = new GetAmbalajUretimFormuQueryHandler(
            new FakeUnitOfWork().AddRepository(kayit));

        var sonuc = await handler.Handle(
            new GetAmbalajUretimFormuQuery { KayitId = kayit.Id },
            CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        var kalem = Assert.Single(sonuc.Value!.Kalemler);
        Assert.Equal(kayit.HesaplananToplamM3, kalem.HesaplananNetM3);
        Assert.Equal(kayit.HesaplananToplamM3, kalem.NetM3);
        Assert.Equal(kayit.SarfM3, kalem.SarfM3);
        Assert.Equal(kayit.ToplamM3, kalem.ToplamM3);
    }

    [Fact]
    public async Task ProjeUretimFormu_YalnizSeciliKayitlariAlirVeKaynakBrutKgleriTekSorgudaGetirir()
    {
        var proje = new Proje
        {
            Id = 1,
            ProjeNo = "PA-FORM",
            Musteri = "Form Müşterisi",
            FBNo = "FB-42"
        };
        var secili = GecerliFormKaydi();
        secili.Id = 41;
        secili.ProjeId = proje.Id;
        secili.ManuelProjeNo = null;
        secili.KaynakModul = AmbalajKaynakModulu.Sandik;
        secili.KaynakKayitId = 501;
        var secilmeyen = GecerliFormKaydi();
        secilmeyen.Id = 42;
        secilmeyen.ProjeId = proje.Id;
        secilmeyen.ManuelProjeNo = null;
        secilmeyen.SandikNo = "2";
        secilmeyen.UretimeAlindi = false;
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
        var handler = new GetAmbalajUretimFormuQueryHandler(unitOfWork);

        var sonuc = await handler.Handle(
            new GetAmbalajUretimFormuQuery { ProjeId = proje.Id },
            CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        var kalem = Assert.Single(sonuc.Value!.Kalemler);
        Assert.Equal(secili.Id, kalem.KayitId);
        Assert.Equal(875m, kalem.BrutKg);
        Assert.Equal("FB-42", sonuc.Value.FBNo);
        Assert.Equal(1, unitOfWork.Repository<Sandik>().FindAsyncSayisi);
    }

    [Fact]
    public async Task IptalVeAktiflestir_UretimeAlindiSeciminiKorur()
    {
        var kayit = new AmbalajUretimKaydi
        {
            Id = 41,
            IsAkisKimligi = Guid.NewGuid(),
            ManuelProjeNo = "MAN-001",
            ManuelProjeAdi = "Manuel Proje",
            SandikNo = "1",
            Ad = "Ana Sandık",
            Adet = 1,
            Boy = 2500,
            En = 1500,
            Yukseklik = 1800,
            AmbalajaDahil = true,
            UretimeAlindi = true,
            UretimDurumu = AmbalajUretimDurumu.Tamamlandi,
            UretimTarihi = new DateTime(2026, 8, 28, 10, 0, 0),
            HesaplananBirimM3 = 1.25m,
            HesaplananToplamM3 = 1.25m,
            SarfM3 = 0.1375m,
            ToplamM3 = 1.3875m
        };
        var unitOfWork = new FakeUnitOfWork()
            .AddRepository(kayit)
            .AddRepository<AmbalajUretimHareketi>();
        var finans = new FakeFinansAktarimService();
        var kullanici = new FakeCurrentUserService();

        var roller = FakeRolService.TumYetkiler();
        var iptalHandler = new AmbalajUretimKaydiIptalEtCommandHandler(unitOfWork, kullanici, finans, roller);
        var iptalSonucu = await iptalHandler.Handle(
            new AmbalajUretimKaydiIptalEtCommand { Id = kayit.Id, Neden = "Test iptali" },
            CancellationToken.None);

        Assert.True(iptalSonucu.IsSuccess);
        Assert.True(kayit.IptalMi);
        Assert.True(kayit.UretimeAlindi);
        var finansAktarimi = Assert.Single(finans.AktarilanGruplar).Single();
        Assert.False(finansAktarimi.KaynakAktif);
        var finansM3 = decimal.Round(finansAktarimi.Adet * finansAktarimi.BirimM3, 6);
        Assert.Equal(kayit.HesaplananToplamM3, finansM3);
        Assert.NotEqual(kayit.ToplamM3, finansM3);
        Assert.NotEqual(kayit.SarfM3, finansM3);

        var aktiflestirHandler = new AmbalajUretimKaydiAktiflestirCommandHandler(
            unitOfWork, kullanici, finans, roller);
        var aktiflestirSonucu = await aktiflestirHandler.Handle(
            new AmbalajUretimKaydiAktiflestirCommand { Id = kayit.Id, Aciklama = "İptal geri alındı" },
            CancellationToken.None);

        Assert.True(aktiflestirSonucu.IsSuccess);
        Assert.False(kayit.IptalMi);
        Assert.True(kayit.UretimeAlindi);
        Assert.Equal(AmbalajUretimDurumu.Tamamlandi, kayit.UretimDurumu);
        Assert.True(finans.AktarilanGruplar.Last().Single().KaynakAktif);
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
        var handler = new AmbalajKaynaklariSenkronizeEtCommandHandler(
            unitOfWork,
            new FakeCurrentUserService(),
            finans,
            FakeRolService.TumYetkiler());

        var sonuc = await handler.Handle(
            new AmbalajKaynaklariSenkronizeEtCommand { ProjeId = hedefProje.Id },
            CancellationToken.None);

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
        IAmbalajKaynakSenkronizasyonService service = new AmbalajKaynaklariSenkronizeEtCommandHandler(
            unitOfWork, new FakeCurrentUserService(), finans, FakeRolService.TumYetkiler());

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
    public async Task KaynakSenkronizasyonu_ArkaPlandaDetayliKayitProjeksiyonuOlusturmaz()
    {
        var proje = new Proje { Id = 82, ProjeNo = "PA-82", Musteri = "M", ProjeTipiId = 1 };
        var sandik = new Sandik
        {
            Id = 820,
            ProjeId = proje.Id,
            SandikNo = "1",
            Ad = "Kaynak sandik",
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
        IAmbalajKaynakSenkronizasyonService service = new AmbalajKaynaklariSenkronizeEtCommandHandler(
            unitOfWork,
            new FakeCurrentUserService(),
            new FakeFinansAktarimService(),
            FakeRolService.TumYetkiler());

        var sonuc = await service.SenkronizeEtAsync(
            proje.Id,
            new FakeCurrentUserService(),
            CancellationToken.None,
            sonucKayitlariniOlustur: false);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(1, sonuc.Value!.Eklenen);
        Assert.Empty(sonuc.Value.Kayitlar);
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
    public async Task M3OverrideHandleri_SeciliEksikOlculuKayittanManuelM3uKaldirmaz()
    {
        var kayit = GecerliFormKaydi();
        kayit.Boy = 0;
        kayit.En = 0;
        kayit.Yukseklik = 0;
        kayit.M3Override = 2.25m;
        var unitOfWork = new FakeUnitOfWork()
            .AddRepository(kayit)
            .AddRepository<AmbalajUretimHareketi>();
        var finans = new FakeFinansAktarimService();
        var handler = new AmbalajM3OverrideGuncelleCommandHandler(
            unitOfWork,
            new FakeCurrentUserService(),
            finans,
            FakeRolService.TumYetkiler());

        var sonuc = await handler.Handle(new AmbalajM3OverrideGuncelleCommand
        {
            Id = kayit.Id,
            M3Override = null,
            Neden = "Manuel değer kaldırılıyor"
        }, CancellationToken.None);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Equal(2.25m, kayit.M3Override);
        Assert.Empty(finans.AktarilanGruplar);
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
        var handler = new AmbalajKaynaklariSenkronizeEtCommandHandler(
            unitOfWork, new FakeCurrentUserService(), finans, FakeRolService.TumYetkiler());
        unitOfWork.ErisimTakibiniBaslat();

        var eksilme = await handler.Handle(
            new AmbalajKaynaklariSenkronizeEtCommand { ProjeId = proje.Id }, CancellationToken.None);

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
        var geriDonus = await handler.Handle(
            new AmbalajKaynaklariSenkronizeEtCommand { ProjeId = proje.Id }, CancellationToken.None);

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
        var handler = new AmbalajKaynaklariSenkronizeEtCommandHandler(
            unitOfWork,
            new FakeCurrentUserService(),
            new FakeFinansAktarimService(),
            FakeRolService.TumYetkiler());

        var sonuc = await handler.Handle(
            new AmbalajKaynaklariSenkronizeEtCommand { ProjeId = proje.Id }, CancellationToken.None);

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
        var handler = new AmbalajKaynaklariSenkronizeEtCommandHandler(
            unitOfWork, new FakeCurrentUserService(), new FakeFinansAktarimService(), FakeRolService.TumYetkiler());

        var sonuc = await handler.Handle(
            new AmbalajKaynaklariSenkronizeEtCommand { ProjeId = proje.Id }, CancellationToken.None);

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
    public async Task BagimsizKayit_DuzenlemeYetkiliKullaniciOlustururVeModulYetkisiyleTumKayitlariListeler()
    {
        var unitOfWork = new FakeUnitOfWork()
            .AddRepository<AmbalajUretimKaydi>()
            .AddRepository<AmbalajUretimHareketi>();
        var roller = FakeRolService.Sinirli(AmbalajMenuKodlari.KayitDuzenle, AmbalajMenuKodlari.Listele);
        var olustur = new AmbalajUretimKaydiOlusturCommandHandler(
            unitOfWork, new FakeCurrentUserService(), new FakeFinansAktarimService(), roller);

        var sonuc = await olustur.Handle(new AmbalajUretimKaydiOlusturCommand
        {
            ManuelProjeNo = "MAN-OWN-1",
            ManuelProjeAdi = "Sahipli proje",
            Tur = AmbalajSandikTuru.Normal,
            SandikNo = "1",
            Adet = 1,
            Boy = 1000,
            En = 1000,
            Yukseklik = 1000
        }, CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal("7", Assert.Single(unitOfWork.Repository<AmbalajUretimKaydi>().Items).CreatedBy);

        unitOfWork.Repository<AmbalajUretimKaydi>().Items.Add(new AmbalajUretimKaydi
        {
            Id = 99, CreatedBy = "8", ManuelProjeNo = "MAN-OTHER", SandikNo = "1", Adet = 1
        });
        unitOfWork.Repository<AmbalajUretimKaydi>().Items.Add(new AmbalajUretimKaydi
        {
            Id = 100, CreatedBy = null, ManuelProjeNo = "MAN-LEGACY", SandikNo = "1", Adet = 1
        });
        var liste = new GetAmbalajUretimKayitlariQueryHandler(unitOfWork, roller, new FakeCurrentUserService());
        var listeSonucu = await liste.Handle(new GetAmbalajUretimKayitlariQuery(), CancellationToken.None);

        Assert.True(listeSonucu.IsSuccess);
        Assert.Equal(3, listeSonucu.Value!.Items.Count);
        Assert.Equal(
            new[] { "MAN-LEGACY", "MAN-OTHER", "MAN-OWN-1" },
            listeSonucu.Value.Items.Select(x => x.ProjeNo).Order().ToArray());
    }

    [Fact]
    public async Task ManuelProjeFormu_AyniNumaradakiTumSandiklariToplar()
    {
        var bir = GecerliFormKaydi();
        bir.Id = 101;
        bir.CreatedBy = "7";
        bir.ManuelProjeNo = "MAN-GRUP";
        var iki = GecerliFormKaydi();
        iki.Id = 102;
        iki.CreatedBy = "7";
        iki.ManuelProjeNo = "MAN-GRUP";
        iki.SandikNo = "2";
        var baskasinin = GecerliFormKaydi();
        baskasinin.Id = 103;
        baskasinin.CreatedBy = "8";
        baskasinin.ManuelProjeNo = "MAN-GRUP";
        var handler = new GetAmbalajUretimFormuQueryHandler(
            new FakeUnitOfWork().AddRepository(bir, iki, baskasinin));

        var sonuc = await handler.Handle(
            new GetAmbalajUretimFormuQuery { ManuelProjeNo = "MAN-GRUP" }, CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(3, sonuc.Value!.Kalemler.Count);
        Assert.Equal(bir.ToplamM3 + iki.ToplamM3 + baskasinin.ToplamM3, sonuc.Value.ToplamM3);
    }

    [Fact]
    public async Task UretimFormuDosyasi_UretimeAlinmamisPlanliKaydi409IleReddeder()
    {
        var kayit = GecerliFormKaydi();
        kayit.UretimeAlindi = false;
        kayit.UretimDurumu = AmbalajUretimDurumu.Planlandi;
        var handler = new GetAmbalajUretimFormuDosyasiQueryHandler(
            new FakeUnitOfWork().AddRepository(kayit), new FakeAmbalajDosyaService());

        var sonuc = await handler.Handle(
            new GetAmbalajUretimFormuDosyasiQuery { KayitId = kayit.Id, Format = "pdf" },
            CancellationToken.None);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains("üretime alınmış", sonuc.Error!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("pdf", "application/pdf", ".pdf")]
    [InlineData("xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx")]
    public async Task UretimFormuDosyasi_SeciliKaydiDogruIcerikTuruVeAdlaDondurur(
        string format,
        string expectedContentType,
        string expectedExtension)
    {
        var kayit = GecerliFormKaydi();
        var handler = new GetAmbalajUretimFormuDosyasiQueryHandler(
            new FakeUnitOfWork().AddRepository(kayit), new FakeAmbalajDosyaService());

        var sonuc = await handler.Handle(
            new GetAmbalajUretimFormuDosyasiQuery { KayitId = kayit.Id, Format = format },
            CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.NotEmpty(sonuc.Value!.Icerik);
        Assert.Equal(expectedContentType, sonuc.Value.IcerikTuru);
        Assert.EndsWith(expectedExtension, sonuc.Value.DosyaAdi, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("pdf", "application/pdf")]
    [InlineData("xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task SeciliSandiklarUretimFormu_AyniProjedekiIdleriTekillestiripYalnizSecilenleriUretir(
        string format,
        string beklenenIcerikTuru)
    {
        var proje = new Proje { Id = 71, ProjeNo = "PA699-02", Musteri = "Test" };
        var bir = GecerliFormKaydi();
        bir.Id = 711;
        bir.ProjeId = proje.Id;
        bir.ManuelProjeNo = null;
        bir.SandikNo = "7";
        var iki = GecerliFormKaydi();
        iki.Id = 712;
        iki.ProjeId = proje.Id;
        iki.ManuelProjeNo = null;
        iki.SandikNo = "8";
        var secilmeyen = GecerliFormKaydi();
        secilmeyen.Id = 713;
        secilmeyen.ProjeId = proje.Id;
        secilmeyen.ManuelProjeNo = null;
        secilmeyen.SandikNo = "9";
        var unitOfWork = new FakeUnitOfWork()
            .AddRepository(proje)
            .AddRepository(bir, iki, secilmeyen);
        var dosyaService = new FakeAmbalajDosyaService();
        var handler = new GetAmbalajUretimFormuDosyasiQueryHandler(unitOfWork, dosyaService);

        var sonuc = await handler.Handle(new GetAmbalajUretimFormuDosyasiQuery
        {
            KayitIdleri = [iki.Id, bir.Id, iki.Id],
            Format = format
        }, CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(beklenenIcerikTuru, sonuc.Value!.IcerikTuru);
        Assert.Equal("PA699-02", dosyaService.SonUretimFormu!.ProjeNo);
        Assert.Equal(new[] { bir.Id, iki.Id }, dosyaService.SonUretimFormu.Kalemler.Select(x => x.KayitId).ToArray());
        Assert.Equal(1, unitOfWork.Repository<AmbalajUretimKaydi>().FindAsyncSayisi);
    }

    [Fact]
    public async Task SeciliSandiklarUretimFormu_BulunamayanIdVarsaKismiFormUretmez()
    {
        var kayit = GecerliFormKaydi();
        var dosyaService = new FakeAmbalajDosyaService();
        var handler = new GetAmbalajUretimFormuDosyasiQueryHandler(
            new FakeUnitOfWork().AddRepository(kayit), dosyaService);

        var sonuc = await handler.Handle(new GetAmbalajUretimFormuDosyasiQuery
        {
            KayitIdleri = [kayit.Id, 9999],
            Format = "pdf"
        }, CancellationToken.None);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(404, sonuc.StatusCode);
        Assert.Contains("9999", sonuc.Error!.Message);
        Assert.Null(dosyaService.SonUretimFormu);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
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
        var dosyaService = new FakeAmbalajDosyaService();
        var handler = new GetAmbalajUretimFormuDosyasiQueryHandler(
            new FakeUnitOfWork().AddRepository(uygun, uygunOlmayan), dosyaService);

        var sonuc = await handler.Handle(new GetAmbalajUretimFormuDosyasiQuery
        {
            KayitIdleri = [uygun.Id, uygunOlmayan.Id],
            Format = "pdf"
        }, CancellationToken.None);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains(uygunOlmayan.Id.ToString(), sonuc.Error!.Message);
        Assert.Null(dosyaService.SonUretimFormu);
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
        var dosyaService = new FakeAmbalajDosyaService();
        var handler = new GetAmbalajUretimFormuDosyasiQueryHandler(
            new FakeUnitOfWork().AddRepository(projeBir, projeIki).AddRepository(bir, iki),
            dosyaService);

        var sonuc = await handler.Handle(new GetAmbalajUretimFormuDosyasiQuery
        {
            KayitIdleri = [bir.Id, iki.Id],
            Format = "pdf"
        }, CancellationToken.None);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains("farklı projelere", sonuc.Error!.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(dosyaService.SonUretimFormu);
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
        var handler = new GetAmbalajUretimFormuDosyasiQueryHandler(
            new FakeUnitOfWork().AddRepository(sistem, manuel), new FakeAmbalajDosyaService());

        var sonuc = await handler.Handle(new GetAmbalajUretimFormuDosyasiQuery
        {
            KayitIdleri = [sistem.Id, manuel.Id],
            Format = "pdf"
        }, CancellationToken.None);

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
        var handler = new GetAmbalajUretimFormuDosyasiQueryHandler(
            new FakeUnitOfWork().AddRepository(bir, iki), new FakeAmbalajDosyaService());

        var sonuc = await handler.Handle(new GetAmbalajUretimFormuDosyasiQuery
        {
            KayitIdleri = [bir.Id, iki.Id],
            Format = "pdf"
        }, CancellationToken.None);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains("farklı manuel projelere", sonuc.Error!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SeciliSandiklarUretimFormuValidatoru_BosFazlaVeKarisikSecimleriReddeder()
    {
        var validator = new GetAmbalajUretimFormuDosyasiQueryValidator();

        Assert.False(validator.Validate(new GetAmbalajUretimFormuDosyasiQuery()).IsValid);
        Assert.False(validator.Validate(new GetAmbalajUretimFormuDosyasiQuery
        {
            KayitIdleri = null!
        }).IsValid);
        Assert.False(validator.Validate(new GetAmbalajUretimFormuDosyasiQuery
        {
            KayitIdleri = Enumerable.Range(1, 501).ToList()
        }).IsValid);
        Assert.False(validator.Validate(new GetAmbalajUretimFormuDosyasiQuery
        {
            KayitIdleri = [1, 0]
        }).IsValid);
        Assert.False(validator.Validate(new GetAmbalajUretimFormuDosyasiQuery
        {
            ProjeId = 1,
            KayitIdleri = [1]
        }).IsValid);
        Assert.True(validator.Validate(new GetAmbalajUretimFormuDosyasiQuery
        {
            KayitIdleri = [1, 1, 2],
            Format = "xlsx"
        }).IsValid);
    }

    [Fact]
    public async Task SeciliSandiklarUretimFormu_BosIdListesiniTumProjeFormunaDonusturmez()
    {
        var kayit = GecerliFormKaydi();
        var dosyaService = new FakeAmbalajDosyaService();
        var handler = new GetAmbalajUretimFormuDosyasiQueryHandler(
            new FakeUnitOfWork().AddRepository(kayit), dosyaService);

        var sonuc = await handler.Handle(new GetAmbalajUretimFormuDosyasiQuery
        {
            KayitIdleri = [],
            Format = "pdf"
        }, CancellationToken.None);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(400, sonuc.StatusCode);
        Assert.Null(dosyaService.SonUretimFormu);
    }

    [Fact]
    public async Task SayfaliListe_PageSizeSinirlarVeFiltreToplaminiTumKayitlardanHesaplar()
    {
        var records = Enumerable.Range(1, 250).Select(id => new AmbalajUretimKaydi
        {
            Id = id,
            CreatedBy = "7",
            ManuelProjeNo = "MAN-PAGE",
            SandikNo = id.ToString(),
            Tur = id <= 100 ? AmbalajSandikTuru.Normal : AmbalajSandikTuru.Ilave,
            Adet = 2,
            HesaplananToplamM3 = 1,
            SarfM3 = 0.11m,
            ToplamM3 = 1.11m
        }).ToArray();
        var handler = new GetAmbalajUretimSayfasiQueryHandler(
            new FakeUnitOfWork().AddRepository(records), FakeRolService.TumYetkiler(), new FakeCurrentUserService());

        var sonuc = await handler.Handle(
            new GetAmbalajUretimSayfasiQuery { PageNumber = 1, PageSize = 3888 }, CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(200, sonuc.Value!.PageSize);
        Assert.Equal(200, sonuc.Value.Items.Count);
        Assert.Equal(250, sonuc.Value.TotalCount);
        Assert.Equal(500, sonuc.Value.FilteredSummary.ToplamSandikAdedi);
        Assert.Equal(100, sonuc.Value.FilteredSummary.ProjeSandikKayitSayisi);
        Assert.Equal(150, sonuc.Value.FilteredSummary.OzelSandikKayitSayisi);
    }

    [Fact]
    public async Task ProjeListesi_SayfadakiProjelerinKaynakVeUretimOzetleriniDogruHesaplar()
    {
        var projeler = new[]
        {
            new Proje { Id = 1, ProjeNo = "PRJ-1", Musteri = "Müşteri 1" },
            new Proje { Id = 2, ProjeNo = "PRJ-2", Musteri = "Müşteri 2" }
        };
        var sandiklar = new[]
        {
            new Sandik { Id = 1, ProjeId = 1, SandikNo = "1", Boy = 100, En = 200, Yukseklik = 300 },
            new Sandik { Id = 2, ProjeId = 1, SandikNo = "2", Boy = null, En = 200, Yukseklik = 300 },
            new Sandik { Id = 3, ProjeId = 2, SandikNo = "1", Boy = 100, En = 200, Yukseklik = 300 }
        };
        var sonUretimTarihi = new DateTime(2026, 8, 3, 14, 30, 0);
        var kayitlar = new[]
        {
            new AmbalajUretimKaydi
            {
                Id = 1, ProjeId = 1, AmbalajaDahil = true, UretimeAlindi = true,
                UretimDurumu = AmbalajUretimDurumu.Uretimde, Adet = 2,
                HesaplananToplamM3 = 2, M3Override = 3, SarfM3 = 0.3m, ToplamM3 = 3.3m,
                UretimTarihi = new DateTime(2026, 8, 1)
            },
            new AmbalajUretimKaydi
            {
                Id = 2, ProjeId = 1, AmbalajaDahil = true, UretimeAlindi = true,
                UretimDurumu = AmbalajUretimDurumu.Tamamlandi, Adet = 1,
                HesaplananToplamM3 = 1, SarfM3 = 0.1m, ToplamM3 = 1.1m,
                UretimTarihi = sonUretimTarihi
            },
            new AmbalajUretimKaydi
            {
                Id = 3, ProjeId = 1, AmbalajaDahil = true, UretimeAlindi = false,
                UretimDurumu = AmbalajUretimDurumu.Planlandi, Adet = 4,
                HesaplananToplamM3 = 40, SarfM3 = 4, ToplamM3 = 44
            },
            new AmbalajUretimKaydi
            {
                Id = 4, ProjeId = 1, AmbalajaDahil = false, UretimeAlindi = true,
                UretimDurumu = AmbalajUretimDurumu.Tamamlandi, Adet = 8,
                HesaplananToplamM3 = 80, SarfM3 = 8, ToplamM3 = 88,
                UretimTarihi = new DateTime(2026, 8, 5)
            },
            new AmbalajUretimKaydi
            {
                Id = 5, ProjeId = 1, AmbalajaDahil = true, UretimeAlindi = true,
                UretimDurumu = AmbalajUretimDurumu.Tamamlandi, Adet = 16, IptalMi = true,
                HesaplananToplamM3 = 160, SarfM3 = 16, ToplamM3 = 176,
                UretimTarihi = new DateTime(2026, 8, 6)
            }
        };
        var unitOfWork = new FakeUnitOfWork()
            .AddRepository(projeler)
            .AddRepository(sandiklar)
            .AddRepository(kayitlar);
        var handler = new GetAmbalajProjeleriQueryHandler(
            unitOfWork, FakeRolService.TumYetkiler(), new FakeCurrentUserService());

        var sonuc = await handler.Handle(
            new GetAmbalajProjeleriQuery { PageNumber = 1, PageSize = 10 }, CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(2, sonuc.Value!.TotalCount);
        var ozet = Assert.Single(sonuc.Value.Items, x => x.ProjeId == 1);
        Assert.Equal(2, ozet.KaynakSandikSayisi);
        Assert.Equal(1, ozet.EksikOlculuKaynakSayisi);
        Assert.Equal(7, ozet.AmbalajaDahilSandikAdedi);
        Assert.Equal(3, ozet.UretimeAlinanSandikAdedi);
        Assert.Equal(1, ozet.TamamlananSandikAdedi);
        Assert.Equal(4m, ozet.NetM3);
        Assert.Equal(0.4m, ozet.SarfM3);
        Assert.Equal(4.4m, ozet.ToplamM3);
        Assert.Equal(sonUretimTarihi, ozet.SonUretimTarihi);
    }

    [Theory]
    [InlineData("699")]
    [InlineData("PA 699")]
    [InlineData("pa699-02")]
    [InlineData("PA-699-02")]
    [InlineData("pa 699 02")]
    [InlineData("PA699–02")]
    public async Task ProjeListesi_ProjeNumarasiniOnekBoslukVeTiredenBagimsizArar(string arama)
    {
        var projeler = new[]
        {
            new Proje { Id = 1, ProjeNo = "PA699-02", Musteri = "Birinci Müşteri", FBNo = "FB-100" },
            new Proje { Id = 2, ProjeNo = "PA700-01", Musteri = "İkinci Müşteri", FBNo = "FB-200" }
        };
        var unitOfWork = new FakeUnitOfWork()
            .AddRepository(projeler)
            .AddRepository(Array.Empty<Sandik>())
            .AddRepository(Array.Empty<AmbalajUretimKaydi>());
        var handler = new GetAmbalajProjeleriQueryHandler(
            unitOfWork, FakeRolService.TumYetkiler(), new FakeCurrentUserService());

        var sonuc = await handler.Handle(
            new GetAmbalajProjeleriQuery { Arama = arama, PageNumber = 1, PageSize = 10 },
            CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        var proje = Assert.Single(sonuc.Value!.Items);
        Assert.Equal("PA699-02", proje.ProjeNo);
    }

    [Theory]
    [InlineData("acme lojistik", 1)]
    [InlineData("fb-2026-700", 2)]
    public async Task ProjeListesi_GelismisProjeNoAramasiMusteriVeFbAramasiniBozmaz(
        string arama,
        int beklenenProjeId)
    {
        var projeler = new[]
        {
            new Proje { Id = 1, ProjeNo = "PA699-02", Musteri = "ACME Lojistik", FBNo = "FB-2026-699" },
            new Proje { Id = 2, ProjeNo = "PA700-01", Musteri = "Başka Müşteri", FBNo = "FB-2026-700" }
        };
        var unitOfWork = new FakeUnitOfWork()
            .AddRepository(projeler)
            .AddRepository(Array.Empty<Sandik>())
            .AddRepository(Array.Empty<AmbalajUretimKaydi>());
        var handler = new GetAmbalajProjeleriQueryHandler(
            unitOfWork, FakeRolService.TumYetkiler(), new FakeCurrentUserService());

        var sonuc = await handler.Handle(
            new GetAmbalajProjeleriQuery { Arama = arama, PageNumber = 1, PageSize = 10 },
            CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        var proje = Assert.Single(sonuc.Value!.Items);
        Assert.Equal(beklenenProjeId, proje.ProjeId);
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

    [Fact]
    public async Task ManuelProjeSecenekleri_AyniNoVeAdiGruplarVeOzetler()
    {
        var bir = GecerliFormKaydi();
        bir.Id = 201; bir.CreatedBy = "7"; bir.ManuelProjeNo = "MAN-LOOKUP"; bir.ManuelProjeAdi = "Lookup";
        var iki = GecerliFormKaydi();
        iki.Id = 202; iki.CreatedBy = "7"; iki.ManuelProjeNo = "MAN-LOOKUP"; iki.ManuelProjeAdi = "Lookup";
        var handler = new GetAmbalajManuelProjeSecenekleriQueryHandler(
            new FakeUnitOfWork().AddRepository(bir, iki));

        var sonuc = await handler.Handle(
            new GetAmbalajManuelProjeSecenekleriQuery { Arama = "lookup" }, CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        var option = Assert.Single(sonuc.Value!.Items);
        Assert.Equal("MAN-LOOKUP", option.No);
        Assert.Equal(2, option.KayitSayisi);
        Assert.Equal(bir.ToplamM3 + iki.ToplamM3, option.ToplamM3);
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

    private static AmbalajUretimKaydiGuncelleCommand GuncellemeKomutu(AmbalajUretimKaydi kayit) => new()
    {
        Id = kayit.Id,
        ProjeId = kayit.ProjeId,
        ManuelProjeNo = kayit.ManuelProjeNo,
        ManuelProjeAdi = kayit.ManuelProjeAdi,
        UstKayitId = kayit.UstKayitId,
        Tur = kayit.Tur,
        SandikNo = kayit.SandikNo,
        Ad = kayit.Ad,
        SandikCinsi = kayit.SandikCinsi,
        DigerSandikCinsi = kayit.DigerSandikCinsi,
        Adet = kayit.Adet,
        Boy = kayit.Boy,
        En = kayit.En,
        Yukseklik = kayit.Yukseklik,
        KullanimAmaci = kayit.KullanimAmaci,
        TalepEdenKisi = kayit.TalepEdenKisi,
        TalepEdenBolum = kayit.TalepEdenBolum,
        TalimatVeren = kayit.TalimatVeren,
        FirinPartiNo = kayit.FirinPartiNo,
        Aciklama = kayit.Aciklama,
        UretimTarihi = kayit.UretimTarihi
    };

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private readonly Dictionary<Type, object> _repositories = new();

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
        public string? MenuKod => null;
    }

    private sealed class BeklenmeyenAmbalajKaynakSenkronizasyonService
        : IAmbalajKaynakSenkronizasyonService
    {
        public Task<Result<AmbalajSenkronizasyonSonucuDto>> SenkronizeEtAsync(
            int projeId,
            ICurrentUserService islemiYapan,
            CancellationToken cancellationToken,
            bool sonucKayitlariniOlustur = true) =>
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

        public byte[] ExcelOlustur(IReadOnlyList<AmbalajRaporSatiri> satirlar, AmbalajRaporOzeti ozet) => [1];
        public byte[] PdfOlustur(IReadOnlyList<AmbalajRaporSatiri> satirlar, AmbalajRaporOzeti ozet) => [2];
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
        private readonly bool _admin;

        private FakeRolService(IEnumerable<string> yetkiler, bool admin = true)
        {
            _yetkiler = yetkiler.ToHashSet(StringComparer.OrdinalIgnoreCase);
            _admin = admin;
        }

        public static FakeRolService Yalniz(params string[] yetkiler) => new(yetkiler);
        public static FakeRolService Sinirli(params string[] yetkiler) => new(yetkiler, false);

        public static FakeRolService TumYetkiler() => new(typeof(AmbalajMenuKodlari)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!));

        public Task<bool> HasUserPermissionAsync(
            int userId,
            string menuKod,
            YetkiTipi requiredYetkiTipi,
            CancellationToken ct = default) => Task.FromResult(_yetkiler.Contains(menuKod));

        public Task<bool> IsAdminAsync(int userId, CancellationToken ct = default) => Task.FromResult(_admin);
        public Task<List<MenuTanimi>> GetMenuAgaciAsync(CancellationToken ct = default) => Task.FromResult(new List<MenuTanimi>());
        public Task<List<RolYetki>> GetRolYetkileriAsync(int rolId, CancellationToken ct = default) => Task.FromResult(new List<RolYetki>());
        public Task YetkileriGuncelleAsync(int rolId, List<RolYetki> yetkiler, CancellationToken ct = default) => Task.CompletedTask;
    }
}
