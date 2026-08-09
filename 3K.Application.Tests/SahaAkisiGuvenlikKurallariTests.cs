using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Application.Tests;

public class SahaAkisiGuvenlikKurallariTests
{
    [Fact]
    public void TemizTekTahsis_SandikTahsisMiktariylaAktarilir()
    {
        var kurgu = TemizKurgu(tahsisMiktari: 4, istenenAdet: 10);

        var sonuc = Dogrula(kurgu);

        Assert.True(sonuc.Basarili);
        var aday = Assert.Single(sonuc.Adaylar);
        Assert.Equal(4, aday.Miktar);
        Assert.Empty(sonuc.Engeller);
    }

    [Fact]
    public void LegacyTekTahsis_TahsisSifirVeEksikIstenenKadar_IstenenMiktarlaAktarilir()
    {
        var kurgu = TemizKurgu(tahsisMiktari: 0, istenenAdet: 10);
        kurgu.Icerik.EksikAdet = 10;

        var sonuc = Dogrula(kurgu);

        Assert.True(sonuc.Basarili);
        var aday = Assert.Single(sonuc.Adaylar);
        Assert.Equal(10, aday.Miktar);
        Assert.Empty(sonuc.Engeller);
    }

    [Fact]
    public void LegacyTekGercekTahsis_TumMiktarlariSifirken_IstenenMiktarlaAktarilir()
    {
        var kurgu = TemizKurgu(tahsisMiktari: 0, istenenAdet: 10);

        var sonuc = Dogrula(kurgu);

        Assert.True(sonuc.Basarili);
        var aday = Assert.Single(sonuc.Adaylar);
        Assert.Equal(10, aday.Miktar);
        Assert.Empty(sonuc.Engeller);
    }

    [Fact]
    public void LegacyTekTahsis_TahsisSifirVeGolgeMiktarIstenenKadar_IstenenMiktarlaAktarilir()
    {
        var kurgu = TemizKurgu(tahsisMiktari: 0, istenenAdet: 10);
        kurgu.Icerik.Miktar = 10;

        var sonuc = Dogrula(kurgu);

        Assert.True(sonuc.Basarili);
        var aday = Assert.Single(sonuc.Adaylar);
        Assert.Equal(10, aday.Miktar);
        Assert.Empty(sonuc.Engeller);
    }

    [Fact]
    public void TamamenSifirGercekKayit_EtkinDegilAmaGuvenlikIncelemesineDahilEdilir()
    {
        var icerik = new SandikIcerik
        {
            Id = 201,
            CekiSatiriId = 101
        };

        Assert.False(SandikBazliSahaAktarimGuvenlikKural.EtkinIcerikMi(icerik));
        Assert.True(SandikBazliSahaAktarimGuvenlikKural.IncelenecekIcerikMi(icerik));
    }

