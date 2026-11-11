using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Tests;

public sealed class NormalProjeSevkDurumHesaplayiciTests
{
    [Theory]
    [InlineData(24, 24, 3, 0, ProjeDurum.EksikSevkEdildi)]
    [InlineData(27, 27, 3, 0, ProjeDurum.EksikSevkEdildi)]
    [InlineData(24, 24, 4, 0, ProjeDurum.SevkEdildi)]
    [InlineData(24, 24, 3, 1, ProjeDurum.SevkEdildi)]
    [InlineData(24, 24, 2, 1, ProjeDurum.EksikSevkEdildi)]
    [InlineData(24, 23, 4, 0, ProjeDurum.EksikSevkEdildi)]
    [InlineData(24, 23, 3, 1, ProjeDurum.EksikSevkEdildi)]
    [InlineData(24, 0, 3, 1, ProjeDurum.SevkEdildi)]
    [InlineData(24, 0, 2, 1, ProjeDurum.EksikSevkEdildi)]
    public void FizikselVeUrunBazliSahaSevki_MevcutNormalProjeKurallariylaUyumludur(
        int sandikSayisi, int sevkSayisi, decimal gelen, decimal gerceklesenSaha, ProjeDurum beklenen)
    {
        var sandiklar = Sandiklar(sandikSayisi, sevkSayisi);
        var satir = new CekiSatiri { Id = 100, IstenenAdet = 4, GelenMiktar = gelen };

        var sonuc = Hesapla(sandiklar, [satir], sahaTamamlama: new() { [100] = gerceklesenSaha });

        Assert.Equal((int)beklenen, sonuc);
        Assert.Equal(gelen, satir.GelenMiktar);
        Assert.Equal(sevkSayisi, sandiklar.Count(s => s.DurumId == (int)SandikDurum.Sevkedildi));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void SandikBazliSahaSevki_TumFizikselOlmayanSandiklariKapatiyorsa_IstisnaKorunur(int fizikselSevk)
    {
        var sandiklar = Sandiklar(3, fizikselSevk);
        var sahadanSevk = sandiklar.Where(s => s.DurumId != (int)SandikDurum.Sevkedildi)
            .Select(s => s.Id).ToHashSet();

        var sonuc = Hesapla(sandiklar, [new CekiSatiri { Id = 100, IstenenAdet = 4 }], sahadanSevk);

        Assert.Equal((int)ProjeDurum.SevkEdildi, sonuc);
    }

    [Fact]
    public void SandikBazliSahaSevki_BirSandigiAcikBirakiyorsa_TamSevkSayilmaz()
    {
        var sonuc = Hesapla(Sandiklar(3, 1),
            [new CekiSatiri { Id = 100, IstenenAdet = 4, GelenMiktar = 4 }], [2]);

        Assert.Equal((int)ProjeDurum.EksikSevkEdildi, sonuc);
    }

    [Fact]
    public void FizikselOlarakDaSevkEdilenSandiginSahaBagi_EksikUrunuAtlatmaz()
    {
        var sonuc = Hesapla(Sandiklar(2, 2), [new CekiSatiri { Id = 100, IstenenAdet = 4 }], [2]);

        Assert.Equal((int)ProjeDurum.EksikSevkEdildi, sonuc);
    }

    [Theory]
    [InlineData(GridDurum.Iptal)]
    [InlineData(GridDurum.GridKapandi)]
    public void IptalVeGridKapandiSatirlari_SahteEksikUretmez(GridDurum durum)
    {
        var sonuc = Hesapla(Sandiklar(1, 1),
            [new CekiSatiri { Id = 100, IstenenAdet = 4, GridDurumuId = (int)durum }]);

        Assert.Equal((int)ProjeDurum.SevkEdildi, sonuc);
    }

    [Fact]
    public void ProjedenGonderilenVeHataliUrun_MevcutKalanKurallariylaHesaplanir()
    {
        var satir = new CekiSatiri { Id = 100, IstenenAdet = 4, GelenMiktar = 4, ProjeGonderilen = 1 };
        Assert.Equal((int)ProjeDurum.EksikSevkEdildi, Hesapla(Sandiklar(1, 1), [satir]));

        satir.ProjeGonderilen = 0;
        satir.HataliMiktar = 1;
        Assert.Equal((int)ProjeDurum.EksikSevkEdildi, Hesapla(Sandiklar(1, 1), [satir]));
    }

    [Fact]
    public void OndalikEksik_YuvarlanarakKaybolmaz()
    {
        var satir = new CekiSatiri { Id = 100, IstenenAdet = 1.25m, GelenMiktar = 1.24m };
        Assert.Equal((int)ProjeDurum.EksikSevkEdildi, Hesapla(Sandiklar(1, 1), [satir]));
        Assert.Equal((int)ProjeDurum.SevkEdildi,
            Hesapla(Sandiklar(1, 1), [satir], sahaTamamlama: new() { [100] = .01m }));
    }

    [Fact]
    public void TuretilmisSatira_AyniSahaTamamlamasiIkinciKezDusulmez()
    {
        var sonuc = Hesapla(Sandiklar(1, 1),
            [new CekiSatiri { Id = 100, KaynakCekiSatiriId = 90, IstenenAdet = 4 }],
            sahaTamamlama: new() { [100] = 4 });

        Assert.Equal((int)ProjeDurum.EksikSevkEdildi, sonuc);
    }

    [Fact]
    public void FizikselVeSahaSevkiYoksa_UrunTamamlanmasiSevkDurumuUretmez()
    {
        var sonuc = Hesapla(Sandiklar(2, 0),
            [new CekiSatiri { Id = 100, IstenenAdet = 4, GelenMiktar = 4 }]);

        Assert.Equal((int)ProjeDurum.Hazirlaniyor, sonuc);
    }

    [Fact]
    public void HenuzKaydedilmemisSandikSevkGeriAlma_DurumHesabinaYansir()
    {
        var sandiklar = Sandiklar(2, 2);
        var satirlar = new[] { new CekiSatiri { Id = 100, IstenenAdet = 4, GelenMiktar = 4 } };
        Assert.Equal((int)ProjeDurum.SevkEdildi, Hesapla(sandiklar, satirlar));

        sandiklar[1].DurumId = (int)SandikDurum.Kapandi;
        Assert.Equal((int)ProjeDurum.EksikSevkEdildi, Hesapla(sandiklar, satirlar));

        sandiklar[0].DurumId = (int)SandikDurum.Kapandi;
        Assert.Equal((int)ProjeDurum.Hazirlaniyor, Hesapla(sandiklar, satirlar));
    }

    // S-27 (PA675-01, PA677-03): bütün sandıklar sevkli, tek açık ürün sahada Grid İptal.
    [Theory]
    [InlineData(0, 3, ProjeDurum.SevkEdildi)]
    [InlineData(1, 2, ProjeDurum.SevkEdildi)]
    [InlineData(0, 2, ProjeDurum.EksikSevkEdildi)]
    [InlineData(0, 0, ProjeDurum.EksikSevkEdildi)]
    public void SahadaIptalEdilenUrun_SevkDurumundaKapanmisSayilir(
        decimal gerceklesenSaha, decimal sahaIptalKapanan, ProjeDurum beklenen)
    {
        var satir = new CekiSatiri { Id = 100, IstenenAdet = 3 };

        var sonuc = Hesapla(Sandiklar(2, 2), [satir],
            sahaTamamlama: new() { [100] = gerceklesenSaha }, sahaIptalKapanan: new() { [100] = sahaIptalKapanan });

        Assert.Equal((int)beklenen, sonuc);
        Assert.Equal(0, satir.GelenMiktar);
    }

    [Fact]
    public void SahaIptali_SevkKaydiYerineGecmez_HicSevkYoksaProjeSevkEdilmisSayilmaz()
    {
        var sonuc = Hesapla(Sandiklar(2, 0),
            [new CekiSatiri { Id = 100, IstenenAdet = 3 }], sahaIptalKapanan: new() { [100] = 3 });

        Assert.Equal((int)ProjeDurum.Hazirlaniyor, sonuc);
    }

    [Fact]
    public void SahaIptali_NormalSandikEldeyken_TamSevkUretmez()
    {
        var sonuc = Hesapla(Sandiklar(2, 1),
            [new CekiSatiri { Id = 100, IstenenAdet = 3 }], sahaIptalKapanan: new() { [100] = 3 });

        Assert.Equal((int)ProjeDurum.EksikSevkEdildi, sonuc);
    }

    [Theory]
    [InlineData(3, 0, 3)]
    [InlineData(3, 1, 2)]
    [InlineData(3, 3, 0)]
    [InlineData(3, 5, 0)]
    [InlineData(-1, 0, 0)]
    [InlineData(2.5, 0.25, 2.25)]
    public void KapananMiktar_GerceklesenIkinciKezDusulmez(decimal aktarilan, decimal gerceklesen, decimal beklenen) =>
        Assert.Equal(beklenen, SahaIptalSevkKapanisKurali.KapananMiktar(aktarilan, gerceklesen));

    [Fact]
    public void SevkKapanisHaritasi_IkiHaritayiToplar_GirdileriDegistirmez()
    {
        var gerceklesen = new Dictionary<int, decimal> { [1] = 1, [2] = 2 };
        var iptal = new Dictionary<int, decimal> { [2] = 1, [3] = 4 };

        var sonuc = SahaIptalSevkKapanisKurali.SevkKapanisHaritasi(gerceklesen, iptal);

        Assert.Equal(new Dictionary<int, decimal> { [1] = 1, [2] = 3, [3] = 4 }, sonuc);
        Assert.Equal(2, gerceklesen[2]);
        Assert.False(gerceklesen.ContainsKey(3));
    }

    private static int Hesapla(List<Sandik> sandiklar, IReadOnlyCollection<CekiSatiri> satirlar,
        HashSet<int>? sahadanSevk = null, Dictionary<int, decimal>? sahaTamamlama = null,
        Dictionary<int, decimal>? sahaIptalKapanan = null) =>
        NormalProjeSevkDurumHesaplayici.Hesapla((int)ProjeDurum.SevkEdildi, sandiklar, satirlar,
            sahadanSevk ?? [], sahaTamamlama ?? [], sahaIptalKapanan ?? []);

    private static List<Sandik> Sandiklar(int toplam, int sevkEdilen) =>
        Enumerable.Range(1, toplam).Select(id => new Sandik
        {
            Id = id,
            ProjeId = 10,
            DurumId = (int)(id <= sevkEdilen ? SandikDurum.Sevkedildi : SandikDurum.Kapandi)
        }).ToList();
}
