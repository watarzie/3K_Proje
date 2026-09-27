using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Tests;

/// <summary>Finans para hesabı tek kaynaktan: iş bedeli, KDV hariç/dahil ve son faturada kuruş kapanışı.</summary>
public sealed class FinansTutarKurallariTests
{
    [Fact]
    public void Kdv_haric_tutar_kurusa_sifirdan_uzaga_yuvarlanir()
    {
        Assert.Equal(new FinansTutar(30.02m, 6.00m, 36.02m), FinansTutarKurallari.Hesapla(3, 10.005m, 20));
    }

    [Theory]
    [InlineData(120, 20, 100, 20)]
    [InlineData(10, 18, 8.47, 1.53)]
    [InlineData(100, 0, 100, 0)]
    public void Kdv_dahil_tutardan_net_ayrilir_kdv_farktir(decimal brut, decimal oran, decimal net, decimal kdv)
    {
        Assert.Equal(new FinansTutar(net, kdv, brut), FinansTutarKurallari.KdvDahilHesapla(brut, oran));
    }

    [Fact]
    public void Po_kalemini_kapatan_fatura_kurus_farkini_kapatir()
    {
        var po = FinansTutarKurallari.Hesapla(1, 100, 20);
        var birinci = FinansTutarKurallari.FaturaKalemiTutari(33.33m, 20, po.Net, po.Kdv, 0, 0);
        var ikinci = FinansTutarKurallari.FaturaKalemiTutari(33.33m, 20, po.Net, po.Kdv, birinci.Net, birinci.Kdv);
        var son = FinansTutarKurallari.FaturaKalemiTutari(33.34m, 20, po.Net, po.Kdv,
            birinci.Net + ikinci.Net, birinci.Kdv + ikinci.Kdv);

        Assert.Equal(6.67m, birinci.Kdv);
        Assert.Equal(6.67m, ikinci.Kdv);
        Assert.Equal(6.66m, son.Kdv);
        Assert.Equal(po.Toplam, birinci.Toplam + ikinci.Toplam + son.Toplam);
    }

    [Theory]
    [InlineData(FinansFiyatlandirmaBirimi.Adet, 4)]
    [InlineData(FinansFiyatlandirmaBirimi.Metrekup, 2.5)]
    [InlineData(FinansFiyatlandirmaBirimi.SabitTutar, 1)]
    [InlineData(FinansFiyatlandirmaBirimi.ManuelToplam, 1)]
    public void Is_bedeli_fiyatlandirma_biriminin_miktarini_kullanir(FinansFiyatlandirmaBirimi birim, decimal miktar)
    {
        var is_ = new FinansIsKaydi { FiyatlandirmaBirimiSnapshot = birim, Adet = 4, ToplamM3 = 2.5m, BirimFiyatSnapshot = 100 };

        Assert.Equal(miktar, FinansTutarKurallari.FiyatlandirmaMiktari(birim, 4, 2.5m));
        Assert.Equal(FinansTutarKurallari.Para(100 * miktar), FinansTutarKurallari.IsNet(is_));
    }

    [Fact]
    public void Manuel_net_tutar_is_bedelini_belirler()
    {
        var is_ = new FinansIsKaydi
        {
            FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.Adet, Adet = 4, BirimFiyatSnapshot = 100, ManuelNetTutar = 75
        };

        Assert.Equal(75m, FinansTutarKurallari.IsNet(is_));
    }
}
