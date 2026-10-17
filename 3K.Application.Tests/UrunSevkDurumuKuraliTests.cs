using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Tests;

/// <summary>
/// R-07: bir adet yalnız fiziksel izle "gitti" sayılır (kendi projesinde ve sahaya aktarılan kısımda saha satırında
/// sevkli sandığa fiilen konan adet). Sandığa atanmak, aktarım kaleminin durumu ve iş tamamlanması sevk değildir.
/// Beklenen adetten trafoyla ve Grid İptal/Grid Kapandı ile kapanan kısım düşülür; fiilen giden adet yine görünür.
/// </summary>
public sealed class UrunSevkDurumuKuraliTests
{
    private const int ProjeId = 10;

    private static Sandik Sandik(int id, SandikDurum durum, int projeId = ProjeId) =>
        new() { Id = id, ProjeId = projeId, SandikNo = id.ToString(), DurumId = (int)durum };

    private static CekiSatiri Satir(decimal istenen, GridDurum grid = GridDurum.TamGeldi, decimal trafo = 0,
        params (Sandik Sandik, decimal Konulan)[] icerikler)
    {
        var satir = new CekiSatiri
        {
            Id = 5, IstenenAdet = istenen, GelenMiktar = istenen, TrafoSevkAdet = trafo, GridDurumuId = (int)grid
        };
        foreach (var (sandik, konulan) in icerikler)
            satir.SandikIcerikleri.Add(new SandikIcerik { Sandik = sandik, KonulanAdet = konulan });
        return satir;
    }

    private static UrunSevkSonucu? Belirle(CekiSatiri satir, params SahaAktarimParcasi[] parcalar) =>
        UrunSevkDurumuKurali.Belirle(satir, ProjeId, parcalar);

