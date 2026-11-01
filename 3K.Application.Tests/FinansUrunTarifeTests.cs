using System.Text.Json;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Services.Finans;

namespace _3K.Application.Tests;

/// <summary>Ürün formu eşleşmeleri aktif kaydeder; fiyat değişikliği tarifeye denetimli yansır.</summary>
public sealed class FinansUrunTarifeTests
{
    [Fact]
    public void Aktif_alani_gonderilmeyen_eslesme_aktif_kabul_edilir()
    {
        const string istek = """
            {"kod":"AMB","ad":"Ambalaj","fiyatlandirmaBirimi":2,"aktif":true,"sira":0,"birimFiyat":550,"paraBirimi":"EUR","kdvOrani":20,
             "eslesmeler":[{"isTuru":1,"sandikAdi":null},{"isTuru":3,"sandikAdi":null,"aktif":false}]}
            """;

        var model = JsonSerializer.Deserialize<FinansUrunKaydetModel>(istek, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        Assert.True(model.Eslesmeler[0].Aktif);
        Assert.False(model.Eslesmeler[1].Aktif);
    }

    /// <summary>Malzemeli eşleşme (ahşap/kontra) yalnız m³ tarifede ve yalnız Ahşap/Kontra değeriyle kabul edilir.</summary>
    [Theory]
    [InlineData(FinansFiyatlandirmaBirimi.Metrekup, FinansSandikMalzemesi.Kontra, true)]
    [InlineData(FinansFiyatlandirmaBirimi.Metrekup, FinansSandikMalzemesi.Ahsap, true)]
    [InlineData(FinansFiyatlandirmaBirimi.Adet, FinansSandikMalzemesi.Ahsap, false)]
    [InlineData(FinansFiyatlandirmaBirimi.Metrekup, FinansSandikMalzemesi.Bilinmiyor, false)]
    public void Malzemeli_eslesme_m3_tarifede_ahsap_veya_kontra_olmalidir(FinansFiyatlandirmaBirimi birim, FinansSandikMalzemesi malzeme, bool gecerli)
    {
        var model = new FinansUrunKaydetModel("KON", "Kontra m³", birim, true, 0,
            [new FinansUrunEslesmesiModel(null, FinansIsTuru.AnaAmbalaj, null, "Kontrplak Sandık", null, malzeme)], 300m, "EUR", 20);

        Assert.Equal(gecerli, new _3K.Application.Features.FinansIslemleri.Validators.FinansUrunKaydetModelValidator().Validate(model).IsValid);
    }

    /// <summary>R1 §11: fiyat değişikliği geçmişi düzenlemez; eski sürüm kapanır, yeni sürüm açılır.</summary>
    [Fact]
    public void Fiyat_degisikligi_eski_surumu_kapatir_ve_yeni_surum_acar()
    {
        using var context = FinansTestServisleri.BaglanmayanContext();
        var service = UrunServisi(context);
        var bugun = TurkeyTime.Now.Date;
        var tarife = new FinansFiyatTarifesi
        {
            Id = 5, FinansUrunId = 3, Yil = 2026, GecerlilikBaslangici = bugun.AddDays(-3),
            GecerlilikBitisi = new DateTime(2200, 12, 31), BirimFiyat = 550, ParaBirimi = "EUR", KdvOrani = 20, Aktif = true
        };
        var urun = new FinansUrun { Id = 3, Kod = "AMB", Ad = "Ambalaj" };
        urun.FiyatTarifeleri.Add(tarife);
        var model = Model(600, "eur", 20);

        var plan = FinansUrunService.FiyatPlani(urun, model)!;
        var yeni = service.PlaniUygula(urun, plan, model);

        Assert.Equal(FinansTarifeIslemi.SurumuKapatVeYeniAc, plan.Islem);
        Assert.Equal(550, tarife.BirimFiyat);
        Assert.Equal(bugun.AddDays(-1), tarife.GecerlilikBitisi);
        Assert.NotNull(yeni);
        Assert.Equal((bugun, new DateTime(2200, 12, 31), 600m, "EUR"),
            (yeni.GecerlilikBaslangici, yeni.GecerlilikBitisi, yeni.BirimFiyat, yeni.ParaBirimi));
        var denetim = Assert.Single(context.ChangeTracker.Entries<FinansDegisiklikGecmisi>()).Entity;
        Assert.Equal(nameof(FinansFiyatTarifesi), denetim.VarlikTuru);
        Assert.Equal(5, denetim.VarlikId);
        Assert.Equal(nameof(FinansFiyatTarifesi.GecerlilikBitisi), denetim.AlanAdi);
    }

    [Fact]
    public void Gecerli_tarife_yoksa_gecerlilik_tarihinden_baslayan_acik_uclu_tarife_acilir()
    {
        using var context = FinansTestServisleri.BaglanmayanContext();
        var service = UrunServisi(context);
        var urun = new FinansUrun { Id = 3, Kod = "AMB", Ad = "Ambalaj" };
        var model = Model(600, "usd", 18) with { GecerlilikBaslangici = new DateTime(2026, 9, 1) };

        var yeni = service.PlaniUygula(urun, FinansUrunService.FiyatPlani(urun, model)!, model);

        Assert.NotNull(yeni);
        Assert.Same(yeni, Assert.Single(urun.FiyatTarifeleri));
        Assert.Equal(new DateTime(2026, 9, 1), yeni.GecerlilikBaslangici);
        Assert.Equal(new DateTime(FinansTarihKurallari.EnBuyukYil, 12, 31), yeni.GecerlilikBitisi);
        Assert.Equal((600m, "USD", 18m, 2026), (yeni.BirimFiyat, yeni.ParaBirimi, yeni.KdvOrani, yeni.Yil));
        Assert.Empty(context.ChangeTracker.Entries<FinansDegisiklikGecmisi>());
    }

    /// <summary>
    /// F12: tarife bugünden başlatıldığında ayın önceki günlerindeki üretim fiyatsız kalıyordu. Geriye dönük
    /// girilen fiyat, sonraki sürümü değiştirmeden aradaki boşluğu kapatır.
    /// </summary>
    [Fact]
    public void Geriye_donuk_fiyat_sonraki_surume_kadar_boslugu_kapatir()
    {
        var mevcut = Tarife(1, new DateTime(2026, 9, 26), FinansTarifeKurallari.AcikUcluBitis, 550);

        var plan = FinansTarifeKurallari.Planla([mevcut], new DateTime(2026, 9, 1), 550, "EUR", 20);

        Assert.Equal(FinansTarifeIslemi.YeniSurum, plan.Islem);
        Assert.Equal((new DateTime(2026, 9, 1), new DateTime(2026, 9, 25)), (plan.YeniBaslangic, plan.YeniBitis));
        Assert.Equal(new DateTime(2026, 9, 26), mevcut.GecerlilikBaslangici);
    }

    [Fact]
    public void Ayni_fiyat_degisiklik_uretmez_ayni_gun_surumu_duzeltme_adayi_olur()
    {
        var mevcut = Tarife(1, new DateTime(2026, 9, 1), FinansTarifeKurallari.AcikUcluBitis, 550);

        Assert.Equal(FinansTarifeIslemi.DegisiklikYok,
            FinansTarifeKurallari.Planla([mevcut], new DateTime(2026, 9, 10), 550, "eur", 20).Islem);
        var duzelt = FinansTarifeKurallari.Planla([mevcut], new DateTime(2026, 9, 1), 575, "EUR", 20);
        Assert.Equal(FinansTarifeIslemi.AyniGunSurumunuDuzelt, duzelt.Islem);
        Assert.Same(mevcut, duzelt.Mevcut);
    }

    [Fact]
    public void Kapali_surum_icindeki_degisiklik_sonraki_surumleri_korur()
    {
        var eski = Tarife(1, new DateTime(2026, 1, 1), new DateTime(2026, 6, 30), 500);
        var sonraki = Tarife(2, new DateTime(2026, 7, 1), FinansTarifeKurallari.AcikUcluBitis, 550);

        var plan = FinansTarifeKurallari.Planla([eski, sonraki], new DateTime(2026, 4, 1), 520, "EUR", 20);

        Assert.Equal(FinansTarifeIslemi.SurumuKapatVeYeniAc, plan.Islem);
        Assert.Same(eski, plan.Mevcut);
        Assert.Equal((new DateTime(2026, 4, 1), new DateTime(2026, 6, 30)), (plan.YeniBaslangic, plan.YeniBitis));
    }

    private static FinansFiyatTarifesi Tarife(int id, DateTime baslangic, DateTime bitis, decimal fiyat) => new()
    {
        Id = id, FinansUrunId = 3, Yil = baslangic.Year, GecerlilikBaslangici = baslangic, GecerlilikBitisi = bitis,
        BirimFiyat = fiyat, ParaBirimi = "EUR", KdvOrani = 20, Aktif = true
    };

    private static FinansUrunKaydetModel Model(decimal fiyat, string paraBirimi, decimal kdv) =>
        new("AMB", "Ambalaj", FinansFiyatlandirmaBirimi.Metrekup, true, 0, [], fiyat, paraBirimi, kdv);

    private static FinansUrunService UrunServisi(AppDbContext context) => new(context, new FinansIslemYurutucu(context),
        new FinansDenetimKaydi(context, new Kullanici()), new FinansFiyatBelirleyici(context));

    private sealed class Kullanici : ICurrentUserService
    {
        public int? UserId => 7;
        public bool IsAuthenticated => true;
    }
}
