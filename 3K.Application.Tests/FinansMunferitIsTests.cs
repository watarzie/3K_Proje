using Microsoft.EntityFrameworkCore;
using _3K.Application.Common;
using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Application.Features.FinansIslemleri.DTOs;
using _3K.Application.Features.FinansIslemleri.Queries;
using _3K.Application.Features.FinansIslemleri.Validators;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Application.Tests;

/// <summary>
/// Münferit İşler sekmesi iskeleti (P8): fiyat elle yazılmaz, ürünün iş tarihindeki tarifesinden gelir; m³ ile
/// fiyatlanan üründe ahşap/kontra m³ zorunludur; liste yalnız elle açılmış özel işleri gösterir.
/// </summary>
public sealed class FinansMunferitIsTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly DateTime Tarih = new(2026, 10, 6);

    [Fact]
    public void Dto_TarihiGunBasinaCekerFiyatAlaniTasimaz()
    {
        var model = new FinansMunferitIsKaydetDto(7, "Haliade-X kasa", "Haliade-X", "GE Vernova", 3, 2, "Adet",
            new DateTime(2026, 10, 6, 15, 30, 0), 1.2m, 0.8m, "Not", "Ali", "Lojistik").MunferitIsModeli();
        Assert.Equal(new FinansMunferitIsKaydetModel(7, "Haliade-X kasa", "Haliade-X", "GE Vernova", 3, 2, "Adet", 1.2m, 0.8m,
            new DateTime(2026, 10, 6), "Not", "Ali", "Lojistik"), model);
        Assert.DoesNotContain(typeof(FinansMunferitIsKaydetDto).GetProperties(), x => x.Name.Contains("Fiyat"));
    }

    [Theory]
    [InlineData(0, "İş", "GE", 1, "Adet", false)]
    [InlineData(5, "", "GE", 1, "Adet", false)]
    [InlineData(5, "İş", "", 1, "Adet", false)]
    [InlineData(5, "İş", "GE", 0, "Adet", false)]
    [InlineData(5, "İş", "GE", 1, "", false)]
    [InlineData(5, "İş", "GE", 1.5, "Adet", true)]
    public void Dogrulayici_UrunIsAdiMusteriAdetVeBirimIster(int urun, string isAdi, string musteri, double adet, string birim, bool gecerli)
        => Assert.Equal(gecerli, new FinansMunferitIsOlusturCommandValidator().Validate(new FinansMunferitIsOlusturCommand
        {
            Model = new(urun, isAdi, null, musteri, null, (decimal)adet, birim, null, null, Tarih, null, null, null)
        }).IsValid);

    [Fact]
    public void Izinler_SekmeVeIsEklemeYeter_FiyatDegistirmeIstenmez()
    {
        Assert.Equal(
            [MenuPermissionRequirement.Write(YetkiKodlari.Finans.SekmeMunferitIsler), MenuPermissionRequirement.Write(YetkiKodlari.Finans.ManuelIsEkle)],
            new FinansMunferitIsOlusturCommand().RequiredMenuPermissions);
        Assert.Equal(
            [MenuPermissionRequirement.Read(YetkiKodlari.Finans.SekmeMunferitIsler), MenuPermissionRequirement.Read(YetkiKodlari.Finans.KayitGoruntule)],
            new FinansMunferitIslerQuery().RequiredMenuPermissions);
        var secenek = new FinansUrunSecenekleriQuery();
        Assert.Equal((YetkiKodlari.Finans.SekmeMunferitIsler, YetkiTipi.R), (secenek.RequiredMenuKod, secenek.RequiredYetkiTipi));
        Assert.Equal(["Id", "Kod", "Ad", "FiyatlandirmaBirimi"], typeof(FinansUrunSecenekModel).GetProperties().Select(x => x.Name));
    }

    /// <summary>
    /// Gerçek SQL: adet ve m³ tarifeli ürünle münferit iş tarifeden fiyatlanır; m³ ürününde hacimsiz istek ve tarifesi
    /// olmayan tarih 409; liste yalnız elle açılmış özel işleri döndürür; ürün seçenekleri aktif ürünleri arar.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_TarifedenFiyatlanir_M3Zorunlu_TarifesizTarihReddedilir_ListeYalnizMunferit()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var finans = new FinansTestServisleri(db, new Kullanici());
            var ozelEslesme = new FinansUrunEslesmesiModel(null, FinansIsTuru.OzelIs, null, null, null, null, null, null);
            var kasa = await finans.Urun.UrunOlusturAsync(new("HX-KASA", "Haliade-X kasa", FinansFiyatlandirmaBirimi.Adet, true, 1,
                [ozelEslesme], 1250m, "EUR", 20, new DateTime(2026, 10, 1)), Ct);
            var kontra = await finans.Urun.UrunOlusturAsync(new("SKIT-M3", "SKIT kontra", FinansFiyatlandirmaBirimi.Metrekup, true, 2,
                [ozelEslesme], 400m, "EUR", 20, new DateTime(2026, 10, 1)), Ct);
            var pasif = await finans.Urun.UrunOlusturAsync(new("HX-ESKI", "Haliade-X eski", FinansFiyatlandirmaBirimi.Adet, false, 3,
                [ozelEslesme], 10m, "EUR", 20, new DateTime(2026, 10, 1)), Ct);
            db.ChangeTracker.Clear();

            var adetIs = await finans.IsKaydi.MunferitIsOlusturAsync(
                new(kasa.Id, "Kanat kasası", "Haliade-X", "GE Vernova", null, 2, "Adet", null, null, Tarih, null, null, null), Ct);
            Assert.Equal((2500m, 1250m, "EUR", 20m, "Münferit İş", FinansIsTuru.OzelIs),
                (adetIs.NetTutar, adetIs.BirimFiyat, adetIs.ParaBirimi, adetIs.KdvOrani, adetIs.RaporGrubu, adetIs.IsTuru));
            Assert.StartsWith("OZL-", adetIs.SandikNo);

            db.ChangeTracker.Clear();
            var hacimsiz = await Assert.ThrowsAsync<IsKuraliException>(() => finans.IsKaydi.MunferitIsOlusturAsync(
                new(kontra.Id, "Kontra kasa", "SKIT", "GE Vernova", null, 2, "Adet", null, null, Tarih, null, null, null), Ct));
            Assert.Contains("m³", hacimsiz.Message);

            db.ChangeTracker.Clear();
            var m3Is = await finans.IsKaydi.MunferitIsOlusturAsync(
                new(kontra.Id, "Kontra kasa", "SKIT", "GE Vernova", null, 2, "Adet", 1.2m, 0.8m, Tarih, null, null, null), Ct);
            Assert.Equal((2m, 1m, 800m, true), (m3Is.ToplamM3, m3Is.BirimM3, m3Is.NetTutar, m3Is.HacimManuel));

            db.ChangeTracker.Clear();
            var tarifesiz = await Assert.ThrowsAsync<IsKuraliException>(() => finans.IsKaydi.MunferitIsOlusturAsync(
                new(kasa.Id, "Eski iş", null, "GE Vernova", null, 1, "Adet", null, null, new DateTime(2026, 9, 15), null, null, null), Ct));
            Assert.Contains("15.09.2026 tarihinde geçerli tarife yok", tarifesiz.Message);
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.IsKaydi.MunferitIsOlusturAsync(
                new(pasif.Id, "Pasif ürün", null, "GE Vernova", null, 1, "Adet", null, null, Tarih, null, null, null), Ct));

            // Liste: münferit işler ve eski "Tek Seferlik İş" kayıtları (elle, düzenli işe bağlı değil); üretim ve düzenli iş dönemi yok.
            db.ChangeTracker.Clear();
            var duzenli = new FinansDuzenliIs { IsAdi = "Kira", Musteri = "GE", Miktar = 1, Birim = "Ay", BirimFiyat = 10, ParaBirimi = "EUR", BaslangicTarihi = Tarih };
            db.Add(duzenli);
            await db.SaveChangesAsync();
            db.AddRange(
                Kayit("Kira dönemi", FinansIsTuru.OzelIs, "DuzenliIs", duzenli.Id),
                Kayit("Ana ambalaj", FinansIsTuru.AnaAmbalaj, "AMBALAJURETIM", null),
                Kayit("Eski tek seferlik", FinansIsTuru.OzelIs, "Manuel", null));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var liste = await finans.Panel.MunferitIslerAsync(new FinansListeFiltre(PageSize: 50, Baslangic: new(2026, 10, 1), Bitis: new(2026, 10, 31)), Ct);
            Assert.Equal(["Eski tek seferlik", "Kanat kasası", "Kontra kasa"], liste.Items.Select(x => x.IsAdi).Order());
            Assert.Equal(["Kontra kasa"], (await finans.Panel.MunferitIslerAsync(new FinansListeFiltre(Arama: "kontra"), Ct)).Items.Select(x => x.IsAdi));

            var secenekler = await finans.Urun.UrunSecenekleriAsync(null, null, Ct);
            Assert.Equal(["HX-KASA", "SKIT-M3"], secenekler.Select(x => x.Kod));
            Assert.Equal(["SKIT-M3"], (await finans.Urun.UrunSecenekleriAsync("skit", FinansIsTuru.OzelIs, Ct)).Select(x => x.Kod));
            Assert.Empty(await finans.Urun.UrunSecenekleriAsync(null, FinansIsTuru.AnaAmbalaj, Ct));
        });
    }

    private static FinansIsKaydi Kayit(string isAdi, FinansIsTuru tur, string kaynak, int? duzenliIsId) => new()
    {
        IsAdi = isAdi, ProjeNo = "QA", Musteri = "QA", IsTuru = tur, KaynakTuru = kaynak, KaynakAktif = true, DuzenliIsId = duzenliIsId,
        Adet = 1, Birim = "Adet", FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.SabitTutar, BirimFiyatSnapshot = 10,
        ParaBirimiSnapshot = "EUR", KdvOraniSnapshot = 20, UretimTarihi = Tarih, FinansTarihi = Tarih,
        FinansDonemi = FinansTarihKurallari.AyBasi(Tarih), KayitTarihi = Tarih,
        KaynakKayitId = kaynak == "AMBALAJURETIM" ? Guid.NewGuid().ToString("D") : null
    };

    private sealed class Kullanici : ICurrentUserService
    {
        public int? UserId => 7;
        public bool IsAuthenticated => true;
    }
}
