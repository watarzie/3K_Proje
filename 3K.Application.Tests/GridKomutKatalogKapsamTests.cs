using System.Linq.Expressions;
using _3K.Application.Common;
using _3K.Application.Features.GridIslemleri.Commands;
using _3K.Application.Features.GridIslemleri.Queries;
using _3K.Application.Features.GridIslemleri.Validators;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Tests;

/// <summary>
/// Grid kataloğundaki komut sınırlarını, bütün seçim kilitlerini ve alan
/// etkilerini gerçek handler'lar üzerinden sabitler. Sevk-partisi/3K yaşam
/// döngüsünün ayrıntılı senaryoları ilgili regresyon testlerinde tutulur.
/// </summary>
public sealed class GridKomutKatalogKapsamTests
{
    [Fact]
    public void TekilGridValidatoru_OnDortTamDortOndaligiKabulEder_BesinciOndaligiVeSevkUyumsuzlugunuReddeder()
    {
        var validator = new GridDurumGuncelleCommandValidator();
        var sinir = 99_999_999_999_999.1234m;
        var gecerli = new GridDurumGuncelleCommand
        {
            CekiSatiriId = 1,
            ProjeId = 10,
            YeniDurumId = (int)GridDurum.TamGeldi,
            GridGelenAdet = sinir,
            TrafoSevkAdet = sinir,
            SevkMiktari = sinir,
            GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi
        };

        Assert.True(validator.Validate(gecerli).IsValid);

        foreach (var gecersiz in new[]
        {
            new GridDurumGuncelleCommand
            {
                CekiSatiriId = 1,
                ProjeId = 10,
                YeniDurumId = (int)GridDurum.EksikGeldi,
                GridGelenAdet = 1.12345m
            },
            new GridDurumGuncelleCommand
            {
                CekiSatiriId = 1,
                ProjeId = 10,
                YeniDurumId = (int)GridDurum.TrafoSevk,
                TrafoSevkAdet = 1.12345m
            },
            new GridDurumGuncelleCommand
            {
                CekiSatiriId = 1,
                ProjeId = 10,
                YeniDurumId = (int)GridDurum.TamGeldi,
                SevkMiktari = 1.12345m,
                GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi
            },
            new GridDurumGuncelleCommand
            {
                CekiSatiriId = 1,
                ProjeId = 10,
                YeniDurumId = (int)GridDurum.TamGeldi,
                SevkMiktari = 1,
                GridSevkDurumuId = (int)GridSevkDurum.Bekliyor
            }
        })
        {
            Assert.False(validator.Validate(gecersiz).IsValid);
        }
    }

    [Fact]
    public async Task TekilGridKomutu_DesteklenmeyenDurumuMutasyonsuzReddeder()
    {
        using var kurgu = new Kurgu();
        var satir = kurgu.SatirEkle(1, 4);
        var once = SatirAnligi.Al(satir);

        var sonuc = await kurgu.TekliDurumGuncelleAsync(satir, (GridDurum)999);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(once, SatirAnligi.Al(satir));
        Assert.Empty(kurgu.Uow.Repo<CekiSatiri>().Updated);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Empty(kurgu.Hareketler);
    }

    [Theory]
    [InlineData("saha")]
    [InlineData("sevk")]
    public async Task TekilGridKomutu_AktifSahaVeyaSevkEdilmisSandiktaMutasyonsuzReddedilir(string kilit)
    {
        using var kurgu = new Kurgu();
        var satir = kurgu.SatirEkle(1, 4);
        var sandik = kurgu.SandikVeIcerikEkle(satir, tahsis: 4).Sandik;
        if (kilit == "saha")
            kurgu.Saha.AktifKaynakSatirIdleri.Add(satir.Id);
        else
            sandik.DurumId = (int)SandikDurum.Sevkedildi;
        var once = SatirAnligi.Al(satir);

        var sonuc = await kurgu.TekliDurumGuncelleAsync(satir, GridDurum.TamGeldi);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(once, SatirAnligi.Al(satir));
        Assert.Empty(kurgu.Uow.Repo<CekiSatiri>().Updated);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Empty(kurgu.Hareketler);
    }

    [Theory]
    [InlineData(GridDurum.TamGeldi)]
    [InlineData(GridDurum.Iptal)]
    [InlineData(GridDurum.GridKapandi)]
    public async Task TekilGridKomutu_TadilattaIkenHerHedefiMutasyonsuzReddeder(GridDurum hedef)
    {
        using var kurgu = new Kurgu();
        var satir = kurgu.SatirEkle(1, 4);
        satir.KaliteDurumId = 51;
        kurgu.Lookup.Ayarla<LookupKaliteDurum>(51, "Tadilatta");
        var once = SatirAnligi.Al(satir);

        var sonuc = await kurgu.TekliDurumGuncelleAsync(satir, hedef);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(once, SatirAnligi.Al(satir));
        Assert.Empty(kurgu.Uow.Repo<CekiSatiri>().Updated);
        Assert.Equal(0, kurgu.Uow.SaveCount);
    }

