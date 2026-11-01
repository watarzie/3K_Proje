using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Application.Features.FinansIslemleri.Validators;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Models;

namespace _3K.Application.Tests;

/// <summary>
/// Katlanır sandık elle fiyatlanır (2026-10-26): tarife, m³ ve ölçüden hesap yok; adet × birim fiyat veya net toplam.
/// Saf kural (<see cref="FinansKatlanirKurali"/>), hacim/malzeme ayrımı, tarife düzeltme koruması ve doğrulayıcılar.
/// Servis/veritabanı akışı: <see cref="FinansPostgresTests.KatlanirSandik_TarifeVeM3Hesaplanmaz_ElleFiyatPoVeFaturadaAynenKullanilir"/>.
/// </summary>
public sealed class FinansKatlanirFiyatTests
{
    [Theory]
    [InlineData(AmbalajSandikCinsi.Katlanir, null, true)]
    [InlineData(AmbalajSandikCinsi.Katlanir, "Kontrplak Sandık", true)]
    [InlineData(null, "Katlanır Sandık", true)]
    [InlineData(null, " katlanır sandık ", true)]
    [InlineData(AmbalajSandikCinsi.Kontrplak, "Katlanır Sandık", false)]
    [InlineData(AmbalajSandikCinsi.Kontrplak, null, false)]
    [InlineData(AmbalajSandikCinsi.AhsapKapali, null, false)]
    [InlineData(AmbalajSandikCinsi.Kafes, null, false)]
    [InlineData(null, null, false)]
    public void Katlanir_sandik_cinsinden_eski_kayitta_tipi_metninden_taninir(AmbalajSandikCinsi? cinsi, string? tipi, bool beklenen) =>
        Assert.Equal(beklenen, FinansKatlanirKurali.Katlanir(cinsi, tipi));

    [Theory]
    [InlineData(FinansFiyatlandirmaBirimi.Adet, true)]
    [InlineData(FinansFiyatlandirmaBirimi.ManuelToplam, true)]
    [InlineData(FinansFiyatlandirmaBirimi.Metrekup, false)]
    [InlineData(FinansFiyatlandirmaBirimi.SabitTutar, false)]
    public void Katlanir_elle_fiyat_adet_veya_net_toplamla_girilir(FinansFiyatlandirmaBirimi birim, bool gecerli)
    {
        Assert.Equal(gecerli, FinansKatlanirKurali.YontemGecerli(birim));
        if (gecerli) FinansKatlanirKurali.FiyatDogrula(birim, null);
        else Assert.Equal(409, Assert.Throws<IsKuraliException>(() => FinansKatlanirKurali.FiyatDogrula(birim, null)).StatusCode);
    }

    [Fact]
    public void Katlanir_elle_fiyat_bileseni_m3_satiri_icermez()
    {
        Assert.True(FinansKatlanirKurali.BilesenlerGecerli([new("Montaj", FinansFiyatlandirmaBirimi.Adet, 3m, 100m),
            new("Nakliye", FinansFiyatlandirmaBirimi.SabitTutar, 1m, 250m)]));
        Assert.False(FinansKatlanirKurali.BilesenlerGecerli([new("Kontra", FinansFiyatlandirmaBirimi.Metrekup, 2m, 300m)]));
        Assert.Throws<IsKuraliException>(() => FinansKatlanirKurali.FiyatDogrula(FinansFiyatlandirmaBirimi.ManuelToplam,
            [new("Ahşap", FinansFiyatlandirmaBirimi.Metrekup, 1m, 150m, FinansSandikMalzemesi.Ahsap)]));
    }

    /// <summary>
    /// Katlanırın malzemesi yoktur (ekran hacim girişini açmaz); yeni katlanır işte m³ 0 olduğundan pay (0, 0)'dır.
    /// Kontrplak gibi m³ ile fiyatlanmış eski katlanır kaydın m³'ü kontra payı olarak gösterilmeye devam eder.
    /// </summary>
    [Fact]
    public void Katlanir_sandigin_malzemesi_yoktur_eski_kaydin_m3_payi_korunur_kontrplak_degismez()
    {
        Assert.Equal(FinansSandikMalzemesi.Bilinmiyor, FinansHacimKurallari.Malzeme(AmbalajSandikCinsi.Katlanir, null));
        Assert.Equal(FinansSandikMalzemesi.Bilinmiyor, FinansHacimKurallari.Malzeme(null, "Katlanır Sandık"));
        Assert.Equal(FinansSandikMalzemesi.Kontra, FinansHacimKurallari.Malzeme(AmbalajSandikCinsi.Kontrplak, null));
        Assert.Equal(FinansSandikMalzemesi.Ahsap, FinansHacimKurallari.Malzeme(AmbalajSandikCinsi.AhsapKapali, null));
        Assert.Equal((0m, 0m), FinansHacimKurallari.Hacim(AmbalajSandikCinsi.Katlanir, null, 0m, null));
        Assert.Equal((0m, 7m), FinansHacimKurallari.Hacim(AmbalajSandikCinsi.Katlanir, null, 7m, null));
        Assert.Equal((2m, 5m), FinansHacimKurallari.Hacim(null, "Katlanır Sandık", 0m,
            [new("Ahşap", FinansFiyatlandirmaBirimi.Metrekup, 2m, 150m, FinansSandikMalzemesi.Ahsap),
             new("Malzemesiz", FinansFiyatlandirmaBirimi.Metrekup, 5m, 300m)]));
        Assert.Equal((0m, 7m), FinansHacimKurallari.Hacim(AmbalajSandikCinsi.Kontrplak, null, 7m, null));
    }

