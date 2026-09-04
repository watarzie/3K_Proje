using _3K.Application.Features.AmbalajIslemleri.Commands;
using _3K.Application.Features.AmbalajIslemleri;
using _3K.Application.Features.AmbalajIslemleri.Validators;
using _3K.Core.Enums;
using _3K.Core.Models;
using _3K.Infrastructure.Services;
using ClosedXML.Excel;

namespace _3K.Application.Tests;

public sealed class AmbalajUretimTests
{
    [Fact]
    public void EnumWireDegerleri_ApiSozlesmesiyleUyumludur()
    {
        Assert.Equal(1, (int)AmbalajSandikTuru.Normal);
        Assert.Equal(2, (int)AmbalajSandikTuru.Ilave);
        Assert.Equal(3, (int)AmbalajSandikTuru.Saha);
        Assert.Equal(4, (int)AmbalajSandikTuru.Yedek);
        Assert.Equal(5, (int)AmbalajSandikTuru.Ic);
        Assert.Equal(6, (int)AmbalajSandikTuru.Diger);
        Assert.Equal(1, (int)AmbalajKaynakModulu.Sandik);
        Assert.Equal(5, (int)AmbalajKaynakModulu.Diger);
        Assert.Equal(1, (int)AmbalajUretimDurumu.Planlandi);
        Assert.Equal(3, (int)AmbalajUretimDurumu.Tamamlandi);
    }

    [Fact]
    public void M3Ozeti_VarsayilanYuzdeOnBirSarfiNettenAyriHesaplar()
    {
        var sonuc = AmbalajHesaplayici.M3OzetiHesapla(2500, 1500, 1800, 2);

        Assert.True(sonuc.HesaplananBirimM3 > 0);
        Assert.Equal(
            Math.Round(AmbalajHesaplayici.Hesapla(2500, 1500, 1800).ToplamHacimM3 * 2, 6, MidpointRounding.AwayFromZero),
            sonuc.HesaplananToplamM3);
        Assert.Equal(Math.Round(sonuc.NetM3 * 0.11m, 6, MidpointRounding.AwayFromZero), sonuc.SarfM3);
        Assert.Equal(sonuc.NetM3 + sonuc.SarfM3, sonuc.ToplamM3);
    }

    [Fact]
    public void M3Ozeti_OndalikliOlculeriKesmedenHesaplar()
    {
        const decimal boy = 2500.5m;
        const decimal en = 1500.25m;
        const decimal yukseklik = 1800.75m;

        var hesap = AmbalajHesaplayici.Hesapla(boy, en, yukseklik);
        var sonuc = AmbalajHesaplayici.M3OzetiHesapla(boy, en, yukseklik, 2);
        var tamSayiyaKesilmisSonuc = AmbalajHesaplayici.M3OzetiHesapla(2500m, 1500m, 1800m, 2);

        Assert.Equal(boy, hesap.IcOlculer.Boy);
        Assert.Equal(en, hesap.IcOlculer.En);
        Assert.Equal(yukseklik, hesap.IcOlculer.Yukseklik);
        Assert.Equal(
            Math.Round(hesap.ToplamHacimM3 * 2m, 6, MidpointRounding.AwayFromZero),
            sonuc.HesaplananToplamM3);
        Assert.NotEqual(tamSayiyaKesilmisSonuc.HesaplananToplamM3, sonuc.HesaplananToplamM3);
    }

    [Fact]
    public void M3Ozeti_OverrideNetiDegistirirAmaHesaplananSnapshotiKorur()
    {
        var normal = AmbalajHesaplayici.M3OzetiHesapla(2500, 1500, 1800, 3);
        var overrideSonuc = AmbalajHesaplayici.M3OzetiHesapla(2500, 1500, 1800, 3, 0.11m, 7.25m);

        Assert.Equal(normal.HesaplananBirimM3, overrideSonuc.HesaplananBirimM3);
        Assert.Equal(normal.HesaplananToplamM3, overrideSonuc.HesaplananToplamM3);
        Assert.Equal(7.25m, overrideSonuc.NetM3);
        Assert.Equal(0.7975m, overrideSonuc.SarfM3);
        Assert.Equal(8.0475m, overrideSonuc.ToplamM3);
    }