    [Theory]
    [InlineData(GridDurum.Gelmedi)]
    [InlineData(GridDurum.Iptal)]
    [InlineData(GridDurum.Sipariste)]
    public async Task TekilTemizleyenDurumlar_SevkMiktarlariniVeParentChildAktifTakibiniTemizler(GridDurum hedef)
    {
        using var kurgu = new Kurgu();
        var satir = kurgu.SatirEkle(1, 5);
        var icerik = kurgu.SandikVeIcerikEkle(satir, tahsis: 5).Icerik;
        satir.GridDurumuId = (int)GridDurum.EksikGeldi;
        satir.GridGelenAdet = 4;
        satir.TrafoSevkAdet = 1;
        satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi;
        satir.GridSevkMiktari = 4;
        satir.AktifGridSevkKarsilananMiktari = 2;
        satir.AktifGridSevkPartisiErkenSonuclandirildiMi = false;
        icerik.AktifGridSevkKarsilananMiktari = 2;

        var sonuc = await kurgu.TekliDurumGuncelleAsync(satir, hedef, aciklama: "temizle");

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal((int)hedef, satir.GridDurumuId);
        Assert.Equal(0, satir.GridGelenAdet);
        Assert.Equal(0, satir.TrafoSevkAdet);
        Assert.Equal((int)GridSevkDurum.SevkEdilmedi, satir.GridSevkDurumuId);
        Assert.Null(satir.GridSevkMiktari);
        Assert.Null(satir.AktifGridSevkKarsilananMiktari);
        Assert.Null(satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.Null(icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(77, satir.GridPersonelId);
        Assert.Equal("temizle", satir.GridAciklama);
        Assert.Contains(satir, kurgu.Uow.Repo<CekiSatiri>().Updated);
        Assert.Single(kurgu.Hareketler);
    }

    [Theory]
    [InlineData(GridDurum.TamGeldi, null, null, 5.5, 0)]
    [InlineData(GridDurum.EksikGeldi, 2.25, null, 2.25, 0)]
    [InlineData(GridDurum.TrafoSevk, 1.25, 2.5, 1.25, 2.5)]
    public async Task TekilTamEksikTrafo_DurumMiktarSozlesmesiniUygular(
        GridDurum hedef,
        double? gridGelen,
        double? trafo,
        double beklenenGrid,
        double beklenenTrafo)
    {
        using var kurgu = new Kurgu();
        var satir = kurgu.SatirEkle(1, 5.5m);
        satir.TrafoSevkAdet = 0.5m;

        var sonuc = await kurgu.TekliDurumGuncelleAsync(
            satir,
            hedef,
            gridGelen: gridGelen.HasValue ? (decimal)gridGelen.Value : null,
            trafo: trafo.HasValue ? (decimal)trafo.Value : null);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal((int)hedef, satir.GridDurumuId);
        Assert.Equal((decimal)beklenenGrid, satir.GridGelenAdet);
        Assert.Equal((decimal)beklenenTrafo, satir.TrafoSevkAdet);
        Assert.Equal(77, satir.GridPersonelId);
    }

    [Fact]
    public async Task TekilGridKapandi_MiktarlariVeAktifPartiyiKorur_SandigiGridLokasyonunaAlir()
    {
        using var kurgu = new Kurgu();
        var satir = kurgu.SatirEkle(1, 5);
        var (sandik, icerik) = kurgu.SandikVeIcerikEkle(satir, tahsis: 5);
        satir.GridDurumuId = (int)GridDurum.EksikGeldi;
        satir.GridGelenAdet = 3;
        satir.TrafoSevkAdet = 1;
        satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi;
        satir.GridSevkMiktari = 3;
        satir.AktifGridSevkKarsilananMiktari = 0;
        satir.AktifGridSevkPartisiErkenSonuclandirildiMi = false;
        icerik.AktifGridSevkKarsilananMiktari = 0;
        sandik.DepoLokasyonId = (int)DepoLokasyon.Belirsiz;

        var sonuc = await kurgu.TekliDurumGuncelleAsync(satir, GridDurum.GridKapandi);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal((int)GridDurum.GridKapandi, satir.GridDurumuId);
        Assert.Equal(3, satir.GridGelenAdet);
        Assert.Equal(1, satir.TrafoSevkAdet);
        Assert.Equal((int)GridSevkDurum.SevkEdildi, satir.GridSevkDurumuId);
        Assert.Equal(3, satir.GridSevkMiktari);
        Assert.Equal(0, satir.AktifGridSevkKarsilananMiktari);
        Assert.False(satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.Equal(0, icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal((int)DepoLokasyon.Grid, sandik.DepoLokasyonId);
        Assert.Equal((int)SurecDurum.Tamamlandi, satir.SurecDurumId);
        Assert.Equal(0, satir.KalanMiktar);
    }

    [Fact]
    public async Task TekilBekliyor_YalnizDurumVeKullaniciIziniDegistirir_MiktarlariKorur()
    {
        using var kurgu = new Kurgu();
        var satir = kurgu.SatirEkle(1, 8);
        satir.GridDurumuId = (int)GridDurum.EksikGeldi;
        satir.GridGelenAdet = 5;
        satir.TrafoSevkAdet = 1;
        satir.GridSevkDurumuId = (int)GridSevkDurum.Bekliyor;
        satir.GridSevkMiktari = 4;
        satir.AktifGridSevkKarsilananMiktari = 0;
        satir.AktifGridSevkPartisiErkenSonuclandirildiMi = false;

        var sonuc = await kurgu.TekliDurumGuncelleAsync(satir, GridDurum.Bekliyor, aciklama: "iz");

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal((int)GridDurum.Bekliyor, satir.GridDurumuId);
        Assert.Equal(5, satir.GridGelenAdet);
        Assert.Equal(1, satir.TrafoSevkAdet);
        Assert.Equal((int)GridSevkDurum.Bekliyor, satir.GridSevkDurumuId);
        Assert.Equal(4, satir.GridSevkMiktari);
        Assert.Equal(0, satir.AktifGridSevkKarsilananMiktari);
        Assert.False(satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.Equal(77, satir.GridPersonelId);
        Assert.Equal("iz", satir.GridAciklama);
    }

    [Fact]
    public async Task TekilSevk_GridGelenUstSinirinaEsitOndalikPartiyiBaslatir()
    {
        using var kurgu = new Kurgu();
        var satir = kurgu.SatirEkle(1, 5.5m);
        var icerik = kurgu.SandikVeIcerikEkle(satir, tahsis: 5.5m).Icerik;

        var sonuc = await kurgu.TekliDurumGuncelleAsync(
            satir,
            GridDurum.EksikGeldi,
            gridGelen: 3.125m,
            sevkDurumu: GridSevkDurum.SevkEdildi,
            sevkMiktari: 3.125m);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(3.125m, satir.GridGelenAdet);
        Assert.Equal(3.125m, satir.GridSevkMiktari);
        Assert.Equal(0, satir.AktifGridSevkKarsilananMiktari);
        Assert.False(satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.Equal(0, icerik.AktifGridSevkKarsilananMiktari);
        Assert.NotNull(satir.GridSevkTarihi);
    }

    [Fact]
    public async Task TrafoSevk_SandiktakiDigerUrunlerTamamsaSandigiOtomatikKapatir()
    {
        using var kurgu = new Kurgu();
        var trafo = kurgu.SatirEkle(1, 2, sandikNo: "7");
        var tamam = kurgu.SatirEkle(2, 1, sandikNo: "7", ceki: trafo.Ceki);
        tamam.UcKDurumuId = (int)UcKDurum.TamGeldi;
        tamam.UcKKarsilamaTipiId = (int)UcKDurum.TamGeldi;
        var sandik = kurgu.SandikVeIcerikEkle(trafo, tahsis: 2, sandikNo: "7").Sandik;
        kurgu.IcerikEkle(tamam, sandik, tahsis: 1);

        var sonuc = await kurgu.TekliDurumGuncelleAsync(
            trafo,
            GridDurum.TrafoSevk,
            gridGelen: 0,
            trafo: 2);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal((int)SandikDurum.Kapandi, sandik.DurumId);
        Assert.Contains(kurgu.Hareketler, h => h.Islem == "Sandık Otomatik Hazır");
    }

    [Theory]
    [InlineData("sevk")]
    [InlineData("saha")]
    [InlineData("tadilat")]
    public async Task TopluSevk_SecimdeTekKilitliSatirVarsaUygunSatiraDaDokunmaz(string kilit)
    {
        using var kurgu = new Kurgu();
        var kilitli = kurgu.SatirEkle(1, 2);
        var uygun = kurgu.SatirEkle(2, 2);
        var kilitliSandik = kurgu.SandikVeIcerikEkle(kilitli, tahsis: 2).Sandik;
        kurgu.SandikVeIcerikEkle(uygun, tahsis: 2);
        if (kilit == "sevk")
            kilitliSandik.DurumId = (int)SandikDurum.Sevkedildi;
        else if (kilit == "saha")
            kurgu.Saha.AktifKaynakSatirIdleri.Add(kilitli.Id);
        else
        {
            kilitli.KaliteDurumId = 51;
            kurgu.Lookup.Ayarla<LookupKaliteDurum>(51, "Tadilatta");
        }
        var kilitliOnce = SatirAnligi.Al(kilitli);
        var uygunOnce = SatirAnligi.Al(uygun);

        var sonuc = await kurgu.TopluSevkAsync(kilitli, uygun);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(kilitliOnce, SatirAnligi.Al(kilitli));
        Assert.Equal(uygunOnce, SatirAnligi.Al(uygun));
        Assert.Empty(kurgu.Uow.Repo<CekiSatiri>().Updated);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Empty(kurgu.Hareketler);
    }

    [Fact]
    public async Task TopluSevk_UcKIslemliSatiriAtlar_UygunSatiriSevkEderVeAtlamayiHareketeYazar()
    {
        using var kurgu = new Kurgu();
        var ucKIslemli = kurgu.SatirEkle(1, 2);
        var uygun = kurgu.SatirEkle(2, 2);
        kurgu.SandikVeIcerikEkle(ucKIslemli, tahsis: 2);
        kurgu.SandikVeIcerikEkle(uygun, tahsis: 2);
        ucKIslemli.GridDurumuId = (int)GridDurum.TamGeldi;
        ucKIslemli.GridGelenAdet = 2;
        ucKIslemli.GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi;
        ucKIslemli.GridSevkMiktari = 2;
        ucKIslemli.AktifGridSevkKarsilananMiktari = 2;
        ucKIslemli.AktifGridSevkPartisiErkenSonuclandirildiMi = false;
        ucKIslemli.UcKDurumuId = (int)UcKDurum.TamGeldi;
        ucKIslemli.UcKKarsilamaTipiId = (int)UcKDurum.TamGeldi;
        ucKIslemli.GelenMiktar = 2;
        var once = SatirAnligi.Al(ucKIslemli);

        var sonuc = await kurgu.TopluSevkAsync(ucKIslemli, uygun);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(once, SatirAnligi.Al(ucKIslemli));
        Assert.Equal((int)GridDurum.TamGeldi, uygun.GridDurumuId);
        Assert.Equal(2, uygun.GridGelenAdet);
        Assert.Equal(2, uygun.GridSevkMiktari);
        Assert.DoesNotContain(ucKIslemli, kurgu.Uow.Repo<CekiSatiri>().Updated);
        Assert.Contains(uygun, kurgu.Uow.Repo<CekiSatiri>().Updated);
        var hareket = Assert.Single(kurgu.Hareketler);
        Assert.Contains("Atlanan (1)", hareket.Aciklama);
        Assert.Contains("3K tarafında işlem yapılmış", hareket.Aciklama);
    }

    [Fact]
    public async Task TopluTerminal_DesteklenmeyenHedefiMutasyonsuzReddeder()
    {
        using var kurgu = new Kurgu();
        var satir = kurgu.SatirEkle(1, 3);
        var once = SatirAnligi.Al(satir);

        var sonuc = await kurgu.TopluTerminalAsync(GridDurum.EksikGeldi, satir);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(once, SatirAnligi.Al(satir));
        Assert.Empty(kurgu.Uow.Repo<CekiSatiri>().Updated);
        Assert.Equal(0, kurgu.Uow.SaveCount);
    }

    [Theory]
    [InlineData("sevk")]
    [InlineData("saha")]
    public async Task TopluTerminal_SecimdeSevkVeyaSahaKilidiVarsaButunSecimiMutasyonsuzReddeder(string kilit)
    {
        using var kurgu = new Kurgu();
        var kilitli = kurgu.SatirEkle(1, 2);
        var uygun = kurgu.SatirEkle(2, 2);
        var sandik = kurgu.SandikVeIcerikEkle(kilitli, tahsis: 2).Sandik;
        kurgu.SandikVeIcerikEkle(uygun, tahsis: 2);
        if (kilit == "sevk")
            sandik.DurumId = (int)SandikDurum.Sevkedildi;
        else
            kurgu.Saha.AktifKaynakSatirIdleri.Add(kilitli.Id);
        var kilitliOnce = SatirAnligi.Al(kilitli);
        var uygunOnce = SatirAnligi.Al(uygun);

        var sonuc = await kurgu.TopluTerminalAsync(GridDurum.Iptal, kilitli, uygun);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(kilitliOnce, SatirAnligi.Al(kilitli));
        Assert.Equal(uygunOnce, SatirAnligi.Al(uygun));
        Assert.Empty(kurgu.Uow.Repo<CekiSatiri>().Updated);
        Assert.Equal(0, kurgu.Uow.SaveCount);
    }

    [Fact]
    public async Task TekilSifirla_TumGridKaliteSurecTakibiniTemizler_KapaliSandigiYenidenAcar()
    {
        using var kurgu = new Kurgu();
        var satir = kurgu.SatirEkle(1, 4);
        var (sandik, icerik) = kurgu.SandikVeIcerikEkle(satir, tahsis: 4);
        sandik.DurumId = (int)SandikDurum.Kapandi;
        satir.GridDurumuId = (int)GridDurum.EksikGeldi;
        satir.GridGelenAdet = 3;
        satir.TrafoSevkAdet = 1;
        satir.GridSevkDurumuId = (int)GridSevkDurum.YenidenSevkGerekli;
        satir.GridSevkMiktari = 3;
        satir.AktifGridSevkKarsilananMiktari = 0;
        satir.AktifGridSevkPartisiErkenSonuclandirildiMi = false;
        satir.YenidenSevkGerekliAdet = 2;
        satir.GridSevkTarihi = DateTime.UtcNow;
        satir.GridPersonelId = 9;
        satir.GridAciklama = "eski";
        satir.KaliteDurumId = 4;
        satir.SurecDurumId = (int)SurecDurum.Imalat;
        icerik.AktifGridSevkKarsilananMiktari = 0;

        var sonuc = await kurgu.TekliSifirlaAsync(satir);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        TumGridAlanlariSifirlandiMi(satir, icerik);
        Assert.Equal((int)SandikDurum.Hazirlaniyor, sandik.DurumId);
        Assert.Equal(1, kurgu.Durum.KalanHesaplamaSayisi);
        Assert.Single(kurgu.Hareketler);
    }

    [Fact]
    public async Task TekilSifirla_UcKIslemiVarsaTumAlanlariMutasyonsuzKorur()
    {
        using var kurgu = new Kurgu();
        var satir = kurgu.SatirEkle(1, 4);
        var icerik = kurgu.SandikVeIcerikEkle(satir, tahsis: 4).Icerik;
        satir.GridDurumuId = (int)GridDurum.TamGeldi;
        satir.GridGelenAdet = 4;
        satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi;
        satir.GridSevkMiktari = 4;
        satir.AktifGridSevkKarsilananMiktari = 1;
        satir.AktifGridSevkPartisiErkenSonuclandirildiMi = false;
        satir.UcKDurumuId = (int)UcKDurum.EksikGeldi;
        satir.GelenMiktar = 1;
        icerik.AktifGridSevkKarsilananMiktari = 1;
        var once = SatirAnligi.Al(satir);

        var sonuc = await kurgu.TekliSifirlaAsync(satir);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(once, SatirAnligi.Al(satir));
        Assert.Equal(1, icerik.AktifGridSevkKarsilananMiktari);
        Assert.Empty(kurgu.Uow.Repo<CekiSatiri>().Updated);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Empty(kurgu.Hareketler);
    }

    [Fact]
    public async Task TopluSifirla_UcKIslemliSatiriAtlar_DigerSatiriVeSandiginiSifirlar()
    {
        using var kurgu = new Kurgu();
        var atlanan = kurgu.SatirEkle(1, 4);
        var uygun = kurgu.SatirEkle(2, 4);
        var atlananKayit = kurgu.SandikVeIcerikEkle(atlanan, tahsis: 4);
        var uygunKayit = kurgu.SandikVeIcerikEkle(uygun, tahsis: 4);
        atlananKayit.Sandik.DurumId = (int)SandikDurum.Kapandi;
        uygunKayit.Sandik.DurumId = (int)SandikDurum.Kapandi;
        foreach (var satir in new[] { atlanan, uygun })
        {
            satir.GridDurumuId = (int)GridDurum.TamGeldi;
            satir.GridGelenAdet = 4;
            satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi;
            satir.GridSevkMiktari = 4;
            satir.AktifGridSevkKarsilananMiktari = 0;
            satir.AktifGridSevkPartisiErkenSonuclandirildiMi = false;
            satir.KaliteDurumId = 2;
            satir.SurecDurumId = (int)SurecDurum.Imalat;
        }
        atlananKayit.Icerik.AktifGridSevkKarsilananMiktari = 2;
        uygunKayit.Icerik.AktifGridSevkKarsilananMiktari = 0;
        atlanan.UcKDurumuId = (int)UcKDurum.TamGeldi;
        atlanan.UcKKarsilamaTipiId = (int)UcKDurum.TamGeldi;
        atlanan.GelenMiktar = 2;
        var atlananOnce = SatirAnligi.Al(atlanan);

        var sonuc = await kurgu.TopluSifirlaAsync(atlanan, uygun);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(atlananOnce, SatirAnligi.Al(atlanan));
        Assert.Equal(2, atlananKayit.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal((int)SandikDurum.Kapandi, atlananKayit.Sandik.DurumId);
        TumGridAlanlariSifirlandiMi(uygun, uygunKayit.Icerik);
        Assert.Equal((int)SandikDurum.Hazirlaniyor, uygunKayit.Sandik.DurumId);
        Assert.DoesNotContain(atlanan, kurgu.Uow.Repo<CekiSatiri>().Updated);
        Assert.Contains(uygun, kurgu.Uow.Repo<CekiSatiri>().Updated);
        Assert.Single(kurgu.Hareketler);
    }

    [Theory]
    [InlineData("sevk")]
    [InlineData("saha")]
    public async Task TopluSifirla_SecimdeSevkVeyaSahaKilidiVarsaButunSecimiMutasyonsuzReddeder(string kilit)
    {
        using var kurgu = new Kurgu();
        var kilitli = kurgu.SatirEkle(1, 3);
        var uygun = kurgu.SatirEkle(2, 3);
        var kilitliKayit = kurgu.SandikVeIcerikEkle(kilitli, tahsis: 3);
        kurgu.SandikVeIcerikEkle(uygun, tahsis: 3);
        kilitli.GridDurumuId = uygun.GridDurumuId = (int)GridDurum.TamGeldi;
        kilitli.GridGelenAdet = uygun.GridGelenAdet = 3;
        if (kilit == "sevk")
            kilitliKayit.Sandik.DurumId = (int)SandikDurum.Sevkedildi;
        else
            kurgu.Saha.AktifKaynakSatirIdleri.Add(kilitli.Id);
        var kilitliOnce = SatirAnligi.Al(kilitli);
        var uygunOnce = SatirAnligi.Al(uygun);

        var sonuc = await kurgu.TopluSifirlaAsync(kilitli, uygun);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(kilitliOnce, SatirAnligi.Al(kilitli));
        Assert.Equal(uygunOnce, SatirAnligi.Al(uygun));
        Assert.Empty(kurgu.Uow.Repo<CekiSatiri>().Updated);
        Assert.Equal(0, kurgu.Uow.SaveCount);
    }

    [Fact]
    public async Task ManuelGridUrunu_EnYeniCekiyeYeniSandikVeTamIcerikleOndalikEklenir()
    {
        using var kurgu = new Kurgu(otomatikCekiOlustur: false);
        kurgu.CekiEkle(10, DateTime.UtcNow.AddDays(-2));
        var yeniCeki = kurgu.CekiEkle(11, DateTime.UtcNow.AddMinutes(-1));

        var sonuc = await kurgu.ManuelUrunEkleAsync(
            sandikNo: "M-7",
            miktar: 2.3456m,
            birimId: (int)Birim.Metre);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        var sandik = Assert.Single(kurgu.Uow.Repo<Sandik>().Rows);
        Assert.Equal("M-7", sandik.SandikNo);
        Assert.Equal((int)SandikDurum.Hazirlaniyor, sandik.DurumId);
        Assert.Equal((int)DepoLokasyon.Belirsiz, sandik.DepoLokasyonId);
        var satir = Assert.Single(kurgu.Uow.Repo<CekiSatiri>().Rows);
        Assert.Equal(yeniCeki.Id, satir.CekiId);
        Assert.True(satir.IsManuelEklenen);
        Assert.Equal(9999, satir.SiraNo);
        Assert.Equal(2.3456m, satir.IstenenAdet);
        Assert.Equal((int)Birim.Metre, satir.BirimId);
        Assert.Equal((int)GridDurum.TamGeldi, satir.GridDurumuId);
        Assert.Equal(2.3456m, satir.GridGelenAdet);
        Assert.Equal((int)GridSevkDurum.SevkEdilmedi, satir.GridSevkDurumuId);
        Assert.Equal((int)UcKDurum.Bekliyor, satir.UcKDurumuId);
        Assert.Equal((int)SurecDurum.Tamamlandi, satir.SurecDurumId);
        var icerik = Assert.Single(kurgu.Uow.Repo<SandikIcerik>().Rows);
        Assert.Equal(sandik.Id, icerik.SandikId);
        Assert.Equal(satir.Id, icerik.CekiSatiriId);
        Assert.Equal(2.3456m, icerik.TahsisMiktari);
        Assert.Equal(2.3456m, icerik.KonulanAdet);
        Assert.Equal(0, icerik.EksikAdet);
        Assert.Equal(3, kurgu.Uow.SaveCount);
        Assert.Single(kurgu.Hareketler);
    }

    [Fact]
    public async Task ManuelGridUrunu_SevkEdilmisSandiktaMutasyonsuzReddedilir()
    {
        using var kurgu = new Kurgu();
        kurgu.SandikEkle("M-7", SandikDurum.Sevkedildi);

        var sonuc = await kurgu.ManuelUrunEkleAsync("M-7", 2);

        Assert.False(sonuc.IsSuccess);
        Assert.Empty(kurgu.Uow.Repo<CekiSatiri>().Rows);
        Assert.Empty(kurgu.Uow.Repo<SandikIcerik>().Rows);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Empty(kurgu.Hareketler);
    }

    [Fact]
    public async Task Kalite_GecerliSeciminTamaminiProjeBagiylaGuncellerVeHareketlendirir()
    {
        using var kurgu = new Kurgu();
        var bir = kurgu.SatirEkle(1, 2);
        var iki = kurgu.SatirEkle(2, 2);
        kurgu.Lookup.Ayarla<LookupKaliteDurum>(4, "Kontrol Edildi");

        var sonuc = await kurgu.KaliteGuncelleAsync(4, bir, iki);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(4, bir.KaliteDurumId);
        Assert.Equal(4, iki.KaliteDurumId);
        Assert.Equal(2, kurgu.Uow.Repo<CekiSatiri>().Updated.Count);
        Assert.Equal(2, kurgu.Hareketler.Count);
        Assert.All(kurgu.Hareketler, hareket => Assert.Equal(77, hareket.KullaniciId));
    }

    [Fact]
    public async Task Kalite_GecersizLookupDegeriniMutasyonsuzReddeder()
    {
        using var kurgu = new Kurgu();
        var satir = kurgu.SatirEkle(1, 2);

        var sonuc = await kurgu.KaliteGuncelleAsync(999, satir);

        Assert.False(sonuc.IsSuccess);
        Assert.Null(satir.KaliteDurumId);
        Assert.Empty(kurgu.Uow.Repo<CekiSatiri>().Updated);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Empty(kurgu.Hareketler);
    }

    [Theory]
    [InlineData("sevk")]
    [InlineData("saha")]
    public async Task Kalite_SecimdeSevkVeyaSahaKilidiVarsaDigerSatiriDaMutasyonsuzKorur(string kilit)
    {
        using var kurgu = new Kurgu();
        var kilitli = kurgu.SatirEkle(1, 2);
        var uygun = kurgu.SatirEkle(2, 2);
        kurgu.Lookup.Ayarla<LookupKaliteDurum>(4, "Kontrol Edildi");
        var kilitliKayit = kurgu.SandikVeIcerikEkle(kilitli, tahsis: 2);
        if (kilit == "sevk")
            kilitliKayit.Sandik.DurumId = (int)SandikDurum.Sevkedildi;
        else
            kurgu.Saha.AktifKaynakSatirIdleri.Add(kilitli.Id);

        var sonuc = await kurgu.KaliteGuncelleAsync(4, kilitli, uygun);

        Assert.False(sonuc.IsSuccess);
        Assert.Null(kilitli.KaliteDurumId);
        Assert.Null(uygun.KaliteDurumId);
        Assert.Empty(kurgu.Uow.Repo<CekiSatiri>().Updated);
        Assert.Equal(0, kurgu.Uow.SaveCount);
    }

    [Fact]
    public void SurecValidatoru_KimlikListeVeEnumSinirlariniDogrular()
    {
        var validator = new SurecDurumGuncelleCommandValidator();
        var gecerli = new SurecDurumGuncelleCommand
        {
            ProjeId = 10,
            CekiSatiriIdler = [1, 2],
            SurecDurumId = (int)SurecDurum.Tedarik
        };
        var gecersiz = new SurecDurumGuncelleCommand
        {
            ProjeId = 0,
            CekiSatiriIdler = [1, 0],
            SurecDurumId = 999
        };

        Assert.True(validator.Validate(gecerli).IsValid);
        var sonuc = validator.Validate(gecersiz);
        Assert.False(sonuc.IsValid);
        Assert.Contains(sonuc.Errors, error => error.PropertyName == nameof(SurecDurumGuncelleCommand.ProjeId));
        Assert.Contains(sonuc.Errors, error => error.PropertyName.Contains(nameof(SurecDurumGuncelleCommand.CekiSatiriIdler)));
        Assert.Contains(sonuc.Errors, error => error.PropertyName == nameof(SurecDurumGuncelleCommand.SurecDurumId));
    }

    [Fact]
    public async Task Surec_TamamlanmisTekSatirVarsaButunSecimi409IleMutasyonsuzReddeder()
    {
        using var kurgu = new Kurgu();
        var tamam = kurgu.SatirEkle(1, 2);
        var acik = kurgu.SatirEkle(2, 2);
        tamam.SurecDurumId = (int)SurecDurum.Tamamlandi;
        acik.SurecDurumId = (int)SurecDurum.Ambar;
        kurgu.Lookup.Ayarla<LookupSurecDurum>((int)SurecDurum.Imalat, "İmalat");

        var sonuc = await kurgu.SurecGuncelleAsync(SurecDurum.Imalat, tamam, acik);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Equal((int)SurecDurum.Tamamlandi, tamam.SurecDurumId);
        Assert.Equal((int)SurecDurum.Ambar, acik.SurecDurumId);
        Assert.Empty(kurgu.Uow.Repo<CekiSatiri>().Updated);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.Empty(kurgu.Hareketler);
    }

    [Theory]
    [InlineData(GridDurum.TamGeldi, 2, 0, null, SurecDurum.Tamamlandi)]
    [InlineData(GridDurum.GridKapandi, 0, 0, null, SurecDurum.Tamamlandi)]
    [InlineData(GridDurum.Iptal, 0, 0, null, null)]
    [InlineData(GridDurum.EksikGeldi, 1, 0, SurecDurum.Tamamlandi, SurecDurum.Tamamlandi)]
    public void SurecOtomatikTamamlama_IptaliHaricEksigiKalmayaniTamamlar_NihaiDurumuGeriAcmaz(
        GridDurum gridDurumu,
        int gridGelen,
        int trafo,
        SurecDurum? mevcut,
        SurecDurum? beklenen)
    {
        var satir = new CekiSatiri
        {
            IstenenAdet = 2,
            GridDurumuId = (int)gridDurumu,
            GridGelenAdet = gridGelen,
            TrafoSevkAdet = trafo,
            SurecDurumId = mevcut.HasValue ? (int)mevcut.Value : null
        };

        GridSurecDurumHelper.SyncSurecTamamlandi(satir);

        Assert.Equal(beklenen.HasValue ? (int)beklenen.Value : null, satir.SurecDurumId);
    }

    [Fact]
    public void GridIsListesi_AktifPartideTeslimSuruyorsaGercekUcKIslemiSirasindaGeciciIsUretmez()
    {
        var satir = new CekiSatiri
        {
            IstenenAdet = 5,
            GridDurumuId = (int)GridDurum.EksikGeldi,
            GridGelenAdet = 3,
            GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi,
            GridSevkMiktari = 3,
            AktifGridSevkKarsilananMiktari = 1,
            AktifGridSevkPartisiErkenSonuclandirildiMi = false,
            UcKDurumuId = (int)UcKDurum.EksikGeldi,
            UcKKarsilamaTipiId = (int)UcKDurum.EksikGeldi,
            GelenMiktar = 1
        };

        var sonuc = GridIsListesiSiniflandirma.Belirle(
            satir,
            gridEksikMiktar: 2,
            kalanMiktar: 4);

        Assert.Null(sonuc);
    }

    private static void TumGridAlanlariSifirlandiMi(CekiSatiri satir, SandikIcerik icerik)
    {
        Assert.Equal((int)GridDurum.Gelmedi, satir.GridDurumuId);
        Assert.Equal(0, satir.GridGelenAdet);
        Assert.Equal(0, satir.TrafoSevkAdet);
        Assert.Equal((int)GridSevkDurum.SevkEdilmedi, satir.GridSevkDurumuId);
        Assert.Null(satir.GridSevkMiktari);
        Assert.Null(satir.AktifGridSevkKarsilananMiktari);
        Assert.Null(satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.Null(icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, satir.YenidenSevkGerekliAdet);
        Assert.Null(satir.GridSevkTarihi);
        Assert.Null(satir.GridPersonelId);
        Assert.Null(satir.GridAciklama);
        Assert.Null(satir.KaliteDurumId);
        Assert.Null(satir.SurecDurumId);
    }

    private sealed record SatirAnligi(
        int GridDurumuId,
        decimal GridGelenAdet,
        decimal TrafoSevkAdet,
        int GridSevkDurumuId,
        decimal? GridSevkMiktari,
        decimal? AktifGridSevkKarsilananMiktari,
        bool? ErkenSonuclandiMi,
        decimal YenidenSevkGerekliAdet,
        int UcKDurumuId,
        decimal GelenMiktar,
        decimal KarsilananMiktar,
        decimal StokKarsilanan,
        decimal ProjeKarsilanan,
        decimal TedarikciKarsilanan,
        int? KaliteDurumId,
        int? SurecDurumId,
        int? GridPersonelId,
        string? GridAciklama)
    {
        public static SatirAnligi Al(CekiSatiri satir) => new(
            satir.GridDurumuId,
            satir.GridGelenAdet,
            satir.TrafoSevkAdet,
            satir.GridSevkDurumuId,
            satir.GridSevkMiktari,
            satir.AktifGridSevkKarsilananMiktari,
            satir.AktifGridSevkPartisiErkenSonuclandirildiMi,
            satir.YenidenSevkGerekliAdet,
            satir.UcKDurumuId,
            satir.GelenMiktar,
            satir.KarsilananMiktar,
            satir.StokKarsilanan,
            satir.ProjeKarsilanan,
            satir.TedarikciKarsilanan,
            satir.KaliteDurumId,
            satir.SurecDurumId,
            satir.GridPersonelId,
            satir.GridAciklama);
    }

    private sealed class Kurgu : IDisposable
    {
        public const int ProjeId = 10;
        private readonly CurrentUserStub _user = new();
        private int _sonrakiCekiId = 10;
        private int _sonrakiSandikId = 100;
        private int _sonrakiIcerikId = 200;

        public Kurgu(bool otomatikCekiOlustur = true)
        {
            Proje = new Proje
            {
                Id = ProjeId,
                ProjeNo = "PA-TEST",
                Musteri = "Test",
                ProjeTipiId = (int)ProjeTipi.Normal,
                DurumId = (int)ProjeDurum.Hazirlaniyor
            };
            if (otomatikCekiOlustur)
                VarsayilanCeki = CekiEkle(_sonrakiCekiId++, DateTime.UtcNow.AddMinutes(-1));
        }

        public TestUnitOfWork Uow { get; } = new();
        public Proje Proje { get; }
        public Ceki? VarsayilanCeki { get; private set; }
        public LookupStub Lookup { get; } = new();
        public SahaStub Saha { get; } = new();
        public DurumStub Durum { get; } = new();
        public List<HareketGecmisi> Hareketler { get; } = [];

        public Ceki CekiEkle(int id, DateTime yuklemeTarihi)
        {
            var ceki = new Ceki
            {
                Id = id,
                ProjeId = ProjeId,
                Proje = Proje,
                YuklemeTarihi = yuklemeTarihi
            };
            Proje.Cekiler.Add(ceki);
            Uow.Repo<Ceki>().Rows.Add(ceki);
            VarsayilanCeki ??= ceki;
            return ceki;
        }

        public CekiSatiri SatirEkle(int id, decimal istenenAdet, string? sandikNo = null, Ceki? ceki = null)
        {
            ceki ??= VarsayilanCeki ?? CekiEkle(_sonrakiCekiId++, DateTime.UtcNow);
            var satir = new CekiSatiri
            {
                Id = id,
                CekiId = ceki.Id,
                Ceki = ceki,
                SiraNo = id,
                BarkodNo = $"B-{id}",
                Aciklama = $"Ürün {id}",
                IstenenAdet = istenenAdet,
                BirimId = (int)Birim.Adet,
                CekideGecenSandikNo = sandikNo ?? id.ToString(),
                FiiliSandikNo = sandikNo ?? id.ToString(),
                GridDurumuId = (int)GridDurum.Bekliyor,
                GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi,
                UcKDurumuId = (int)UcKDurum.Bekliyor,
                UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor,
                DurumId = (int)UrunDurum.Bekliyor
            };
            ceki.CekiSatirlari.Add(satir);
            Uow.Repo<CekiSatiri>().Rows.Add(satir);
            return satir;
        }

        public (Sandik Sandik, SandikIcerik Icerik) SandikVeIcerikEkle(
            CekiSatiri satir,
            decimal tahsis,
            string? sandikNo = null)
        {
            var no = sandikNo ?? satir.FiiliSandikNo ?? satir.CekideGecenSandikNo;
            var sandik = Uow.Repo<Sandik>().Rows.FirstOrDefault(s => s.ProjeId == ProjeId && s.SandikNo == no)
                ?? SandikEkle(no, SandikDurum.Hazirlaniyor);
            var icerik = IcerikEkle(satir, sandik, tahsis);
            return (sandik, icerik);
        }

        public Sandik SandikEkle(string sandikNo, SandikDurum durum)
        {
            var sandik = new Sandik
            {
                Id = _sonrakiSandikId++,
                ProjeId = ProjeId,
                Proje = Proje,
                SandikNo = sandikNo,
                DurumId = (int)durum,
                DepoLokasyonId = (int)DepoLokasyon.Belirsiz
            };
            Proje.Sandiklar.Add(sandik);
            Uow.Repo<Sandik>().Rows.Add(sandik);
            return sandik;
        }

        public SandikIcerik IcerikEkle(CekiSatiri satir, Sandik sandik, decimal tahsis)
        {
            var icerik = new SandikIcerik
            {
                Id = _sonrakiIcerikId++,
                SandikId = sandik.Id,
                Sandik = sandik,
                CekiSatiriId = satir.Id,
                CekiSatiri = satir,
                TahsisMiktari = tahsis,
                EksikAdet = tahsis
            };
            sandik.SandikIcerikleri.Add(icerik);
            satir.SandikIcerikleri.Add(icerik);
            Uow.Repo<SandikIcerik>().Rows.Add(icerik);
            return icerik;
        }

        public Task<Result> TekliDurumGuncelleAsync(
            CekiSatiri satir,
            GridDurum hedef,
            decimal? gridGelen = null,
            decimal? trafo = null,
            GridSevkDurum? sevkDurumu = null,
            decimal? sevkMiktari = null,
            string? aciklama = null) =>
            new GridDurumGuncelleCommandHandler(
                    Uow,
                    _user,
                    Durum,
                    new HareketStub(Hareketler),
                    Lookup,
                    Saha)
                .Handle(new GridDurumGuncelleCommand
                {
                    CekiSatiriId = satir.Id,
                    ProjeId = ProjeId,
                    YeniDurumId = (int)hedef,
                    GridGelenAdet = gridGelen,
                    TrafoSevkAdet = trafo,
                    GridSevkDurumuId = sevkDurumu.HasValue ? (int)sevkDurumu.Value : null,
                    SevkMiktari = sevkMiktari,
                    Aciklama = aciklama
                }, default);

        public Task<Result> TopluSevkAsync(params CekiSatiri[] satirlar) =>
            new GridTopluSevkCommandHandler(Uow, _user, Durum, new HareketStub(Hareketler), Lookup, Saha)
                .Handle(new GridTopluSevkCommand
                {
                    ProjeId = ProjeId,
                    CekiSatiriIdler = satirlar.Select(s => s.Id).ToList(),
                    Aciklama = "toplu sevk"
                }, default);

        public Task<Result> TopluTerminalAsync(GridDurum hedef, params CekiSatiri[] satirlar) =>
            new GridTopluDurumGuncelleCommandHandler(Uow, _user, Durum, new HareketStub(Hareketler), Saha)
                .Handle(new GridTopluDurumGuncelleCommand
                {
                    ProjeId = ProjeId,
                    CekiSatiriIdler = satirlar.Select(s => s.Id).ToList(),
                    HedefDurumId = (int)hedef,
                    Aciklama = "toplu terminal"
                }, default);

        public Task<Result> TekliSifirlaAsync(CekiSatiri satir) =>
            new GridDurumSifirlaCommandHandler(Uow, _user, Durum, new HareketStub(Hareketler), Saha)
                .Handle(new GridDurumSifirlaCommand
                {
                    ProjeId = ProjeId,
                    CekiSatiriId = satir.Id,
                    Aciklama = "geri al"
                }, default);

        public Task<Result> TopluSifirlaAsync(params CekiSatiri[] satirlar) =>
            new GridTopluSifirlaCommandHandler(Uow, _user, Durum, new HareketStub(Hareketler), Saha)
                .Handle(new GridTopluSifirlaCommand
                {
                    ProjeId = ProjeId,
                    CekiSatiriIdler = satirlar.Select(s => s.Id).ToList(),
                    Aciklama = "toplu geri al"
                }, default);

        public Task<Result> ManuelUrunEkleAsync(string sandikNo, decimal miktar, int? birimId = null) =>
            new GridManuelUrunEkleCommandHandler(Uow, new HareketStub(Hareketler), _user)
                .Handle(new GridManuelUrunEkleCommand
                {
                    ProjeId = ProjeId,
                    SandikNo = sandikNo,
                    SandikIsmi = "Manuel sandık",
                    BarkodNo = "MANUEL",
                    Aciklama = "Manuel ürün",
                    EklemeNedeni = "Test",
                    IstenenAdet = miktar,
                    BirimId = birimId
                }, default);

        public Task<Result> KaliteGuncelleAsync(int kaliteDurumId, params CekiSatiri[] satirlar) =>
            new KaliteDurumGuncelleCommandHandler(Uow, new HareketStub(Hareketler), _user, Lookup, Saha)
                .Handle(new KaliteDurumGuncelleCommand
                {
                    ProjeId = ProjeId,
                    CekiSatiriIdler = satirlar.Select(s => s.Id).ToList(),
                    KaliteDurumId = kaliteDurumId
                }, default);

        public Task<Result> SurecGuncelleAsync(SurecDurum surec, params CekiSatiri[] satirlar) =>
            new SurecDurumGuncelleCommandHandler(Uow, new HareketStub(Hareketler), _user, Lookup, Saha)
                .Handle(new SurecDurumGuncelleCommand
                {
                    ProjeId = ProjeId,
                    CekiSatiriIdler = satirlar.Select(s => s.Id).ToList(),
                    SurecDurumId = (int)surec
                }, default);

        public void Dispose() => Uow.Dispose();
    }

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        private readonly Dictionary<Type, object> _repositories = [];

        public bool HasActiveTransaction { get; private set; }
        public int SaveCount { get; private set; }

        public Repo<T> Repo<T>() where T : BaseEntity
        {
            if (!_repositories.TryGetValue(typeof(T), out var repository))
                _repositories[typeof(T)] = repository = new Repo<T>();
            return (Repo<T>)repository;
        }

        public IGenericRepository<T> GetRepository<T>() where T : BaseEntity => Repo<T>();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            HasActiveTransaction = true;
            try
            {
                return await operation(cancellationToken);
            }
            finally
            {
                HasActiveTransaction = false;
            }
        }

        public void RegisterAfterCommit(Func<CancellationToken, Task> callback) => throw new NotSupportedException();
        public void RegisterAfterRollback(Func<CancellationToken, Task> callback) => throw new NotSupportedException();
        public void Dispose() { }
    }

    private sealed class Repo<T> : IGenericRepository<T> where T : BaseEntity
    {
        private int _sonrakiId = 10_000;

        public List<T> Rows { get; } = [];
        public List<T> Updated { get; } = [];

        public Task<T?> GetByIdAsync(int id) => Task.FromResult(Rows.SingleOrDefault(entity => entity.Id == id));
        public Task<IEnumerable<T>> GetAllAsync() => Task.FromResult<IEnumerable<T>>(Rows);
        public Task<IEnumerable<T>> GetAllWithIncludeAsync<TProperty>(Expression<Func<T, TProperty>> include) => GetAllAsync();
        public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
            Task.FromResult<IEnumerable<T>>(Rows.Where(predicate.Compile()).ToList());
        public IQueryable<T> Queryable() => Rows.AsQueryable();

        public Task AddAsync(T entity)
        {
            if (entity.Id == 0)
                entity.Id = _sonrakiId++;
            Rows.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(T entity) => Updated.Add(entity);
        public void Remove(T entity) => Rows.Remove(entity);
    }

    private sealed class CurrentUserStub : ICurrentUserService
    {
        public int? UserId => 77;
        public bool IsAuthenticated => true;
        public string? MenuKod => null;
    }

    private sealed class DurumStub : IDurumHesaplaService
    {
        public int KalanHesaplamaSayisi { get; private set; }

        public int HesaplaGenelDurum(int gridDurumuId, int ucKDurumuId) => (int)UrunDurum.Bekliyor;

        public void HesaplaKalanVeDurum(CekiSatiri satir)
        {
            KalanHesaplamaSayisi++;
        }
    }

    private sealed class HareketStub(List<HareketGecmisi> hareketler) : IHareketService
    {
        public Task HareketKaydetAsync(HareketGecmisi hareket)
        {
            hareketler.Add(hareket);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<HareketGecmisi>> GetProjeHareketleriAsync(int projeId) => throw new NotSupportedException();
        public Task<IEnumerable<HareketGecmisi>> GetUrunHareketleriAsync(string referansTipi, string referansId) => throw new NotSupportedException();
        public Task<(IEnumerable<HareketGecmisi> Items, int TotalCount)> GetPaginatedProjeHareketleriAsync(
            int projeId,
            string? searchTerm,
            int? islemTipiId,
            int pageNumber,
            int pageSize) => throw new NotSupportedException();
    }

    private sealed class LookupStub : ILookupCacheService
    {
        private readonly Dictionary<(Type Type, int Id), string> _degerler = [];

        public void Ayarla<TLookup>(int id, string deger) where TLookup : LookupBase =>
            _degerler[(typeof(TLookup), id)] = deger;

        public string GetDeger<TLookup>(int id) where TLookup : LookupBase =>
            _degerler.GetValueOrDefault((typeof(TLookup), id), string.Empty);

        public Task WarmupAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RefreshAsync<TLookup>(CancellationToken ct = default) where TLookup : LookupBase => Task.CompletedTask;
    }

    private sealed class SahaStub : ISahaTamamlamaService
    {
        public HashSet<int> AktifKaynakSatirIdleri { get; } = [];

        public Task<bool> AktifTamamlamaVarMiAsync(int kaynakCekiSatiriId, CancellationToken cancellationToken = default) =>
            Task.FromResult(AktifKaynakSatirIdleri.Contains(kaynakCekiSatiriId));

        public Task<Dictionary<int, decimal>> GetAktifTamamlamaMapAsync(
            IEnumerable<int> ids,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ids
                .Where(AktifKaynakSatirIdleri.Contains)
                .Distinct()
                .ToDictionary(id => id, _ => 1m));

        public Task SenkronizeKaynakProjelerAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<Dictionary<int, decimal>> GetAktifIsTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetAktifGerceklesenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetSevkEdilenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetSevkEdilenGerceklesenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<HashSet<int>> GetAktifSandikBazliAktarimSatirIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<KaynakSandikSahaAktarimDurumu> GetKaynakSandikSahaAktarimDurumuAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SenkronizeKaynakProjelerBySahaSandikIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
