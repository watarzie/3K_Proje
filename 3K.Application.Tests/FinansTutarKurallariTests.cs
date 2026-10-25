using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;
using _3K.Infrastructure.Services.Finans;

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

    /// <summary>
    /// SQL projeksiyonlarındaki iş net tutarı domain kuralıyla aynı kalmalıdır. Derlenen ifade yarımları
    /// bankacı yuvarlamasıyla, PostgreSQL round sıfırdan uzağa yuvarlar; örnekler yarım değer içermez.
    /// </summary>
    [Theory]
    [InlineData(FinansFiyatlandirmaBirimi.Adet)]
    [InlineData(FinansFiyatlandirmaBirimi.Metrekup)]
    [InlineData(FinansFiyatlandirmaBirimi.SabitTutar)]
    [InlineData(FinansFiyatlandirmaBirimi.ManuelToplam)]
    public void Sql_net_ifadesi_is_net_kuraliyla_ayni_sonucu_verir(FinansFiyatlandirmaBirimi birim)
    {
        var sqlNet = FinansSorgulari.IsNetIfadesi.Compile();
        var is_ = new FinansIsKaydi { FiyatlandirmaBirimiSnapshot = birim, Adet = 3, ToplamM3 = 2.4567m, BirimFiyatSnapshot = 10.2345m };

        Assert.Equal(FinansTutarKurallari.IsNet(is_), sqlNet(is_));
        is_.ManuelNetTutar = 75.5m;
        Assert.Equal(FinansTutarKurallari.IsNet(is_), sqlNet(is_));
    }

    /// <summary>
    /// Malzemeli fiyat (kontrplak): kontra m³ kontra, ahşap m³ ahşap tarifesiyle fiyatlanır. Hacim henüz elle
    /// girilmediyse kayıt m³'ünün tamamı kontra sayılır. SQL ifadesi aynı sonucu verir.
    /// </summary>
    [Fact]
    public void Malzemeli_net_ahsap_ve_kontra_m3_u_kendi_tarifesiyle_fiyatlar_sql_ile_ayni()
    {
        var sqlNet = FinansSorgulari.IsNetIfadesi.Compile();
        var is_ = new FinansIsKaydi
        {
            FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.Metrekup, Adet = 2, ToplamM3 = 3.4m,
            AhsapM3 = 1.2m, KontraM3 = 2.2m, BirimFiyatSnapshot = 300m, AhsapBirimFiyatSnapshot = 150m
        };

        Assert.Equal(300m * 2.2m + 150m * 1.2m, FinansTutarKurallari.IsNet(is_));
        Assert.Equal(FinansTutarKurallari.IsNet(is_), sqlNet(is_));

        is_.AhsapM3 = null;
        is_.KontraM3 = null;
        Assert.Equal(300m * 3.4m, FinansTutarKurallari.IsNet(is_));
        Assert.Equal(FinansTutarKurallari.IsNet(is_), sqlNet(is_));

        is_.ManuelNetTutar = 99m;
        Assert.Equal(99m, FinansTutarKurallari.IsNet(is_));
        Assert.Equal(99m, sqlNet(is_));
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
