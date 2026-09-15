using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

public class OrtakMiktarDurumIsKurallariTests
{
    [Fact]
    public void SevkinTamTeslimi_IhtiyacinTamamlanmasiDegildir_35Ihtiyac32Teslim3Kalan()
    {
        var satir = new CekiSatiri { IstenenAdet = 35m, GridGelenAdet = 35m,
            GridDurumuId = (int)GridDurum.TamGeldi, GridSevkMiktari = 32m,
            UcKDurumuId = (int)UcKDurum.TamGeldi, GelenMiktar = 32m };
        var service = new DurumHesaplaService();
        satir.DurumId = service.HesaplaGenelDurum(satir.GridDurumuId, satir.UcKDurumuId);
        Assert.Equal((int)UrunDurum.Tamamlandi, satir.DurumId);
        service.HesaplaKalanVeDurum(satir);
        Assert.Equal(3m, satir.KalanMiktar);
        Assert.Equal((int)UrunDurum.KismiTamamlandi, satir.DurumId);
        Assert.Equal((int)UcKDurum.TamGeldi, satir.UcKDurumuId);
    }

    [Fact]
    public void OndalikKaynaklarNetToplanir_SevkVeYardimciToplamIkinciKezDusulmez()
    {
        var satir = new CekiSatiri { IstenenAdet = 10.0001m, GelenMiktar = 2.1234m,
            StokKarsilanan = 1.2345m, ProjeKarsilanan = 1.1111m, TedarikciKarsilanan = 2.2222m,
            ProjeGonderilen = .2222m, TrafoSevkAdet = .5001m, KarsilananMiktar = 4.5678m,
            GridGelenAdet = 9m, GridSevkMiktari = 5m, OrijinalIstenenAdet = 1m };
        Assert.Equal(6.4690m, satir.KumulatifToplam);
        Assert.Equal(3.0310m, satir.KalanMiktar);
        Assert.Equal(3.5311m, satir.EksikMiktar);
        Assert.Equal(.5m, satir.GridEksikMiktar);
        satir.GridSevkMiktari = 9m;
        satir.KarsilananMiktar = 99m;
        satir.OrijinalIstenenAdet = 999m;
        Assert.Equal(3.0310m, satir.KalanMiktar);
        Assert.Equal(6.4690m, satir.KumulatifToplam);
    }

