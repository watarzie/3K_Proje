using _3K.Application.Features.UcKIslemleri.Commands;
using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Application.Tests;

public sealed class UcKAktifSevkPaketiKuralTests
{
    [Fact]
    public void CanliPA580Durumu_SonBirAdetAktifPaketOlarakHesaplanir()
    {
        var satir = CanliSatir();

        var sonuc = UcKAktifSevkPaketiKural.HesaplaTeslimMiktari(satir);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(1m, sonuc.Value);
        Assert.Equal(2m, UcKAktifSevkPaketiKural.HesaplaFizikselKalan(satir));
    }

    [Fact]
    public void TelafiPaketi_KumulatifSandikMiktarindanBagimsizTamDeltaOlarakAlinir()
    {
        var satir = CanliSatir();
        satir.GridSevkMiktari = 2;

        var sonuc = UcKAktifSevkPaketiKural.HesaplaTeslimMiktari(satir);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(2m, sonuc.Value);
    }

    [Fact]
    public void AktifSevkDeltasiYoksa_KumulatifGridGelenFallbackOlarakKullanilmaz()
    {
        foreach (var aktifSevk in new decimal?[] { null, 0m })
        {
            var satir = CanliSatir();
            satir.GridSevkMiktari = aktifSevk;
            satir.GridGelenAdet = 10;

            var sonuc = UcKAktifSevkPaketiKural.HesaplaTeslimMiktari(satir);

            Assert.False(sonuc.IsSuccess);
            Assert.Equal(409, sonuc.StatusCode);
        }
    }

    [Fact]
    public void AktifSevkFizikselKalandanBuyukse_SessizceKirpilmaz()
    {
        var satir = CanliSatir();
        satir.GridSevkMiktari = 3;

        var sonuc = UcKAktifSevkPaketiKural.HesaplaTeslimMiktari(satir);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains("fiziksel kalanından", sonuc.Error!.Message);
    }

    [Fact]
    public void YapayHataliDurumKalani_FizikselTeslimMiktariSayilmaz()
    {
        var satir = CanliSatir();
        satir.GelenMiktar = 18;
        satir.GridSevkMiktari = 1;
        satir.HataliMiktar = 1;
        satir.DurumId = (int)UrunDurum.HataliUyumsuzGonderim;

        Assert.Equal(1m, satir.KalanMiktar);

        var sonuc = UcKAktifSevkPaketiKural.HesaplaTeslimMiktari(satir);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(0m, UcKAktifSevkPaketiKural.HesaplaFizikselKalan(satir));
    }

    [Fact]
    public void IlkNormalSevk_YeniPaketDalinaGirmez()
    {
        var satir = CanliSatir();
        satir.ProjeGonderilen = 0;
        satir.GelenMiktar = 0;

        Assert.False(UcKAktifSevkPaketiKural.YeniPaketTeslimiMi(satir));
    }

    [Fact]
    public void TransferOlmayanParcaliYenidenSevk_AktifDeltaOlarakAlinir()
    {
        var satir = CanliSatir();
        satir.ProjeGonderilen = 0;
        satir.GelenMiktar = 6;
        satir.GridSevkMiktari = 4;

        Assert.True(UcKAktifSevkPaketiKural.YeniPaketTeslimiMi(satir));

        var sonuc = UcKAktifSevkPaketiKural.HesaplaTeslimMiktari(satir);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(4m, sonuc.Value);
    }

    [Fact]
    public void CokSandikliTelafide_TumSandiklarSecilmelidir()
    {
        var mevcutIcerikler = new[] { 101, 102 };

        Assert.False(UcKAktifSevkPaketiKural.TumSandiklarSecildiMi(
            mevcutIcerikler,
            new int?[] { 101 }));
        Assert.True(UcKAktifSevkPaketiKural.TumSandiklarSecildiMi(
            mevcutIcerikler,
            new int?[] { 101, 102 }));
        Assert.True(UcKAktifSevkPaketiKural.TumSandiklarSecildiMi(
            mevcutIcerikler,
            new int?[] { null }));
    }

    private static CekiSatiri CanliSatir()
    {
        return new CekiSatiri
        {
            Id = 12852,
            IstenenAdet = 10,
            GridGelenAdet = 10,
            GridSevkMiktari = 1,
            GelenMiktar = 16,
            ProjeGonderilen = 8,
            StokKarsilanan = 0,
            ProjeKarsilanan = 0,
            TedarikciKarsilanan = 0,
            TrafoSevkAdet = 0,
            GridDurumuId = (int)GridDurum.TamGeldi,
            GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi,
            UcKDurumuId = (int)UcKDurum.Bekliyor,
            UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor,
            DurumId = (int)UrunDurum.KismiTamamlandi
        };
    }
}
