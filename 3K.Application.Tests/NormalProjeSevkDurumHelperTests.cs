using _3K.Core.Enums;
using _3K.Core.Helpers;

namespace _3K.Application.Tests;

public sealed class NormalProjeSevkDurumHelperTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    public void SevkKaydiYoksa_UrunTamamlanmasiTekBasinaSevkDurumuOlusturmaz(
        int toplamSandik,
        bool tumUrunlerTamamlandi)
    {
        var sonuc = NormalProjeSevkDurumHelper.Hesapla(
            toplamSandik,
            sevkEdilenSandik: 0,
            sahaSevkiyleTamamlamaVar: false,
            tumUrunlerSevkKapsamindaTamamlandi: tumUrunlerTamamlandi);

        Assert.Null(sonuc);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void KismiNormalSandikSevki_UrunBazliSahaTamamlamasiOlsaDaEksikSevkKalir(
        bool sahaSevkiyleTamamlamaVar,
        bool tumUrunlerTamamlandi)
    {
        var sonuc = NormalProjeSevkDurumHelper.Hesapla(
            toplamSandik: 3,
            sevkEdilenSandik: 2,
            sahaSevkiyleTamamlamaVar: sahaSevkiyleTamamlamaVar,
            tumUrunlerSevkKapsamindaTamamlandi: tumUrunlerTamamlandi);

        Assert.Equal((int)ProjeDurum.EksikSevkEdildi, sonuc);
    }

    [Theory]
    [InlineData(false, false, ProjeDurum.EksikSevkEdildi)]
    [InlineData(true, false, ProjeDurum.EksikSevkEdildi)]
    [InlineData(false, true, ProjeDurum.SevkEdildi)]
    [InlineData(true, true, ProjeDurum.SevkEdildi)]
    public void TumFizikselSandiklarSevkEdilseDe_UrunlerinSevkKapsamindakiTamamlanmasiEsastir(
        bool sahaSevkiyleTamamlamaVar,
        bool tumUrunlerTamamlandi,
        ProjeDurum beklenenDurum)
    {
        var sonuc = NormalProjeSevkDurumHelper.Hesapla(
            toplamSandik: 3,
            sevkEdilenSandik: 3,
            sahaSevkiyleTamamlamaVar: sahaSevkiyleTamamlamaVar,
            tumUrunlerSevkKapsamindaTamamlandi: tumUrunlerTamamlandi);

        Assert.Equal((int)beklenenDurum, sonuc);
    }

    [Theory]
    [InlineData(false, ProjeDurum.EksikSevkEdildi)]
    [InlineData(true, ProjeDurum.SevkEdildi)]
    public void YalnizSahaSevkiVarsa_MevcutUrunTamamlamaKuraliKorunur(
        bool tumUrunlerTamamlandi,
        ProjeDurum beklenenDurum)
    {
        var sonuc = NormalProjeSevkDurumHelper.Hesapla(
            toplamSandik: 3,
            sevkEdilenSandik: 0,
            sahaSevkiyleTamamlamaVar: true,
            tumUrunlerSevkKapsamindaTamamlandi: tumUrunlerTamamlandi);

        Assert.Equal((int)beklenenDurum, sonuc);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    public void DogrulanmisSandikBazliSahaSevki_TumKalanSandiklariKapatiyorsaOzelKuralKorunur(
        int fizikselSevkEdilenSandik,
        bool tumUrunlerTamamlandi)
    {
        // Bu bayrak, cagiran akisin tum kaynak satirlarin aktif sandik-bazli
        // defterde temsilini ve sevkiyat kapsamini dogruladigi anlamina gelir.
        // Yardimci metodun gorevi defteri yeniden yorumlamak degildir.
        var sonuc = NormalProjeSevkDurumHelper.Hesapla(
            toplamSandik: 3,
            sevkEdilenSandik: fizikselSevkEdilenSandik,
            sahaSevkiyleTamamlamaVar: true,
            tumUrunlerSevkKapsamindaTamamlandi: tumUrunlerTamamlandi,
            sahaSandiklariylaTumSandiklarEtkinSevkEdildi: true);

        Assert.Equal((int)ProjeDurum.SevkEdildi, sonuc);
    }
}
