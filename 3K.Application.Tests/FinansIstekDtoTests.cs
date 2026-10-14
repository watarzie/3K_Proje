using _3K.Application.Features.FinansIslemleri.DTOs;
using _3K.Core.Enums;

namespace _3K.Application.Tests;

/// <summary>Ekran istekleri servis modellerine iş varsayılanlarıyla dönüştürülür.</summary>
public sealed class FinansIstekDtoTests
{
    [Fact]
    public void Gider_istegindeki_bos_alanlar_gider_varsayilanlariyla_tamamlanir()
    {
        var model = new FinansGiderKaydetDto(new DateTime(2026, 9, 17), 3, null, "Nakliyeci", "Nakliye", 250m,
            "EUR", false, 20m, ProjeId: null, IsTuru: null).GiderModeli();

        Assert.Equal(new DateTime(2026, 9, 1), model.FinansDonemi);
        Assert.Equal(1m, model.Miktar);
        Assert.Equal("Tutar", model.Birim);
        Assert.Equal(250m, model.BirimFiyat);
        Assert.Null(model.ManuelProjeNo);
    }

    [Fact]
    public void Gider_istegindeki_acik_degerler_korunur()
    {
        var model = new FinansGiderKaydetDto(new DateTime(2026, 9, 17), 3, "Alt", "Firma", "Kereste", 999m,
            "TRY", true, 20m, 12, FinansIsTuru.AnaAmbalaj, Miktar: 4m, Birim: "m³", BirimFiyat: 125m,
            FinansDonemi: new DateTime(2026, 10, 1), FinansTarihi: new DateTime(2026, 10, 5)).GiderModeli();

        Assert.Equal((new DateTime(2026, 10, 1), 4m, "m³", 125m), (model.FinansDonemi, model.Miktar, model.Birim, model.BirimFiyat));
        Assert.Equal(new DateTime(2026, 10, 5), model.FinansTarihi);
        Assert.Equal((12, FinansIsTuru.AnaAmbalaj), (model.ProjeId, model.IsTuru));
    }

    /// <summary>Özel işte m³ yönteminde ahşap ve kontra m³ ayrı girilir; birim m³ bileşik toplam / miktardır.</summary>
    [Fact]
    public void Ozel_is_m3_yonteminde_ahsap_kontra_bilesik_toplamdan_birim_m3_uretir()
    {
        var m3 = new FinansOzelIsKaydetDto("SKIT", "GE Vernova", null, "Kontra kasa", null, 2m, "Adet", new DateTime(2026, 10, 1),
            FinansHesaplamaYontemi.Metrekup, "Özel İş", 150m, "EUR", 20m, AhsapM3: 1.2m, KontraM3: 0.8m).IsKaydiModeli();
        Assert.Equal((1m, 1.2m, 0.8m), (m3.BirimM3, m3.AhsapM3, m3.KontraM3));

        var adet = new FinansOzelIsKaydetDto("SKIT", "GE Vernova", null, "Montaj", null, 2m, "Adet", new DateTime(2026, 10, 1),
            FinansHesaplamaYontemi.DegiskenAdet, "Özel İş", 150m, "EUR", 20m, AhsapM3: 5m).IsKaydiModeli();
        Assert.Equal((0m, (decimal?)null, (decimal?)null), (adet.BirimM3, adet.AhsapM3, adet.KontraM3));
    }

    [Fact]
    public void Ozel_is_m3_yonteminde_hacimsiz_istek_dogrulamadan_gecmez()
    {
        var validator = new _3K.Application.Features.FinansIslemleri.Validators.FinansIsKaydiKaydetModelValidator();
        var hacimsiz = new FinansOzelIsKaydetDto("SKIT", "GE Vernova", null, "Kontra kasa", null, 2m, "Adet", new DateTime(2026, 10, 1),
            FinansHesaplamaYontemi.Metrekup, "Özel İş", 150m, "EUR", 20m).IsKaydiModeli();
        Assert.False(validator.Validate(hacimsiz).IsValid);
        Assert.True(validator.Validate(hacimsiz with { AhsapM3 = 0, KontraM3 = 1 }).IsValid);
    }

    [Fact]
    public void Duzenli_is_istegi_ozel_is_turunde_ve_urunsuz_kaydedilir()
    {
        var model = new FinansDuzenliIsKaydetDto(5, "Depo kirası", "Kira", "Müşteri", null, "Haftalik",
            new DateTime(2026, 1, 1), null, 5, 1m, "Ay", 1000m, "TRY", 20m, true).DuzenliIsModeli();

        Assert.Equal(FinansIsTuru.OzelIs, model.IsTuru);
        Assert.Equal("Kira", model.OzelIsTuru);
        Assert.Null(model.FinansUrunId);
        Assert.Null(model.ManuelProjeNo);
        Assert.Equal(FinansHesaplamaYontemi.DegiskenAdet, model.HesaplamaYontemi);
    }
}
