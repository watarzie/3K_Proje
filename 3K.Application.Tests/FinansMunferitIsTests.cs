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
/// Münferit İşler: form yalnız tarife + proje (sistem projesi veya serbest ad) + tarih + miktar ister; iş adı tarifenin
/// adıdır, fiyat tarifenin iş tarihindeki sürümünden gelir; m³ tarifesinde ahşap/kontra m³ zorunludur; liste ve ana panel
/// yalnız elle açılmış (düzenli işe bağlı olmayan) özel işleri gösterir.
/// </summary>
public sealed class FinansMunferitIsTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly DateTime Tarih = new(2026, 10, 6);

    [Fact]
    public void Dto_TarihiGunBasinaCeker_ProjeAdiniKirpar_IsAdiFiyatMusteriTasimaz()
    {
        var model = new FinansMunferitIsKaydetDto(7, null, "  Rüzgar Sahası ", new DateTime(2026, 10, 6, 15, 30, 0), 2, 1.2m, 0.8m)
            .MunferitIsModeli();
        Assert.Equal(new FinansMunferitIsKaydetModel(7, null, "Rüzgar Sahası", new DateTime(2026, 10, 6), 2, 1.2m, 0.8m), model);
        Assert.Null(new FinansMunferitIsKaydetDto(7, 3, "   ", Tarih).MunferitIsModeli().ProjeAdi);
        Assert.DoesNotContain(typeof(FinansMunferitIsKaydetDto).GetProperties(),
            x => x.Name.Contains("Fiyat") || x.Name is "IsAdi" or "Musteri" or "Birim" or "Aciklama");
    }

    [Theory]
    [InlineData(0, 3, null, 1, false)]
    [InlineData(5, null, null, 1, false)]
    [InlineData(5, 3, "Saha X", 1, false)]
    [InlineData(5, null, "   ", 1, false)]
    [InlineData(5, 3, null, 0, false)]
    [InlineData(5, 3, null, 1, true)]
    [InlineData(5, null, "Saha X", 1.5, true)]
    public void Dogrulayici_TarifeVeTekProjeKaynagiIster(int urun, int? projeId, string? projeAdi, double adet, bool gecerli)
        => Assert.Equal(gecerli, new FinansMunferitIsOlusturCommandValidator().Validate(new FinansMunferitIsOlusturCommand
        {
            Model = new(urun, projeId, projeAdi, Tarih, (decimal)adet)
        }).IsValid);

    [Fact]
    public void Izinler_YalnizMunferitIslerSekmesiYeter_FiyatDegistirmeIstenmez()
    {
        Assert.Equal(
            [MenuPermissionRequirement.Write(YetkiKodlari.Finans.SekmeMunferitIsler)],
            new FinansMunferitIsOlusturCommand().RequiredMenuPermissions);
        Assert.Equal(
            [MenuPermissionRequirement.Read(YetkiKodlari.Finans.SekmeMunferitIsler)],
            new FinansMunferitIslerQuery().RequiredMenuPermissions);
        var secenek = new FinansUrunSecenekleriQuery();
        Assert.Equal((YetkiKodlari.Finans.SekmeMunferitIsler, YetkiTipi.R), (secenek.RequiredMenuKod, secenek.RequiredYetkiTipi));
        Assert.Equal(["Id", "Kod", "Ad", "FiyatlandirmaBirimi", "BirimFiyat", "ParaBirimi", "KdvOrani"],
            typeof(FinansUrunSecenekModel).GetProperties().Select(x => x.Name));
    }

    /// <summary>
    /// Gerçek SQL: adet ve m³ tarifesiyle münferit iş tarifeden fiyatlanır, iş adı tarifenin adıdır; serbest proje adı
    /// proje no ve müşteri olur, sistem projesinde müşteri projeden gelir; m³ tarifesinde hacimsiz istek ve tarifesi olmayan
    /// tarih 409; liste ve panel yalnız elle açılmış özel işleri gösterir; seçenekler o tarihteki tarife fiyatını taşır.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_TarifedenFiyatlanir_ProjeSistemdenVeyaSerbest_M3Zorunlu_ListeVePanelYalnizMunferit()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var finans = new FinansTestServisleri(db, new Kullanici());
            var ozelEslesme = new FinansUrunEslesmesiModel(null, FinansIsTuru.OzelIs, null, null, null);
            var kasa = await finans.Urun.UrunOlusturAsync(new("HX-KASA", "Haliade-X kasa", FinansFiyatlandirmaBirimi.Adet, true, 1,
                [ozelEslesme], 1250m, "EUR", 20, new DateTime(2026, 10, 1)), Ct);
            var kontra = await finans.Urun.UrunOlusturAsync(new("SKIT-M3", "SKIT kontra", FinansFiyatlandirmaBirimi.Metrekup, true, 2,
                [ozelEslesme], 400m, "EUR", 20, new DateTime(2026, 10, 1)), Ct);
            var pasif = await finans.Urun.UrunOlusturAsync(new("HX-ESKI", "Haliade-X eski", FinansFiyatlandirmaBirimi.Adet, false, 3,
                [ozelEslesme], 10m, "EUR", 20, new DateTime(2026, 10, 1)), Ct);
            db.Projeler.Add(new Proje { Id = 301, ProjeNo = "PA-301", Musteri = "GE Vernova", ProjeTipiId = (int)ProjeTipi.Normal });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var adetIs = await finans.IsKaydi.MunferitIsOlusturAsync(new(kasa.Id, null, "Rüzgar Sahası", Tarih, 2), Ct);
            Assert.Equal((2500m, 1250m, "EUR", 20m, "Münferit İş", FinansIsTuru.OzelIs),
                (adetIs.NetTutar, adetIs.BirimFiyat, adetIs.ParaBirimi, adetIs.KdvOrani, adetIs.RaporGrubu, adetIs.IsTuru));
            Assert.Equal(("Haliade-X kasa", "Rüzgar Sahası", "Rüzgar Sahası", (int?)null, 2m, "Adet"),
                (adetIs.IsAdi, adetIs.ProjeNo, adetIs.Musteri, adetIs.ProjeId, adetIs.Adet, adetIs.Birim));
            Assert.StartsWith("OZL-", adetIs.SandikNo);

            db.ChangeTracker.Clear();
            var hacimsiz = await Assert.ThrowsAsync<IsKuraliException>(() => finans.IsKaydi.MunferitIsOlusturAsync(
                new(kontra.Id, 301, null, Tarih), Ct));
            Assert.Contains("m³", hacimsiz.Message);

            db.ChangeTracker.Clear();
            var m3Is = await finans.IsKaydi.MunferitIsOlusturAsync(new(kontra.Id, 301, null, Tarih, 5, 1.2m, 0.8m), Ct);
            Assert.Equal((2m, 2m, 1m, 800m, true), (m3Is.ToplamM3, m3Is.BirimM3, m3Is.Adet, m3Is.NetTutar, m3Is.HacimManuel));
            Assert.Equal(("SKIT kontra", "PA-301", "GE Vernova", (int?)301, "m³"), (m3Is.IsAdi, m3Is.ProjeNo, m3Is.Musteri, m3Is.ProjeId, m3Is.Birim));

            db.ChangeTracker.Clear();
            var tarifesiz = await Assert.ThrowsAsync<IsKuraliException>(() => finans.IsKaydi.MunferitIsOlusturAsync(
                new(kasa.Id, null, "Eski iş", new DateTime(2026, 9, 15)), Ct));
            Assert.Contains("15.09.2026 tarihinde geçerli tarife yok", tarifesiz.Message);
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.IsKaydi.MunferitIsOlusturAsync(new(pasif.Id, 301, null, Tarih), Ct));
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.IsKaydi.MunferitIsOlusturAsync(new(kasa.Id, 301, "İkisi birden", Tarih), Ct));

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
            Assert.Equal(["Eski tek seferlik", "Haliade-X kasa", "SKIT kontra"], liste.Items.Select(x => x.IsAdi).Order());
            Assert.Equal(["SKIT kontra"], (await finans.Panel.MunferitIslerAsync(new FinansListeFiltre(Arama: "kontra"), Ct)).Items.Select(x => x.IsAdi));

            // Ana panel: dönemdeki münferit işler (düzenli iş dönemi ve üretim kaydı hariç) ve sayısı.
            var panel = await finans.Panel.PanelAsync(new DateTime(2026, 10, 1), new DateTime(2026, 10, 31), Ct);
            Assert.Equal(3, panel.MunferitIsSayisi);
            Assert.Equal(["Eski tek seferlik", "Haliade-X kasa", "SKIT kontra"], panel.MunferitIsler!.Select(x => x.IsAdi).Order());
            Assert.Equal(2500m, panel.MunferitIsler!.Single(x => x.IsAdi == "Haliade-X kasa").NetTutar);

            var secenekler = await finans.Urun.UrunSecenekleriAsync(null, null, Tarih, Ct);
            Assert.Equal(["HX-KASA", "SKIT-M3"], secenekler.Select(x => x.Kod));
            Assert.Equal((1250m, "EUR", 20m), (secenekler[0].BirimFiyat, secenekler[0].ParaBirimi, secenekler[0].KdvOrani));
            Assert.Null((await finans.Urun.UrunSecenekleriAsync(null, null, new DateTime(2026, 9, 15), Ct))[0].BirimFiyat);
            Assert.Equal(["SKIT-M3"], (await finans.Urun.UrunSecenekleriAsync("skit", FinansIsTuru.OzelIs, Tarih, Ct)).Select(x => x.Kod));
            Assert.Empty(await finans.Urun.UrunSecenekleriAsync(null, FinansIsTuru.AnaAmbalaj, Tarih, Ct));
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
