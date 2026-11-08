using _3K.Core.Entities;
using _3K.Infrastructure.Services.Raporlar;

namespace _3K.Application.Tests;

/// <summary>
/// Gerçekleşen çeki raporunda sandık parçasının ürün bilgisi (seri no, SKT, lot) AÇIKLAMA hücresinde, çeki/Grid/3K
/// açıklamalarının altında gösterilir; ayrı sütun yoktur.
/// </summary>
public sealed class RaporUrunBilgisiAciklamaTests
{
    private static SandikIcerik Icerik(int id, string sandikNo, string? urunBilgisi) => new()
    {
        Id = id,
        UrunBilgisi = urunBilgisi,
        Sandik = new Sandik { Id = id + 100, SandikNo = sandikNo }
    };

    [Fact]
    public void UrunBilgisiOlmayanSatirda_AciklamaDegismez()
    {
        var satir = new CekiSatiri { GridAciklama = "caner bayram", SandikIcerikleri = [Icerik(1, "17", null), Icerik(2, "18", "  ")] };

        Assert.Equal("Grid: caner bayram", RaporBicimleri.GetGerceklesenRaporAciklama(satir));
    }

    [Fact]
    public void TekBilgi_AciklamalarinAltindaEtiketleGosterilir()
    {
        var satir = new CekiSatiri
        {
            Remarks = "6",
            GridAciklama = "caner bayram",
            SandikIcerikleri = [Icerik(1, "17", "SN: A-1001, SKT: 03.2027")]
        };

        Assert.Equal(
            "Çeki: 6\nGrid: caner bayram\nÜrün bilgisi: SN: A-1001, SKT: 03.2027",
            RaporBicimleri.GetGerceklesenRaporAciklama(satir));
    }

    [Fact]
    public void YalnizUrunBilgisiVarsa_TekSatirOlur()
    {
        var satir = new CekiSatiri { SandikIcerikleri = [Icerik(1, "3", "Lot: L-42")] };

        Assert.Equal("Ürün bilgisi: Lot: L-42", RaporBicimleri.GetGerceklesenRaporAciklama(satir));
    }

    [Fact]
    public void AltSatirlaraYazilanBilgi_VirgulleTekSatiraBirlesir()
    {
        var satirlar = RaporBicimleri.UrunBilgisiSatirlari([Icerik(1, "1", " SN: A-1001,\r\n\r\nSKT: 03.2027 ;\nLot: L-42 ")]);

        Assert.Equal(["Ürün bilgisi: SN: A-1001, SKT: 03.2027, Lot: L-42"], satirlar);
    }

    [Fact]
    public void BolunmusUrundeAyniBilgi_BirKezYazilir()
    {
        var satirlar = RaporBicimleri.UrunBilgisiSatirlari([Icerik(1, "2", "SN: A-1"), Icerik(2, "10", "SN: A-1"), Icerik(3, "4", null)]);

        Assert.Equal(["Ürün bilgisi: SN: A-1"], satirlar);
    }

    [Fact]
    public void BolunmusUrundeFarkliBilgi_SandikNumarasiylaDogalSiradaYazilir()
    {
        var satirlar = RaporBicimleri.UrunBilgisiSatirlari([Icerik(1, "10", "SN: A-1002"), Icerik(2, "2", "SN: A-1001"), Icerik(3, "4", null)]);

        Assert.Equal(["Ürün bilgisi (Sandık 2): SN: A-1001", "Ürün bilgisi (Sandık 10): SN: A-1002"], satirlar);
    }
}
