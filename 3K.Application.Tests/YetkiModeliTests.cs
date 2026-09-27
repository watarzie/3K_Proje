using _3K.Application.Behaviors;
using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.Commands;
using _3K.Application.Features.RolIslemleri.Queries;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace _3K.Application.Tests;

/// <summary>
/// Menü/işlem/alan yetkilerinin tek modeli: bütün kodlar (Finans ve Ambalaj dahil) aynı N/R/W
/// kuralıyla değerlendirilir. Kişisel ret → kişisel izin → rol → ret; üst düğüm tavandır ve
/// üst W alt izni kendiliğinden vermez.
/// </summary>
public class YetkiModeliTests
{
    private const int FinansKok = 47, PoGir = 5211, KaliciSil = 5231, BirimFiyatGoruntule = 5213;
    private const int AmbalajKok = 46, AmbalajKayitDuzenle = 5100, AmbalajFormGoruntule = 5127;

    [Theory]
    [InlineData(1, false, false)]
    [InlineData(3, false, false)]
    [InlineData(1, true, true)]
    [InlineData(3, null, true)]
    [InlineData(1, null, false)]
    public async Task KisiselRetIzinVeRolOnceligiUygulanir(int rolSeviyesi, bool? karar, bool beklenen)
    {
        using var context = CreateContext(rolSeviyesi, karar);
        Assert.Equal(beklenen, await new RolService(context).HasUserPermissionAsync(7, YetkiKodlari.Finans.PoGir, YetkiTipi.W));
        Assert.False(await new RolService(context).HasUserPermissionAsync(7, YetkiKodlari.Finans.KaliciSil, YetkiTipi.W));
    }

    [Theory]
    [InlineData(YetkiTipi.N, false, false, false)]
    [InlineData(YetkiTipi.R, true, true, false)]
    [InlineData(YetkiTipi.W, true, true, true)]
    public async Task UstDugumAltDugumunTavanidir(YetkiTipi kokSeviyesi, bool kokOkuma, bool altOkuma, bool altYazma)
    {
        using var context = CreateContext((int)YetkiTipi.W, null);
        context.RolYetkileri = new OrtakMemorySet<RolYetki>([
            new() { RolId = 1, MenuTanimiId = FinansKok, YetkiTipiId = (int)kokSeviyesi },
            new() { RolId = 1, MenuTanimiId = PoGir, YetkiTipiId = (int)YetkiTipi.W }
        ]);
        var service = new RolService(context);
        Assert.Equal(kokOkuma, await service.HasUserPermissionAsync(7, YetkiKodlari.Finans.Modul, YetkiTipi.R));
        // İşlem kodu da diğer menüler gibi okunur: kök R ise alt W, R'ye sınırlanır.
        Assert.Equal(altOkuma, await service.HasUserPermissionAsync(7, YetkiKodlari.Finans.PoGir, YetkiTipi.R));
        Assert.Equal(altYazma, await service.HasUserPermissionAsync(7, YetkiKodlari.Finans.PoGir, YetkiTipi.W));
    }

    [Fact]
    public async Task UstW_AltIzniKendiligindenVermez()
    {
        using var context = CreateContext((int)YetkiTipi.N, null);
        var service = new RolService(context);
        Assert.True(await service.HasUserPermissionAsync(7, YetkiKodlari.Finans.Modul, YetkiTipi.W));
        Assert.False(await service.HasUserPermissionAsync(7, YetkiKodlari.Finans.PoGir, YetkiTipi.R));
    }

    [Fact]
    public async Task YalnizKokKisiselIzni_RolunSinirladigiAltYazmayiAcmaz()
    {
        using var context = CreateContext((int)YetkiTipi.W, null);
        context.RolYetkileri = new OrtakMemorySet<RolYetki>([
            new() { RolId = 1, MenuTanimiId = FinansKok, YetkiTipiId = (int)YetkiTipi.R },
            new() { RolId = 1, MenuTanimiId = PoGir, YetkiTipiId = (int)YetkiTipi.W }
        ]);
        context.KullaniciYetkileri = new OrtakMemorySet<KullaniciYetki>([
            new() { KullaniciId = 7, MenuTanimiId = FinansKok, IzinVerildi = true }
        ]);
        Assert.False(await new RolService(context).HasUserPermissionAsync(7, YetkiKodlari.Finans.PoGir, YetkiTipi.W));

        context.KullaniciYetkileri = new OrtakMemorySet<KullaniciYetki>([
            new() { KullaniciId = 7, MenuTanimiId = FinansKok, IzinVerildi = true },
            new() { KullaniciId = 7, MenuTanimiId = PoGir, IzinVerildi = true }
        ]);
        Assert.True(await new RolService(context).HasUserPermissionAsync(7, YetkiKodlari.Finans.PoGir, YetkiTipi.W));
    }

