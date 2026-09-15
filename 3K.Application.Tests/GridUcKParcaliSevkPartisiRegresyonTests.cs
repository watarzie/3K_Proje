using System.Linq.Expressions;
using _3K.Application.Common;
using _3K.Application.Features.CekiIslemleri.Commands;
using _3K.Application.Features.GridIslemleri.Commands;
using _3K.Application.Features.GridIslemleri.Validators;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Validators;
using _3K.Application.Features.UcKIslemleri.Commands;
using _3K.Application.Features.UcKIslemleri.Validators;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

/// <summary>
/// PA699-02 / FCT01181927 vakasının yaşam döngüsünü sabitler:
/// üç adet Grid'e gelir, ilk partide iki adet sevk edilip 3K'da karşılanır,
/// kalan bir adet ise aynı satır üzerinden ikinci parti olarak tamamlanır.
/// </summary>
public sealed class GridUcKParcaliSevkPartisiRegresyonTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task TamTeslimdenSonraCekiIkiUcDuzenlenir_KalanBirTekliVeTopluAkistaTamamlanir(
        bool topluGrid, bool topluUcK)
    {
        using var kurgu = new Kurgu(
            istenen: 2, tahsis: 2, gridGelen: 2, aktifSevk: 2,
            gelen: 0, konulan: 0, aktifPartideKarsilanan: 0);
        var ilkTeslim = await kurgu.TekliUcKTamGeldiAsync();
        Assert.True(ilkTeslim.IsSuccess, ilkTeslim.Error?.Message);
        Assert.Equal(0, kurgu.Satir.KalanMiktar);
        Assert.Equal(2, kurgu.Icerik.KonulanAdet);

        var duzenleme = await kurgu.CekiMiktariniDuzenleAsync(3);

        Assert.True(duzenleme.IsSuccess, duzenleme.Error?.Message);
        Assert.Equal(2m, kurgu.Satir.OrijinalIstenenAdet);
        Assert.Equal(3, kurgu.Icerik.TahsisMiktari);
        Assert.Equal(1, kurgu.Icerik.EksikAdet);
        Assert.Equal(1, kurgu.Satir.KalanMiktar);
        Assert.Equal(1, kurgu.Satir.GridEksikMiktar);
        Assert.Equal((int)GridDurum.EksikGeldi, kurgu.Satir.GridDurumuId);
        Assert.Equal(2, kurgu.Satir.GridGelenAdet);
        Assert.Equal(2, kurgu.Satir.GelenMiktar);
        Assert.Equal(2, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(2, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        var karar = GridUcKSevkPartisiKurali.DevamSevkiniDegerlendir(kurgu.Satir);
        Assert.True(karar.YeniPartiMi);
        Assert.Equal(1, karar.UstSinir);

        var devam = topluGrid
            ? await kurgu.TopluGridSevkEtAsync()
            : await kurgu.TekliGridSevkEtAsync(1);
        Assert.True(devam.IsSuccess, devam.Error?.Message);
        Assert.Equal(1, kurgu.Satir.GridSevkMiktari);
        Assert.Equal(2, kurgu.Satir.GelenMiktar);
        Assert.Equal(2, kurgu.Icerik.KonulanAdet);

        var sonTeslim = topluUcK
            ? await kurgu.TopluUcKTamGeldiAsync()
            : await kurgu.TekliUcKTamGeldiAsync();
        Assert.True(sonTeslim.IsSuccess, sonTeslim.Error?.Message);
        Assert.Equal(3, kurgu.Satir.GelenMiktar);
        Assert.Equal(3, kurgu.Icerik.KonulanAdet);
        Assert.Equal(0, kurgu.Satir.KalanMiktar);
        Assert.Equal(0, kurgu.Icerik.EksikAdet);
        Assert.False(GridUcKSevkPartisiKurali.DevamSevkiniDegerlendir(kurgu.Satir).YeniPartiMi);

        await kurgu.TekliUcKTamGeldiAsync();
        Assert.Equal(3, kurgu.Satir.GelenMiktar);
        Assert.Equal(3, kurgu.Icerik.KonulanAdet);
    }

    [Fact]
    public async Task LegacyTamTeslimdenSonraMiktarArtisi_TeslimiSifirlamadanKalaniSevkEder()
    {
        using var kurgu = new Kurgu(istenen: 2, tahsis: 2, gridGelen: 2);
        var duzenleme = await kurgu.CekiMiktariniDuzenleAsync(3);
        Assert.True(duzenleme.IsSuccess, duzenleme.Error?.Message);
        Assert.Null(kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Null(kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(2, kurgu.Satir.GelenMiktar);

        var devam = await kurgu.TekliGridSevkEtAsync(1);
        Assert.True(devam.IsSuccess, devam.Error?.Message);
        var teslim = await kurgu.TekliUcKTamGeldiAsync();
        Assert.True(teslim.IsSuccess, teslim.Error?.Message);
        Assert.Equal(3, kurgu.Satir.GelenMiktar);
        Assert.Equal(3, kurgu.Icerik.KonulanAdet);
        Assert.Equal(0, kurgu.Satir.KalanMiktar);
    }

    [Theory]
    [InlineData(UcKDurum.TedarikcidenGeldi)]
    [InlineData(UcKDurum.StoktanKarsilandi)]
    public async Task CekiArtisindanDoganYeniEksik_KaynaktanKarsilanincaEskiGridTesliminiDegistirmez(UcKDurum kaynak)
    {
        using var kurgu = new Kurgu(istenen: 2, tahsis: 2, gridGelen: 2,
            aktifPartideKarsilanan: 2);
        var stok = kaynak == UcKDurum.StoktanKarsilandi ? kurgu.StokEkle(1) : null;
        var duzenleme = await kurgu.CekiMiktariniDuzenleAsync(3);
        Assert.True(duzenleme.IsSuccess, duzenleme.Error?.Message);

        var karsilama = await kurgu.TekliUcKDurumGuncelleAsync(kaynak, gelenAdet: 1, stokKaydiId: stok?.Id);

        Assert.True(karsilama.IsSuccess, karsilama.Error?.Message);
        Assert.Equal(2, kurgu.Satir.GelenMiktar);
        Assert.Equal(2, kurgu.Satir.GridGelenAdet);
        Assert.Equal(2, kurgu.Satir.GridSevkMiktari);
        Assert.Equal(2, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(3, kurgu.Icerik.KonulanAdet);
        Assert.Equal(0, kurgu.Satir.KalanMiktar);
        Assert.Equal(0, kurgu.Icerik.EksikAdet);
        Assert.Equal(kaynak == UcKDurum.StoktanKarsilandi ? 1 : 0, kurgu.Satir.StokKarsilanan);
        Assert.Equal(kaynak == UcKDurum.TedarikcidenGeldi ? 1 : 0, kurgu.Satir.TedarikciKarsilanan);
        if (stok != null) Assert.Equal(0, stok.Miktar);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CekiArtisiSonrasiDevamSevki_KaliteVeNihaiSevkKilidiniDelmez(bool nihaiSevk)
    {
        using var kurgu = new Kurgu(istenen: 2, tahsis: 2, gridGelen: 2,
            aktifPartideKarsilanan: 2);
        var duzenleme = await kurgu.CekiMiktariniDuzenleAsync(3);
        Assert.True(duzenleme.IsSuccess, duzenleme.Error?.Message);
        if (nihaiSevk)
            kurgu.Icerik.Sandik.DurumId = (int)SandikDurum.Sevkedildi;
        else
            kurgu.Satir.KaliteDurumId = LookupStub.TadilattaId;

        var devam = await kurgu.TekliGridSevkEtAsync(1);

        Assert.False(devam.IsSuccess);
        Assert.Equal(2, kurgu.Satir.GridSevkMiktari);
        Assert.Equal(2, kurgu.Satir.GelenMiktar);
        Assert.Equal(2, kurgu.Icerik.KonulanAdet);
        Assert.Equal(2, kurgu.Satir.AktifGridSevkKarsilananMiktari);
    }

    [Fact]
    public async Task CekiArtisi_MevcutPartiHenuzTamTeslimDegilseDevamKilidiniDelmez()
    {
        using var kurgu = new Kurgu(
            istenen: 2, tahsis: 2, gridGelen: 2, aktifSevk: 2,
            gelen: 1, konulan: 1, aktifPartideKarsilanan: 1);
        var duzenleme = await kurgu.CekiMiktariniDuzenleAsync(3);
        Assert.True(duzenleme.IsSuccess, duzenleme.Error?.Message);
        Assert.False(GridUcKSevkPartisiKurali.DevamSevkiniDegerlendir(kurgu.Satir).YeniPartiMi);
        var devam = await kurgu.TekliGridSevkEtAsync(1);
        Assert.False(devam.IsSuccess);
        Assert.Equal(2, kurgu.Satir.GridSevkMiktari);
        Assert.Equal(1, kurgu.Satir.GelenMiktar);
        Assert.Equal(1, kurgu.Icerik.KonulanAdet);
        Assert.Equal(1, kurgu.Satir.AktifGridSevkKarsilananMiktari);
    }

    [Fact]
    public async Task TekliGridIkinciParti_TopluUcKKarsilama_KalaniTamamlarVeTekrarCagriCiftSaymaz()
    {
        using var kurgu = new Kurgu();

        var sevk = await kurgu.TekliGridSevkEtAsync(1);

        Assert.True(sevk.IsSuccess, sevk.Error?.Message);
        kurgu.IkinciPartiAcildiMi(1);

        var karsilama = await kurgu.TopluUcKTamGeldiAsync();

        Assert.True(karsilama.IsSuccess, karsilama.Error?.Message);
        kurgu.TamamenKarsilandiMi();

        var tekrar = await kurgu.TopluUcKTamGeldiAsync();

        Assert.True(tekrar.IsSuccess, tekrar.Error?.Message);
        kurgu.TamamenKarsilandiMi();
    }

    [Fact]
    public async Task TopluGridIkinciParti_TekliUcKKarsilama_KalaniTamamlarVeTahsisDegismez()
    {
        using var kurgu = new Kurgu();

        var sevk = await kurgu.TopluGridSevkEtAsync();

        Assert.True(sevk.IsSuccess, sevk.Error?.Message);
        kurgu.IkinciPartiAcildiMi(1);

        var karsilama = await kurgu.TekliUcKTamGeldiAsync();

        Assert.True(karsilama.IsSuccess, karsilama.Error?.Message);
        kurgu.TamamenKarsilandiMi();
        Assert.Equal(3, kurgu.Icerik.TahsisMiktari);
    }

    [Fact]
    public async Task StokKarsilamasi_TahsisKapasitesiYetersizseStoguTekBasinaDusurmez()
    {
        using var kurgu = new Kurgu(
            istenen: 3,
            tahsis: 2,
            gridGelen: 2,
            aktifSevk: 2,
            gelen: 2,
            konulan: 2,
            aktifPartideKarsilanan: 2);
        kurgu.Satir.GridDurumuId = (int)GridDurum.EksikGeldi;
        var stok = kurgu.StokEkle(10);

        var sonuc = await kurgu.TekliUcKDurumGuncelleAsync(
            UcKDurum.StoktanKarsilandi,
            gelenAdet: 1,
            stokKaydiId: stok.Id);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains("sandıklara tahsis", sonuc.Error!.Message);
        Assert.Equal(10, stok.Miktar);
        Assert.Equal(0, kurgu.Satir.StokKarsilanan);
        Assert.Equal(2, kurgu.Satir.GelenMiktar);
        Assert.Equal(0, kurgu.Uow.SaveCount);
    }

    [Fact]
    public async Task CokluSandiktaGeriGonderim_SandikSecilmedenFizikselDagilimiTahminEtmez()
    {
        using var kurgu = new Kurgu(
            istenen: 4,
            tahsis: 2,
            gridGelen: 4,
            aktifSevk: 4,
            gelen: 4,
            konulan: 2,
            aktifPartideKarsilanan: 4);
        kurgu.Icerik.AktifGridSevkKarsilananMiktari = 2;
        var ikinci = kurgu.IkinciTahsisEkle(tahsis: 2, konulan: 2);
        ikinci.AktifGridSevkKarsilananMiktari = 2;

        var sonuc = await kurgu.TekliUcKDurumGuncelleAsync(
            UcKDurum.GeriGonderildi,
            gelenAdet: 1,
            sandikIcerikId: null);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains("sandık seçilmelidir", sonuc.Error!.Message);
        Assert.Equal(4, kurgu.Satir.GelenMiktar);
        Assert.Equal(4, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(2, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(2, ikinci.AktifGridSevkKarsilananMiktari);
    }

    [Fact]
    public async Task ProjeyeAktarilanGridMiktari_GrideIadeUstSinirindanDusulur()
    {
        using var kurgu = new Kurgu(
            istenen: 4,
            tahsis: 4,
            gridGelen: 4,
            aktifSevk: 4,
            gelen: 4,
            konulan: 4,
            aktifPartideKarsilanan: 4);
        kurgu.Satir.ProjeGonderilen = 2;

        var fazlaIade = await kurgu.TekliUcKDurumGuncelleAsync(
            UcKDurum.GeriGonderildi,
            gelenAdet: 3);

        Assert.False(fazlaIade.IsSuccess);
        Assert.Equal(4, kurgu.Satir.GelenMiktar);

        var izinliIade = await kurgu.TekliUcKDurumGuncelleAsync(
            UcKDurum.GeriGonderildi,
            gelenAdet: 2);

        Assert.True(izinliIade.IsSuccess, izinliIade.Error?.Message);
        Assert.Equal(2, kurgu.Satir.GelenMiktar);
        Assert.Equal(2, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.True(kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
    }

    [Fact]
    public async Task EksikPartiAlternatifKaynaktanKapanincaEskiPartiYenidenTeslimeAcilmaz()
    {
        using var kurgu = new Kurgu(
            istenen: 5,
            tahsis: 5,
            gridGelen: 5,
            aktifSevk: 5,
            gelen: 0,
            konulan: 0,
            aktifPartideKarsilanan: 0);

        var eksik = await kurgu.TekliUcKDurumGuncelleAsync(
            UcKDurum.EksikGeldi,
            gelenAdet: 3);
        Assert.True(eksik.IsSuccess, eksik.Error?.Message);
        Assert.True(kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi);

        var telafi = await kurgu.TekliUcKDurumGuncelleAsync(
            UcKDurum.TedarikcidenGeldi,
            gelenAdet: 2);

        Assert.True(telafi.IsSuccess, telafi.Error?.Message);
        Assert.Equal(0, kurgu.Satir.KalanMiktar);
        Assert.Equal((int)GridSevkDurum.SevkEdildi, kurgu.Satir.GridSevkDurumuId);
        Assert.True(kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.False(GridUcKSevkPartisiKurali.AktifPartiTeslimeAcikMi(kurgu.Satir));
        Assert.True(GridUcKSevkPartisiKurali.GeriGonderimeAcikMi(kurgu.Satir, kurgu.Icerik));
    }

    [Fact]
    public async Task LegacyAcikPartideIlkKismiTeslim_YasamDongusunuMaterializeEderVeKalanTeslimiEngellemez()
    {
        using var kurgu = new Kurgu(
            istenen: 5,
            tahsis: 5,
            gridGelen: 5,
            aktifSevk: 5,
            gelen: 0,
            konulan: 0,
            aktifPartideKarsilanan: null);

        var ilkTeslim = await kurgu.EskiTekliUcKTeslimAlAsync(2);

        Assert.True(ilkTeslim.IsSuccess, ilkTeslim.Error?.Message);
        Assert.Equal(2, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.False(kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.Equal(2, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.False(GridUcKSevkPartisiKurali.LegacyAktifPartiBelirsizMi(kurgu.Satir));
        Assert.True(GridUcKSevkPartisiKurali.AktifPartiTeslimEdilebilirMi(kurgu.Satir));

        var kalanTeslim = await kurgu.EskiTekliUcKTeslimAlAsync(3);

        Assert.True(kalanTeslim.IsSuccess, kalanTeslim.Error?.Message);
        Assert.Equal(5, kurgu.Satir.GelenMiktar);
        Assert.Equal(5, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.False(kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.True(GridUcKSevkPartisiKurali.GeriGonderimeAcikMi(kurgu.Satir, kurgu.Icerik));

        var iade = await kurgu.TekliUcKDurumGuncelleAsync(
            UcKDurum.GeriGonderildi,
            gelenAdet: 1);

        Assert.True(iade.IsSuccess, iade.Error?.Message);
        Assert.Equal(4, kurgu.Satir.GelenMiktar);
    }

    [Fact]
    public async Task LegacyGelmediPartisi_AlternatifKaynakKismiKapatilmadanOnceSonuclanmisOlarakMaterializeEdilir()
    {
        using var kurgu = new Kurgu(
            istenen: 5,
            tahsis: 5,
            gridGelen: 0,
            aktifSevk: 5,
            gelen: 0,
            konulan: 0,
            aktifPartideKarsilanan: null);
        kurgu.Satir.GridDurumuId = (int)GridDurum.Gelmedi;
        kurgu.Satir.GridSevkDurumuId = (int)GridSevkDurum.YenidenSevkGerekli;
        kurgu.Satir.YenidenSevkGerekliAdet = 5;
        kurgu.Satir.UcKDurumuId = (int)UcKDurum.Gelmedi;
        kurgu.Satir.UcKKarsilamaTipiId = (int)UcKDurum.Gelmedi;
        kurgu.Satir.TeslimTarihi = DateTime.UtcNow;

        var sonuc = await kurgu.TekliUcKDurumGuncelleAsync(
            UcKDurum.TedarikcidenGeldi,
            gelenAdet: 2);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(0, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.True(kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.Equal(0, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(3, kurgu.Satir.YenidenSevkGerekliAdet);
        Assert.Equal(3, kurgu.Satir.KalanMiktar);
        Assert.False(GridUcKSevkPartisiKurali.LegacyAktifPartiBelirsizMi(kurgu.Satir));

        var devamKarari = GridUcKSevkPartisiKurali.DevamSevkiniDegerlendir(kurgu.Satir);
        Assert.True(devamKarari.YeniPartiMi);
        Assert.Equal(GridUcKDevamSevkTipi.YenidenSevk, devamKarari.Tip);
        Assert.Equal(3, devamKarari.UstSinir);
    }

    [Fact]
    public async Task LegacyGelmediPartisi_TopluTedarikciSeciliSandikKadarKapatirVeKalanDevamSevkiniKorur()
    {
        using var kurgu = new Kurgu(
            istenen: 5,
            tahsis: 2,
            gridGelen: 0,
            aktifSevk: 5,
            gelen: 0,
            konulan: 0,
            aktifPartideKarsilanan: null);
        var ikinciIcerik = kurgu.IkinciTahsisEkle(tahsis: 3, konulan: 0);
        kurgu.Satir.GridDurumuId = (int)GridDurum.Gelmedi;
        kurgu.Satir.GridSevkDurumuId = (int)GridSevkDurum.YenidenSevkGerekli;
        kurgu.Satir.YenidenSevkGerekliAdet = 5;
        kurgu.Satir.UcKDurumuId = (int)UcKDurum.Gelmedi;
        kurgu.Satir.UcKKarsilamaTipiId = (int)UcKDurum.Gelmedi;
        kurgu.Satir.TeslimTarihi = DateTime.UtcNow;

        var sonuc = await kurgu.TopluTedarikcidenKarsilaAsync(kurgu.Icerik.Id);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(0, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.True(kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.Equal(0, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, ikinciIcerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(2, kurgu.Icerik.TedarikciKarsilanan);
        Assert.Equal(2, kurgu.Icerik.KonulanAdet);
        Assert.Equal(0, ikinciIcerik.KonulanAdet);
        Assert.Equal(3, kurgu.Satir.YenidenSevkGerekliAdet);
        Assert.Equal(3, kurgu.Satir.KalanMiktar);

        var devamKarari = GridUcKSevkPartisiKurali.DevamSevkiniDegerlendir(kurgu.Satir);
        Assert.True(devamKarari.YeniPartiMi);
        Assert.Equal(GridUcKDevamSevkTipi.YenidenSevk, devamKarari.Tip);
        Assert.Equal(3, devamKarari.UstSinir);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LegacyTamPartide_SadeceTedarikciPayliSandikSifirlanincaAktifGridPartisiBelirsizlesmez(
        bool toplu)
    {
        using var kurgu = new Kurgu(
            istenen: 4,
            tahsis: 2,
            gridGelen: 2,
            aktifSevk: 2,
            gelen: 2,
            konulan: 2,
            aktifPartideKarsilanan: null);
        var gridPayliIcerik = kurgu.IkinciTahsisEkle(tahsis: 2, konulan: 2);
        kurgu.Satir.KarsilananMiktar = 2;
        kurgu.Satir.TedarikciKarsilanan = 2;
        kurgu.Satir.UcKDurumuId = (int)UcKDurum.TamGeldi;
        kurgu.Satir.UcKKarsilamaTipiId = (int)UcKDurum.TamGeldi;
        kurgu.Icerik.TedarikciKarsilanan = 2;

        var sonuc = toplu
            ? await kurgu.TopluUcKSifirlaAsync(kurgu.Icerik.Id)
            : await kurgu.TekliUcKSifirlaAsync(kurgu.Icerik.Id);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(2, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.False(kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.Equal(0, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(2, gridPayliIcerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, kurgu.Icerik.KonulanAdet);
        Assert.Equal(2, gridPayliIcerik.KonulanAdet);
        Assert.Equal(2, kurgu.Satir.GelenMiktar);
        Assert.Equal(0, kurgu.Satir.TedarikciKarsilanan);
        Assert.False(GridUcKSevkPartisiKurali.LegacyAktifPartiBelirsizMi(kurgu.Satir));
        Assert.True(GridUcKSevkPartisiKurali.GeriGonderimeAcikMi(kurgu.Satir, gridPayliIcerik));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AktifGridPartisiOlmayanKaynakKarsilamasi_TamResetSonrasiTekrarUygulanabilir(
        bool toplu)
    {
        using var kurgu = new Kurgu(
            istenen: 3,
            tahsis: 3,
            gridGelen: 0,
            aktifSevk: 0,
            gelen: 0,
            konulan: 0,
            aktifPartideKarsilanan: null);
        kurgu.Satir.GridDurumuId = (int)GridDurum.Gelmedi;
        kurgu.Satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi;
        kurgu.Satir.GridSevkMiktari = null;

        var ilkKarsilama = toplu
            ? await kurgu.TopluTedarikcidenKarsilaAsync(kurgu.Icerik.Id)
            : await kurgu.TekliUcKDurumGuncelleAsync(
                UcKDurum.TedarikcidenGeldi,
                gelenAdet: 3);

        Assert.True(ilkKarsilama.IsSuccess, ilkKarsilama.Error?.Message);

        var reset = toplu
            ? await kurgu.TopluUcKSifirlaAsync(null)
            : await kurgu.TekliUcKSifirlaAsync(null);

        Assert.True(reset.IsSuccess, reset.Error?.Message);
        Assert.Null(kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Null(kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.Null(kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, kurgu.Satir.KarsilananMiktar);
        Assert.Equal(0, kurgu.Satir.TedarikciKarsilanan);
        Assert.Equal(0, kurgu.Icerik.KonulanAdet);

        var ikinciKarsilama = toplu
            ? await kurgu.TopluTedarikcidenKarsilaAsync(kurgu.Icerik.Id)
            : await kurgu.TekliUcKDurumGuncelleAsync(
                UcKDurum.TedarikcidenGeldi,
                gelenAdet: 3);

        Assert.True(ikinciKarsilama.IsSuccess, ikinciKarsilama.Error?.Message);
        Assert.NotEqual(409, ikinciKarsilama.StatusCode);
        Assert.Equal(3, kurgu.Satir.TedarikciKarsilanan);
        Assert.Equal(3, kurgu.Icerik.TedarikciKarsilanan);
        Assert.Equal(3, kurgu.Icerik.KonulanAdet);
        Assert.Null(kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Null(kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.Null(kurgu.Icerik.AktifGridSevkKarsilananMiktari);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GridEksikSatirinTamamlamaSevki_EskiKalanSemantiginiTekliVeTopludaKorur(
        bool toplu)
    {
        using var kurgu = new Kurgu(
            istenen: 3,
            tahsis: 3,
            gridGelen: 2,
            aktifSevk: 2,
            gelen: 2,
            konulan: 2,
            aktifPartideKarsilanan: 2);
        kurgu.Satir.GridDurumuId = (int)GridDurum.EksikGeldi;

        var sonuc = toplu
            ? await kurgu.TopluGridSevkEtAsync()
            : await kurgu.TekliGridSevkEtAsync(1);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal((int)GridDurum.TamGeldi, kurgu.Satir.GridDurumuId);
        Assert.Equal(3, kurgu.Satir.GridGelenAdet);
        Assert.Equal(1, kurgu.Satir.GridSevkMiktari);
        Assert.Equal(0, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal((int)UcKDurum.Bekliyor, kurgu.Satir.UcKDurumuId);
        Assert.Equal((int)UcKDurum.Bekliyor, kurgu.Satir.UcKKarsilamaTipiId);
        Assert.Equal(2, kurgu.Satir.GelenMiktar);
        Assert.Equal(2, kurgu.Icerik.KonulanAdet);
    }

    [Fact]
    public async Task IkinciParti_IstenenMiktardanFazlaSevkEdilemezVeOncekiTeslimiDegistirmez()
    {
        using var kurgu = new Kurgu();

        var sonuc = await kurgu.TekliGridSevkEtAsync(2);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(2, kurgu.Satir.GelenMiktar);
        Assert.Equal(2, kurgu.Icerik.KonulanAdet);
        Assert.Equal(3, kurgu.Icerik.TahsisMiktari);
        Assert.Equal(1, kurgu.Satir.KalanMiktar);
    }

    [Theory]
    [InlineData(1, 2, GridSevkDurum.YenidenSevkGerekli)]
    [InlineData(3, 0, GridSevkDurum.SevkEdildi)]
    public async Task YenidenSevkPartisi_KismiSevkteKalanIhtiyaciVeDurumuKorur(
        decimal sevkMiktari,
        decimal beklenenKalan,
        GridSevkDurum beklenenDurum)
    {
        using var kurgu = new Kurgu(
            istenen: 5,
            tahsis: 5,
            gridGelen: 5,
            aktifSevk: 3,
            gelen: 2,
            konulan: 2,
            aktifPartideKarsilanan: 2);
        kurgu.Satir.GridSevkDurumuId = (int)GridSevkDurum.YenidenSevkGerekli;
        kurgu.Satir.YenidenSevkGerekliAdet = 3;
        kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi = true;

        var sonuc = await kurgu.TekliGridSevkEtAsync(sevkMiktari);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(sevkMiktari, kurgu.Satir.GridSevkMiktari);
        Assert.Equal(beklenenKalan, kurgu.Satir.YenidenSevkGerekliAdet);
        Assert.Equal((int)beklenenDurum, kurgu.Satir.GridSevkDurumuId);
        Assert.Equal(2, kurgu.Satir.GelenMiktar);
        Assert.Equal(2, kurgu.Icerik.KonulanAdet);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CokluSandikDevamSevki_ChildGorunumMiktarlariylaParentGridToplamlariniDaraltmaz(
        bool trafoAkisi)
    {
        using var kurgu = new Kurgu(
            istenen: 10,
            tahsis: 5,
            gridGelen: 6,
            aktifSevk: 6,
            gelen: 4,
            konulan: 2,
            aktifPartideKarsilanan: 4);
        var ikinci = kurgu.IkinciTahsisEkle(tahsis: 5, konulan: 2);
        kurgu.Icerik.AktifGridSevkKarsilananMiktari = 2;
        ikinci.AktifGridSevkKarsilananMiktari = 2;
        kurgu.Satir.GridDurumuId = trafoAkisi
            ? (int)GridDurum.TrafoSevk
            : (int)GridDurum.EksikGeldi;
        kurgu.Satir.TrafoSevkAdet = trafoAkisi ? 4 : 0;
        kurgu.Satir.GridSevkDurumuId = (int)GridSevkDurum.YenidenSevkGerekli;
        kurgu.Satir.YenidenSevkGerekliAdet = 2;
        kurgu.Satir.UcKDurumuId = (int)UcKDurum.EksikGeldi;
        kurgu.Satir.UcKKarsilamaTipiId = (int)UcKDurum.EksikGeldi;
        kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi = true;

        // Çoklu sandık query'sinin bir child için ürettiği 3/2 miktarlarını taklit eder.
        var sonuc = await kurgu.TekliGridDurumGuncelleAsync(
            trafoAkisi ? GridDurum.TrafoSevk : GridDurum.EksikGeldi,
            gridGelenAdet: 3,
            trafoSevkAdet: trafoAkisi ? 2 : null,
            sevkMiktari: 2);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(trafoAkisi ? (int)GridDurum.TrafoSevk : (int)GridDurum.EksikGeldi, kurgu.Satir.GridDurumuId);
        Assert.Equal(6, kurgu.Satir.GridGelenAdet);
        Assert.Equal(trafoAkisi ? 4 : 0, kurgu.Satir.TrafoSevkAdet);
        Assert.Equal(2, kurgu.Satir.GridSevkMiktari);
        Assert.Equal(0, kurgu.Satir.YenidenSevkGerekliAdet);
        Assert.Equal((int)GridSevkDurum.SevkEdildi, kurgu.Satir.GridSevkDurumuId);
        Assert.Equal(0, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, ikinci.AktifGridSevkKarsilananMiktari);
    }

    [Fact]
    public async Task IlkSevkAkisi_RequestGridMiktarlariniParentKaydaUygulamayaDevamEder()
    {
        using var kurgu = new Kurgu(
            istenen: 10,
            tahsis: 10,
            gridGelen: 0,
            aktifSevk: 0,
            gelen: 0,
            konulan: 0,
            aktifPartideKarsilanan: 0);
        kurgu.Satir.GridDurumuId = (int)GridDurum.Gelmedi;
        kurgu.Satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi;

        var sonuc = await kurgu.TekliGridDurumGuncelleAsync(
            GridDurum.EksikGeldi,
            gridGelenAdet: 6,
            trafoSevkAdet: null,
            sevkMiktari: 2);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal((int)GridDurum.EksikGeldi, kurgu.Satir.GridDurumuId);
        Assert.Equal(6, kurgu.Satir.GridGelenAdet);
        Assert.Equal(0, kurgu.Satir.TrafoSevkAdet);
        Assert.Equal(2, kurgu.Satir.GridSevkMiktari);
        Assert.Equal(0, kurgu.Satir.AktifGridSevkKarsilananMiktari);
    }

    [Fact]
    public async Task YenidenSevkPartisi_EksikKarsilanincaPartiEksigiMevcutIhtiyacaBirKezEklenir()
    {
        using var kurgu = new Kurgu(
            istenen: 4,
            tahsis: 4,
            gridGelen: 4,
            aktifSevk: 2,
            gelen: 2,
            konulan: 2);
        kurgu.Satir.GridSevkDurumuId = (int)GridSevkDurum.YenidenSevkGerekli;
        kurgu.Satir.YenidenSevkGerekliAdet = 2;

        var sevk = await kurgu.TekliGridSevkEtAsync(1);
        var eksik = await kurgu.TekliUcKDurumGuncelleAsync(UcKDurum.EksikGeldi, .5m);

        Assert.True(sevk.IsSuccess, sevk.Error?.Message);
        Assert.True(eksik.IsSuccess, eksik.Error?.Message);
        Assert.Equal(1.5m, kurgu.Satir.YenidenSevkGerekliAdet);
        Assert.Equal((int)GridSevkDurum.YenidenSevkGerekli, kurgu.Satir.GridSevkDurumuId);
        Assert.Equal(2.5m, kurgu.Satir.GelenMiktar);
        Assert.Equal(.5m, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(.5m, kurgu.Icerik.AktifGridSevkKarsilananMiktari);

        var tekrar = await kurgu.TekliUcKDurumGuncelleAsync(UcKDurum.EksikGeldi, .5m);

        Assert.False(tekrar.IsSuccess);
        Assert.Equal(1.5m, kurgu.Satir.YenidenSevkGerekliAdet);
        Assert.Equal(2.5m, kurgu.Satir.GelenMiktar);
        Assert.Equal(.5m, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(.5m, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
    }

    [Fact]
    public async Task YenidenSevkPartisi_GelmediysePartininTamamiMevcutIhtiyacaBirKezEklenir()
    {
        using var kurgu = new Kurgu(
            istenen: 4,
            tahsis: 4,
            gridGelen: 4,
            aktifSevk: 2,
            gelen: 2,
            konulan: 2);
        kurgu.Satir.GridSevkDurumuId = (int)GridSevkDurum.YenidenSevkGerekli;
        kurgu.Satir.YenidenSevkGerekliAdet = 2;

        var sevk = await kurgu.TekliGridSevkEtAsync(1);
        var gelmedi = await kurgu.TekliUcKDurumGuncelleAsync(UcKDurum.Gelmedi);

        Assert.True(sevk.IsSuccess, sevk.Error?.Message);
        Assert.True(gelmedi.IsSuccess, gelmedi.Error?.Message);
        Assert.Equal(2, kurgu.Satir.YenidenSevkGerekliAdet);
        Assert.Equal((int)GridSevkDurum.YenidenSevkGerekli, kurgu.Satir.GridSevkDurumuId);
        Assert.Equal(2, kurgu.Satir.GelenMiktar);
        Assert.Equal(0, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, kurgu.Icerik.AktifGridSevkKarsilananMiktari);

        var tekrar = await kurgu.TekliUcKDurumGuncelleAsync(UcKDurum.Gelmedi);

        Assert.False(tekrar.IsSuccess);
        Assert.Equal(2, kurgu.Satir.YenidenSevkGerekliAdet);
        Assert.Equal(2, kurgu.Satir.GelenMiktar);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EksikPartiSandikBazliSifirlaninca_YalnizPartiEksigiBorctanDusulurVeKalanBorcKorunur(
        bool toplu)
    {
        using var kurgu = new Kurgu(
            istenen: 4,
            tahsis: 2,
            gridGelen: 4,
            aktifSevk: 1,
            gelen: 2.5m,
            konulan: 2,
            aktifPartideKarsilanan: .5m);
        var ikinci = kurgu.IkinciTahsisEkle(tahsis: 2, konulan: .5m);
        kurgu.Icerik.AktifGridSevkKarsilananMiktari = 0;
        ikinci.AktifGridSevkKarsilananMiktari = .5m;
        kurgu.Satir.GridSevkDurumuId = (int)GridSevkDurum.YenidenSevkGerekli;
        kurgu.Satir.YenidenSevkGerekliAdet = 1.5m;
        kurgu.Satir.UcKDurumuId = (int)UcKDurum.EksikGeldi;
        kurgu.Satir.UcKKarsilamaTipiId = (int)UcKDurum.EksikGeldi;
        kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi = true;
        kurgu.Satir.TeslimTarihi = DateTime.UtcNow;

        var sonuc = toplu
            ? await kurgu.TopluUcKSifirlaAsync(ikinci.Id)
            : await kurgu.TekliUcKSifirlaAsync(ikinci.Id);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(1, kurgu.Satir.YenidenSevkGerekliAdet);
        Assert.Equal((int)GridSevkDurum.YenidenSevkGerekli, kurgu.Satir.GridSevkDurumuId);
        Assert.Equal((int)UcKDurum.Bekliyor, kurgu.Satir.UcKDurumuId);
        Assert.Equal((int)UcKDurum.Bekliyor, kurgu.Satir.UcKKarsilamaTipiId);
        Assert.Null(kurgu.Satir.TeslimTarihi);
        Assert.Equal(2, kurgu.Satir.GelenMiktar);
        Assert.Equal(0, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.False(kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.Equal(0, ikinci.AktifGridSevkKarsilananMiktari);
        Assert.Equal(2, kurgu.Icerik.KonulanAdet);
        Assert.Equal(0, ikinci.KonulanAdet);

        var tekrar = toplu
            ? await kurgu.TopluUcKSifirlaAsync(ikinci.Id)
            : await kurgu.TekliUcKSifirlaAsync(ikinci.Id);

        Assert.False(tekrar.IsSuccess);
        Assert.Equal(1, kurgu.Satir.YenidenSevkGerekliAdet);
        Assert.Equal((int)GridSevkDurum.YenidenSevkGerekli, kurgu.Satir.GridSevkDurumuId);
        Assert.Equal((int)UcKDurum.Bekliyor, kurgu.Satir.UcKDurumuId);
        Assert.Equal((int)UcKDurum.Bekliyor, kurgu.Satir.UcKKarsilamaTipiId);
    }

    [Fact]
    public void FazlaKarari_NormalKalanYerineMevcutBusinessKosullariniKullanir()
    {
        var satir = new CekiSatiri
        {
            IstenenAdet = 2,
            GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi,
            GridSevkMiktari = 2,
            AktifGridSevkKarsilananMiktari = 2,
            AktifGridSevkPartisiErkenSonuclandirildiMi = false,
            UcKDurumuId = (int)UcKDurum.TamGeldi,
            UcKKarsilamaTipiId = (int)UcKDurum.TamGeldi,
            GelenMiktar = 2
        };

        Assert.False(GridUcKSevkPartisiKurali.AktifPartiTeslimEdilebilirMi(satir));
        Assert.True(GridUcKSevkPartisiKurali.AktifPartiFazlaTeslimeAcikMi(satir));
    }

    [Fact]
    public async Task TamTeslimSonrasiGeriGonderim_OncekiPartidenGelenMiktariDaraltmadanMevcutAkisiKorur()
    {
        using var kurgu = new Kurgu(
            istenen: 3,
            tahsis: 3,
            gridGelen: 3,
            aktifSevk: 1,
            gelen: 3,
            konulan: 3,
            aktifPartideKarsilanan: 1);

        var sonuc = await kurgu.TekliUcKDurumGuncelleAsync(
            UcKDurum.GeriGonderildi,
            gelenAdet: 2);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(1, kurgu.Satir.GelenMiktar);
        Assert.Equal(2, kurgu.Satir.GeriGonderilenMiktar);
        Assert.Equal(2, kurgu.Satir.YenidenSevkGerekliAdet);
        Assert.Equal(0, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal((int)GridSevkDurum.YenidenSevkGerekli, kurgu.Satir.GridSevkDurumuId);
    }

    [Fact]
    public async Task GeriGonderim_KarisikKaynakliSandiktaGridPayiniAsamaz()
    {
        using var kurgu = new Kurgu(
            istenen: 3,
            tahsis: 3,
            gridGelen: 2,
            aktifSevk: 1,
            gelen: 1,
            konulan: 2,
            aktifPartideKarsilanan: 1);
        kurgu.Satir.StokKarsilanan = 1;
        kurgu.Icerik.StokKarsilanan = 1;

        var sonuc = await kurgu.TekliUcKDurumGuncelleAsync(
            UcKDurum.GeriGonderildi,
            gelenAdet: 2);

        Assert.False(sonuc.IsSuccess);
        Assert.Contains("Grid kaynaklı", sonuc.Error?.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, kurgu.Satir.GelenMiktar);
        Assert.Equal(0, kurgu.Satir.GeriGonderilenMiktar);
        Assert.Equal(0, kurgu.Satir.YenidenSevkGerekliAdet);
        Assert.Equal(1, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(1, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(2, kurgu.Icerik.KonulanAdet);
        Assert.Equal(0, kurgu.Uow.SaveCount);
    }

    [Fact]
    public async Task GeriGonderim_TeslimiSurmekteOlanAktifPartiyiBozamaz()
    {
        using var kurgu = new Kurgu(
            istenen: 3,
            tahsis: 3,
            gridGelen: 3,
            aktifSevk: 2,
            gelen: 1,
            konulan: 1,
            aktifPartideKarsilanan: 1);

        var sonuc = await kurgu.TekliUcKDurumGuncelleAsync(
            UcKDurum.GeriGonderildi,
            gelenAdet: .5m);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Equal(1, kurgu.Satir.GelenMiktar);
        Assert.Equal(1, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(1, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, kurgu.Satir.GeriGonderilenMiktar);
    }

    [Fact]
    public async Task EksikFinalizasyonuSonrasi_ParcaliGeriGonderimTekrarliCalisir()
    {
        using var kurgu = new Kurgu(
            istenen: 3,
            tahsis: 3,
            gridGelen: 3,
            aktifSevk: 2,
            gelen: 0,
            konulan: 0,
            aktifPartideKarsilanan: 0);

        var eksik = await kurgu.TekliUcKDurumGuncelleAsync(UcKDurum.EksikGeldi, .5m);
        var ilkGeri = await kurgu.TekliUcKDurumGuncelleAsync(UcKDurum.GeriGonderildi, .25m);
        var ikinciGeri = await kurgu.TekliUcKDurumGuncelleAsync(UcKDurum.GeriGonderildi, .25m);

        Assert.True(eksik.IsSuccess, eksik.Error?.Message);
        Assert.True(ilkGeri.IsSuccess, ilkGeri.Error?.Message);
        Assert.True(ikinciGeri.IsSuccess, ikinciGeri.Error?.Message);
        Assert.Equal(0, kurgu.Satir.GelenMiktar);
        Assert.Equal(.5m, kurgu.Satir.GeriGonderilenMiktar);
        Assert.Equal(0, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, kurgu.Icerik.KonulanAdet);
    }

    [Fact]
    public async Task SandikYonetimi_AktifGridUcKAkisindaKonulanAdediDogrudanDegistiremez()
    {
        using var kurgu = new Kurgu(
            istenen: 3,
            tahsis: 3,
            gridGelen: 3,
            aktifSevk: 1,
            gelen: 2,
            konulan: 2,
            aktifPartideKarsilanan: 0);

        var sonuc = await kurgu.NormalSandikIcerikGuncelleAsync(konulanAdet: 3);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains("Grid/3K akışı", sonuc.Error?.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, kurgu.Satir.GelenMiktar);
        Assert.Equal(0, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(2, kurgu.Icerik.KonulanAdet);
        Assert.Equal(0, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, kurgu.Uow.SaveCount);
    }

    [Fact]
    public async Task SandikYonetimi_GridUcKAkisiBaslamadanMevcutKonulanAdetDuzenlemesiniKorur()
    {
        using var kurgu = new Kurgu(
            istenen: 3,
            tahsis: 3,
            gridGelen: 0,
            aktifSevk: 0,
            gelen: 0,
            konulan: 0);
        kurgu.Satir.GridSevkMiktari = null;
        kurgu.Satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi;

        var sonuc = await kurgu.NormalSandikIcerikGuncelleAsync(konulanAdet: 1);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(1, kurgu.Icerik.KonulanAdet);
        Assert.Null(kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Null(kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(1, kurgu.Uow.SaveCount);
    }

    [Fact]
    public async Task SandikYonetimi_AktifGridUcKAkisindaYeniTahsisOlusturamaz()
    {
        using var kurgu = new Kurgu(
            istenen: 3,
            tahsis: 3,
            gridGelen: 3,
            aktifSevk: 1,
            gelen: 2,
            konulan: 2,
            aktifPartideKarsilanan: 0);
        var hedefSandik = kurgu.BosHedefSandikEkle();

        var sonuc = await kurgu.NormalSandikIcerikGuncelleAsync(sandikId: hedefSandik.Id);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains("yeni tahsis", sonuc.Error?.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(kurgu.Uow.Repo<SandikIcerik>().Rows);
        Assert.Equal(0, kurgu.Uow.SaveCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AktifPartiTeslimBeklerken_HenuzUcKIslemiYoksaSevkMiktariMutlakToplamOlarakGuncellenir(
        bool toplu)
    {
        using var kurgu = new Kurgu(
            istenen: 3,
            tahsis: 3,
            gridGelen: 3,
            aktifSevk: 2,
            gelen: 0,
            konulan: 0,
            aktifPartideKarsilanan: 0);

        var sonuc = toplu
            ? await kurgu.TopluGridSevkEtAsync()
            : await kurgu.TekliGridSevkEtAsync(3);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(3, kurgu.Satir.GridSevkMiktari);
        Assert.Equal(0, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.False(kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.Equal(0, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, kurgu.Satir.GelenMiktar);
        Assert.Equal(1, kurgu.Uow.SaveCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LegacyAktifPartiTeslimBeklerken_HenuzUcKIslemiYoksaYeniTakipleMutlakToplamaGuncellenir(
        bool toplu)
    {
        using var kurgu = new Kurgu(
            istenen: 3,
            tahsis: 3,
            gridGelen: 3,
            aktifSevk: 2,
            gelen: 0,
            konulan: 0,
            aktifPartideKarsilanan: null);

        var sonuc = toplu
            ? await kurgu.TopluGridSevkEtAsync()
            : await kurgu.TekliGridSevkEtAsync(3);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(3, kurgu.Satir.GridSevkMiktari);
        Assert.Equal(0, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.False(kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.Equal(0, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.False(GridUcKSevkPartisiKurali.LegacyAktifPartiBelirsizMi(kurgu.Satir));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SevkMiktariAzaltilipAyniDegerTekrarKaydedildigindeToplanmaz_MutlakDegerKorunur(
        bool toplu)
    {
        using var kurgu = new Kurgu(
            istenen: 3,
            tahsis: 3,
            gridGelen: 3,
            aktifSevk: 3,
            gelen: 0,
            konulan: 0,
            aktifPartideKarsilanan: 0);
        // Toplu akış aktif çeki miktarını sevk eder. Revizyonla miktarın 2'ye
        // düşmesini taklit ederek tekil ve topluda aynı mutlak değer sözleşmesini sınarız.
        kurgu.Satir.IstenenAdet = 2;

        var ilk = toplu
            ? await kurgu.TopluGridSevkEtAsync()
            : await kurgu.TekliGridSevkEtAsync(2);
        var tekrar = toplu
            ? await kurgu.TopluGridSevkEtAsync()
            : await kurgu.TekliGridSevkEtAsync(2);

        Assert.True(ilk.IsSuccess, ilk.Error?.Message);
        Assert.True(tekrar.IsSuccess, tekrar.Error?.Message);
        Assert.Equal(2, kurgu.Satir.IstenenAdet);
        Assert.Equal(2, kurgu.Satir.GridGelenAdet);
        Assert.Equal(2, kurgu.Satir.GridSevkMiktari);
        Assert.Equal(0, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.False(kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.Equal(0, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(2, kurgu.Uow.SaveCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GercekUcKTeslimiBasladiktanSonra_AktifSevkMiktariEzilemez(bool toplu)
    {
        using var kurgu = new Kurgu(
            istenen: 3,
            tahsis: 3,
            gridGelen: 3,
            aktifSevk: 2,
            gelen: 1,
            konulan: 1,
            aktifPartideKarsilanan: 1);

        var sonuc = toplu
            ? await kurgu.TopluGridSevkEtAsync()
            : await kurgu.TekliGridSevkEtAsync(3);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(2, kurgu.Satir.GridSevkMiktari);
        Assert.Equal(1, kurgu.Satir.GelenMiktar);
        Assert.Equal(1, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(1, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(1, kurgu.Icerik.KonulanAdet);
        Assert.Equal(0, kurgu.Uow.SaveCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DecimalSevkMiktari_UcKIslemiOncesiMutlakToplamOlarakGuncellenir(bool toplu)
    {
        using var kurgu = new Kurgu(
            istenen: 2.75m,
            tahsis: 2.75m,
            gridGelen: 2.75m,
            aktifSevk: 1.25m,
            gelen: 0,
            konulan: 0,
            aktifPartideKarsilanan: 0);

        var sonuc = toplu
            ? await kurgu.TopluGridSevkEtAsync()
            : await kurgu.TekliGridSevkEtAsync(2.75m);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(2.75m, kurgu.Satir.GridSevkMiktari);
        Assert.Equal(0, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.False(kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.Equal(0, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CokluSandiktaSevkMiktariEzildiktenSonra_UcKTeslimiYeniMutlakToplamiDagitir(
        bool toplu)
    {
        using var kurgu = new Kurgu(
            istenen: 3,
            tahsis: 1.5m,
            gridGelen: 3,
            aktifSevk: 2,
            gelen: 0,
            konulan: 0,
            aktifPartideKarsilanan: 0);
        var ikinciIcerik = kurgu.IkinciTahsisEkle(tahsis: 1.5m, konulan: 0);
        ikinciIcerik.AktifGridSevkKarsilananMiktari = 0;

        var overwrite = toplu
            ? await kurgu.TopluGridSevkEtAsync()
            : await kurgu.TekliGridSevkEtAsync(3);
        var teslim = await kurgu.EskiTekliUcKTeslimAlAsync(3);

        Assert.True(overwrite.IsSuccess, overwrite.Error?.Message);
        Assert.True(teslim.IsSuccess, teslim.Error?.Message);
        Assert.Equal(3, kurgu.Satir.GridSevkMiktari);
        Assert.Equal(3, kurgu.Satir.GelenMiktar);
        Assert.Equal(3, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(1.5m, kurgu.Icerik.KonulanAdet);
        Assert.Equal(1.5m, ikinciIcerik.KonulanAdet);
        Assert.Equal(1.5m, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(1.5m, ikinciIcerik.AktifGridSevkKarsilananMiktari);
    }

    [Fact]
    public async Task TopluIptal_UcKIslemiSonrasiKaynakMiktarlariniKorurVeAktifPartiTakibiniTemizler()
    {
        using var kurgu = new Kurgu(
            istenen: 6,
            tahsis: 6,
            gridGelen: 5,
            aktifSevk: 3,
            gelen: 2,
            konulan: 5,
            aktifPartideKarsilanan: 2);
        kurgu.Satir.KarsilananMiktar = 3;
        kurgu.Satir.StokKarsilanan = 1;
        kurgu.Satir.ProjeKarsilanan = 1;
        kurgu.Satir.TedarikciKarsilanan = 1;
        kurgu.Icerik.StokKarsilanan = 1;
        kurgu.Icerik.ProjeKarsilanan = 1;
        kurgu.Icerik.TedarikciKarsilanan = 1;

        var sonuc = await kurgu.TopluGridDurumGuncelleAsync(GridDurum.Iptal);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal((int)GridDurum.Iptal, kurgu.Satir.GridDurumuId);
        Assert.Equal(0, kurgu.Satir.GridGelenAdet);
        Assert.Equal((int)GridSevkDurum.SevkEdilmedi, kurgu.Satir.GridSevkDurumuId);
        Assert.Null(kurgu.Satir.GridSevkMiktari);
        Assert.Null(kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Null(kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.Null(kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(2, kurgu.Satir.GelenMiktar);
        Assert.Equal(3, kurgu.Satir.KarsilananMiktar);
        Assert.Equal(1, kurgu.Satir.StokKarsilanan);
        Assert.Equal(1, kurgu.Satir.ProjeKarsilanan);
        Assert.Equal(1, kurgu.Satir.TedarikciKarsilanan);
        Assert.Equal(5, kurgu.Icerik.KonulanAdet);
        Assert.Equal(1, kurgu.Icerik.StokKarsilanan);
        Assert.Equal(1, kurgu.Icerik.ProjeKarsilanan);
        Assert.Equal(1, kurgu.Icerik.TedarikciKarsilanan);
        Assert.Equal(0, kurgu.Satir.KalanMiktar);
    }

    [Fact]
    public async Task TopluGridKapandi_UcKIslemiSonrasiMiktarlariVeAktifPartiyiKorur_SandigiGrideAlir()
    {
        using var kurgu = new Kurgu(
            istenen: 6,
            tahsis: 6,
            gridGelen: 5,
            aktifSevk: 3,
            gelen: 2,
            konulan: 5,
            aktifPartideKarsilanan: 2);
        kurgu.Satir.KarsilananMiktar = 3;
        kurgu.Satir.StokKarsilanan = 1;
        kurgu.Satir.ProjeKarsilanan = 1;
        kurgu.Satir.TedarikciKarsilanan = 1;
        kurgu.Icerik.StokKarsilanan = 1;
        kurgu.Icerik.ProjeKarsilanan = 1;
        kurgu.Icerik.TedarikciKarsilanan = 1;

        var sonuc = await kurgu.TopluGridDurumGuncelleAsync(GridDurum.GridKapandi);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal((int)GridDurum.GridKapandi, kurgu.Satir.GridDurumuId);
        Assert.Equal(5, kurgu.Satir.GridGelenAdet);
        Assert.Equal((int)GridSevkDurum.SevkEdildi, kurgu.Satir.GridSevkDurumuId);
        Assert.Equal(3, kurgu.Satir.GridSevkMiktari);
        Assert.Equal(2, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.False(kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.Equal(2, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(2, kurgu.Satir.GelenMiktar);
        Assert.Equal(3, kurgu.Satir.KarsilananMiktar);
        Assert.Equal(1, kurgu.Satir.StokKarsilanan);
        Assert.Equal(1, kurgu.Satir.ProjeKarsilanan);
        Assert.Equal(1, kurgu.Satir.TedarikciKarsilanan);
        Assert.Equal(5, kurgu.Icerik.KonulanAdet);
        Assert.Equal((int)DepoLokasyon.Grid, kurgu.Icerik.Sandik.DepoLokasyonId);
        Assert.Equal(0, kurgu.Satir.KalanMiktar);
    }

    [Fact]
    public async Task TopluTamGeldi_GercekUcKIslemiVarkenTerminalMuafiyetiniKullanamaz()
    {
        using var kurgu = new Kurgu(
            istenen: 5,
            tahsis: 5,
            gridGelen: 3,
            aktifSevk: 2,
            gelen: 1,
            konulan: 1,
            aktifPartideKarsilanan: 1);
        kurgu.Satir.GridDurumuId = (int)GridDurum.EksikGeldi;

        var sonuc = await kurgu.TopluGridDurumGuncelleAsync(GridDurum.TamGeldi);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal((int)GridDurum.EksikGeldi, kurgu.Satir.GridDurumuId);
        Assert.Equal(3, kurgu.Satir.GridGelenAdet);
        Assert.Equal(2, kurgu.Satir.GridSevkMiktari);
        Assert.Equal(1, kurgu.Satir.GelenMiktar);
        Assert.Equal(1, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, kurgu.Uow.SaveCount);
    }

    [Fact]
    public void YenidenSevkKarari_BorcuMevcutDecimalKalanaGoreSinirlar()
    {
        var satir = new CekiSatiri
        {
            IstenenAdet = 5,
            GridDurumuId = (int)GridDurum.TamGeldi,
            GridGelenAdet = 5,
            GridSevkDurumuId = (int)GridSevkDurum.YenidenSevkGerekli,
            GridSevkMiktari = 2,
            AktifGridSevkKarsilananMiktari = 1,
            AktifGridSevkPartisiErkenSonuclandirildiMi = true,
            UcKDurumuId = (int)UcKDurum.EksikGeldi,
            UcKKarsilamaTipiId = (int)UcKDurum.EksikGeldi,
            GelenMiktar = 3.75m,
            YenidenSevkGerekliAdet = 4.5m
        };

        var karar = GridUcKSevkPartisiKurali.DevamSevkiniDegerlendir(satir);

        Assert.True(karar.YeniPartiMi);
        Assert.Equal(GridUcKDevamSevkTipi.YenidenSevk, karar.Tip);
        Assert.Equal(1.25m, satir.KalanMiktar);
        Assert.Equal(1.25m, karar.UstSinir);
    }

    [Fact]
    public void YenidenSevkBorcuBulunsaDaKalanSifirsa_DevamPartisiUretilmez()
    {
        var satir = new CekiSatiri
        {
            IstenenAdet = 5,
            GridDurumuId = (int)GridDurum.TamGeldi,
            GridGelenAdet = 5,
            GridSevkDurumuId = (int)GridSevkDurum.YenidenSevkGerekli,
            GridSevkMiktari = 2,
            AktifGridSevkKarsilananMiktari = 1,
            AktifGridSevkPartisiErkenSonuclandirildiMi = true,
            UcKDurumuId = (int)UcKDurum.TedarikcidenGeldi,
            UcKKarsilamaTipiId = (int)UcKDurum.TedarikcidenGeldi,
            GelenMiktar = 3,
            TedarikciKarsilanan = 2,
            KarsilananMiktar = 2,
            YenidenSevkGerekliAdet = 4
        };

        var karar = GridUcKSevkPartisiKurali.DevamSevkiniDegerlendir(satir);

        Assert.Equal(0, satir.KalanMiktar);
        Assert.False(karar.YeniPartiMi);
        Assert.Equal(GridUcKDevamSevkTipi.Yok, karar.Tip);
        Assert.Equal(0, karar.UstSinir);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GridKapandi_TekliVeTopluMevcutSevkMiktarlariniKorur(bool toplu)
    {
        using var kurgu = new Kurgu(
            istenen: 5,
            tahsis: 5,
            gridGelen: 5,
            aktifSevk: 2,
            gelen: 0,
            konulan: 0,
            aktifPartideKarsilanan: 0);

        var sonuc = await kurgu.GridKapandiAsync(toplu);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal((int)GridDurum.GridKapandi, kurgu.Satir.GridDurumuId);
        Assert.Equal(5, kurgu.Satir.GridGelenAdet);
        Assert.Equal(2, kurgu.Satir.GridSevkMiktari);
        Assert.Equal(0, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EskiTeslimEndpointleri_GridKapandiSatiriDegistiremez(bool toplu)
    {
        using var kurgu = new Kurgu(
            istenen: 3,
            tahsis: 3,
            gridGelen: 3,
            aktifSevk: 2,
            gelen: 0,
            konulan: 0,
            aktifPartideKarsilanan: 0);
        kurgu.Satir.GridDurumuId = (int)GridDurum.GridKapandi;

        var sonuc = toplu
            ? await kurgu.EskiTopluUcKTeslimAlAsync(1, kurgu.Icerik.Id)
            : await kurgu.EskiTekliUcKTeslimAlAsync(1);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(0, kurgu.Satir.GelenMiktar);
        Assert.Equal(0, kurgu.Icerik.KonulanAdet);
        Assert.Equal(0, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, kurgu.Uow.SaveCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TopluUcKTamGeldiYollari_KaliteTadilattaSatiriTeslimAlamaz(bool alternatifHandler)
    {
        using var kurgu = new Kurgu(
            istenen: 3,
            tahsis: 3,
            gridGelen: 3,
            aktifSevk: 2,
            gelen: 0,
            konulan: 0,
            aktifPartideKarsilanan: 0);
        kurgu.Satir.KaliteDurumId = LookupStub.TadilattaId;

        var sonuc = alternatifHandler
            ? await kurgu.AlternatifTopluUcKTamGeldiAsync()
            : await kurgu.TopluUcKTamGeldiAsync();

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(0, kurgu.Satir.GelenMiktar);
        Assert.Equal(0, kurgu.Icerik.KonulanAdet);
        Assert.Equal(0, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
    }

    [Fact]
    public async Task LegacyPartiSayaciBosken_MevcutIkiAdetTekrarKarsilanmaz()
    {
        using var kurgu = new Kurgu();
        Assert.Null(kurgu.Satir.AktifGridSevkKarsilananMiktari);

        var sevk = await kurgu.TekliGridSevkEtAsync(1);
        var karsilama = await kurgu.TekliUcKTamGeldiAsync();

        Assert.True(sevk.IsSuccess, sevk.Error?.Message);
        Assert.True(karsilama.IsSuccess, karsilama.Error?.Message);
        Assert.Equal(3, kurgu.Satir.GelenMiktar);
        Assert.Equal(3, kurgu.Icerik.KonulanAdet);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void LegacyPartiSayaciBosken_TamDurumdaGelenAktifSevkeEsitDegilseOtomatikDevamAcmaz(
        decimal kumulatifGelen)
    {
        var satir = new CekiSatiri
        {
            IstenenAdet = 4,
            GridDurumuId = (int)GridDurum.TamGeldi,
            GridGelenAdet = 4,
            GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi,
            GridSevkMiktari = 2,
            AktifGridSevkKarsilananMiktari = null,
            UcKDurumuId = (int)UcKDurum.TamGeldi,
            UcKKarsilamaTipiId = (int)UcKDurum.TamGeldi,
            GelenMiktar = kumulatifGelen
        };

        var karar = GridUcKSevkPartisiKurali.DevamSevkiniDegerlendir(satir);

        Assert.True(GridUcKSevkPartisiKurali.LegacyAktifPartiBelirsizMi(satir));
        Assert.False(GridUcKSevkPartisiKurali.AktifPartiTeslimEdilebilirMi(satir));
        Assert.False(karar.YeniPartiMi);
        Assert.Equal(GridUcKDevamSevkTipi.Yok, karar.Tip);
        Assert.Equal(0, karar.UstSinir);
    }

    [Fact]
    public void LegacyPartiSayaciBosken_EksikDurumMiktarlarEsitOlsaDaBelirsizKalir()
    {
        var satir = new CekiSatiri
        {
            IstenenAdet = 3,
            GridDurumuId = (int)GridDurum.TamGeldi,
            GridGelenAdet = 3,
            GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi,
            GridSevkMiktari = 2,
            AktifGridSevkKarsilananMiktari = null,
            UcKDurumuId = (int)UcKDurum.EksikGeldi,
            UcKKarsilamaTipiId = (int)UcKDurum.EksikGeldi,
            GelenMiktar = 2
        };

        Assert.True(GridUcKSevkPartisiKurali.LegacyAktifPartiBelirsizMi(satir));
        Assert.False(GridUcKSevkPartisiKurali.DevamSevkiniDegerlendir(satir).YeniPartiMi);
        Assert.False(GridUcKSevkPartisiKurali.AktifPartiTeslimEdilebilirMi(satir));
    }

    [Fact]
    public async Task EskiTekliTeslimEndpointi_AktifPartiUstSiniriniAsamazVeTekrarCiftSaymaz()
    {
        using var kurgu = new Kurgu();
        var sevk = await kurgu.TekliGridSevkEtAsync(1);

        var fazla = await kurgu.EskiTekliUcKTeslimAlAsync(2);

        Assert.True(sevk.IsSuccess, sevk.Error?.Message);
        Assert.False(fazla.IsSuccess);
        Assert.Equal(2, kurgu.Satir.GelenMiktar);
        Assert.Equal(2, kurgu.Icerik.KonulanAdet);
        Assert.Equal(0, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, kurgu.Icerik.AktifGridSevkKarsilananMiktari);

        var teslim = await kurgu.EskiTekliUcKTeslimAlAsync(1);
        var tekrar = await kurgu.EskiTekliUcKTeslimAlAsync(1);

        Assert.True(teslim.IsSuccess, teslim.Error?.Message);
        Assert.False(tekrar.IsSuccess);
        kurgu.TamamenKarsilandiMi();
    }

    [Fact]
    public async Task EskiTopluTeslimEndpointi_FazlaIstegiAktifPartiyeSinirlarVeSeciliChildSayaciniKorur()
    {
        using var kurgu = new Kurgu(
            istenen: 3,
            tahsis: 1,
            gridGelen: 3,
            aktifSevk: 2,
            gelen: 2,
            konulan: 1);
        var ikinci = kurgu.IkinciTahsisEkle(tahsis: 2, konulan: 1);
        var sevk = await kurgu.TekliGridSevkEtAsync(1);

        var teslim = await kurgu.EskiTopluUcKTeslimAlAsync(2, ikinci.Id);
        var tekrar = await kurgu.EskiTopluUcKTeslimAlAsync(2, ikinci.Id);

        Assert.True(sevk.IsSuccess, sevk.Error?.Message);
        Assert.True(teslim.IsSuccess, teslim.Error?.Message);
        Assert.True(tekrar.IsSuccess, tekrar.Error?.Message);
        Assert.Equal(3, kurgu.Satir.GelenMiktar);
        Assert.Equal(1, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(1, ikinci.AktifGridSevkKarsilananMiktari);
        Assert.Equal(1, kurgu.Icerik.KonulanAdet);
        Assert.Equal(2, ikinci.KonulanAdet);
    }

    [Fact]
    public async Task PA702TahsisUyumsuzlugu_KapasiteyiAsanUcKKarsilamasiniEngeller()
    {
        using var kurgu = new Kurgu(
            istenen: 4,
            tahsis: 2,
            gridGelen: 4,
            aktifSevk: 4,
            gelen: 0,
            konulan: 0,
            aktifPartideKarsilanan: 0);

        // Eski istemci/satır seçimi SandikIcerikId göndermediğinde de kapasite
        // kontrolü sessizce aşılmamalıdır.
        var sonuc = await kurgu.TekliUcKTamGeldiAsync(sandikIcerikId: null);

        Assert.False(sonuc.IsSuccess);
        Assert.Contains("sandık tahsis", sonuc.Error?.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, kurgu.Icerik.TahsisMiktari);
        Assert.Equal(0, kurgu.Icerik.KonulanAdet);
        Assert.Equal(0, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, kurgu.Uow.SaveCount);
    }

    [Fact]
    public async Task CokluTahsis_IkinciPartiSayaclariniSandikBazindaTutarVeTekrarIstegiKaydirmaz()
    {
        using var kurgu = new Kurgu(
            istenen: 3,
            tahsis: 1,
            gridGelen: 3,
            aktifSevk: 2,
            gelen: 2,
            konulan: 1);
        var ikinci = kurgu.IkinciTahsisEkle(tahsis: 2, konulan: 1);

        var sevk = await kurgu.TekliGridSevkEtAsync(1);

        Assert.True(sevk.IsSuccess, sevk.Error?.Message);
        Assert.Equal(0, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, ikinci.AktifGridSevkKarsilananMiktari);
        Assert.Contains(kurgu.Uow.Repo<SandikIcerik>().Updated, i => i.Id == kurgu.Icerik.Id);
        Assert.Contains(kurgu.Uow.Repo<SandikIcerik>().Updated, i => i.Id == ikinci.Id);

        var karsilama = await kurgu.TekliUcKTamGeldiAsync(ikinci.Id);

        Assert.True(karsilama.IsSuccess, karsilama.Error?.Message);
        Assert.Equal(1, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(1, ikinci.AktifGridSevkKarsilananMiktari);
        Assert.Equal(1, kurgu.Icerik.TahsisMiktari);
        Assert.Equal(1, kurgu.Icerik.KonulanAdet);
        Assert.Equal(2, ikinci.TahsisMiktari);
        Assert.Equal(2, ikinci.KonulanAdet);

        var tekrar = await kurgu.TekliUcKTamGeldiAsync(kurgu.Icerik.Id);

        Assert.True(tekrar.IsSuccess, tekrar.Error?.Message);
        Assert.Equal(3, kurgu.Satir.GelenMiktar);
        Assert.Equal(1, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(1, ikinci.AktifGridSevkKarsilananMiktari);
        Assert.Equal(1, kurgu.Icerik.KonulanAdet);
        Assert.Equal(2, ikinci.KonulanAdet);
    }

    [Fact]
    public async Task CokluTahsis_SeciliSandikParentAktifPartisininTamaminiTekBasinaTuketemez()
    {
        using var kurgu = new Kurgu(
            istenen: 4,
            tahsis: 2,
            gridGelen: 4,
            aktifSevk: 2,
            gelen: 0,
            konulan: 0,
            aktifPartideKarsilanan: 0);
        var ikinci = kurgu.IkinciTahsisEkle(tahsis: 2, konulan: 0);
        ikinci.AktifGridSevkKarsilananMiktari = 0;

        var ilkTeslim = await kurgu.TekliUcKTamGeldiAsync(kurgu.Icerik.Id);

        Assert.True(ilkTeslim.IsSuccess, ilkTeslim.Error?.Message);
        Assert.Equal(1, kurgu.Satir.GelenMiktar);
        Assert.Equal(1, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(1, kurgu.Icerik.KonulanAdet);
        Assert.Equal(1, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, ikinci.KonulanAdet);
        Assert.Equal(0, ikinci.AktifGridSevkKarsilananMiktari);

        var ayniSandikTekrar = await kurgu.TekliUcKTamGeldiAsync(kurgu.Icerik.Id);
        var ikinciTeslim = await kurgu.TekliUcKTamGeldiAsync(ikinci.Id);

        Assert.True(ayniSandikTekrar.IsSuccess, ayniSandikTekrar.Error?.Message);
        Assert.True(ikinciTeslim.IsSuccess, ikinciTeslim.Error?.Message);
        Assert.Equal(2, kurgu.Satir.GelenMiktar);
        Assert.Equal(2, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(1, kurgu.Icerik.KonulanAdet);
        Assert.Equal(1, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(1, ikinci.KonulanAdet);
        Assert.Equal(1, ikinci.AktifGridSevkKarsilananMiktari);
    }

    [Fact]
    public async Task SandiklarArasiKismiTasima_AktifPartiVeKaynakKiriliminiAtomikTasir()
    {
        using var kurgu = new Kurgu(
            istenen: 3,
            tahsis: 2,
            gridGelen: 3,
            aktifSevk: 1,
            gelen: 1,
            konulan: 2,
            aktifPartideKarsilanan: 1);
        kurgu.Satir.KarsilananMiktar = 1;
        kurgu.Satir.StokKarsilanan = 1;
        kurgu.Icerik.StokKarsilanan = 1;
        var hedef = kurgu.IkinciTahsisEkle(tahsis: 1, konulan: 0);
        hedef.AktifGridSevkKarsilananMiktari = 0;

        var sonuc = await kurgu.SandikUrunTasiAsync(hedef.SandikId, 1);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(1, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(.5m, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(.5m, hedef.AktifGridSevkKarsilananMiktari);
        Assert.Equal(.5m, kurgu.Icerik.StokKarsilanan);
        Assert.Equal(.5m, hedef.StokKarsilanan);
        Assert.Equal(1, kurgu.Icerik.TahsisMiktari);
        Assert.Equal(2, hedef.TahsisMiktari);
        Assert.Equal(1, kurgu.Icerik.KonulanAdet);
        Assert.Equal(1, hedef.KonulanAdet);
    }

    [Fact]
    public async Task FiiliSandikDegisikligi_AktifPartiVeKaynakKirilimlariniKorur()
    {
        using var kurgu = new Kurgu(
            istenen: 3,
            tahsis: 3,
            gridGelen: 3,
            aktifSevk: 1,
            gelen: 1,
            konulan: 2,
            aktifPartideKarsilanan: 1);
        kurgu.Icerik.StokKarsilanan = .5m;
        kurgu.Icerik.ProjeKarsilanan = .25m;
        kurgu.Icerik.TedarikciKarsilanan = .25m;
        kurgu.Icerik.Miktar = 3;
        kurgu.Icerik.KaynakProjeNo = "PA100-01";
        var hedef = kurgu.BosHedefSandikEkle();

        var sonuc = await kurgu.FiiliSandikDegistirAsync(hedef.SandikNo);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        var tasinan = Assert.Single(kurgu.Uow.Repo<SandikIcerik>().Rows);
        Assert.Equal(hedef.Id, tasinan.SandikId);
        Assert.Equal(3, tasinan.TahsisMiktari);
        Assert.Equal(2, tasinan.KonulanAdet);
        Assert.Equal(1, tasinan.EksikAdet);
        Assert.Equal(1, tasinan.AktifGridSevkKarsilananMiktari);
        Assert.Equal(.5m, tasinan.StokKarsilanan);
        Assert.Equal(.25m, tasinan.ProjeKarsilanan);
        Assert.Equal(.25m, tasinan.TedarikciKarsilanan);
        Assert.Equal(3, tasinan.Miktar);
        Assert.Equal("PA100-01", tasinan.KaynakProjeNo);
        Assert.Equal(hedef.SandikNo, kurgu.Satir.FiiliSandikNo);
    }

    [Fact]
    public async Task CokluTahsis_SecimsizTeslimAktifPartiyiChildPaylariniAsmayanSekildeDagitir()
    {
        using var kurgu = new Kurgu(
            istenen: 4,
            tahsis: 2,
            gridGelen: 4,
            aktifSevk: 2,
            gelen: 0,
            konulan: 0,
            aktifPartideKarsilanan: 0);
        var ikinci = kurgu.IkinciTahsisEkle(tahsis: 2, konulan: 0);
        ikinci.AktifGridSevkKarsilananMiktari = 0;

        var teslim = await kurgu.EskiTekliUcKTeslimAlAsync(2);

        Assert.True(teslim.IsSuccess, teslim.Error?.Message);
        Assert.Equal(2, kurgu.Satir.GelenMiktar);
        Assert.Equal(2, kurgu.Satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(1, kurgu.Icerik.KonulanAdet);
        Assert.Equal(1, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(1, ikinci.KonulanAdet);
        Assert.Equal(1, ikinci.AktifGridSevkKarsilananMiktari);
    }

    [Fact]
    public void MiktarAlanlari_VeritabaniHassasiyetindenFazlaOndalikBasamagiReddeder()
    {
        var gridGecersiz = new GridDurumGuncelleCommand
        {
            CekiSatiriId = 1,
            ProjeId = 1,
            YeniDurumId = (int)GridDurum.TamGeldi,
            GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi,
            SevkMiktari = 0.00001m
        };
        var uckGecersiz = new UcKDurumGuncelleCommand
        {
            CekiSatiriId = 1,
            ProjeId = 1,
            KarsilamaTipiId = (int)UcKDurum.EksikGeldi,
            GelenAdet = 0.00001m
        };
        var eskiTekliGecersiz = new UcKTeslimAlCommand
        {
            CekiSatiriId = 1,
            ProjeId = 1,
            GelenMiktar = 0.00001m
        };
        var eskiTopluGecersiz = new UcKTopluTeslimAlCommand
        {
            ProjeId = 1,
            Urunler =
            [
                new TopluTeslimItem
                {
                    CekiSatiriId = 1,
                    GelenMiktar = 0.00001m
                }
            ]
        };

        Assert.False(new GridDurumGuncelleCommandValidator().Validate(gridGecersiz).IsValid);
        Assert.False(new UcKDurumGuncelleCommandValidator().Validate(uckGecersiz).IsValid);
        Assert.False(new UcKTeslimAlCommandValidator().Validate(eskiTekliGecersiz).IsValid);
        Assert.False(new UcKTopluTeslimAlCommandValidator().Validate(eskiTopluGecersiz).IsValid);

        gridGecersiz.SevkMiktari = 0.0001m;
        uckGecersiz.GelenAdet = 0.0001m;
        eskiTekliGecersiz.GelenMiktar = 0.0001m;
        eskiTopluGecersiz.Urunler[0].GelenMiktar = 0.0001m;

        Assert.True(new GridDurumGuncelleCommandValidator().Validate(gridGecersiz).IsValid);
        Assert.True(new UcKDurumGuncelleCommandValidator().Validate(uckGecersiz).IsValid);
        Assert.True(new UcKTeslimAlCommandValidator().Validate(eskiTekliGecersiz).IsValid);
        Assert.True(new UcKTopluTeslimAlCommandValidator().Validate(eskiTopluGecersiz).IsValid);
    }

    private sealed class Kurgu : IDisposable
    {
        public const int ProjeId = 69902;
        public const int SatirId = 23;
        public const int IcerikId = 2300;

        private readonly DurumHesaplaService _durum = new();
        private readonly CurrentUserStub _user = new();
        private readonly HareketStub _hareket = new();
        private readonly LookupStub _lookup = new();
        private readonly SahaStub _saha = new();
        private readonly StokStub _stok = new();

        public TestUnitOfWork Uow { get; } = new();
        public CekiSatiri Satir { get; }
        public SandikIcerik Icerik { get; }

        public Kurgu(
            decimal istenen = 3,
            decimal tahsis = 3,
            decimal gridGelen = 3,
            decimal aktifSevk = 2,
            decimal gelen = 2,
            decimal konulan = 2,
            decimal? aktifPartideKarsilanan = null)
        {
            var proje = new Proje
            {
                Id = ProjeId,
                ProjeNo = "PA699-02",
                ProjeTipiId = (int)ProjeTipi.Normal
            };
            var ceki = new Ceki { Id = 699, ProjeId = proje.Id, Proje = proje };
            Satir = new CekiSatiri
            {
                Id = SatirId,
                CekiId = ceki.Id,
                Ceki = ceki,
                SiraNo = 23,
                BarkodNo = "FCT01181927",
                Aciklama = "PA699 parçalı sevk ürünü",
                IstenenAdet = istenen,
                BirimId = (int)Birim.Adet,
                CekideGecenSandikNo = "11",
                FiiliSandikNo = "11",
                GridDurumuId = (int)GridDurum.TamGeldi,
                GridGelenAdet = gridGelen,
                GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi,
                GridSevkMiktari = aktifSevk,
                AktifGridSevkKarsilananMiktari = aktifPartideKarsilanan,
                AktifGridSevkPartisiErkenSonuclandirildiMi = aktifPartideKarsilanan.HasValue ? false : null,
                UcKDurumuId = gelen > 0 ? (int)UcKDurum.TamGeldi : (int)UcKDurum.Bekliyor,
                UcKKarsilamaTipiId = gelen > 0 ? (int)UcKDurum.TamGeldi : (int)UcKDurum.Bekliyor,
                GelenMiktar = gelen,
                DurumId = gelen > 0 ? (int)UrunDurum.KismiTamamlandi : (int)UrunDurum.GriddeHazir
            };
            var sandik = new Sandik
            {
                Id = 1100,
                ProjeId = proje.Id,
                Proje = proje,
                SandikNo = "11",
                DurumId = (int)SandikDurum.Hazirlaniyor,
                DepoLokasyonId = (int)DepoLokasyon.UcK
            };
            Icerik = new SandikIcerik
            {
                Id = IcerikId,
                SandikId = sandik.Id,
                Sandik = sandik,
                CekiSatiriId = Satir.Id,
                CekiSatiri = Satir,
                TahsisMiktari = tahsis,
                KonulanAdet = konulan,
                EksikAdet = Math.Max(tahsis - konulan, 0),
                AktifGridSevkKarsilananMiktari = aktifPartideKarsilanan,
                BirimId = (int)Birim.Adet
            };

            proje.Cekiler.Add(ceki);
            proje.Sandiklar.Add(sandik);
            ceki.CekiSatirlari.Add(Satir);
            Satir.SandikIcerikleri.Add(Icerik);
            sandik.SandikIcerikleri.Add(Icerik);

            Uow.Repo<Proje>().Rows.Add(proje);
            Uow.Repo<Ceki>().Rows.Add(ceki);
            Uow.Repo<CekiSatiri>().Rows.Add(Satir);
            Uow.Repo<Sandik>().Rows.Add(sandik);
            Uow.Repo<SandikIcerik>().Rows.Add(Icerik);
        }

        public SandikIcerik IkinciTahsisEkle(decimal tahsis, decimal konulan)
        {
            var sandik = new Sandik
            {
                Id = 1200,
                ProjeId = ProjeId,
                SandikNo = "12",
                DurumId = (int)SandikDurum.Hazirlaniyor,
                DepoLokasyonId = (int)DepoLokasyon.UcK
            };
            var icerik = new SandikIcerik
            {
                Id = 2400,
                SandikId = sandik.Id,
                Sandik = sandik,
                CekiSatiriId = Satir.Id,
                CekiSatiri = Satir,
                TahsisMiktari = tahsis,
                KonulanAdet = konulan,
                EksikAdet = Math.Max(tahsis - konulan, 0),
                BirimId = (int)Birim.Adet
            };
            Satir.SandikIcerikleri.Add(icerik);
            sandik.SandikIcerikleri.Add(icerik);
            Uow.Repo<Sandik>().Rows.Add(sandik);
            Uow.Repo<SandikIcerik>().Rows.Add(icerik);
            return icerik;
        }

        public Sandik BosHedefSandikEkle()
        {
            var sandik = new Sandik
            {
                Id = 1300,
                ProjeId = ProjeId,
                SandikNo = "13",
                DurumId = (int)SandikDurum.Hazirlaniyor,
                DepoLokasyonId = (int)DepoLokasyon.UcK
            };
            Uow.Repo<Sandik>().Rows.Add(sandik);
            return sandik;
        }

        public StokKaydi StokEkle(decimal miktar)
        {
            var stok = new StokKaydi
            {
                Id = 3300,
                MalzemeKodu = Satir.BarkodNo,
                MalzemeAdi = Satir.Aciklama,
                Miktar = miktar,
                BirimId = Satir.BirimId,
                DurumId = (int)StokDurum.Aktif
            };
            Uow.Repo<StokKaydi>().Rows.Add(stok);
            return stok;
        }

        public Task<Result<CekiSatiriAnaVeriGuncelleDto>> CekiMiktariniDuzenleAsync(decimal miktar) =>
            new CekiSatiriAnaVeriGuncelleCommandHandler(Uow, _durum, _saha)
                .Handle(new CekiSatiriAnaVeriGuncelleCommand
                {
                    CekiSatiriId = Satir.Id, SiraNo = Satir.SiraNo,
                    BarkodNo = Satir.BarkodNo, Aciklama = Satir.Aciklama,
                    IstenenAdet = miktar, BirimId = Satir.BirimId,
                    SandikNo = Satir.CekideGecenSandikNo!
                }, default);

        public Task<Result> TekliGridSevkEtAsync(decimal sevkMiktari) =>
            new GridDurumGuncelleCommandHandler(Uow, _user, _durum, _hareket, _lookup, _saha)
                .Handle(new GridDurumGuncelleCommand
                {
                    ProjeId = ProjeId,
                    CekiSatiriId = Satir.Id,
                    YeniDurumId = (int)GridDurum.TamGeldi,
                    GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi,
                    SevkMiktari = sevkMiktari,
                    Aciklama = "Kalan parti"
                }, default);

        public Task<Result> TekliGridDurumGuncelleAsync(
            GridDurum yeniDurum,
            decimal? gridGelenAdet,
            decimal? trafoSevkAdet,
            decimal sevkMiktari) =>
            new GridDurumGuncelleCommandHandler(Uow, _user, _durum, _hareket, _lookup, _saha)
                .Handle(new GridDurumGuncelleCommand
                {
                    ProjeId = ProjeId,
                    CekiSatiriId = Satir.Id,
                    YeniDurumId = (int)yeniDurum,
                    GridGelenAdet = gridGelenAdet,
                    TrafoSevkAdet = trafoSevkAdet,
                    GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi,
                    SevkMiktari = sevkMiktari,
                    Aciklama = "Sandık bazlı Grid paneli regresyonu"
                }, default);

        public Task<Result> TopluGridSevkEtAsync() =>
            new GridTopluSevkCommandHandler(Uow, _user, _durum, _hareket, _lookup, _saha)
                .Handle(new GridTopluSevkCommand
                {
                    ProjeId = ProjeId,
                    CekiSatiriIdler = [Satir.Id],
                    Aciklama = "Kalan parti"
                }, default);

        public Task<Result> TekliUcKTamGeldiAsync(int? sandikIcerikId = IcerikId) =>
            new UcKDurumGuncelleCommandHandler(Uow, _user, _durum, _hareket, _lookup, _saha)
                .Handle(new UcKDurumGuncelleCommand
                {
                    ProjeId = ProjeId,
                    CekiSatiriId = Satir.Id,
                    SandikIcerikId = sandikIcerikId,
                    KarsilamaTipiId = (int)UcKDurum.TamGeldi,
                    Aciklama = "Parti teslimi"
                }, default);

        public Task<Result> TekliUcKDurumGuncelleAsync(
            UcKDurum durum,
            decimal? gelenAdet = null,
            int? sandikIcerikId = IcerikId,
            int? stokKaydiId = null) =>
            new UcKDurumGuncelleCommandHandler(Uow, _user, _durum, _hareket, _lookup, _saha)
                .Handle(new UcKDurumGuncelleCommand
                {
                    ProjeId = ProjeId,
                    CekiSatiriId = Satir.Id,
                    SandikIcerikId = sandikIcerikId,
                    KarsilamaTipiId = (int)durum,
                    GelenAdet = gelenAdet,
                    StokKaydiId = stokKaydiId,
                    GeriGonderilmeSebebiId = durum == UcKDurum.GeriGonderildi ? 1 : null,
                    Aciklama = "Parti geri bildirimi"
                }, default);

        public Task<Result> NormalSandikIcerikGuncelleAsync(
            int? konulanAdet = null,
            int? ucKDurumuId = null,
            int? sandikId = null) =>
            new UrunGuncelleCommandHandler(Uow, _hareket, _saha)
                .Handle(new UrunGuncelleCommand
                {
                    ProjeId = ProjeId,
                    CekiSatiriId = Satir.Id,
                    SandikIcerikId = Icerik.Id,
                    SandikId = sandikId ?? Icerik.SandikId,
                    KonulanAdet = konulanAdet,
                    UcKDurumuId = ucKDurumuId,
                    KullaniciId = _user.UserId ?? 0
                }, default);

        public Task<Result> GridKapandiAsync(bool toplu) => toplu
            ? new GridTopluDurumGuncelleCommandHandler(Uow, _user, _durum, _hareket, _saha)
                .Handle(new GridTopluDurumGuncelleCommand
                {
                    ProjeId = ProjeId,
                    CekiSatiriIdler = [Satir.Id],
                    HedefDurumId = (int)GridDurum.GridKapandi,
                    Aciklama = "Grid kapandı parity regresyonu"
                }, default)
            : new GridDurumGuncelleCommandHandler(Uow, _user, _durum, _hareket, _lookup, _saha)
                .Handle(new GridDurumGuncelleCommand
                {
                    ProjeId = ProjeId,
                    CekiSatiriId = Satir.Id,
                    YeniDurumId = (int)GridDurum.GridKapandi,
                    Aciklama = "Grid kapandı parity regresyonu"
                }, default);

        public Task<Result> TopluGridDurumGuncelleAsync(GridDurum hedefDurum) =>
            new GridTopluDurumGuncelleCommandHandler(Uow, _user, _durum, _hareket, _saha)
                .Handle(new GridTopluDurumGuncelleCommand
                {
                    ProjeId = ProjeId,
                    CekiSatiriIdler = [Satir.Id],
                    HedefDurumId = (int)hedefDurum,
                    Aciklama = "Toplu terminal durum regresyonu"
                }, default);

        public Task<Result> SandikUrunTasiAsync(int hedefSandikId, decimal miktar) =>
            new SandikUrunTasiCommandHandler(Uow, _user, _saha)
                .Handle(new SandikUrunTasiCommand
                {
                    KaynakSandikIcerikId = Icerik.Id,
                    HedefSandikId = hedefSandikId,
                    TasinanAdet = miktar,
                    ProjeId = ProjeId,
                    IslemAnahtari = Guid.NewGuid()
                }, default);

        public Task<Result> FiiliSandikDegistirAsync(string hedefSandikNo) =>
            new FiiliSandikDegistirCommandHandler(Uow, _hareket, _saha)
                .Handle(new FiiliSandikDegistirCommand
                {
                    CekiSatiriId = Satir.Id,
                    YeniFiiliSandikNo = hedefSandikNo,
                    ProjeId = ProjeId,
                    KullaniciId = _user.UserId ?? 0
                }, default);

        public Task<Result> EskiTekliUcKTeslimAlAsync(int gelenMiktar) =>
            new UcKTeslimAlCommandHandler(Uow, _user, _durum, _hareket, _saha, _lookup)
                .Handle(new UcKTeslimAlCommand
                {
                    ProjeId = ProjeId,
                    CekiSatiriId = Satir.Id,
                    GelenMiktar = gelenMiktar,
                    Aciklama = "Eski tekli endpoint regresyonu"
                }, default);

        public Task<Result> EskiTopluUcKTeslimAlAsync(int gelenMiktar, int? sandikIcerikId) =>
            new UcKTopluTeslimAlCommandHandler(Uow, _user, _durum, _hareket, _saha, _lookup)
                .Handle(new UcKTopluTeslimAlCommand
                {
                    ProjeId = ProjeId,
                    Urunler =
                    [
                        new TopluTeslimItem
                        {
                            CekiSatiriId = Satir.Id,
                            SandikIcerikId = sandikIcerikId,
                            GelenMiktar = gelenMiktar
                        }
                    ],
                    Aciklama = "Eski toplu endpoint regresyonu"
                }, default);

        public Task<Result> TopluUcKTamGeldiAsync() =>
            new UcKTopluTamGeldiCommandHandler(Uow, _user, _durum, _hareket, _saha, _lookup)
                .Handle(new UcKTopluTamGeldiCommand
                {
                    ProjeId = ProjeId,
                    Secimler =
                    [
                        new UcKSandikSecimDto
                        {
                            CekiSatiriId = Satir.Id,
                            SandikIcerikId = Icerik.Id
                        }
                    ],
                    Aciklama = "Parti teslimi"
                }, default);

        public Task<Result> TopluTedarikcidenKarsilaAsync(int? sandikIcerikId) =>
            new UcKTopluTedarikciCommandHandler(Uow, _user, _durum, _hareket, _saha)
                .Handle(new UcKTopluTedarikciCommand
                {
                    ProjeId = ProjeId,
                    CekiSatiriIdler = [Satir.Id],
                    Secimler =
                    [
                        new UcKSandikSecimDto
                        {
                            CekiSatiriId = Satir.Id,
                            SandikIcerikId = sandikIcerikId
                        }
                    ],
                    Aciklama = "Legacy Gelmedi toplu tedarikçi regresyonu"
                }, default);

        public Task<Result> AlternatifTopluUcKTamGeldiAsync() =>
            new TopluDurumGuncelleCommandHandler(Uow, _user, _durum, _hareket, _saha, _lookup)
                .Handle(new TopluDurumGuncelleCommand
                {
                    ProjeId = ProjeId,
                    CekiSatiriIdler = [Satir.Id],
                    Secimler =
                    [
                        new UcKSandikSecimDto
                        {
                            CekiSatiriId = Satir.Id,
                            SandikIcerikId = Icerik.Id
                        }
                    ],
                    Aciklama = "Alternatif toplu parti teslimi"
                }, default);

        public Task<Result> TekliUcKSifirlaAsync(int? sandikIcerikId) =>
            new UcKDurumSifirlaCommandHandler(Uow, _user, _durum, _hareket, _saha)
                .Handle(new UcKDurumSifirlaCommand
                {
                    ProjeId = ProjeId,
                    CekiSatiriId = Satir.Id,
                    SandikIcerikId = sandikIcerikId,
                    Aciklama = "Aktif parti sandık geri alma regresyonu"
                }, default);

        public Task<Result> TopluUcKSifirlaAsync(int? sandikIcerikId) =>
            new UcKTopluSifirlaCommandHandler(Uow, _user, _durum, _hareket, _saha)
                .Handle(new UcKTopluSifirlaCommand
                {
                    ProjeId = ProjeId,
                    Secimler =
                    [
                        new UcKSandikSecimDto
                        {
                            CekiSatiriId = Satir.Id,
                            SandikIcerikId = sandikIcerikId
                        }
                    ],
                    Aciklama = "Aktif parti sandık toplu geri alma regresyonu"
                }, default);

        public void IkinciPartiAcildiMi(decimal miktar)
        {
            Assert.Equal((int)GridDurum.TamGeldi, Satir.GridDurumuId);
            Assert.Equal(3, Satir.GridGelenAdet);
            Assert.Equal((int)GridSevkDurum.SevkEdildi, Satir.GridSevkDurumuId);
            Assert.Equal(miktar, Satir.GridSevkMiktari);
            Assert.Equal(0, Satir.AktifGridSevkKarsilananMiktari);
            Assert.Equal(0, Icerik.AktifGridSevkKarsilananMiktari);
            Assert.Equal((int)UcKDurum.Bekliyor, Satir.UcKDurumuId);
            Assert.Equal((int)UcKDurum.Bekliyor, Satir.UcKKarsilamaTipiId);
            Assert.Equal(2, Satir.GelenMiktar);
            Assert.Equal(1, Satir.KalanMiktar);
            Assert.Equal(3, Icerik.TahsisMiktari);
            Assert.Equal(2, Icerik.KonulanAdet);
            Assert.Equal(1, Icerik.EksikAdet);
        }

        public void TamamenKarsilandiMi()
        {
            Assert.Equal(3, Satir.GelenMiktar);
            Assert.Equal(1, Satir.AktifGridSevkKarsilananMiktari);
            Assert.Equal(1, Icerik.AktifGridSevkKarsilananMiktari);
            Assert.Equal(0, Satir.KalanMiktar);
            Assert.Equal((int)UrunDurum.Tamamlandi, Satir.DurumId);
            Assert.Equal(3, Icerik.TahsisMiktari);
            Assert.Equal(3, Icerik.KonulanAdet);
            Assert.Equal(0, Icerik.EksikAdet);
        }

        public void Dispose() => Uow.Dispose();
    }

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        private readonly Dictionary<Type, object> _repos = [];
        public int SaveCount { get; private set; }
        public bool HasActiveTransaction { get; private set; }

        public Repo<T> Repo<T>() where T : BaseEntity
        {
            if (!_repos.TryGetValue(typeof(T), out var repo))
                _repos[typeof(T)] = repo = new Repo<T>();
            return (Repo<T>)repo;
        }

        public IGenericRepository<T> GetRepository<T>() where T : BaseEntity => Repo<T>();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
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
        public List<T> Rows { get; } = [];
        public List<T> Updated { get; } = [];
        public Task<T?> GetByIdAsync(int id) => Task.FromResult(Rows.SingleOrDefault(e => e.Id == id));
        public Task<IEnumerable<T>> GetAllAsync() => Task.FromResult<IEnumerable<T>>(Rows);
        public Task<IEnumerable<T>> GetAllWithIncludeAsync<TProperty>(Expression<Func<T, TProperty>> include) => GetAllAsync();
        public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
            Task.FromResult<IEnumerable<T>>(Rows.Where(predicate.Compile()).ToList());
        public IQueryable<T> Queryable() => Rows.AsQueryable();
        public Task AddAsync(T entity)
        {
            if (entity.Id <= 0)
                entity.Id = Rows.Count == 0 ? 1 : Rows.Max(e => e.Id) + 1;
            Rows.Add(entity);
            return Task.CompletedTask;
        }
        public void Update(T entity) => Updated.Add(entity);
        public void Remove(T entity) => Rows.Remove(entity);
    }

    private sealed class CurrentUserStub : ICurrentUserService
    {
        public int? UserId => 7;
        public bool IsAuthenticated => true;
        public string? MenuKod => null;
    }

    private sealed class HareketStub : IHareketService
    {
        public Task HareketKaydetAsync(HareketGecmisi hareket) => Task.CompletedTask;
        public Task<IEnumerable<HareketGecmisi>> GetProjeHareketleriAsync(int projeId) => throw new NotSupportedException();
        public Task<IEnumerable<HareketGecmisi>> GetUrunHareketleriAsync(string referansTipi, string referansId) => throw new NotSupportedException();
        public Task<(IEnumerable<HareketGecmisi> Items, int TotalCount)> GetPaginatedProjeHareketleriAsync(
            int projeId, string? searchTerm, int? islemTipiId, int pageNumber, int pageSize) => throw new NotSupportedException();
    }

    private sealed class LookupStub : ILookupCacheService
    {
        public const int TadilattaId = 999;

        public string GetDeger<TLookup>(int id) where TLookup : LookupBase =>
            typeof(TLookup) == typeof(LookupKaliteDurum) && id == TadilattaId
                ? "Tadilatta"
                : id.ToString();
        public Task WarmupAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RefreshAsync<TLookup>(CancellationToken ct = default) where TLookup : LookupBase => Task.CompletedTask;
    }

    private sealed class SahaStub : ISahaTamamlamaService
    {
        public Task<bool> AktifTamamlamaVarMiAsync(int kaynakCekiSatiriId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Dictionary<int, decimal>> GetAktifTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => Task.FromResult(new Dictionary<int, decimal>());
        public Task<Dictionary<int, decimal>> GetAktifIsTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetAktifGerceklesenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetSevkEdilenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetSevkEdilenGerceklesenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<HashSet<int>> GetAktifSandikBazliAktarimSatirIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<KaynakSandikSahaAktarimDurumu> GetKaynakSandikSahaAktarimDurumuAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SenkronizeKaynakProjelerAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SenkronizeKaynakProjelerBySahaSandikIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StokStub : IStokService
    {
        public Task<IEnumerable<StokKaydi>> GetUygunStoklarAsync(string? searchTerm = null) => throw new NotSupportedException();
        public Task<StokKaydi?> GetStokByIdAsync(int stokKaydiId) => throw new NotSupportedException();
        public Task<bool> StokYeterliMi(int stokKaydiId, decimal miktar) => throw new NotSupportedException();
        public Task<bool> StokDusAsync(int stokKaydiId, decimal miktar) => throw new NotSupportedException();
        public Task<StokKaydi> StokKaydiOlusturAsync(StokKaydi stokKaydi) => throw new NotSupportedException();
        public Task<IEnumerable<StokKaydi>> GetTumStoklarAsync() => throw new NotSupportedException();
        public Task<(IEnumerable<StokKaydi> Items, int TotalCount)> GetPaginatedStoklarAsync(
            string? searchTerm, int pageNumber, int pageSize) => throw new NotSupportedException();
    }
}
