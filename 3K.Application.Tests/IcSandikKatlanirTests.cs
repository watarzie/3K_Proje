using _3K.Application.Features.AmbalajIslemleri;
using _3K.Application.Features.FinansIslemleri.Validators;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Tests;

/// <summary>
/// "İç Sandık Katlanır" (2026-11-23): ayrı sandık cinsi, yalnız iç sandıkta; "Katlanır Sandık"ın elle fiyat kuralı
/// uygulanmaz, m³ hesaplanmaz; tarife eşleşmesi iç sandık iş türünde, kayıtlı tipe bağlı ve adet birimli üründe olur.
/// Servis/veritabanı akışı: <see cref="FinansPostgresTests.IcSandikKatlanir_TipinAdetTarifesiyleHesaplanir_KatlanirElleKalir"/>.
/// </summary>
public sealed class IcSandikKatlanirTests
{
    [Fact]
    public void Sandik_tipi_metni_ile_cinsi_birbirine_doner_ve_gecerli_tiptir()
    {
        Assert.Equal(AmbalajSandikCinsi.IcKatlanir, AmbalajPlanlamaYardimcisi.SandikCinsiCoz("İç Sandık Katlanır"));
        Assert.Equal("İç Sandık Katlanır", AmbalajPlanlamaYardimcisi.SandikTipiMetni(AmbalajSandikCinsi.IcKatlanir));
        Assert.Equal("İç Sandık Katlanır", AmbalajUretimYardimcilari.CinsMetni(AmbalajSandikCinsi.IcKatlanir, null));
        Assert.Equal("İç Sandık Katlanır", FinansEtiketleri.SandikCinsi(AmbalajSandikCinsi.IcKatlanir));
        Assert.True(AmbalajPlanlamaYardimcisi.GecerliSandikTipi("İç Sandık Katlanır"));
        // Mevcut katlanır değişmez.
        Assert.Equal(AmbalajSandikCinsi.Katlanir, AmbalajPlanlamaYardimcisi.SandikCinsiCoz("Katlanır Sandık"));
    }

    [Theory]
    [InlineData(AmbalajSandikTuru.Ic, true)]
    [InlineData(AmbalajSandikTuru.Saha, false)]
    [InlineData(AmbalajSandikTuru.Yedek, false)]
    [InlineData(AmbalajSandikTuru.Ilave, false)]
    [InlineData(AmbalajSandikTuru.Normal, false)]
    public void Ic_sandik_katlanir_yalniz_ic_sandikta_secilir(AmbalajSandikTuru tur, bool uygun)
    {
        Assert.Equal(uygun, IcSandikKatlanirKurali.TurUygun(AmbalajSandikCinsi.IcKatlanir, tur));
        Assert.True(IcSandikKatlanirKurali.TurUygun(AmbalajSandikCinsi.Katlanir, tur));
    }

    [Theory]
    [InlineData(AmbalajSandikCinsi.IcKatlanir, null, true)]
    [InlineData(null, " İç Sandık Katlanır ", true)]
    [InlineData(AmbalajSandikCinsi.Katlanir, null, false)]
    [InlineData(null, "Katlanır Sandık", false)]
    [InlineData(AmbalajSandikCinsi.AhsapKapali, "İç Sandık Katlanır", false)]
    public void Ic_sandik_katlanir_elle_fiyat_kuralina_girmez(AmbalajSandikCinsi? cinsi, string? tipi, bool icKatlanir)
    {
        Assert.Equal(icKatlanir, IcSandikKatlanirKurali.IcKatlanir(cinsi, tipi));
        if (icKatlanir) Assert.False(FinansKatlanirKurali.Katlanir(cinsi, tipi));
    }

    [Fact]
    public void Ic_sandik_katlanirda_m3_hesaplanmaz_malzemesi_yoktur()
    {
        Assert.False(AmbalajUretimPolitikasi.M3HesaplanabilirMi(AmbalajSandikCinsi.IcKatlanir));
        Assert.Equal(FinansSandikMalzemesi.Bilinmiyor, FinansHacimKurallari.Malzeme(AmbalajSandikCinsi.IcKatlanir, null));
        Assert.False(FinansHacimKurallari.ElleGirilebilir(AmbalajSandikCinsi.IcKatlanir, null, false));
    }

    [Fact]
    public void Tutar_adet_kere_tipin_tarife_fiyatidir()
    {
        var kayit = new FinansIsKaydi
        {
            SandikCinsi = AmbalajSandikCinsi.IcKatlanir, Adet = 3, IcSandikSablonId = 4,
            FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.Adet, BirimFiyatSnapshot = 120m
        };

        Assert.Equal(360m, FinansTutarKurallari.IsNet(kayit));
    }

    /// <summary>Eşleşme yalnız işin kendi tipine bağlı, malzemesiz ve adet birimli üründe uygundur.</summary>
    [Theory]
    [InlineData(4, 4, FinansFiyatlandirmaBirimi.Adet, false, true)]
    [InlineData(4, null, FinansFiyatlandirmaBirimi.Adet, false, false)]
    [InlineData(4, 5, FinansFiyatlandirmaBirimi.Adet, false, false)]
    [InlineData(null, null, FinansFiyatlandirmaBirimi.Adet, false, false)]
    [InlineData(4, 4, FinansFiyatlandirmaBirimi.Metrekup, false, false)]
    [InlineData(4, 4, FinansFiyatlandirmaBirimi.Adet, true, false)]
    public void Tarife_eslesmesi_kendi_tipine_bagli_adet_urunu_olmalidir(
        int? isSablonu, int? eslesmeSablonu, FinansFiyatlandirmaBirimi birim, bool malzemeli, bool uygun)
    {
        var eslesme = new FinansUrunEslesmesi
        {
            IcSandikSablonId = eslesmeSablonu, Malzeme = malzemeli ? FinansSandikMalzemesi.Ahsap : null,
            FinansUrun = new FinansUrun { FiyatlandirmaBirimi = birim }
        };
        var kayit = new FinansIsKaydi { SandikCinsi = AmbalajSandikCinsi.IcKatlanir, IcSandikSablonId = isSablonu };

        Assert.Equal(uygun, IcSandikKatlanirKurali.EslesmeUygun(eslesme, kayit));
    }

    /// <summary>"İç Sandık Katlanır" tipli eşleşme iç sandık iş türünde ve kayıtlı tipe bağlı olmalı; pasif eşleşme serbesttir.</summary>
    [Theory]
    [InlineData(FinansIsTuru.IcSandik, 4, true, true)]
    [InlineData(FinansIsTuru.IcSandik, null, true, false)]
    [InlineData(FinansIsTuru.SahaSandigi, 4, true, false)]
    [InlineData(FinansIsTuru.SahaSandigi, null, false, true)]
    public void Ic_sandik_katlanir_tarife_eslesmesi_ic_sandik_tipine_baglanir(FinansIsTuru isTuru, int? sablonId, bool aktif, bool gecerli)
    {
        var model = new FinansUrunKaydetModel("ICK", "İç katlanır", FinansFiyatlandirmaBirimi.Adet, true, 0,
            [new FinansUrunEslesmesiModel(null, isTuru, null, IcSandikKatlanirKurali.SandikTipiAdi, sablonId, Aktif: aktif)], 120m, "EUR", 20);

        var sonuc = new FinansUrunKaydetModelValidator().Validate(model);

        Assert.Equal(gecerli, sonuc.IsValid);
        if (!gecerli) Assert.Contains(sonuc.Errors, x => x.ErrorMessage == IcSandikKatlanirKurali.EslesmeMesaji);
    }
}