    [Fact]
    public async Task KisiselYetkiListesi_RolunSinirladigiAltSeviyeyiGosterir()
    {
        using var context = CreateContext((int)YetkiTipi.W, null);
        context.RolYetkileri = new OrtakMemorySet<RolYetki>([
            new() { RolId = 1, MenuTanimiId = FinansKok, YetkiTipiId = (int)YetkiTipi.R },
            new() { RolId = 1, MenuTanimiId = PoGir, YetkiTipiId = (int)YetkiTipi.W }
        ]);
        context.KullaniciYetkileri = new OrtakMemorySet<KullaniciYetki>([
            new() { KullaniciId = 7, MenuTanimiId = FinansKok, IzinVerildi = true }
        ]);
        var permissions = await new KullaniciYetkiService(context, new MemoryRoles(), new OrtakUser(), new OrtakMemoryUow())
            .GetAsync(7);
        Assert.NotNull(permissions);
        Assert.Equal((int)YetkiTipi.W, permissions.Single(x => x.MenuTanimiId == FinansKok).EtkinYetkiTipiId);
        var child = permissions.Single(x => x.Kod == YetkiKodlari.Finans.PoGir);
        Assert.Equal((int)YetkiTipi.W, child.RolYetkiTipiId);
        Assert.Equal((int)YetkiTipi.R, child.EtkinYetkiTipiId);
    }

    [Fact]
    public async Task IptalEdilenIzin_SonrakiIstekteUygulanir()
    {
        using var context = CreateContext((int)YetkiTipi.W, null);
        Assert.True(await new RolService(context).HasUserPermissionAsync(7, YetkiKodlari.Finans.PoGir, YetkiTipi.W));
        context.KullaniciYetkileri = new OrtakMemorySet<KullaniciYetki>([new()
        { KullaniciId = 7, MenuTanimiId = PoGir, IzinVerildi = false }]);
        // Her istek yeni scoped servis alır; oturum/JWT yetkiyi önbelleklemez.
        Assert.False(await new RolService(context).HasUserPermissionAsync(7, YetkiKodlari.Finans.PoGir, YetkiTipi.W));
    }

    [Fact]
    public async Task AyniIstekteYetkiSorgulariTekKezOkunur()
    {
        using var context = CreateContext((int)YetkiTipi.W, null);
        var service = new RolService(context);
        Assert.True(await service.HasUserPermissionAsync(7, YetkiKodlari.Finans.PoGir, YetkiTipi.W));
        context.KullaniciYetkileri = new OrtakMemorySet<KullaniciYetki>([new()
        { KullaniciId = 7, MenuTanimiId = PoGir, IzinVerildi = false }]);
        Assert.True(await service.HasUserPermissionAsync(7, YetkiKodlari.Finans.PoGir, YetkiTipi.W));
    }

    [Fact]
    public async Task KisiselIzin_YalnizKendiKodunuAcar()
    {
        using var context = CreateContext((int)YetkiTipi.N, null);
        context.KullaniciYetkileri = new OrtakMemorySet<KullaniciYetki>([new()
        { KullaniciId = 7, MenuTanimiId = BirimFiyatGoruntule, IzinVerildi = true }]);
        var service = new RolService(context);
        Assert.True(await service.HasUserPermissionAsync(7, YetkiKodlari.Finans.BirimFiyatGoruntule, YetkiTipi.R));
        Assert.False(await service.HasUserPermissionAsync(7, YetkiKodlari.Finans.BirimFiyatDegistir, YetkiTipi.W));
        Assert.False(await service.HasUserPermissionAsync(7, YetkiKodlari.Finans.PoGir, YetkiTipi.R));
    }

