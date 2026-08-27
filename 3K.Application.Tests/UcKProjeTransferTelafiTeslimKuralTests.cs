using _3K.Application.Common;
using _3K.Application.Features.UcKIslemleri.Commands;
using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Application.Tests;

public class UcKProjeTransferTelafiTeslimKuralTests
{
    [Fact]
    public void Satir86_KismiTransferSonrasiYeniSekizAdedinTamaminiTeslimAlir()
    {
        var satir = TelafiBekleyenSatir(istenen: 10, gelen: 10, projeGonderilen: 8, gridSevk: 8);

        var aktif = UcKProjeTransferTelafiTeslimKural.AktifMi(satir, sandikIcerikSayisi: 1);
        var eklenecek = BasariliMiktar(UcKProjeTransferTelafiTeslimKural.TeslimMiktariniHesapla(
            aktif,
            mevcutHesaplananMiktar: 6,
            satir));
        satir.GelenMiktar += eklenecek;

        Assert.True(aktif);
        Assert.Equal(8, eklenecek);
        Assert.Equal(18, satir.GelenMiktar);
        Assert.Equal(10, satir.KumulatifToplam);
        Assert.Equal(0, satir.KalanMiktar);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(1)]
    public void TamTransferSatirlari_MevcutDogruDavranisiKorur(decimal miktar)
    {
        var satir = TelafiBekleyenSatir(miktar, miktar, miktar, miktar);

        var aktif = UcKProjeTransferTelafiTeslimKural.AktifMi(satir, sandikIcerikSayisi: 1);
        var eklenecek = BasariliMiktar(UcKProjeTransferTelafiTeslimKural.TeslimMiktariniHesapla(
            aktif,
            mevcutHesaplananMiktar: miktar,
            satir));
        satir.GelenMiktar += eklenecek;

        Assert.True(aktif);
        Assert.Equal(miktar, eklenecek);
        Assert.Equal(0, satir.KalanMiktar);
    }

    [Fact]
    public void NormalIlkSevk_ProjeTransferDaliDevreyeGirmez()
    {
        var satir = TelafiBekleyenSatir(istenen: 10, gelen: 0, projeGonderilen: 0, gridSevk: 10);

        var aktif = UcKProjeTransferTelafiTeslimKural.AktifMi(satir, sandikIcerikSayisi: 1);
        var sonuc = BasariliMiktar(UcKProjeTransferTelafiTeslimKural.TeslimMiktariniHesapla(
            aktif,
            mevcutHesaplananMiktar: 10,
            satir));

        Assert.False(aktif);
        Assert.Equal(10, sonuc);
    }

    [Fact]
    public void NormalKumulatifSevk_ProjeTransferDaliDevreyeGirmez()
    {
        var satir = TelafiBekleyenSatir(istenen: 10, gelen: 6, projeGonderilen: 0, gridSevk: 4);

        var aktif = UcKProjeTransferTelafiTeslimKural.AktifMi(satir, sandikIcerikSayisi: 1);
        var sonuc = BasariliMiktar(UcKProjeTransferTelafiTeslimKural.TeslimMiktariniHesapla(
            aktif,
            mevcutHesaplananMiktar: 4,
            satir));

        Assert.False(aktif);
        Assert.Equal(4, sonuc);
    }

    [Fact]
    public void CokSandikliSatir_OzelDalaGirmez()
    {
        var satir = TelafiBekleyenSatir(istenen: 10, gelen: 10, projeGonderilen: 8, gridSevk: 8);

        Assert.False(UcKProjeTransferTelafiTeslimKural.AktifMi(satir, sandikIcerikSayisi: 2));
    }

    [Fact]
    public void TelafiPaketindeSandikDoluGorunseBile_TeslimHesabiAtlanmaz()
    {
        Assert.True(UcKProjeTransferTelafiTeslimKural.TeslimIslemiGerekliMi(
            aktif: true,
            sandikKalanMiktari: 0));

        Assert.False(UcKProjeTransferTelafiTeslimKural.TeslimIslemiGerekliMi(
            aktif: false,
            sandikKalanMiktari: 0));
    }

    [Fact]
    public void YeniPaketAcilmadiysa_OzelDalaGirmez()
    {
        var satir = TelafiBekleyenSatir(istenen: 10, gelen: 10, projeGonderilen: 8, gridSevk: 8);
        satir.GridSevkDurumuId = (int)GridSevkDurum.YenidenSevkGerekli;

        Assert.False(UcKProjeTransferTelafiTeslimKural.AktifMi(satir, sandikIcerikSayisi: 1));
    }

    [Fact]
    public void TeslimAlinmisPaketTekrarCalistirilmaz()
    {
        var satir = TelafiBekleyenSatir(istenen: 10, gelen: 10, projeGonderilen: 8, gridSevk: 8);
        satir.UcKDurumuId = (int)UcKDurum.TamGeldi;
        satir.UcKKarsilamaTipiId = (int)UcKDurum.TamGeldi;
        satir.TeslimTarihi = new DateTime(2026, 8, 26, 17, 1, 0);

        var aktif = UcKProjeTransferTelafiTeslimKural.AktifMi(satir, sandikIcerikSayisi: 1);
        var sonuc = BasariliMiktar(UcKProjeTransferTelafiTeslimKural.TeslimMiktariniHesapla(
            aktif,
            mevcutHesaplananMiktar: 0,
            satir));

        Assert.False(aktif);
        Assert.Equal(0, sonuc);
    }

    [Fact]
    public void TelafiSevkiHamKalaniAsarsa_Islem409IleReddedilir()
    {
        var satir = TelafiBekleyenSatir(istenen: 10, gelen: 10, projeGonderilen: 4, gridSevk: 5);
        var aktif = UcKProjeTransferTelafiTeslimKural.AktifMi(satir, sandikIcerikSayisi: 1);

        var sonuc = UcKProjeTransferTelafiTeslimKural.TeslimMiktariniHesapla(
            aktif,
            mevcutHesaplananMiktar: 0,
            satir);

        Assert.True(aktif);
        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
    }

    [Fact]
    public void HataliUrunYapayKalanBir_UzerindenTelafiPaketiAcilmaz()
    {
        var satir = TelafiBekleyenSatir(istenen: 10, gelen: 18, projeGonderilen: 8, gridSevk: 1);
        satir.HataliMiktar = 1;

        Assert.Equal(1, satir.KalanMiktar);
        Assert.Equal(0, UcKProjeTransferTelafiTeslimKural.HamKalanMiktar(satir));
        Assert.False(UcKProjeTransferTelafiTeslimKural.AktifMi(satir, sandikIcerikSayisi: 1));
    }

    private static decimal BasariliMiktar(Result<decimal> result)
    {
        Assert.True(result.IsSuccess, result.Error?.Message);
        return result.Value;
    }

    private static CekiSatiri TelafiBekleyenSatir(
        decimal istenen,
        decimal gelen,
        decimal projeGonderilen,
        decimal gridSevk)
    {
        return new CekiSatiri
        {
            IstenenAdet = istenen,
            GelenMiktar = gelen,
            ProjeGonderilen = projeGonderilen,
            GridDurumuId = (int)GridDurum.TamGeldi,
            GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi,
            GridSevkMiktari = gridSevk,
            UcKDurumuId = (int)UcKDurum.Bekliyor,
            UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor,
            TeslimTarihi = null
        };
    }
}