    [Theory]
    [InlineData(GridDurum.Iptal)]
    [InlineData(GridDurum.GridKapandi)]
    public void TerminalDurum_IsiKapatirFakatFizikselTeslimUretmez(GridDurum terminal)
    {
        var satir = new CekiSatiri { IstenenAdet = 3.1234m, GridDurumuId = (int)terminal,
            GelenMiktar = .1234m, HataliMiktar = .0001m, DurumId = (int)UrunDurum.HataliUyumsuzGonderim };
        Assert.Equal(0m, satir.KalanMiktar);
        Assert.Equal(0m, satir.GridEksikMiktar);
        new DurumHesaplaService().HesaplaKalanVeDurum(satir);
        Assert.Equal((int)UrunDurum.Tamamlandi, satir.DurumId);
        Assert.Equal(.1234m, satir.GelenMiktar);
        Assert.Equal(.0001m, satir.HataliMiktar);
        Assert.Equal(0m, satir.StokKarsilanan);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HataliUyumsuzluk_FizikselEksikOlmasaDaIsiAcikTutar(bool hataliMiktar)
    {
        var satir = new CekiSatiri { IstenenAdet = 1.0001m, GelenMiktar = 1.0001m,
            HataliMiktar = hataliMiktar ? .0001m : 0m,
            DurumId = hataliMiktar ? (int)UrunDurum.Tamamlandi : (int)UrunDurum.HataliUyumsuzGonderim };
        Assert.Equal(1m, satir.KalanMiktar);
        Assert.Equal(0m, satir.EksikMiktar);
        new DurumHesaplaService().HesaplaKalanVeDurum(satir);
        Assert.NotEqual((int)UrunDurum.Tamamlandi, satir.DurumId);
        Assert.Equal(1.0001m, satir.GelenMiktar);
    }

    public static IEnumerable<object[]> OncelikliDurumlar()
    {
        foreach (var grid in Enum.GetValues<GridDurum>())
        {
            yield return [grid, UcKDurum.Paketlendi, UrunDurum.Tamamlandi];
            yield return [grid, UcKDurum.IadeEdildi, UrunDurum.GeriGonderildi];
        }
    }

    [Theory]
    [MemberData(nameof(OncelikliDurumlar))]
    public void PaketlemeVeIade_IlkAsamadaButunGridDurumlarindanOnceliklidir(GridDurum grid, UcKDurum uck, UrunDurum expected)
        => Assert.Equal((int)expected, new DurumHesaplaService().HesaplaGenelDurum((int)grid, (int)uck));

    [Theory]
    [InlineData(GridDurum.Iptal, UcKDurum.TamGeldi, UrunDurum.IptalVeyaPasif)]
    [InlineData(GridDurum.Sipariste, UcKDurum.TamGeldi, UrunDurum.Sipariste)]
    [InlineData(GridDurum.Gelmedi, UcKDurum.TamGeldi, UrunDurum.Gelmedi)]
    [InlineData(GridDurum.TrafoSevk, UcKDurum.TamGeldi, UrunDurum.Tamamlandi)]
    [InlineData(GridDurum.TrafoSevk, UcKDurum.KontrolEdildi, UrunDurum.Tamamlandi)]
    [InlineData(GridDurum.TrafoSevk, UcKDurum.EksikGeldi, UrunDurum.Eksik)]
    [InlineData(GridDurum.TrafoSevk, UcKDurum.Bekliyor, UrunDurum.TrafoSevk)]
    [InlineData(GridDurum.TamGeldi, UcKDurum.TamGeldi, UrunDurum.Tamamlandi)]
    [InlineData(GridDurum.TamGeldi, UcKDurum.KontrolEdildi, UrunDurum.Tamamlandi)]
    [InlineData(GridDurum.TamGeldi, UcKDurum.EksikGeldi, UrunDurum.Eksik)]
    [InlineData(GridDurum.TamGeldi, UcKDurum.Gelmedi, UrunDurum.Kayip)]
    [InlineData(GridDurum.TamGeldi, UcKDurum.Bekliyor, UrunDurum.GriddeHazir)]
    [InlineData(GridDurum.EksikGeldi, UcKDurum.TamGeldi, UrunDurum.KismiTamamlandi)]
    [InlineData(GridDurum.EksikGeldi, UcKDurum.KontrolEdildi, UrunDurum.KismiTamamlandi)]
    [InlineData(GridDurum.EksikGeldi, UcKDurum.EksikGeldi, UrunDurum.Eksik)]
    [InlineData(GridDurum.EksikGeldi, UcKDurum.Gelmedi, UrunDurum.Kayip)]
    [InlineData(GridDurum.EksikGeldi, UcKDurum.Bekliyor, UrunDurum.GriddeEksik)]
    [InlineData(GridDurum.Bekliyor, UcKDurum.TamGeldi, UrunDurum.Tamamlandi)]
    [InlineData(GridDurum.Bekliyor, UcKDurum.KontrolEdildi, UrunDurum.Tamamlandi)]
    [InlineData(GridDurum.Bekliyor, UcKDurum.EksikGeldi, UrunDurum.Eksik)]
    [InlineData(GridDurum.Bekliyor, UcKDurum.ProjedenKarsilandi, UrunDurum.Tamamlandi)]
    [InlineData(GridDurum.Bekliyor, UcKDurum.StoktanKarsilandi, UrunDurum.Tamamlandi)]
    [InlineData(GridDurum.Bekliyor, UcKDurum.TedarikcidenGeldi, UrunDurum.Tamamlandi)]
    [InlineData(GridDurum.Bekliyor, UcKDurum.BaskaProyeVerildi, UrunDurum.BaskaProyeVerildi)]
    [InlineData(GridDurum.Bekliyor, UcKDurum.GeriGonderildi, UrunDurum.GeriGonderildi)]
    [InlineData(GridDurum.Bekliyor, UcKDurum.HataliUrun, UrunDurum.HataliUrun)]
    [InlineData(GridDurum.Bekliyor, UcKDurum.Bekliyor, UrunDurum.Bekliyor)]
    public void IlkGenelDurum_BelgelenenKararTablosunuIzler(GridDurum grid, UcKDurum uck, UrunDurum expected)
        => Assert.Equal((int)expected, new DurumHesaplaService().HesaplaGenelDurum((int)grid, (int)uck));

    [Theory]
    [InlineData("0", "0", UrunDurum.Tamamlandi, UrunDurum.Eksik)]
    [InlineData("0.0001", "0", UrunDurum.Tamamlandi, UrunDurum.KismiTamamlandi)]
    [InlineData("0", "0.0001", UrunDurum.Tamamlandi, UrunDurum.KismiTamamlandi)]
    [InlineData("1", "0", UrunDurum.Eksik, UrunDurum.Tamamlandi)]
    [InlineData("1.0001", "0", UrunDurum.Gelmedi, UrunDurum.Tamamlandi)]
    [InlineData("0.0001", "0", UrunDurum.GriddeHazir, UrunDurum.GriddeHazir)]
    public void IkinciAsama_ZeroKismiTamSinirlariniAyirir(string delivered, string trafo, UrunDurum initial, UrunDurum expected)
    {
        var satir = new CekiSatiri { IstenenAdet = 1m, GelenMiktar = Parse(delivered), TrafoSevkAdet = Parse(trafo), DurumId = (int)initial };
        new DurumHesaplaService().HesaplaKalanVeDurum(satir);
        Assert.Equal((int)expected, satir.DurumId);
    }

    [Theory]
    [InlineData(false, "0.5000", "1.0001")]
    [InlineData(false, "2", "0")]
    [InlineData(true, "0.5000", "1.5001")]
    public void SahaEtkinKalan_KaynaktaDusulurKopyadaTekrarDusulmez(bool sahaKopyasi, string sahaMiktari, string expected)
    {
        var satir = new CekiSatiri { Id = 10, IstenenAdet = 2m, GelenMiktar = .4999m, KaynakCekiSatiriId = sahaKopyasi ? 9 : null };
        Assert.Equal(Parse(expected), CekiSatiriKalanHelper.HesaplaEtkinKalan(satir, new Dictionary<int, decimal> { [10] = Parse(sahaMiktari) }));
        Assert.Equal(1.5001m, CekiSatiriKalanHelper.HesaplaEtkinKalan(satir, new Dictionary<int, decimal>()));
        Assert.Equal(.4999m, satir.GelenMiktar);
    }

    [Theory]
    [InlineData("Grid")][InlineData("GridGelen")][InlineData("Trafo")][InlineData("SevkDurum")]
    [InlineData("SevkMiktar")][InlineData("Borc")][InlineData("UcK")][InlineData("KarsilamaTipi")]
    [InlineData("Gelen")][InlineData("Karsilanan")][InlineData("Hatali")][InlineData("Stok")]
    [InlineData("Proje")][InlineData("ProjeCikis")][InlineData("Tedarikci")][InlineData("Iade")]
    [InlineData("Kalite")][InlineData("Surec")]
    public void ManuelSilme_SifirdanFarkliHerOperasyonIziniKorur(string operation)
    {
        var satir = new CekiSatiri { GridDurumuId = (int)GridDurum.Gelmedi, GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi,
            UcKDurumuId = (int)UcKDurum.Bekliyor, UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor };
        Assert.False(ManuelUrunSilmeKurali.IslemGormusMu(satir));
        switch (operation)
        {
            case "Grid": satir.GridDurumuId = (int)GridDurum.Iptal; break;
            case "GridGelen": satir.GridGelenAdet = .0001m; break;
            case "Trafo": satir.TrafoSevkAdet = .0001m; break;
            case "SevkDurum": satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi; break;
            case "SevkMiktar": satir.GridSevkMiktari = .0001m; break;
            case "Borc": satir.YenidenSevkGerekliAdet = .0001m; break;
            case "UcK": satir.UcKDurumuId = (int)UcKDurum.Gelmedi; break;
            case "KarsilamaTipi": satir.UcKKarsilamaTipiId = (int)UcKDurum.Gelmedi; break;
            case "Gelen": satir.GelenMiktar = .0001m; break;
            case "Karsilanan": satir.KarsilananMiktar = .0001m; break;
            case "Hatali": satir.HataliMiktar = .0001m; break;
            case "Stok": satir.StokKarsilanan = .0001m; break;
            case "Proje": satir.ProjeKarsilanan = .0001m; break;
            case "ProjeCikis": satir.ProjeGonderilen = .0001m; break;
            case "Tedarikci": satir.TedarikciKarsilanan = .0001m; break;
            case "Iade": satir.GeriGonderilenMiktar = .0001m; break;
            case "Kalite": satir.KaliteDurumId = 1; break;
            case "Surec": satir.SurecDurumId = 1; break;
        }
        Assert.True(ManuelUrunSilmeKurali.IslemGormusMu(satir));
    }

    [Theory]
    [InlineData("Iptal", false)][InlineData("Tamamlandi", false)][InlineData("GridKapandi", true)]
    [InlineData("Gelen", true)][InlineData("Stok", true)][InlineData("Proje", true)][InlineData("Tedarikci", true)]
    public void DepoUygunlugu_IsTamamlanmasiDegilFizikselKaynakIziniIzler(string source, bool expected)
    {
        var sandik = new Sandik { DurumId = (int)SandikDurum.Kapandi };
        var satir = new CekiSatiri();
        switch (source)
        {
            case "Iptal": satir.GridDurumuId = (int)GridDurum.Iptal; break;
            case "Tamamlandi": satir.DurumId = (int)UrunDurum.Tamamlandi; break;
            case "GridKapandi": satir.GridDurumuId = (int)GridDurum.GridKapandi; break;
            case "Gelen": satir.GelenMiktar = .0001m; break;
            case "Stok": satir.StokKarsilanan = .0001m; break;
            case "Proje": satir.ProjeKarsilanan = .0001m; break;
            case "Tedarikci": satir.TedarikciKarsilanan = .0001m; break;
        }
        SandikIcerik[] icerik = [new() { CekiSatiriId = 10, CekiSatiri = satir }];
        Assert.Equal(expected, SandikDepoKurali.DepoLokasyonuAtanabilir(sandik, icerik));
        sandik.DurumId = (int)SandikDurum.Sevkedildi;
        sandik.SevkiyatDuzeltmeAcikMi = true;
        Assert.False(SandikDepoKurali.DepoLokasyonuAtanabilir(sandik, icerik));
    }

    private static decimal Parse(string value) => decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
}