    [Fact]
    public async Task ModulKokuKapaliyken_AltIzinIstegiAcamaz()
    {
        using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().Options);
        context.Kullanicilar = new OrtakMemorySet<Kullanici>([new() { Id = 7, RolId = 1 }]);
        context.MenuTanimlari = new OrtakMemorySet<MenuTanimi>([
            new() { Id = AmbalajKok, Kod = YetkiKodlari.Ambalaj.Listele },
            new() { Id = AmbalajFormGoruntule, Kod = YetkiKodlari.Ambalaj.FormGoruntule, ParentId = AmbalajKok }
        ]);
        context.RolYetkileri = new OrtakMemorySet<RolYetki>([
            new() { RolId = 1, MenuTanimiId = AmbalajFormGoruntule, YetkiTipiId = (int)YetkiTipi.R }
        ]);
        context.KullaniciYetkileri = new OrtakMemorySet<KullaniciYetki>([]);
        var result = await new AuthorizationBehavior<
            _3K.Application.Features.AmbalajIslemleri.Queries.GetAmbalajUretimFormuQuery,
            Result<AmbalajUretimFormuModel>>(new OrtakUser(), new RolService(context)).Handle(new(),
            () => throw new Xunit.Sdk.XunitException("Modül kökü kapalıyken alt işlem çalışmamalı"), default);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task AnaParasalIzinOlmadanTumAltAlanlarKapaliKalir()
    {
        var roles = new MemoryRoles();
        foreach (var kod in ModulKodlari()) roles.Grants[kod] = 3;
        roles.Grants.Remove(YetkiKodlari.Finans.ParasalVeriGoruntule);
        var result = await new AlanErisimService(roles, new OrtakUser()).GetAsync();
        Assert.False(result.ParasalVeri);
        Assert.False(result.BirimFiyat);
        Assert.False(result.Tutar);
        Assert.False(result.Gelir);
        Assert.False(result.Gider);
        Assert.False(result.Karlilik);
        Assert.True(result.Olcu);
    }

    [Fact]
    public async Task ParasalAnaIzin_AltIzinleriKendiligindenVermez()
    {
        var roles = new MemoryRoles();
        roles.Grants[YetkiKodlari.Finans.ParasalVeriGoruntule] = 2;
        roles.Grants[YetkiKodlari.Finans.TutarGoruntule] = 2;
        var result = await new AlanErisimService(roles, new OrtakUser()).GetAsync();
        Assert.True(result.ParasalVeri);
        Assert.True(result.Tutar);
        Assert.False(result.BirimFiyat);
        Assert.False(result.Gelir);
        Assert.False(result.Gider);
        Assert.False(result.Karlilik);
    }