    [Fact]
    public void Katlanir_elle_fiyatlaninca_hacim_temizlenir_bedel_adet_kere_birim_fiyattir()
    {
        var kayit = new FinansIsKaydi
        {
            SandikCinsi = AmbalajSandikCinsi.Katlanir, Adet = 4, BirimM3 = 0.5m, ToplamM3 = 2m, AhsapM3 = 0.5m, KontraM3 = 1.5m,
            HacimManuel = true, FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.Adet, BirimFiyatSnapshot = 275m
        };

        FinansKatlanirKurali.HacmiTemizle(kayit);

        Assert.Equal((0m, 0m, (decimal?)null, (decimal?)null, false), (kayit.BirimM3, kayit.ToplamM3, kayit.AhsapM3, kayit.KontraM3, kayit.HacimManuel));
        Assert.Equal(1100m, FinansTutarKurallari.IsNet(kayit));
        kayit.ManuelNetTutar = 1250m;
        Assert.Equal(1250m, FinansTutarKurallari.IsNet(kayit));
    }

    /// <summary>Tarifeyle fiyatlanmış eski katlanır kalem tarife düzeltmesinde korunur; aynı kalem kontrplak olsaydı düzeltilirdi.</summary>
    [Fact]
    public void Tarife_duzeltme_katlanir_kalemi_korur()
    {
        var surum = new FinansFiyatTarifesi { Id = 12, BirimFiyat = 500m, KdvOrani = 20m, ParaBirimi = "EUR" };
        FinansIsKaydi Kalem(AmbalajSandikCinsi cinsi) => new()
        {
            KaynakAktif = true, TarifeIdSnapshot = 12, SandikCinsi = cinsi, FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.Adet,
            Adet = 2, BirimFiyatSnapshot = 500m, KdvOraniSnapshot = 20m, ParaBirimiSnapshot = "EUR"
        };

        Assert.Equal(FinansTarifeDuzeltmeKurali.Katlanir, FinansTarifeDuzeltmeKurali.KorunmaNedeni(Kalem(AmbalajSandikCinsi.Katlanir), surum));
        Assert.Null(FinansTarifeDuzeltmeKurali.KorunmaNedeni(Kalem(AmbalajSandikCinsi.Kontrplak), surum));
    }

    /// <summary>
    /// Yeni aktif katlanır tarife eşleşmesi reddedilir; pasif eşleşme, önceden kaydedilmiş eşleşme (ürünün diğer
    /// güncellemeleri kilitlenmesin; fiyatlamada yok sayılır) ve diğer sandık tipleri kabul edilir.
    /// </summary>
    [Theory]
    [InlineData("Katlanır Sandık", true, null, false)]
    [InlineData(" katlanır sandık", true, null, false)]
    [InlineData("Katlanır Sandık", false, null, true)]
    [InlineData("Katlanır Sandık", true, 7, true)]
    [InlineData("Kontrplak Sandık", true, null, true)]
    [InlineData(null, true, null, true)]
    public void Katlanir_sandik_tarife_eslesmesi_tanimlanamaz(string? sandikTipi, bool aktif, int? id, bool gecerli)
    {
        var model = new FinansUrunKaydetModel("KAT", "Katlanır kasa", FinansFiyatlandirmaBirimi.Adet, true, 0,
            [new FinansUrunEslesmesiModel(id, FinansIsTuru.SahaSandigi, null, sandikTipi, null, Aktif: aktif)], 300m, "EUR", 20);

        var sonuc = new FinansUrunKaydetModelValidator().Validate(model);

        Assert.Equal(gecerli, sonuc.IsValid);
        if (!gecerli) Assert.Contains(sonuc.Errors, x => x.ErrorMessage == FinansKatlanirKurali.TarifeMesaji);
    }

    /// <summary>
    /// Fiyatlandırma gerekçesi doğrulayıcıda isteğe bağlıdır (katlanır sandık için); zorunluluğu kayda göre servis denetler.
    /// </summary>
    [Theory]
    [InlineData("", true)]
    [InlineData(null, true)]
    [InlineData("Tutar müşteriyle anlaşıldı", true)]
    public void Fiyatlandirma_gerekcesi_dogrulayicida_istege_baglidir(string? gerekce, bool gecerli)
    {
        var komut = new FinansFiyatlandirCommand
        {
            Id = 5,
            Model = new(FinansFiyatlandirmaBirimi.Adet, 3, 0, 450m, "EUR", 20, gerekce!)
        };

        Assert.Equal(gecerli, new FinansFiyatlandirCommandValidator().Validate(komut).IsValid);
    }

    [Fact]
    public void Fiyatlandirma_gerekcesi_uzunluk_sinirini_asamaz()
    {
        var komut = new FinansFiyatlandirCommand
        {
            Id = 5,
            Model = new(FinansFiyatlandirmaBirimi.Adet, 3, 0, 450m, "EUR", 20, new string('x', FinansGerekceKurali.EnUzun + 1))
        };

        Assert.False(new FinansFiyatlandirCommandValidator().Validate(komut).IsValid);
    }
}
