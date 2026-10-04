using System.Text.Json;
using _3K.Application.Common;
using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Application.Features.FinansIslemleri.Queries;
using _3K.Application.Features.FinansIslemleri.Validators;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Models;

namespace _3K.Application.Tests;

/// <summary>
/// Hatalı tarife düzeltme kuralı: yalnız sürümün tarife değeriyle fiyatlanmış açık kalem düzeltilir; PO'da sabitlenmiş,
/// elle fiyatlanmış, bileşenli ya da iptal kalem korunur. Yetki, doğrulama ve önizlemenin alan maskesi de sabitlenir.
/// </summary>
public sealed class FinansTarifeDuzeltmeTests
{
    private static FinansFiyatTarifesi Surum() => new() { Id = 12, BirimFiyat = 500m, KdvOrani = 20m, ParaBirimi = "EUR" };

    private static FinansIsKaydi Kalem() => new()
    {
        Id = 1, ProjeNo = "PA727-03", IsAdi = "Sarf kereste", KaynakAktif = true, TarifeIdSnapshot = 12,
        FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.Metrekup, Adet = 1, ToplamM3 = 2.5m,
        BirimFiyatSnapshot = 500m, KdvOraniSnapshot = 20m, ParaBirimiSnapshot = "EUR"
    };

    private static FinansSiparisKalemi PoSatiri(bool iptal) => new() { FinansSiparis = new FinansSiparis { IptalEdildi = iptal } };

    [Fact]
    public void Tarife_degeriyle_fiyatlanmis_acik_kalem_duzeltilir_iptal_po_kalemi_engellemez()
    {
        var kalem = Kalem();
        Assert.Null(FinansTarifeDuzeltmeKurali.KorunmaNedeni(kalem, Surum()));

        kalem.SiparisKalemleri.Add(PoSatiri(iptal: true));
        Assert.Null(FinansTarifeDuzeltmeKurali.KorunmaNedeni(kalem, Surum()));

        // Para birimi büyük/küçük harf farkı elle değişiklik sayılmaz.
        kalem.ParaBirimiSnapshot = "eur";
        Assert.Null(FinansTarifeDuzeltmeKurali.KorunmaNedeni(kalem, Surum()));
    }

    public static TheoryData<string, Action<FinansIsKaydi>> KorunanKalemler => new()
    {
        { FinansTarifeDuzeltmeKurali.PoyaBagli, k => k.SiparisKalemleri.Add(PoSatiri(iptal: false)) },
        { FinansTarifeDuzeltmeKurali.IptalVeyaPasif, k => k.IptalEdildi = true },
        { FinansTarifeDuzeltmeKurali.IptalVeyaPasif, k => k.KaynakAktif = false },
        { FinansTarifeDuzeltmeKurali.ManuelNet, k => k.ManuelNetTutar = 900m },
        { FinansTarifeDuzeltmeKurali.Bilesenli, k => k.HesaplamaYontemi = FinansHesaplamaYontemi.Bilesenli },
        { FinansTarifeDuzeltmeKurali.ElleFiyat, k => k.BirimFiyatSnapshot = 480m },
        { FinansTarifeDuzeltmeKurali.ElleFiyat, k => k.KdvOraniSnapshot = 10m },
        { FinansTarifeDuzeltmeKurali.ElleFiyat, k => k.ParaBirimiSnapshot = "TRY" },
    };

    [Theory]
    [MemberData(nameof(KorunanKalemler))]
    public void Po_manuel_bilesenli_iptal_ve_elle_fiyatli_kalem_korunur(string neden, Action<FinansIsKaydi> degistir)
    {
        var kalem = Kalem();
        degistir(kalem);
        Assert.Equal(neden, FinansTarifeDuzeltmeKurali.KorunmaNedeni(kalem, Surum()));
    }

    [Fact]
    public void Yeni_net_kalemin_fiyatlandirma_miktariyla_ve_iki_ondalikla_hesaplanir()
    {
        Assert.Equal(1412.50m, FinansTarifeDuzeltmeKurali.YeniNet(Kalem(), 565m));
        var adetli = Kalem();
        adetli.FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.Adet;
        adetli.Adet = 3;
        Assert.Equal(33.34m, FinansTarifeDuzeltmeKurali.YeniNet(adetli, 11.1133m));
    }