    [Fact]
    public void SevkliSandigaKonanAdet_TamSevkEdildi_KismiAdetleVerilir()
    {
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.SevkEdildi, 2, 2),
            Belirle(Satir(2, icerikler: [(Sandik(1, SandikDurum.Sevkedildi), 2)])));
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.KismiSevk, 3, 5),
            Belirle(Satir(5, icerikler: [(Sandik(1, SandikDurum.Sevkedildi), 3)])));
        // Bölünmüş satır: sevkli sandıktaki 2 gitti, elde kalan kapalı sandıktaki 3 gitmedi.
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.KismiSevk, 2, 5),
            Belirle(Satir(5, icerikler: [(Sandik(1, SandikDurum.Sevkedildi), 2), (Sandik(2, SandikDurum.Kapandi), 3)])));
    }

    /// <summary>
    /// Sevk edilen sandığa atanmış ama içine konmamış ürün ve tamamlanmış ama sandığı elde kalan ürün gitmemiştir.
    /// </summary>
    [Fact]
    public void SandigaAtanmakVeyaTamamlanmak_SevkDegildir()
    {
        var konmamis = Satir(1, GridDurum.Gelmedi, icerikler: [(Sandik(1, SandikDurum.Sevkedildi), 0)]);
        konmamis.GelenMiktar = 0;
        Assert.Equal(UrunSevkDurumu.SevkEdilmedi, Belirle(konmamis)?.Durum);

        var eldeTamamlanan = Satir(2, icerikler: [(Sandik(3, SandikDurum.Kapandi), 2)]);
        Assert.Equal(0, eldeTamamlanan.KalanMiktar);
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.SevkEdilmedi, 0, 2), Belirle(eldeTamamlanan));

        // Sandığı olmayan satır da gitmemiştir; başka projenin sevkli sandığı bu projenin sevki sayılmaz.
        Assert.Equal(UrunSevkDurumu.SevkEdilmedi, Belirle(Satir(1))?.Durum);
        Assert.Equal(UrunSevkDurumu.SevkEdilmedi,
            Belirle(Satir(1, icerikler: [(Sandik(9, SandikDurum.Sevkedildi, projeId: 99), 1)]))?.Durum);
    }

    /// <summary>
    /// Grid İptal/Grid Kapandı satırı teslim beklemez: hiçbir şey gitmediyse "-", sonradan iptal/kapandı yapılmış
    /// ama fiilen sevkli sandığa konup giden ürün "Sevk Edildi" görünür (gerçekten giden gizlenmez).
    /// </summary>
    [Theory]
    [InlineData(GridDurum.Iptal)]
    [InlineData(GridDurum.IptalEdildi)]
    [InlineData(GridDurum.GridKapandi)]
    public void IptalVeGridKapandi_GitmediyseSevkYok_GittiyseSevkEdildi(GridDurum grid)
    {
        Assert.Null(Belirle(Satir(1, grid, icerikler: [(Sandik(1, SandikDurum.Sevkedildi), 0)])));
        Assert.Null(Belirle(Satir(1, grid, icerikler: [(Sandik(3, SandikDurum.Kapandi), 1)])));
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.SevkEdildi, 2, 2),
            Belirle(Satir(2, grid, icerikler: [(Sandik(1, SandikDurum.Sevkedildi), 2)])));
        Assert.Equal("-", UrunSevkDurumuKurali.Etiket(null));
        Assert.Null(Belirle(Satir(0)));
    }

    /// <summary>
    /// Trafoyla kapanan adet 3K'dan beklenmez ve sevk edilmiş sayılmaz (trafonun sevki 3K'da tutulmaz): tamamı
    /// trafoyla kapanan satır "Trafoyla", kısmi trafoda kalan kısım kendi sandık sevkine göre değerlendirilir.
    /// </summary>
    [Fact]
    public void Trafo_AyriGosterilir_KalanKisimSandiktanDegerlendirilir()
    {
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.Trafoyla, 0, 0),
            Belirle(Satir(2, GridDurum.TrafoSevk, trafo: 2, icerikler: [(Sandik(1, SandikDurum.Sevkedildi), 0)])));
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.SevkEdildi, 3, 3),
            Belirle(Satir(5, GridDurum.TrafoSevk, trafo: 2, icerikler: [(Sandik(1, SandikDurum.Sevkedildi), 3)])));
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.SevkEdilmedi, 0, 3),
            Belirle(Satir(5, GridDurum.TrafoSevk, trafo: 2, icerikler: [(Sandik(3, SandikDurum.Kapandi), 3)])));
        Assert.Equal("Trafoyla", UrunSevkDurumuKurali.Etiket(UrunSevkDurumu.Trafoyla));
    }

    /// <summary>
    /// Sahaya aktarılan kısım saha satırının fiziksel izinden okunur: saha sandığı sevkli olsa da içine konmamış
    /// ürün gitmemiştir; saha satırı iptal/kapandıysa o kısım beklenmez; kısmi aktarım normal kısımla toplanır.
    /// </summary>
    [Fact]
    public void SahayaAktarilanKisim_SahaSatirininFizikselIzindenOkunur()
    {
        // Normal sandık sevkli ama ürün sahaya aktarıldığı için içine konmamış.
        var aktarilmis = Satir(2, icerikler: [(Sandik(1, SandikDurum.Sevkedildi), 0)]);

        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.SevkEdildi, 2, 2),
            Belirle(aktarilmis, new SahaAktarimParcasi(2, SevkliSandigaKonulan: 2, Trafo: 0, IptalVeyaKapandi: false)));
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.SevkEdilmedi, 0, 2),
            Belirle(aktarilmis, new SahaAktarimParcasi(2, 0, 0, false)));
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.KismiSevk, 1, 2),
            Belirle(aktarilmis, new SahaAktarimParcasi(2, 1, 0, false)));
        // Saha satırı iptal/kapandı ve hiçbir şey gitmedi: beklenen yok.
        Assert.Null(Belirle(aktarilmis, new SahaAktarimParcasi(2, 0, 0, true)));
        // Sahada trafoyla kapanan kısım trafo sayılır.
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.Trafoyla, 0, 0),
            Belirle(aktarilmis, new SahaAktarimParcasi(2, 0, 2, false)));
        // Saha tarafındaki eski fazla iz kalem miktarını aşamaz.
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.KismiSevk, 1, 3),
            Belirle(Satir(3), new SahaAktarimParcasi(1, 5, 0, false)));

        var kismiAktarim = Satir(3, icerikler: [(Sandik(1, SandikDurum.Sevkedildi), 1)]);
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.SevkEdildi, 3, 3),
            Belirle(kismiAktarim, new SahaAktarimParcasi(2, 2, 0, false)));
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.KismiSevk, 1, 3),
            Belirle(kismiAktarim, new SahaAktarimParcasi(2, 0, 0, false)));
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.SevkEdildi, 1, 1),
            Belirle(kismiAktarim, new SahaAktarimParcasi(2, 0, 0, true)));
    }

    [Fact]
    public void EtiketOzetVeSandikDurumu()
    {
        Assert.Equal("Sevk Edildi", UrunSevkDurumuKurali.Etiket(UrunSevkDurumu.SevkEdildi));
        Assert.Equal("Kısmi Sevk", UrunSevkDurumuKurali.Etiket(UrunSevkDurumu.KismiSevk));
        Assert.Equal("Sevk Edilmedi", UrunSevkDurumuKurali.Etiket(UrunSevkDurumu.SevkEdilmedi));
        Assert.Equal(UrunSevkDurumu.SevkEdildi, UrunSevkDurumuKurali.SandikDurumu(Sandik(1, SandikDurum.Sevkedildi)));
        Assert.Equal(UrunSevkDurumu.SevkEdilmedi, UrunSevkDurumuKurali.SandikDurumu(Sandik(2, SandikDurum.Kapandi)));
        Assert.Equal("2 sevk edildi / 1 kısmi sevk / 1 sevk edilmedi / 1 trafoyla", UrunSevkDurumuKurali.OzetMetni(
        [
            new UrunSevkSonucu(UrunSevkDurumu.SevkEdildi, 1, 1), new UrunSevkSonucu(UrunSevkDurumu.SevkEdildi, 2, 2),
            new UrunSevkSonucu(UrunSevkDurumu.KismiSevk, 1, 2), new UrunSevkSonucu(UrunSevkDurumu.SevkEdilmedi, 0, 1),
            new UrunSevkSonucu(UrunSevkDurumu.Trafoyla, 0, 0), null
        ]));
    }
}