    [Fact]
    public async Task KendiMenusu_KisiselRetEdilenDugumuGondermez()
    {
        var uow = new OrtakMemoryUow();
        uow.Repo<Kullanici>().Rows.Add(new() { Id = 7, RolId = 1 });
        uow.Repo<Rol>().Rows.Add(new() { Id = 1, Ad = "Admin" });
        uow.Repo<KullaniciYetki>().Rows.Add(new() { KullaniciId = 7, MenuTanimiId = 5200, IzinVerildi = false });
        var roles = new MemoryRoles();
        roles.Menus.Add(new() { Id = 5200, Kod = YetkiKodlari.Finans.GelirGoruntule });
        roles.Permissions.Add(new() { MenuTanimiId = 5200, RolId = 1, YetkiTipiId = 2 });
        var result = await new GetKullaniciMenuQueryHandler(new OrtakUser(), uow, roles).Handle(new(), default);
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task RolDetayiVeKullaniciMenusu_AltDugumuKokunSeviyesiyleSinirlar()
    {
        var uow = new OrtakMemoryUow();
        uow.Repo<Rol>().Rows.Add(new() { Id = 7, Ad = "Sınırlı rol" });
        uow.Repo<Kullanici>().Rows.Add(new() { Id = 7, RolId = 7 });
        var roles = new MemoryRoles();
        roles.Menus.Add(new MenuTanimi { Id = FinansKok, Kod = YetkiKodlari.Finans.Modul,
            Children = [new MenuTanimi { Id = PoGir, Kod = YetkiKodlari.Finans.PoGir, ParentId = FinansKok }] });
        roles.Permissions.AddRange([
            new RolYetki { RolId = 7, MenuTanimiId = FinansKok, YetkiTipiId = (int)YetkiTipi.R },
            new RolYetki { RolId = 7, MenuTanimiId = PoGir, YetkiTipiId = (int)YetkiTipi.W }
        ]);
        var detail = await new GetRolDetayQueryHandler(uow, roles).Handle(new() { RolId = 7 }, default);
        Assert.Equal((int)YetkiTipi.R, Assert.Single(Assert.Single(detail.Value!.MenuAgaci).Children).YetkiTipiId);

        uow.Repo<KullaniciYetki>().Rows.Add(new() { KullaniciId = 7, MenuTanimiId = FinansKok, IzinVerildi = true });
        var own = await new GetKullaniciMenuQueryHandler(new OrtakUser(), uow, roles).Handle(new(), default);
        Assert.Equal((int)YetkiTipi.R, Assert.Single(Assert.Single(own.Value!).Children).YetkiTipiId);

        uow.Repo<KullaniciYetki>().Rows.Add(new() { KullaniciId = 7, MenuTanimiId = PoGir, IzinVerildi = true });
        own = await new GetKullaniciMenuQueryHandler(new OrtakUser(), uow, roles).Handle(new(), default);
        Assert.Equal((int)YetkiTipi.W, Assert.Single(Assert.Single(own.Value!).Children).YetkiTipiId);
    }

    [Fact]
    public async Task KendiMenusu_YetkisizKokAltindakiDugumleriGizler()
    {
        var uow = new OrtakMemoryUow();
        uow.Repo<Kullanici>().Rows.Add(new() { Id = 7, RolId = 1 });
        uow.Repo<Rol>().Rows.Add(new() { Id = 1, Ad = "Admin" });
        var roles = new MemoryRoles();
        roles.Menus.Add(new MenuTanimi { Id = 5, Kod = YetkiKodlari.Sandik.Yonetimi,
            Children = [new MenuTanimi { Id = 14, Kod = YetkiKodlari.Sandik.GridModulu, ParentId = 5 }] });
        roles.Permissions.Add(new RolYetki { RolId = 1, MenuTanimiId = 14, YetkiTipiId = (int)YetkiTipi.W });
        var result = await new GetKullaniciMenuQueryHandler(new OrtakUser(), uow, roles).Handle(new(), default);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task RolGuncelleme_RolYoneticisiSahipOlmadigiIzniRoleEkleyebilir()
    {
        var (uow, roles) = RoleFixture();
        var result = await new RolGuncelleCommandHandler(uow, roles, new OrtakUser()).Handle(new()
        {
            Id = 7, Ad = "Yeni ad", Yetkiler = [
                new() { MenuTanimiId = FinansKok, YetkiTipiId = 3 },
                new() { MenuTanimiId = KaliciSil, YetkiTipiId = 3 }
            ]
        }, default);
        Assert.True(result.IsSuccess);
        Assert.Equal("Yeni ad", uow.Repo<Rol>().Rows.Single().Ad);
        Assert.Contains(roles.Permissions, x => x.MenuTanimiId == KaliciSil && x.YetkiTipiId == 3);
        Assert.Equal(1, roles.UpdateCount);
        Assert.Single(uow.Repo<YetkiDegisikligi>().Rows);
    }

    [Fact]
    public async Task RolGuncelleme_AltSeviyeKokunSeviyesiyleKaydedilir()
    {
        var (uow, roles) = RoleFixture();
        var result = await new RolGuncelleCommandHandler(uow, roles, new OrtakUser()).Handle(new()
        {
            Id = 7, Ad = "Test rolü", Yetkiler = [
                new() { MenuTanimiId = AmbalajKok, YetkiTipiId = 2 },
                new() { MenuTanimiId = AmbalajKayitDuzenle, YetkiTipiId = 3 }
            ]
        }, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, roles.Permissions.Single(x => x.MenuTanimiId == AmbalajKok).YetkiTipiId);
        Assert.Equal(2, roles.Permissions.Single(x => x.MenuTanimiId == AmbalajKayitDuzenle).YetkiTipiId);
        Assert.Equal(1, roles.UpdateCount);
    }

    [Fact]
    public async Task RolGuncelleme_UstYetkiSonraAcilsaEskiAltYazmaGeriGelmez()
    {
        var (uow, roles) = RoleFixture();
        var first = await new RolGuncelleCommandHandler(uow, roles, new OrtakUser()).Handle(new()
        {
            Id = 7, Ad = "Test rolü", Yetkiler = [
                new() { MenuTanimiId = FinansKok, YetkiTipiId = (int)YetkiTipi.R },
                new() { MenuTanimiId = PoGir, YetkiTipiId = (int)YetkiTipi.W }
            ]
        }, default);
        Assert.True(first.IsSuccess);
        Assert.Equal((int)YetkiTipi.R, roles.Permissions.Single(x => x.MenuTanimiId == PoGir).YetkiTipiId);
        var second = await new RolGuncelleCommandHandler(uow, roles, new OrtakUser()).Handle(new()
        {
            Id = 7, Ad = "Test rolü", Yetkiler = [
                new() { MenuTanimiId = FinansKok, YetkiTipiId = (int)YetkiTipi.W },
                new() { MenuTanimiId = PoGir, YetkiTipiId = (int)YetkiTipi.N }
            ]
        }, default);
        Assert.True(second.IsSuccess);
        Assert.Equal((int)YetkiTipi.N, roles.Permissions.Single(x => x.MenuTanimiId == PoGir).YetkiTipiId);
    }

    [Theory]
    [InlineData(AmbalajKayitDuzenle, YetkiTipi.R)]
    [InlineData(BirimFiyatGoruntule, YetkiTipi.W)]
    public async Task RolGuncelleme_FinansVeAmbalajKodlariDigerMenulerGibiRVeyaWAlir(int menuId, YetkiTipi seviye)
    {
        var (uow, roles) = RoleFixture();
        var kokId = menuId == AmbalajKayitDuzenle ? AmbalajKok : FinansKok;
        var result = await new RolGuncelleCommandHandler(uow, roles, new OrtakUser()).Handle(new()
        {
            Id = 7, Ad = "Test rolü", Yetkiler = [
                new() { MenuTanimiId = kokId, YetkiTipiId = (int)YetkiTipi.W },
                new() { MenuTanimiId = menuId, YetkiTipiId = (int)seviye }
            ]
        }, default);
        Assert.True(result.IsSuccess);
        Assert.Equal((int)seviye, roles.Permissions.Single(x => x.MenuTanimiId == menuId).YetkiTipiId);
    }

    [Fact]
    public async Task RolGuncelleme_RolYoneticisiAmbalajYetkisiOlmadanYalnizKokOkumaIzniniAtayabilir()
    {
        var (uow, roles) = RoleFixture();
        var result = await new RolGuncelleCommandHandler(uow, roles, new OrtakUser()).Handle(new()
        {
            Id = 7, Ad = "Test rolü", Yetkiler = [new() { MenuTanimiId = AmbalajKok, YetkiTipiId = 2 }]
        }, default);

        Assert.True(result.IsSuccess);
        var permission = Assert.Single(roles.Permissions);
        Assert.Equal(AmbalajKok, permission.MenuTanimiId);
        Assert.Equal(2, permission.YetkiTipiId);
        Assert.Equal(1, roles.UpdateCount);
        Assert.Single(uow.Repo<YetkiDegisikligi>().Rows);
    }

    [Fact]
    public async Task RolGuncelleme_RolYonetimiSaltOkumaOlanAktorYetkiAtayamaz()
    {
        var (uow, roles) = RoleFixture();
        roles.Grants[YetkiKodlari.RolYonetimi] = 2;
        var result = await new RolGuncelleCommandHandler(uow, roles, new OrtakUser()).Handle(new()
        {
            Id = 7, Ad = "Yeni ad", Yetkiler = [new() { MenuTanimiId = AmbalajKayitDuzenle, YetkiTipiId = 3 }]
        }, default);

        Assert.Equal(403, result.StatusCode);
        Assert.Equal("Rol izinlerini değiştirme yetkiniz bulunmuyor.", result.Error!.Message);
        Assert.Equal("Test rolü", uow.Repo<Rol>().Rows.Single().Ad);
        Assert.Equal(0, uow.SaveCount);
        Assert.Equal(0, roles.UpdateCount);
    }

    [Theory]
    [InlineData(99999, 3, "Bilinmeyen izin.")]
    [InlineData(AmbalajKayitDuzenle, 4, "Yinelenen veya geçersiz izin.")]
    public async Task RolGuncelleme_GecersizVeyaBilinmeyenIzniReddeder(int menuId, int seviye, string hata)
    {
        var (uow, roles) = RoleFixture();
        var result = await new RolGuncelleCommandHandler(uow, roles, new OrtakUser()).Handle(new()
        {
            Id = 7, Ad = "Yeni ad", Yetkiler = [new() { MenuTanimiId = menuId, YetkiTipiId = seviye }]
        }, default);

        Assert.Equal(400, result.StatusCode);
        Assert.Equal(hata, result.Error!.Message);
        Assert.Equal("Test rolü", uow.Repo<Rol>().Rows.Single().Ad);
        Assert.Equal(0, uow.SaveCount);
        Assert.Equal(0, roles.UpdateCount);
    }

    [Fact]
    public async Task RolGuncelleme_YinelenenIzniReddeder()
    {
        var (uow, roles) = RoleFixture();
        var result = await new RolGuncelleCommandHandler(uow, roles, new OrtakUser()).Handle(new()
        {
            Id = 7, Ad = "Yeni ad", Yetkiler =
            [
                new() { MenuTanimiId = AmbalajKayitDuzenle, YetkiTipiId = 3 },
                new() { MenuTanimiId = AmbalajKayitDuzenle, YetkiTipiId = 3 }
            ]
        }, default);

        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Yinelenen veya geçersiz izin.", result.Error!.Message);
        Assert.Equal(0, uow.SaveCount);
        Assert.Equal(0, roles.UpdateCount);
    }

    [Fact]
    public async Task RolGuncelleme_BosListeTumYetkileriKaldirir()
    {
        var (uow, roles) = RoleFixture();
        roles.Permissions.Add(new() { RolId = 7, MenuTanimiId = FinansKok, YetkiTipiId = 3 });
        var result = await new RolGuncelleCommandHandler(uow, roles, new OrtakUser()).Handle(new()
        { Id = 7, Ad = "Test rolü", Yetkiler = [] }, default);
        Assert.True(result.IsSuccess);
        Assert.Empty(roles.Permissions);
        Assert.Single(uow.Repo<YetkiDegisikligi>().Rows);
    }

    [Fact]
    public async Task RolGuncelleme_YalnizAtanmisKullanicilaraCommitSonrasiYetkiOlayiGonderir()
    {
        var (uow, roles) = RoleFixture();
        uow.Repo<Kullanici>().Rows.AddRange([
            new() { Id = 8, RolId = 7 },
            new() { Id = 9, RolId = 7 },
            new() { Id = 10, RolId = 1 }
        ]);
        var events = new List<(int[] Idler, string Olay)>();
        var notifier = new RecordingNotifier((ids, olay) =>
        {
            Assert.False(uow.HasActiveTransaction);
            Assert.True(uow.SaveCount > 0);
            events.Add((ids.ToArray(), olay));
        });

        var result = await new RolGuncelleCommandHandler(uow, roles, new OrtakUser(), notifier)
            .Handle(new() { Id = 7, Ad = "Test rolü", Yetkiler = [] }, default);

        Assert.True(result.IsSuccess);
        var sent = Assert.Single(events);
        Assert.Equal([8, 9], sent.Idler);
        Assert.Equal(SseOlaylari.YetkiGuncellendi, sent.Olay);
    }

    [Fact]
    public async Task RolGuncelleme_RedHalindeYetkiOlayiGondermez()
    {
        var (uow, roles) = RoleFixture();
        var events = new List<string>();
        var notifier = new RecordingNotifier((_, olay) => events.Add(olay));

        var result = await new RolGuncelleCommandHandler(uow, roles, new OrtakUser(), notifier)
            .Handle(new() { Id = 7, Ad = "", Yetkiler = [] }, default);

        Assert.False(result.IsSuccess);
        Assert.Empty(events);
    }

    [Fact]
    public async Task RolOlusturma_IzinsizRolOlusturur_YoneticiAdiniReddeder()
    {
        var (uow, _) = RoleFixture();
        var handler = new RolOlusturCommandHandler(uow, new OrtakUser());
        var created = await handler.Handle(new() { Ad = "  Yeni rol  " }, default);
        Assert.True(created.IsSuccess);
        Assert.Equal("Yeni rol", created.Value!.Ad);
        Assert.Single(uow.Repo<YetkiDegisikligi>().Rows);

        Assert.Equal(400, (await handler.Handle(new() { Ad = " admin " }, default)).StatusCode);
    }

    [Fact]
    public async Task RolSilme_YoneticiRolunuKimligiyleKorur()
    {
        var uow = new OrtakMemoryUow();
        uow.Repo<Rol>().Rows.Add(new() { Id = SistemRolleri.AdminId, Ad = "Yeniden adlandırılmış" });
        var result = await new RolSilCommandHandler(uow).Handle(new() { Id = SistemRolleri.AdminId }, default);
        Assert.False(result.IsSuccess);
        Assert.Single(uow.Repo<Rol>().Rows);
    }

    [Fact]
    public async Task KisiselIzinServisi_KendiIzniniGuncellemeyiYazmadanReddeder()
    {
        using var context = CreateContext(1, null);
        var roles = new MemoryRoles();
        roles.Grants[YetkiKodlari.Kullanicilar] = 3;
        var uow = new OrtakMemoryUow();
        var service = new KullaniciYetkiService(context, roles, new OrtakUser(), uow);
        Assert.Equal(403, (await service.UpdateAsync(7, [new(5200, true)])).DurumKodu);
        Assert.Equal(403, (await service.RolAtamayiDogrulaAsync(7, 1)).DurumKodu);
        Assert.Equal(0, uow.SaveCount);
    }

    [Fact]
    public async Task RolAtama_AktordenHedefinEtkinSeviyesiniIster()
    {
        using var context = CreateContext((int)YetkiTipi.N, null);
        context.Roller = new OrtakMemorySet<Rol>([new() { Id = 77, Ad = "Eski rol" }]);
        context.RolYetkileri = new OrtakMemorySet<RolYetki>([
            new() { RolId = 77, MenuTanimiId = FinansKok, YetkiTipiId = (int)YetkiTipi.R },
            new() { RolId = 77, MenuTanimiId = PoGir, YetkiTipiId = (int)YetkiTipi.W }
        ]);
        var roles = new MemoryRoles();
        roles.Grants[YetkiKodlari.Kullanicilar] = (int)YetkiTipi.W;
        roles.Grants[YetkiKodlari.Finans.Modul] = (int)YetkiTipi.W;
        roles.Grants[YetkiKodlari.Finans.PoGir] = (int)YetkiTipi.R;
        var service = new KullaniciYetkiService(context, roles, new OrtakUser(), new OrtakMemoryUow());

        // Rol kökü R olduğu için alt işlem R'ye sınırlanır; aktörün R'si yeterlidir.
        Assert.True((await service.RolAtamayiDogrulaAsync(8, 77)).Basarili);

        context.KullaniciYetkileri = new OrtakMemorySet<KullaniciYetki>([
            new() { KullaniciId = 8, MenuTanimiId = FinansKok, IzinVerildi = true },
            new() { KullaniciId = 8, MenuTanimiId = PoGir, IzinVerildi = true }
        ]);
        Assert.Equal(403, (await service.RolAtamayiDogrulaAsync(8, 77)).DurumKodu);
        context.KullaniciYetkileri = new OrtakMemorySet<KullaniciYetki>([]);

        context.RolYetkileri = new OrtakMemorySet<RolYetki>([
            new() { RolId = 77, MenuTanimiId = FinansKok, YetkiTipiId = (int)YetkiTipi.W },
            new() { RolId = 77, MenuTanimiId = PoGir, YetkiTipiId = (int)YetkiTipi.W }
        ]);
        Assert.Equal(403, (await service.RolAtamayiDogrulaAsync(8, 77)).DurumKodu);
    }

    [Fact]
    public void OnayBaglami_IcIceScopeBitinceKapanir()
    {
        var execution = new ApprovalExecutionContext();
        using (execution.BeginApprovedExecution())
        {
            using (execution.BeginApprovedExecution())
                Assert.True(execution.IsExecutingApprovedCommand);
            Assert.True(execution.IsExecutingApprovedCommand);
        }
        Assert.False(execution.IsExecutingApprovedCommand);
    }

    [Fact]
    public void YetkiAgaci_DonguVeKayipUstDugumuKapaliSayar()
    {
        var agac = new YetkiAgaci([
            new YetkiDugumu(1, "a", 2),
            new YetkiDugumu(2, "b", 1),
            new YetkiDugumu(3, "c", 99),
            new YetkiDugumu(4, "d", null)
        ]);
        var seviyeler = agac.RolSeviyeleri(new Dictionary<int, int> { [1] = 3, [2] = 3, [3] = 3, [4] = 3 });
        Assert.Equal(YetkiTipi.N, seviyeler[1]);
        Assert.Equal(YetkiTipi.N, seviyeler[2]);
        Assert.Equal(YetkiTipi.N, seviyeler[3]);
        Assert.Equal(YetkiTipi.W, seviyeler[4]);
    }

    private static IEnumerable<string> ModulKodlari() =>
        new[] { typeof(YetkiKodlari.Finans), typeof(YetkiKodlari.Ambalaj) }
            .SelectMany(t => t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!);

    private static AppDbContext CreateContext(int rolSeviyesi, bool? karar)
    {
        var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().Options);
        context.Kullanicilar = new OrtakMemorySet<Kullanici>([new() { Id = 7, RolId = 1 }]);
        context.MenuTanimlari = new OrtakMemorySet<MenuTanimi>([
            new() { Id = FinansKok, Kod = YetkiKodlari.Finans.Modul },
            new() { Id = PoGir, Kod = YetkiKodlari.Finans.PoGir, ParentId = FinansKok },
            new() { Id = KaliciSil, Kod = YetkiKodlari.Finans.KaliciSil, ParentId = FinansKok },
            new() { Id = BirimFiyatGoruntule, Kod = YetkiKodlari.Finans.BirimFiyatGoruntule, ParentId = FinansKok },
            new() { Id = 5214, Kod = YetkiKodlari.Finans.BirimFiyatDegistir, ParentId = FinansKok }
        ]);
        context.RolYetkileri = new OrtakMemorySet<RolYetki>([
            new() { RolId = 1, MenuTanimiId = FinansKok, YetkiTipiId = (int)YetkiTipi.W },
            new() { RolId = 1, MenuTanimiId = PoGir, YetkiTipiId = rolSeviyesi }
        ]);
        context.KullaniciYetkileri = new OrtakMemorySet<KullaniciYetki>(karar.HasValue
            ? [new() { KullaniciId = 7, MenuTanimiId = PoGir, IzinVerildi = karar.Value }]
            : []);
        return context;
    }

    private static (OrtakMemoryUow, MemoryRoles) RoleFixture()
    {
        var uow = new OrtakMemoryUow();
        uow.Repo<Rol>().Rows.Add(new() { Id = 7, Ad = "Test rolü" });
        uow.Repo<MenuTanimi>().Rows.AddRange([
            new() { Id = AmbalajKok, Kod = YetkiKodlari.Ambalaj.Listele },
            new() { Id = FinansKok, Kod = YetkiKodlari.Finans.Modul },
            new() { Id = AmbalajKayitDuzenle, Kod = YetkiKodlari.Ambalaj.KayitDuzenle, ParentId = AmbalajKok },
            new() { Id = PoGir, Kod = YetkiKodlari.Finans.PoGir, ParentId = FinansKok },
            new() { Id = KaliciSil, Kod = YetkiKodlari.Finans.KaliciSil, ParentId = FinansKok },
            new() { Id = BirimFiyatGoruntule, Kod = YetkiKodlari.Finans.BirimFiyatGoruntule, ParentId = FinansKok }
        ]);
        var roles = new MemoryRoles();
        roles.Grants[YetkiKodlari.RolYonetimi] = 3;
        return (uow, roles);
    }

    private sealed class MemoryRoles : IRolService
    {
        public Dictionary<string, int> Grants { get; } = [];
        public List<RolYetki> Permissions { get; private set; } = [];
        public List<MenuTanimi> Menus { get; } = [];
        public int UpdateCount { get; private set; }
        public Task<bool> HasUserPermissionAsync(int userId, string code, YetkiTipi required, CancellationToken ct = default)
            => Task.FromResult(Grants.GetValueOrDefault(code, 1) >= (int)required);
        public Task<List<MenuTanimi>> GetMenuAgaciAsync(CancellationToken ct = default) => Task.FromResult(Menus);
        public Task<List<RolYetki>> GetRolYetkileriAsync(int rolId, CancellationToken ct = default) =>
            Task.FromResult(Permissions.Where(x => x.RolId == 0 || x.RolId == rolId).ToList());
        public Task YetkileriGuncelleAsync(int rolId, List<RolYetki> yetkiler, CancellationToken ct = default)
        { Permissions = yetkiler; UpdateCount++; return Task.CompletedTask; }
    }

    private sealed class RecordingNotifier(Action<IEnumerable<int>, string> onNotify) : ISseNotifier
    {
        public Task SubscribeAsync(object context, int kullaniciId) => Task.CompletedTask;
        public Task NotifyUsersAsync(IEnumerable<int> kullaniciIdleri, string eventName, string data = "refresh")
        {
            onNotify(kullaniciIdleri, eventName);
            return Task.CompletedTask;
        }
        public Task BroadcastApprovalUpdateAsync() => Task.CompletedTask;
    }
}
