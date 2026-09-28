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

    [Fact]
    public void Bugun_gecerli_tarifenin_fiyat_degisikligi_denetime_yazilir()
    {
        using var context = FinansTestServisleri.BaglanmayanContext();
        var service = UrunServisi(context);
        var tarife = new FinansFiyatTarifesi
        {
            Id = 5, FinansUrunId = 3, Yil = 2026, GecerlilikBaslangici = TurkeyTime.Now.Date.AddDays(-3),
            GecerlilikBitisi = new DateTime(2200, 12, 31), BirimFiyat = 550, ParaBirimi = "EUR", KdvOrani = 20, Aktif = true
        };
        var urun = new FinansUrun { Id = 3, Kod = "AMB", Ad = "Ambalaj" };
        urun.FiyatTarifeleri.Add(tarife);

        var yeni = service.ApplyDirectPrice(urun, Model(600, "eur", 20));

        Assert.Null(yeni);
        Assert.Equal(600, tarife.BirimFiyat);
        var denetim = Assert.Single(context.ChangeTracker.Entries<FinansDegisiklikGecmisi>()).Entity;
        Assert.Equal(nameof(FinansFiyatTarifesi), denetim.VarlikTuru);
        Assert.Equal(5, denetim.VarlikId);
        Assert.Equal(nameof(FinansFiyatTarifesi.BirimFiyat), denetim.AlanAdi);
        Assert.Equal("550", denetim.EskiDeger);
        Assert.Equal("600", denetim.YeniDeger);
    }

    [Fact]
    public void Gecerli_tarife_yoksa_bugunden_baslayan_acik_uclu_tarife_acilir()
    {
        using var context = FinansTestServisleri.BaglanmayanContext();
        var service = UrunServisi(context);
        var urun = new FinansUrun { Id = 3, Kod = "AMB", Ad = "Ambalaj" };

        var yeni = service.ApplyDirectPrice(urun, Model(600, "usd", 18));

        Assert.NotNull(yeni);
        Assert.Same(yeni, Assert.Single(urun.FiyatTarifeleri));
        Assert.Equal(TurkeyTime.Now.Date, yeni.GecerlilikBaslangici);
        Assert.Equal(new DateTime(FinansTarihKurallari.EnBuyukYil, 12, 31), yeni.GecerlilikBitisi);
        Assert.Equal((600m, "USD", 18m), (yeni.BirimFiyat, yeni.ParaBirimi, yeni.KdvOrani));
        Assert.Empty(context.ChangeTracker.Entries<FinansDegisiklikGecmisi>());
    }

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
