using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Tests;

/// <summary>
/// R-07: satır yalnız kendi projesinin sevkiyatıyla değerlendirilir. Grid İptal ürün sevk edilmez; Grid Kapandı ürün
/// Grid tarafında sevk edilmiştir; diğerlerinde giden adet projenin sevkli sandığına fiilen konan, 3K'nın teslim
/// aldığı adettir. Bir kısmı gittiyse Eksik Sevk, hiç gitmediyse Sevk Edilmedi; trafoyla kapanan ayrı gösterilir.
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
            satir.SandikIcerikleri.Add(new SandikIcerik { Sandik = sandik, SandikId = sandik.Id, KonulanAdet = konulan });
        return satir;
    }

    private static UrunSevkSonucu? Belirle(CekiSatiri satir, Sandik? tahsissizSandik = null) =>
        UrunSevkDurumuKurali.Belirle(satir, ProjeId, tahsissizSandik);

    /// <summary>Kullanıcının örneği: 40'ın 20'si gelip sevk edildiyse Eksik Sevk (20/40).</summary>
    [Fact]
    public void SevkliSandigaKonanAdet_TamSevkEdildi_BirKismiEksikSevk()
    {
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.SevkEdildi, 2, 2),
            Belirle(Satir(2, icerikler: [(Sandik(1, SandikDurum.Sevkedildi), 2)])));
        var eksikGelen = Satir(40, GridDurum.EksikGeldi, icerikler: [(Sandik(1, SandikDurum.Sevkedildi), 20)]);
        eksikGelen.GelenMiktar = 20;
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.EksikSevk, 20, 40), Belirle(eksikGelen));
        // Bölünmüş satır: sevkli sandıktaki 2 gitti, elde kalan kapalı sandıktaki 3 gitmedi.
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.EksikSevk, 2, 5),
            Belirle(Satir(5, icerikler: [(Sandik(1, SandikDurum.Sevkedildi), 2), (Sandik(2, SandikDurum.Kapandi), 3)])));
    }

    /// <summary>
    /// Hiç gelmeyen, sevkli sandığa atanmış ama içine konmamış (ör. sahaya aktarılmış), tamamlanmış ama sandığı elde
    /// kalan ve sandığı olmayan ürün bu projenin sevkiyatıyla gitmemiştir; başka projenin sevkli sandığı sayılmaz.
    /// </summary>
    [Fact]
    public void GelmeyenVeyaSandigiGitmeyen_SevkEdilmedi()
    {
        var konmamis = Satir(40, GridDurum.Gelmedi, icerikler: [(Sandik(1, SandikDurum.Sevkedildi), 0)]);
        konmamis.GelenMiktar = 0;
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.SevkEdilmedi, 0, 40), Belirle(konmamis));

        var eldeTamamlanan = Satir(2, icerikler: [(Sandik(3, SandikDurum.Kapandi), 2)]);
        Assert.Equal(0, eldeTamamlanan.KalanMiktar);
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.SevkEdilmedi, 0, 2), Belirle(eldeTamamlanan));

        Assert.Equal(UrunSevkDurumu.SevkEdilmedi, Belirle(Satir(1))?.Durum);
        Assert.Equal(UrunSevkDurumu.SevkEdilmedi,
            Belirle(Satir(1, icerikler: [(Sandik(9, SandikDurum.Sevkedildi, projeId: 99), 1)]))?.Durum);
    }

    /// <summary>
    /// Giden adet 3K'nın teslim aldığı adetle sınırlıdır. Grid'in gelen/sevk beyanı (manuel eklemenin sandığa
    /// kendiliğinden yazdığı konan adet) 3K teslimi değildir (canlı PA675-01 #9999 FCT00685015). Stoktan, projeden ve
    /// tedarikçiden karşılanan teslim sayılır; projeye gönderilen düşülür.
    /// </summary>
    [Fact]
    public void YalnizUcKTeslimiSayilir_GridBeyaniSayilmaz()
    {
        var gridBeyani = Satir(1, GridDurum.TamGeldi, icerikler: [(Sandik(17, SandikDurum.Sevkedildi), 1)]);
        gridBeyani.GelenMiktar = 0;
        gridBeyani.GridGelenAdet = 1;
        gridBeyani.GridSevkMiktari = 1;
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.SevkEdilmedi, 0, 1), Belirle(gridBeyani));

        var kismiTeslim = Satir(2, GridDurum.EksikGeldi, icerikler: [(Sandik(1, SandikDurum.Sevkedildi), 2)]);
        kismiTeslim.GelenMiktar = 1;
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.EksikSevk, 1, 2), Belirle(kismiTeslim));

        var stoktan = Satir(3, icerikler: [(Sandik(1, SandikDurum.Sevkedildi), 3)]);
        stoktan.GelenMiktar = 0;
        stoktan.StokKarsilanan = 1;
        stoktan.ProjeKarsilanan = 1;
        stoktan.TedarikciKarsilanan = 1;
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.SevkEdildi, 3, 3), Belirle(stoktan));

        var gonderilen = Satir(3, icerikler: [(Sandik(1, SandikDurum.Sevkedildi), 3)]);
        gonderilen.ProjeGonderilen = 1;
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.EksikSevk, 2, 3), Belirle(gonderilen));
    }

    /// <summary>Grid İptal ürün sevk edilmez; sandık izinden bağımsızdır (istenen adedi 0'a çekilmiş satır dahil).</summary>
    [Theory]
    [InlineData(GridDurum.Iptal)]
    [InlineData(GridDurum.IptalEdildi)]
    public void GridIptal_SevkEdilmedi(GridDurum grid)
    {
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.SevkEdilmedi, 0, 1),
            Belirle(Satir(1, grid, icerikler: [(Sandik(1, SandikDurum.Sevkedildi), 0)])));
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.SevkEdilmedi, 0, 2),
            Belirle(Satir(2, grid, icerikler: [(Sandik(1, SandikDurum.Sevkedildi), 2)])));
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.SevkEdilmedi, 0, 0),
            Belirle(Satir(0, grid, icerikler: [(Sandik(1, SandikDurum.Sevkedildi), 0)])));
    }

    /// <summary>Grid Kapandı ürün Grid tarafında kapatılıp orada sevk edilmiştir: Sevk Edildi (Grid Kapandı).</summary>
    [Fact]
    public void GridKapandi_GridTarafindaSevkEdildi()
    {
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.GridKapandi, 1, 1),
            Belirle(Satir(1, GridDurum.GridKapandi, icerikler: [(Sandik(1, SandikDurum.Sevkedildi), 0)])));
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.GridKapandi, 4, 4),
            Belirle(Satir(4, GridDurum.GridKapandi, icerikler: [(Sandik(3, SandikDurum.Kapandi), 4)])));
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.GridKapandi, 0, 0), Belirle(Satir(0, GridDurum.GridKapandi)));
        Assert.Equal("Sevk Edildi (Grid Kapandı)", UrunSevkDurumuKurali.Etiket(UrunSevkDurumu.GridKapandi));
        // Grid kararı ve istenen adedi olmayan satırın sevk durumu yoktur.
        Assert.Null(Belirle(Satir(0)));
        Assert.Equal("-", UrunSevkDurumuKurali.Etiket(null));
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
        // Kısmi trafo + kısmi sevk: payda trafo düşülmüş beklenen adettir (10 − 4 = 6).
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.EksikSevk, 3, 6),
            Belirle(Satir(10, GridDurum.TrafoSevk, trafo: 4,
                icerikler: [(Sandik(1, SandikDurum.Sevkedildi), 3), (Sandik(2, SandikDurum.Kapandi), 3)])));
        Assert.Equal("Trafoyla", UrunSevkDurumuKurali.Etiket(UrunSevkDurumu.Trafoyla));
    }

    /// <summary>
    /// Hiç sandık içeriği olmayan eski satır, sistemin etkin içerik kuralıyla sandık numarasından eşlenen sandıkta
    /// 3K'nın teslim aldığı adetle sayılır (canlı PA600-50 sıra 73/74); içeriği olan satırda bu eşleme kullanılmaz.
    /// </summary>
    [Fact]
    public void TahsissizSatir_SandikNumarasiylaEslesenSandiktanOkunur()
    {
        var sevkli = Sandik(36, SandikDurum.Sevkedildi);
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.SevkEdildi, 4, 4), Belirle(Satir(4), sevkli));
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.SevkEdilmedi, 0, 4), Belirle(Satir(4), Sandik(37, SandikDurum.Kapandi)));
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.SevkEdilmedi, 0, 4),
            Belirle(Satir(4), Sandik(38, SandikDurum.Sevkedildi, projeId: 99)));

        var kismiTeslim = Satir(4);
        kismiTeslim.GelenMiktar = 2;
        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.EksikSevk, 2, 4), Belirle(kismiTeslim, sevkli));

        Assert.Equal(new UrunSevkSonucu(UrunSevkDurumu.SevkEdilmedi, 0, 4),
            Belirle(Satir(4, icerikler: [(Sandik(3, SandikDurum.Kapandi), 4)]), sevkli));
    }

    [Fact]
    public void EtiketOzetVeSandikDurumu()
    {
        Assert.Equal("Sevk Edildi", UrunSevkDurumuKurali.Etiket(UrunSevkDurumu.SevkEdildi));
        Assert.Equal("Eksik Sevk", UrunSevkDurumuKurali.Etiket(UrunSevkDurumu.EksikSevk));
        Assert.Equal("Sevk Edilmedi", UrunSevkDurumuKurali.Etiket(UrunSevkDurumu.SevkEdilmedi));
        Assert.Equal(UrunSevkDurumu.SevkEdildi, UrunSevkDurumuKurali.SandikDurumu(Sandik(1, SandikDurum.Sevkedildi)));
        Assert.Equal(UrunSevkDurumu.SevkEdilmedi, UrunSevkDurumuKurali.SandikDurumu(Sandik(2, SandikDurum.Kapandi)));
        Assert.Equal("Aktarıldı", UrunSevkDurumuKurali.SandikSahaAktarimMetni(7, 7));
        Assert.Equal("Aktarıldı", UrunSevkDurumuKurali.SandikSahaAktarimMetni(0, 0));
        Assert.Equal("Kısmen (1/7 ürün)", UrunSevkDurumuKurali.SandikSahaAktarimMetni(1, 7));
        Assert.Equal("2 sevk edildi / 1 sevk edildi (Grid Kapandı) / 1 eksik sevk / 1 sevk edilmedi / 1 trafoyla",
            UrunSevkDurumuKurali.OzetMetni(
            [
                new UrunSevkSonucu(UrunSevkDurumu.SevkEdildi, 1, 1), new UrunSevkSonucu(UrunSevkDurumu.SevkEdildi, 2, 2),
                new UrunSevkSonucu(UrunSevkDurumu.GridKapandi, 1, 1), new UrunSevkSonucu(UrunSevkDurumu.EksikSevk, 1, 2),
                new UrunSevkSonucu(UrunSevkDurumu.SevkEdilmedi, 0, 1), new UrunSevkSonucu(UrunSevkDurumu.Trafoyla, 0, 0), null
            ]));
    }
}