    [Theory]
    [InlineData(2499, 2)]
    [InlineData(2500, 3)]
    [InlineData(2501, 3)]
    [InlineData(3999, 3)]
    [InlineData(4000, 4)]
    [InlineData(4001, 4)]
    [InlineData(4999, 4)]
    [InlineData(5000, 5)]
    [InlineData(5989, 5)]
    [InlineData(5990, 6)]
    [InlineData(6999, 6)]
    [InlineData(7000, 7)]
    public void Hesapla_StandartAyakSinirlariniDegistirmez(decimal boy, int beklenenAyak)
    {
        var sonuc = AmbalajHesaplayici.Hesapla(boy, 1500, 1800);

        Assert.Equal(beklenenAyak, sonuc.AyakAdedi);
    }

    [Theory]
    [InlineData(4000, 4)]
    [InlineData(4001, 5)]
    [InlineData(5989, 5)]
    [InlineData(5990, 6)]
    [InlineData(6999, 6)]
    [InlineData(7000, 7)]
    public void Hesapla_GenlesmeKabiIcinOzelAyakKuraliniUygular(decimal boy, int beklenenAyak)
    {
        var profil = AmbalajAyakProfiliBelirleyici.Belirle("  GENLEŞME    KABI  ");
        var hesap = AmbalajHesaplayici.Hesapla(boy, 1500, 1800, profil);
        var ozet = AmbalajHesaplayici.M3OzetiHesapla(boy, 1500, 1800, 1, ayakProfili: profil);

        Assert.Equal(AmbalajAyakProfili.GenlesmeKabi, profil);
        Assert.Equal(beklenenAyak, hesap.AyakAdedi);
        Assert.Equal(Math.Round(hesap.ToplamHacimM3, 6, MidpointRounding.AwayFromZero), ozet.HesaplananBirimM3);
    }

    [Fact]
    public void M3Ozeti_KaynakSandikDisOlculeriniReferanstakiGibiIcOlcuyeCevirir()
    {
        var kayit = new _3K.Core.Entities.AmbalajUretimKaydi
        {
            KaynakModul = AmbalajKaynakModulu.Sandik,
            KaynakKayitId = 42,
            Adet = 1,
            Boy = 3100m,
            En = 1900m,
            Yukseklik = 1925m
        };

        AmbalajUretimYardimcilari.M3DegerleriniHesapla(kayit);
        var referans = AmbalajHesaplayici.M3OzetiHesapla(3008m, 1808m, 1670m, 1);

        Assert.Equal(referans.HesaplananBirimM3, kayit.HesaplananBirimM3);
        Assert.Equal(referans.HesaplananToplamM3, kayit.HesaplananToplamM3);
    }

    [Fact]
    public void KaynakStandartSandik_AyakEsigindeReferanstakiGibiIcBoyKullanir()
    {
        var kayit = new _3K.Core.Entities.AmbalajUretimKaydi
        {
            KaynakModul = AmbalajKaynakModulu.Sandik,
            KaynakKayitId = 43,
            Adet = 1,
            Boy = 2550m,
            En = 1500m,
            Yukseklik = 1800m,
            Ad = "Radyatör"
        };

        AmbalajUretimYardimcilari.M3DegerleriniHesapla(kayit);
        var referans = AmbalajHesaplayici.M3OzetiHesapla(2458m, 1408m, 1545m, 1);

        Assert.Equal(2, AmbalajHesaplayici.Hesapla(2458m, 1408m, 1545m).AyakAdedi);
        Assert.Equal(referans.HesaplananToplamM3, kayit.HesaplananToplamM3);
    }

    [Fact]
    public void KaynakGenlesmeKabi_OzelAyakEsigindeDisBoyKullanir()
    {
        var kayit = new _3K.Core.Entities.AmbalajUretimKaydi
        {
            KaynakModul = AmbalajKaynakModulu.Sandik,
            KaynakKayitId = 44,
            Adet = 1,
            Boy = 4050m,
            En = 1500m,
            Yukseklik = 1800m,
            Ad = "Genleşme Kabı"
        };
        var profil = AmbalajAyakProfili.GenlesmeKabi;

        AmbalajUretimYardimcilari.M3DegerleriniHesapla(kayit);
        var referans = AmbalajHesaplayici.M3OzetiHesapla(
            3958m, 1408m, 1545m, 1, ayakProfili: profil, ayakHesapBoyu: 4050m);

        Assert.Equal(5, AmbalajHesaplayici.Hesapla(3958m, 1408m, 1545m, profil, 4050m).AyakAdedi);
        Assert.Equal(referans.HesaplananToplamM3, kayit.HesaplananToplamM3);
    }

