using _3K.Application.Common;
using _3K.Core.Entities;

namespace _3K.Application.Tests;

/// <summary>
/// Tahsis kayıtlarını değiştirmeden kullanılan okuma/fallback hesaplarının
/// tek sandık, çok sandık ve dört ondalık sınırlarını doğrular.
/// </summary>
public sealed class SandikTahsisOkumaKurallariTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void PozitifTahsis_TahsisSayisindanVeAnaMiktardanBagimsizKorunur(int tahsisSayisi)
    {
        var satir = new CekiSatiri { IstenenAdet = 12.75m };
        var icerik = new SandikIcerik { TahsisMiktari = 2.3456m, KonulanAdet = 1 };

        var sonuc = SandikTahsisHelper.HesaplaSandikMiktari(satir, icerik, tahsisSayisi);

        Assert.Equal(2.3456m, sonuc);
    }

    [Theory]
    [InlineData(1, 7.25, 2.5, 7.25)]
    [InlineData(2, 7.25, 2.5, 2.5)]
    [InlineData(3, -7.25, -2.5, 0)]
    public void LegacySifirTahsis_TekKayittaAnaMiktari_CokKayittaFizikselMiktariFallbackAlir(
        int tahsisSayisi,
        double istenen,
        double konulan,
        double beklenen)
    {
        var satir = new CekiSatiri { IstenenAdet = (decimal)istenen };
        var icerik = new SandikIcerik { TahsisMiktari = 0, KonulanAdet = (decimal)konulan };

        var sonuc = SandikTahsisHelper.HesaplaSandikMiktari(satir, icerik, tahsisSayisi);

        Assert.Equal((decimal)beklenen, sonuc);
    }

    [Fact]
    public void TahsisKaydiYoksa_NegatifAnaMiktarSifiraSinirlanir()
    {
        var sonuc = SandikTahsisHelper.HesaplaSandikMiktari(
            new CekiSatiri { IstenenAdet = -1.25m },
            icerik: null,
            tahsisSayisi: 0);

        Assert.Equal(0, sonuc);
    }

    [Fact]
    public void TekTahsisSatirPayi_EskiTahsisleAnaOperasyonToplaminiKirpmaz()
    {
        var sonuc = SandikTahsisHelper.ToplamdanSatirPayi(
            toplam: 9.8765m,
            sandikMiktari: 2,
            toplamTahsisMiktari: 2,
            tahsisSayisi: 1);

        Assert.Equal(9.8765m, sonuc);
    }

    [Fact]
    public void CokluTahsisSatirPayi_ToplamTahsisOraniniVeSandikUstSiniriniKorur()
    {
        var ilk = SandikTahsisHelper.ToplamdanSatirPayi(9, 2, 6, tahsisSayisi: 2);
        var ikinci = SandikTahsisHelper.ToplamdanSatirPayi(9, 4, 6, tahsisSayisi: 2);

        Assert.Equal(2, ilk);
        Assert.Equal(4, ikinci);
        Assert.Equal(6, ilk + ikinci);
    }

    [Fact]
    public void TekTahsisKalanPayi_MerkeziKalaniDortOndaliklaKorur()
    {
        var sonuc = SandikTahsisHelper.HesaplaKalanPaylari(
            etkinToplamKalan: 1.23456m,
            tahsisMiktarlari: [10],
            tamamlananMiktarlari: [9]);

        Assert.Equal([1.2346m], sonuc);
    }

    [Fact]
    public void CokluTahsisKalanPayi_FizikselAcikOranindaDagilirVeYuvarlamaToplaminiKorur()
    {
        var sonuc = SandikTahsisHelper.HesaplaKalanPaylari(
            etkinToplamKalan: 1,
            tahsisMiktarlari: [2, 4, 6],
            tamamlananMiktarlari: [1, 2, 3]);

        Assert.Equal([0.1667m, 0.3333m, 0.5m], sonuc);
        Assert.Equal(1, sonuc.Sum());
    }

    [Fact]
    public void FizikselAcikYokkenMerkeziUyariKalani_IlkPozitifTahsisSatirindaGosterilir()
    {
        var sonuc = SandikTahsisHelper.HesaplaKalanPaylari(
            etkinToplamKalan: 1,
            tahsisMiktarlari: [0, 2, 3],
            tamamlananMiktarlari: [0, 2, 3]);

        Assert.Equal([0, 1, 0], sonuc);
    }

    [Fact]
    public void KalanPayi_ListeleriFarkliUzunluktaysaAcikHataVerir()
    {
        var hata = Assert.Throws<ArgumentException>(() => SandikTahsisHelper.HesaplaKalanPaylari(
            etkinToplamKalan: 1,
            tahsisMiktarlari: [1, 2],
            tamamlananMiktarlari: [1]));

        Assert.Contains("aynı uzunlukta", hata.Message);
    }

    [Fact]
    public void BosTahsisListesindeKalanDagitimiBosDoner()
    {
        var sonuc = SandikTahsisHelper.HesaplaKalanPaylari(5, [], []);

        Assert.Empty(sonuc);
    }
}