    [Fact]
    public void Negatif_fiyat_aralik_disi_kdv_ve_ayni_deger_reddedilir_yalniz_kdv_duzeltmesi_gecerlidir()
    {
        Assert.Throws<IsKuraliException>(() => FinansTarifeDuzeltmeKurali.Dogrula(Surum(), -1m, 20m));
        Assert.Throws<IsKuraliException>(() => FinansTarifeDuzeltmeKurali.Dogrula(Surum(), 565m, 101m));
        var ayni = Assert.Throws<IsKuraliException>(() => FinansTarifeDuzeltmeKurali.Dogrula(Surum(), 500m, 20m));
        Assert.Contains("aynı", ayni.Message);
        FinansTarifeDuzeltmeKurali.Dogrula(Surum(), 500m, 10m);
        FinansTarifeDuzeltmeKurali.Dogrula(Surum(), 565m, 20m);
    }

    [Fact]
    public void Duzeltme_ve_onizleme_birim_fiyat_degistirme_yazma_yetkisi_ister()
    {
        Assert.Equal(YetkiKodlari.Finans.BirimFiyatDegistir, new FinansTarifeDuzeltCommand().RequiredMenuKod);
        Assert.Equal(YetkiTipi.W, new FinansTarifeDuzeltCommand().RequiredYetkiTipi);
        var onizleme = Assert.Single(new FinansTarifeDuzeltmeOnizlemeQuery().RequiredMenuPermissions);
        Assert.Equal(MenuPermissionRequirement.Write(YetkiKodlari.Finans.BirimFiyatDegistir), onizleme);
    }

    [Fact]
    public void Gerekcesiz_duzeltme_dogrulamadan_gecmez()
    {
        var validator = new FinansTarifeDuzeltCommandValidator();
        FinansTarifeDuzeltCommand Komut(string gerekce) => new() { UrunId = 3, TarifeId = 12, Model = new(565m, 20m, gerekce) };

        Assert.False(validator.Validate(Komut("kısa")).IsValid);
        Assert.False(validator.Validate(new FinansTarifeDuzeltCommand { UrunId = 0, TarifeId = 12, Model = new(565m, 20m, "500 yanlış girildi") }).IsValid);
        Assert.True(validator.Validate(Komut("500 yanlışlıkla girildi")).IsValid);
    }

    [Fact]
    public void Onizlemede_fiyat_kdv_ve_tutar_yetkisiz_kullaniciya_null_doner_sayilar_gorunur()
    {
        var model = new FinansTarifeDuzeltmeOnizlemeModel
        {
            TarifeId = 12, UrunId = 3, UrunAdi = "AĞAÇ KERESTE", ParaBirimi = "EUR",
            Mevcut = new(500m, 20m, 1250m), Yeni = new(565m, 20m, 1412.5m),
            DuzeltilecekKalemSayisi = 1, KorunanKalemSayisi = 2,
            Kalemler = [new(1, "PA727-03", "Sarf kereste", new DateTime(2026, 10, 4), "Manuel", true, null, 1250m)]
        };
        var json = JsonSerializer.SerializeToNode(model, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        // Parasal veri görülür, birim fiyat ve tutar görülmez.
        FinansAlanProjeksiyonu.Uygula(json, new AlanErisimYetkileri(
            Olcu: true, UretimM3: true, Sarf: true, ParasalVeri: true, BirimFiyat: false, Tutar: false, Gelir: true, Gider: true, Karlilik: true));

        foreach (var deger in new[] { "mevcut", "yeni" })
        {
            Assert.Null(json[deger]!["birimFiyat"]);
            Assert.Null(json[deger]!["kdvOrani"]);
            Assert.Null(json[deger]!["netTutar"]);
        }
        Assert.Null(json["kalemler"]![0]!["netTutar"]);
        Assert.Equal(1, json["duzeltilecekKalemSayisi"]!.GetValue<int>());
        Assert.Equal(2, json["korunanKalemSayisi"]!.GetValue<int>());
        Assert.Equal("PA727-03", json["kalemler"]![0]!["projeNo"]!.GetValue<string>());
    }
}