    [Theory]
    [InlineData("Radyatör", 5989, 5)]
    [InlineData("Radyatör", 5990, 6)]
    [InlineData("Radyatör", 6999, 6)]
    [InlineData("Radyatör", 7000, 7)]
    [InlineData("Genleşme Kabı", 5989, 5)]
    [InlineData("Genleşme Kabı", 5990, 6)]
    [InlineData("Genleşme Kabı", 6999, 6)]
    [InlineData("Genleşme Kabı", 7000, 7)]
    public void KaynakSandik_UzunSandikAyakKuraliniDisBoydanUygular(
        string ad,
        decimal disBoy,
        int beklenenAyak)
    {
        var kayit = new _3K.Core.Entities.AmbalajUretimKaydi
        {
            KaynakModul = AmbalajKaynakModulu.Sandik,
            KaynakKayitId = 45,
            Adet = 1,
            Boy = disBoy,
            En = 1500m,
            Yukseklik = 1800m,
            Ad = ad
        };

        var profil = AmbalajAyakProfiliBelirleyici.Belirle(kayit.Ad);
        var icOlculer = AmbalajUretimYardimcilari.HesaplamaIcOlculeriniGetir(kayit);
        var hesap = AmbalajHesaplayici.Hesapla(
            icOlculer.Boy,
            icOlculer.En,
            icOlculer.Yukseklik,
            profil,
            kayit.Boy);

        Assert.Equal(beklenenAyak, hesap.AyakAdedi);
    }

    [Theory]
    [InlineData("genleşme kabı")]
    [InlineData("GENLESME-KABI")]
    [InlineData("Genleşme\tKabı")]
    public void AyakProfili_TurkceCaseBoslukVeNoktalamaFarklariniNormalizeEder(string ad)
    {
        Assert.Equal(AmbalajAyakProfili.GenlesmeKabi, AmbalajAyakProfiliBelirleyici.Belirle(ad));
    }