    [Fact]
    public void LegacyTekTahsis_EksikEtkinTahsisleEsitDegilse_IslenmisSayilir()
    {
        var kurgu = TemizKurgu(tahsisMiktari: 0, istenenAdet: 10);
        kurgu.Icerik.EksikAdet = 5;

        var sonuc = Dogrula(kurgu);

        Assert.False(sonuc.Basarili);
        Assert.Empty(sonuc.Adaylar);
        Assert.Contains(sonuc.Engeller, e => e.Contains("işlenmiş", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void LegacyKaydinIkinciTamamenSifirGercekTahsisGolgesiVarsa_FallbackUygulanmaz()
    {
        var kurgu = TemizKurgu(tahsisMiktari: 0, istenenAdet: 10);
        var ikinciIcerik = new SandikIcerik
        {
            Id = 202,
            SandikId = 2,
            CekiSatiriId = kurgu.Satir.Id,
            CekiSatiri = kurgu.Satir
        };
        kurgu.TumFizikselIcerikler.Add(ikinciIcerik);

        var sonuc = Dogrula(kurgu);

        Assert.False(sonuc.Basarili);
        Assert.Empty(sonuc.Adaylar);
        Assert.Contains(sonuc.Engeller, e => e.Contains("bölünmüş", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(sonuc.Engeller, e => e.Contains("sıfır veya negatif", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(-1)]
    public void LegacyTekTahsis_GolgeMiktarKismiVeyaNegatifse_FallbackUygulanmaz(decimal golgeMiktar)
    {
        var kurgu = TemizKurgu(tahsisMiktari: 0, istenenAdet: 10);
        kurgu.Icerik.Miktar = golgeMiktar;

        var sonuc = Dogrula(kurgu);

        Assert.False(sonuc.Basarili);
        Assert.Empty(sonuc.Adaylar);
        Assert.Contains(sonuc.Engeller, e => e.Contains("sıfır veya negatif", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void NegatifTahsisliTekKayit_LegacyFallbackYerineBozukVeriOlarakEngellenir()
    {
        var kurgu = TemizKurgu(tahsisMiktari: -1, istenenAdet: 10);

        var sonuc = Dogrula(kurgu);

        Assert.False(sonuc.Basarili);
        Assert.Empty(sonuc.Adaylar);
        Assert.Contains(sonuc.Engeller, e => e.Contains("sıfır veya negatif", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void IstenenMiktariSifirLegacyKayit_EtkinTahsisUretmez()
    {
        var kurgu = TemizKurgu(tahsisMiktari: 0, istenenAdet: 0);

        var sonuc = Dogrula(kurgu);

        Assert.False(sonuc.Basarili);
        Assert.Empty(sonuc.Adaylar);
        Assert.Contains(sonuc.Engeller, e => e.Contains("sıfır veya negatif", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SentetikLegacyKayit_GercekFizikselTahsisOlmadanAktarilmaz()
    {
        var kurgu = TemizKurgu(tahsisMiktari: 0, istenenAdet: 10);
        kurgu.Icerik.Id = -kurgu.Satir.Id;
        kurgu.Icerik.TahsisMiktari = 10;
        kurgu.Icerik.EksikAdet = 10;

        var sonuc = Dogrula(kurgu);

        Assert.False(sonuc.Basarili);
        Assert.Empty(sonuc.Adaylar);
        Assert.Contains(sonuc.Engeller, e => e.Contains("gerçek SandikIcerik", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void LegacyTekTahsisIcinAktifSahaPlaniVarsa_IkinciAktarimEngellenir()
    {
        var kurgu = TemizKurgu(tahsisMiktari: 0, istenenAdet: 10);
        kurgu.Icerik.EksikAdet = 10;
        kurgu.AktifTamamlamaMap[kurgu.Satir.Id] = 10;

        var sonuc = Dogrula(kurgu);

        Assert.False(sonuc.Basarili);
        Assert.Empty(sonuc.Adaylar);
        Assert.Contains(sonuc.Engeller, e => e.Contains("aktif saha aktarımı", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GridSevkiBaslamisUrun_TumSandikAktariminiEngeller()
    {
        var kurgu = TemizKurgu();
        kurgu.Satir.GridDurumuId = (int)GridDurum.TamGeldi;
        kurgu.Satir.GridGelenAdet = 10;
        kurgu.Satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi;
        kurgu.Satir.GridSevkMiktari = 10;

        var sonuc = Dogrula(kurgu);

        Assert.False(sonuc.Basarili);
        Assert.Empty(sonuc.Adaylar);
        Assert.Contains(sonuc.Engeller, e => e.Contains("Grid", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UcKKabulBaslamisUrun_SessizceAtlanmakYerineTumAktarimiEngeller()
    {
        var kurgu = TemizKurgu();
        kurgu.Satir.UcKDurumuId = (int)UcKDurum.EksikGeldi;
        kurgu.Satir.UcKKarsilamaTipiId = (int)UcKDurum.EksikGeldi;
        kurgu.Satir.GelenMiktar = 1;
        kurgu.Icerik.KonulanAdet = 1;

        var sonuc = Dogrula(kurgu);

        Assert.False(sonuc.Basarili);
        Assert.Empty(sonuc.Adaylar);
        Assert.Contains(sonuc.Engeller, e => e.Contains("3K", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AyniUrunBirdenFazlaSandigaBolunmusse_AktarimEngellenir()
    {
        var kurgu = TemizKurgu(tahsisMiktari: 5);
        var ikinciIcerik = new SandikIcerik
        {
            Id = 202,
            SandikId = 2,
            CekiSatiriId = kurgu.Satir.Id,
            CekiSatiri = kurgu.Satir,
            TahsisMiktari = 5
        };
        kurgu.TumFizikselIcerikler.Add(ikinciIcerik);

        var sonuc = Dogrula(kurgu);

        Assert.False(sonuc.Basarili);
        Assert.Empty(sonuc.Adaylar);
        Assert.Contains(sonuc.Engeller, e => e.Contains("bölünmüş", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MevcutAktifSahaPlaniVarsa_IkinciAktarimEngellenir()
    {
        var kurgu = TemizKurgu();
        kurgu.AktifTamamlamaMap[kurgu.Satir.Id] = 2;

        var sonuc = Dogrula(kurgu);

        Assert.False(sonuc.Basarili);
        Assert.Empty(sonuc.Adaylar);
        Assert.Contains(sonuc.Engeller, e => e.Contains("aktif saha aktarımı", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SandiktakiTekBirUrunUygunDegilse_HicbirUrunAdayDonmez()
    {
        var kurgu = TemizKurgu(tahsisMiktari: 5);
        var ikinciSatir = YeniBaslangicSatiri(id: 102, istenenAdet: 5);
        ikinciSatir.KaliteDurumId = 1;
        var ikinciIcerik = new SandikIcerik
        {
            Id = 202,
            SandikId = kurgu.Sandik.Id,
            CekiSatiriId = ikinciSatir.Id,
            CekiSatiri = ikinciSatir,
            TahsisMiktari = 5
        };

        kurgu.EtkinIcerikler.Add(ikinciIcerik);
        kurgu.KaynakSatirlar[ikinciSatir.Id] = ikinciSatir;
        kurgu.TumFizikselIcerikler.Add(ikinciIcerik);

        var sonuc = Dogrula(kurgu);

        Assert.False(sonuc.Basarili);
        Assert.Empty(sonuc.Adaylar);
        Assert.Contains(sonuc.Engeller, e => e.Contains("kalite", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void KaliteVeyaSurecGormusSahaSatiri_IslemGormusSayilir()
    {
        var satir = YeniBaslangicSatiri();
        Assert.False(SahaAktarimGeriAlmaPolicy.IslemGormusMu(satir, Array.Empty<SandikIcerik>()));

        satir.KaliteDurumId = 1;
        Assert.True(SahaAktarimGeriAlmaPolicy.IslemGormusMu(satir, Array.Empty<SandikIcerik>()));

        satir.KaliteDurumId = null;
        satir.SurecDurumId = 1;
        Assert.True(SahaAktarimGeriAlmaPolicy.IslemGormusMu(satir, Array.Empty<SandikIcerik>()));
    }

    [Fact]
    public void UcKGeriAlmaSonrasiTahsisKadarEksik_BaslangicDurumuSayilir()
    {
        var satir = YeniBaslangicSatiri();
        var icerik = new SandikIcerik
        {
            TahsisMiktari = 10,
            KonulanAdet = 0,
            EksikAdet = 10
        };

        Assert.False(SahaAktarimGeriAlmaPolicy.IslemGormusMu(satir, new[] { icerik }));

        icerik.EksikAdet = 5;
        Assert.True(SahaAktarimGeriAlmaPolicy.IslemGormusMu(satir, new[] { icerik }));
    }

    [Fact]
    public void ManuelUrunKaliteVeyaSurecGormusse_SilinemezSayilir()
    {
        var satir = YeniBaslangicSatiri();
        Assert.False(ManuelUrunSilmeKurali.IslemGormusMu(satir));

        satir.KaliteDurumId = 1;
        Assert.True(ManuelUrunSilmeKurali.IslemGormusMu(satir));

        satir.KaliteDurumId = null;
        satir.SurecDurumId = 1;
        Assert.True(ManuelUrunSilmeKurali.IslemGormusMu(satir));

        satir.SurecDurumId = null;
        satir.GridDurumuId = (int)GridDurum.TamGeldi;
        Assert.True(ManuelUrunSilmeKurali.IslemGormusMu(satir));
    }

    [Theory]
    [InlineData(SandikDurum.Bos, false)]
    [InlineData(SandikDurum.Hazirlaniyor, false)]
    [InlineData(SandikDurum.Kapandi, true)]
    [InlineData(SandikDurum.Sevkedildi, false)]
    public void YalnizKapaliSandikNormalSevkeHazirdir(SandikDurum durum, bool beklenen)
    {
        var sandik = new Sandik { DurumId = (int)durum };

        Assert.Equal(beklenen, SandikSevkKilidiHelper.SandikSevkeHazirMi(sandik));
    }

    private static SandikBazliSahaAktarimDogrulamaSonucu Dogrula(Kurgu kurgu)
    {
        return SandikBazliSahaAktarimGuvenlikKural.Dogrula(
            new[] { kurgu.Sandik },
            new Dictionary<int, IReadOnlyCollection<SandikIcerik>>
            {
                [kurgu.Sandik.Id] = kurgu.EtkinIcerikler
            },
            kurgu.KaynakSatirlar,
            kurgu.TumFizikselIcerikler,
            kurgu.AktifTamamlamaMap);
    }

    private static Kurgu TemizKurgu(decimal tahsisMiktari = 10, decimal istenenAdet = 10)
    {
        var sandik = new Sandik { Id = 1, SandikNo = "S1", DurumId = (int)SandikDurum.Hazirlaniyor };
        var satir = YeniBaslangicSatiri(istenenAdet: istenenAdet);
        var icerik = new SandikIcerik
        {
            Id = 201,
            SandikId = sandik.Id,
            CekiSatiriId = satir.Id,
            CekiSatiri = satir,
            TahsisMiktari = tahsisMiktari
        };

        return new Kurgu(
            sandik,
            satir,
            icerik,
            new List<SandikIcerik> { icerik },
            new Dictionary<int, CekiSatiri> { [satir.Id] = satir },
            new List<SandikIcerik> { icerik },
            new Dictionary<int, decimal>());
    }

    private static CekiSatiri YeniBaslangicSatiri(int id = 101, decimal istenenAdet = 10)
    {
        return new CekiSatiri
        {
            Id = id,
            SiraNo = id,
            BarkodNo = $"B-{id}",
            Aciklama = $"Ürün {id}",
            IstenenAdet = istenenAdet,
            GridDurumuId = (int)GridDurum.Gelmedi,
            GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi,
            UcKDurumuId = (int)UcKDurum.Bekliyor,
            UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor
        };
    }

    private sealed record Kurgu(
        Sandik Sandik,
        CekiSatiri Satir,
        SandikIcerik Icerik,
        List<SandikIcerik> EtkinIcerikler,
        Dictionary<int, CekiSatiri> KaynakSatirlar,
        List<SandikIcerik> TumFizikselIcerikler,
        Dictionary<int, decimal> AktifTamamlamaMap);
}