    [Theory]
    [InlineData(0, 1000, 1000)]
    [InlineData(1000, 0, 1000)]
    [InlineData(1000, 1000, -1)]
    public void Hesapla_GecersizOlcuyuReddeder(decimal boy, decimal en, decimal yukseklik)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AmbalajHesaplayici.Hesapla(boy, en, yukseklik));
    }

    [Fact]
    public void Hesapla_DarIcSandiktaReferansFormulunuDegistirmez()
    {
        var sonuc = AmbalajHesaplayici.Hesapla(300, 100, 250);
        var yanDuvarUstTahtasi = Assert.Single(sonuc.Parcalar, parca => parca.Kod == "YD_12");

        Assert.Equal(100m - (2m * 93m), yanDuvarUstTahtasi.Uzunluk);
    }

    [Fact]
    public void OlusturmaValidatoru_ManuelProjeKimliginiVeIcSandikUstunuZorunluTutar()
    {
        var validator = new AmbalajUretimKaydiOlusturCommandValidator();
        var command = GecerliKomut();
        command.ProjeId = null;
        command.ManuelProjeNo = "MAN-001";
        command.ManuelProjeAdi = null;
        command.Tur = AmbalajSandikTuru.Ic;
        command.UstKayitId = null;

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("manuel proje", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("üst sandık", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void OlusturmaValidatoru_AmbalajDisiKaydiUretimeAldirmaz()
    {
        var validator = new AmbalajUretimKaydiOlusturCommandValidator();
        var command = GecerliKomut();
        command.AmbalajaDahil = false;
        command.UretimeAlindi = true;

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("üretime alınamaz", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void SarfOraniValidatoru_SinirDisiOraniReddeder(decimal oran)
    {
        var validator = new AmbalajSarfOraniGuncelleCommandValidator();
        var result = validator.Validate(new AmbalajSarfOraniGuncelleCommand
        {
            Id = 1,
            SarfOrani = oran,
            Neden = "Yetkili düzeltmesi"
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void AmbalajValidatorUzunluklari_VeritabaniKolonlariylaUyumludur()
    {
        var olusturmaValidatoru = new AmbalajUretimKaydiOlusturCommandValidator();
        var sinirdaOlusturma = GecerliKomut();
        sinirdaOlusturma.KullanimAmaci = new string('a', 250);
        var uzunOlusturma = GecerliKomut();
        uzunOlusturma.KullanimAmaci = new string('a', 251);

        Assert.True(olusturmaValidatoru.Validate(sinirdaOlusturma).IsValid);
        Assert.Contains(
            olusturmaValidatoru.Validate(uzunOlusturma).Errors,
            hata => hata.PropertyName == nameof(uzunOlusturma.KullanimAmaci));

        var m3Validatoru = new AmbalajM3OverrideGuncelleCommandValidator();
        Assert.True(m3Validatoru.Validate(new AmbalajM3OverrideGuncelleCommand
        {
            Id = 1,
            M3Override = 1,
            Neden = new string('a', 500)
        }).IsValid);
        Assert.Contains(m3Validatoru.Validate(new AmbalajM3OverrideGuncelleCommand
        {
            Id = 1,
            M3Override = 1,
            Neden = new string('a', 501)
        }).Errors, hata => hata.PropertyName == "Neden");

        var iptalValidatoru = new AmbalajUretimKaydiIptalEtCommandValidator();
        Assert.True(iptalValidatoru.Validate(new AmbalajUretimKaydiIptalEtCommand
        {
            Id = 1,
            Neden = new string('a', 500)
        }).IsValid);
        Assert.Contains(iptalValidatoru.Validate(new AmbalajUretimKaydiIptalEtCommand
        {
            Id = 1,
            Neden = new string('a', 501)
        }).Errors, hata => hata.PropertyName == "Neden");
    }

    [Fact]
    public void UretimFormuServisi_GecerliExcelVePdfUretir()
    {
        var kalem = UretimFormuKalemiOlustur(1, "1");
        var form = new AmbalajUretimFormuModel
        {
            ProjeId = 1,
            ProjeNo = "PA-TEST",
            ProjeAdi = "Test Müşteri",
            FBNo = "FB-TEST",
            NetM3 = kalem.NetM3,
            SarfM3 = kalem.SarfM3,
            ToplamM3 = kalem.ToplamM3,
            Kalemler = [kalem]
        };
        var service = new AmbalajRaporDosyaService();

        var excel = service.UretimFormuExcelOlustur(form);
        var pdf = service.UretimFormuPdfOlustur(form);

        Assert.True(excel.Length > 1_000);
        Assert.Equal("PK", System.Text.Encoding.ASCII.GetString(excel, 0, 2));
        Assert.True(pdf.Length > 1_000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.Equal(3, PdfSayfaSayisi(pdf));
    }

    [Fact]
    public void UretimFormu_AyniRadyatorSandiklariniBesOndanTekGrubaDonusturur()
    {
        var kalemler = Enumerable.Range(5, 6)
            .Select((sandikNo, index) =>
            {
                var kalem = UretimFormuKalemiOlustur(sandikNo, sandikNo.ToString());
                kalem.UretimTarihi = new DateTime(2026, 8, 1).AddDays(index);
                kalem.M3Override = kalem.NetM3 + index / 100m;
                kalem.NetM3 += index / 100m;
                kalem.SarfM3 += index / 1000m;
                kalem.ToplamM3 = kalem.NetM3 + kalem.SarfM3;
                return kalem;
            })
            .ToList();
        var form = new AmbalajUretimFormuModel
        {
            ProjeId = 1,
            ProjeNo = "PA-GRUP",
            ProjeAdi = "Test Müşteri",
            Kalemler = kalemler,
            NetM3 = kalemler.Sum(kalem => kalem.NetM3),
            SarfM3 = kalemler.Sum(kalem => kalem.SarfM3),
            ToplamM3 = kalemler.Sum(kalem => kalem.ToplamM3)
        };

        var grup = Assert.Single(AmbalajUretimFormuGruplayici.Grupla(kalemler));
        var pdf = new AmbalajRaporDosyaService().UretimFormuPdfOlustur(form);

        Assert.Equal("5-10", grup.SandikNo);
        Assert.Equal(6, grup.Adet);
        Assert.Equal(kalemler.Sum(kalem => kalem.NetM3), grup.NetM3);
        Assert.Equal(kalemler.Sum(kalem => kalem.SarfM3), grup.SarfM3);
        Assert.Equal(kalemler.Sum(kalem => kalem.ToplamM3), grup.ToplamM3);
        var ilkParca = kalemler[0].Parcalar[0];
        var gruplananParca = Assert.Single(grup.Parcalar, parca => parca.Kod == ilkParca.Kod);
        Assert.Equal(kalemler.Sum(kalem => kalem.Parcalar.Single(parca => parca.Kod == ilkParca.Kod).TeorikAdet), gruplananParca.TeorikAdet);
        Assert.Equal(kalemler.Sum(kalem => kalem.Parcalar.Single(parca => parca.Kod == ilkParca.Kod).HacimM3), gruplananParca.HacimM3);
        Assert.Equal(3, PdfSayfaSayisi(pdf));
    }

    [Fact]
    public void UretimFormuGruplayici_FarkliOlcuKullanimAmaciVeyaFirinPartisiniBirlestirmez()
    {
        var olcuBir = UretimFormuKalemiOlustur(1, "1");
        var olcuIki = UretimFormuKalemiOlustur(2, "2");
        olcuIki.IcOlculer = new AmbalajOlculeri(
            olcuIki.IcOlculer.Boy + 1,
            olcuIki.IcOlculer.En,
            olcuIki.IcOlculer.Yukseklik);

        var amacBir = UretimFormuKalemiOlustur(3, "3");
        var amacIki = UretimFormuKalemiOlustur(4, "4");
        amacIki.KullanimAmaci = "Genleşme Kabı";

        var firinBir = UretimFormuKalemiOlustur(5, "5");
        var firinIki = UretimFormuKalemiOlustur(6, "6");
        firinBir.FirinPartiNo = "FP-1";
        firinIki.FirinPartiNo = "FP-2";

        Assert.Equal(2, AmbalajUretimFormuGruplayici.Grupla([olcuBir, olcuIki]).Count);
        Assert.Equal(2, AmbalajUretimFormuGruplayici.Grupla([amacBir, amacIki]).Count);
        Assert.Equal(2, AmbalajUretimFormuGruplayici.Grupla([firinBir, firinIki]).Count);
    }

    [Fact]
    public void UretimFormuGruplayici_KesintiliKoliNumaralarindaOlmayanSandigiAraligaKatmaz()
    {
        var kalemler = new[] { 10, 5, 8, 7 }
            .Select(sandikNo => UretimFormuKalemiOlustur(sandikNo, sandikNo.ToString()))
            .ToList();

        var grup = Assert.Single(AmbalajUretimFormuGruplayici.Grupla(kalemler));

        Assert.Equal("5, 7-8, 10", grup.SandikNo);
        Assert.Equal(4, grup.Adet);
    }

    [Fact]
    public void UretimFormuGruplayici_GruplariSandikNumarasinaGoreDogalSiralar()
    {
        var on = UretimFormuKalemiOlustur(10, "10");
        on.KullanimAmaci = "Radyatör 10";
        var iki = UretimFormuKalemiOlustur(2, "2");
        iki.KullanimAmaci = "Radyatör 2";

        var gruplar = AmbalajUretimFormuGruplayici.Grupla([on, iki]);

        Assert.Equal(["2", "10"], gruplar.Select(grup => grup.SandikNo));
    }

    [Fact]
    public void UretimFormuExcel_AyniRadyatorSandiklariniBesOnVeAltiAdetOlarakYazar()
    {
        var kalemler = Enumerable.Range(5, 6)
            .Select(sandikNo => UretimFormuKalemiOlustur(sandikNo, sandikNo.ToString()))
            .ToList();
        var form = new AmbalajUretimFormuModel
        {
            ProjeId = 1,
            ProjeNo = "PA-GRUP",
            ProjeAdi = "Test Müşteri",
            Kalemler = kalemler,
            NetM3 = kalemler.Sum(kalem => kalem.NetM3),
            SarfM3 = kalemler.Sum(kalem => kalem.SarfM3),
            ToplamM3 = kalemler.Sum(kalem => kalem.ToplamM3)
        };

        var excel = new AmbalajRaporDosyaService().UretimFormuExcelOlustur(form);
        using var stream = new MemoryStream(excel);
        using var workbook = new XLWorkbook(stream);
        var detail = workbook.Worksheet("Kesim Listesi");

        Assert.Equal("5-10", detail.Cell(2, 1).GetString());
        Assert.Equal(6, detail.Cell(2, 5).GetValue<int>());
        Assert.All(
            detail.RowsUsed().Skip(1),
            row => Assert.Equal("5-10", row.Cell(1).GetString()));
    }

    [Fact]
    public void AmbalajPdfVarliklari_TestCiktisinaKopyalanir()
    {
        var varlikDizini = Path.Combine(AppContext.BaseDirectory, "Assets", "Ambalaj");

        Assert.True(Directory.Exists(varlikDizini));
        Assert.Equal(39, Directory.GetFiles(varlikDizini).Length);
        Assert.True(File.Exists(Path.Combine(varlikDizini, "3d-2.png")));
        Assert.True(File.Exists(Path.Combine(varlikDizini, "3d-6.png")));
        Assert.True(File.Exists(Path.Combine(varlikDizini, "3d-7.png")));
        Assert.True(File.Exists(Path.Combine(varlikDizini, "3boyut52.jpg")));
        Assert.True(File.Exists(Path.Combine(varlikDizini, "alt-ayak-7.png")));
        Assert.True(File.Exists(Path.Combine(varlikDizini, "on-duvar-7.png")));
        Assert.True(File.Exists(Path.Combine(varlikDizini, "palet-ust-7.png")));
        Assert.True(File.Exists(Path.Combine(varlikDizini, "ust-tavan-7.png")));
    }

    private static AmbalajUretimFormuKalemiModel UretimFormuKalemiOlustur(int kayitId, string sandikNo)
    {
        var hesap = AmbalajHesaplayici.Hesapla(2500, 1500, 1800);
        var netM3 = Math.Round(hesap.ToplamHacimM3, 6, MidpointRounding.AwayFromZero);
        var sarfM3 = Math.Round(netM3 * AmbalajHesaplayici.VarsayilanSarfOrani, 6, MidpointRounding.AwayFromZero);
        return new AmbalajUretimFormuKalemiModel
        {
            KayitId = kayitId,
            IsAkisKimligi = Guid.NewGuid(),
            SandikNo = sandikNo,
            SandikAdi = "Ana Sandık",
            SandikTuru = "Normal",
            SandikCinsi = "Ahşap Kapalı",
            Adet = 1,
            BrutKg = 1250,
            KullanimAmaci = "Radyatör",
            IcOlculer = hesap.IcOlculer,
            DisOlculer = hesap.DisOlculer,
            UstKizakAdedi = hesap.UstKizakAdedi,
            AyakAdedi = hesap.AyakAdedi,
            YanKusakAdedi = hesap.YanKusakAdedi,
            OnDuvarYuksekligi = hesap.OnDuvarYuksekligi,
            FormulVersiyonu = AmbalajHesaplayici.FormulVersiyonu,
            HesaplananNetM3 = netM3,
            NetM3 = netM3,
            SarfOrani = AmbalajHesaplayici.VarsayilanSarfOrani,
            SarfM3 = sarfM3,
            ToplamM3 = netM3 + sarfM3,
            Parcalar = hesap.Parcalar.Select(parca => new AmbalajUretimFormuParcasiModel
            {
                Kod = parca.Kod,
                Grup = parca.Grup,
                Aciklama = parca.Aciklama,
                Malzeme = parca.Malzeme,
                KesitEn = parca.KesitEn,
                KesitYukseklik = parca.KesitYukseklik,
                Uzunluk = parca.Uzunluk,
                TeorikAdet = parca.Adet,
                KesimAdedi = parca.UretimAdedi,
                HacimM3 = Math.Round(parca.HacimM3, 6, MidpointRounding.AwayFromZero)
            }).ToList()
        };
    }

    private static int PdfSayfaSayisi(byte[] pdf)
    {
        var metin = System.Text.Encoding.Latin1.GetString(pdf);
        return System.Text.RegularExpressions.Regex.Matches(
            metin,
            @"/Type\s*/Page(?!s)",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant).Count;
    }

    private static AmbalajUretimKaydiOlusturCommand GecerliKomut() => new()
    {
        ProjeId = 1,
        Tur = AmbalajSandikTuru.Normal,
        KaynakModul = AmbalajKaynakModulu.Manuel,
        SandikNo = "1",
        Ad = "Ana sandık",
        SandikCinsi = AmbalajSandikCinsi.AhsapKapali,
        Adet = 1,
        Boy = 2500,
        En = 1500,
        Yukseklik = 1800,
        AmbalajaDahil = true
    };
}
