using System.Security.Claims;
using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using _3K.Application.Behaviors;
using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.Commands;
using _3K.Application.Features.RolIslemleri.DTOs;
using _3K.Application.Features.RolIslemleri.Queries;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Services;
using _3K_API.Controllers;

namespace _3K.Application.Tests;

public class GranularYetkiTests
{
    [Fact]
    public void KaldirilanYetki_DigerMenuSeedSiralariniKaydirmaz()
    {
        using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=permission_seed_metadata_only;Username=unused;Password=unused")
            .Options);
        var seeds = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(MenuTanimi))!.GetSeedData();
        Assert.DoesNotContain(seeds, x => (int)x[nameof(MenuTanimi.Id)]! == 5000);
        Assert.Equal(2, seeds.Single(x => (int)x[nameof(MenuTanimi.Id)]! == 5100)[nameof(MenuTanimi.Sira)]);
    }

    [Theory]
    [InlineData(1, false, false)]
    [InlineData(3, false, false)]
    [InlineData(1, true, true)]
    [InlineData(3, null, true)]
    [InlineData(1, null, false)]
    public async Task GercekRolService_KisiselRetIzinVeRolOnceliginiUygular(int roleLevel, bool? decision, bool expected)
    {
        using var context = CreateContext(roleLevel, decision);
        Assert.Equal(expected, await new RolService(context).HasUserPermissionAsync(7, YetkiKodlari.Finans.PoGir, YetkiTipi.W));
        Assert.False(await new RolService(context).HasUserPermissionAsync(7, YetkiKodlari.Finans.KaliciSil, YetkiTipi.W));
    }

    [Theory]
    [InlineData(YetkiTipi.N, false, false)]
    [InlineData(YetkiTipi.R, true, false)]
    [InlineData(YetkiTipi.W, true, true)]
    public async Task GercekRolService_UstYetkiAltYazmayiSinirlar(YetkiTipi rootLevel,
        bool expectedRead, bool expectedWrite)
    {
        using var context = CreateContext((int)YetkiTipi.W, null);
        context.RolYetkileri = new OrtakMemorySet<RolYetki>([
            new() { RolId = 1, MenuTanimiId = 47, YetkiTipiId = (int)rootLevel },
            new() { RolId = 1, MenuTanimiId = YetkiKatalogu.Bul(YetkiKodlari.Finans.PoGir)!.Id,
                YetkiTipiId = (int)YetkiTipi.W }
        ]);
        var service = new RolService(context);
        Assert.Equal(expectedRead, await service.HasUserPermissionAsync(7, YetkiKodlari.Finans.Modul, YetkiTipi.R));
        Assert.Equal(expectedWrite, await service.HasUserPermissionAsync(7, YetkiKodlari.Finans.PoGir, YetkiTipi.W));
        Assert.Equal(expectedWrite, await service.HasUserPermissionAsync(7, YetkiKodlari.Finans.PoGir, YetkiTipi.R));
    }

    [Fact]
    public async Task GercekRolService_YalnizKokKisiselIzniDormantAltYazmayiDiriltmez()
    {
        using var context = CreateContext((int)YetkiTipi.W, null);
        context.RolYetkileri = new OrtakMemorySet<RolYetki>([
            new() { RolId = 1, MenuTanimiId = 47, YetkiTipiId = (int)YetkiTipi.R },
            new() { RolId = 1, MenuTanimiId = YetkiKatalogu.Bul(YetkiKodlari.Finans.PoGir)!.Id,
                YetkiTipiId = (int)YetkiTipi.W }
        ]);
        context.KullaniciYetkileri = new OrtakMemorySet<KullaniciYetki>([
            new() { KullaniciId = 7, MenuTanimiId = 47, IzinVerildi = true }
        ]);
        var service = new RolService(context);
        Assert.False(await service.HasUserPermissionAsync(7, YetkiKodlari.Finans.PoGir, YetkiTipi.W));
        context.KullaniciYetkileri = new OrtakMemorySet<KullaniciYetki>([
            new() { KullaniciId = 7, MenuTanimiId = 47, IzinVerildi = true },
            new() { KullaniciId = 7, MenuTanimiId = YetkiKatalogu.Bul(YetkiKodlari.Finans.PoGir)!.Id,
                IzinVerildi = true }
        ]);
        Assert.True(await service.HasUserPermissionAsync(7, YetkiKodlari.Finans.PoGir, YetkiTipi.W));
    }

    [Fact]
    public async Task KisiselYetkiListesi_KokKisiselIznindeDormantAltYazmayiGostermez()
    {
        using var context = CreateContext((int)YetkiTipi.W, null);
        context.RolYetkileri = new OrtakMemorySet<RolYetki>([
            new() { RolId = 1, MenuTanimiId = 47, YetkiTipiId = (int)YetkiTipi.R },
            new() { RolId = 1, MenuTanimiId = YetkiKatalogu.Bul(YetkiKodlari.Finans.PoGir)!.Id,
                YetkiTipiId = (int)YetkiTipi.W }
        ]);
        context.KullaniciYetkileri = new OrtakMemorySet<KullaniciYetki>([
            new() { KullaniciId = 7, MenuTanimiId = 47, IzinVerildi = true }
        ]);
        var service = new KullaniciYetkiService(context, new MemoryRoles(), new OrtakUser(), new OrtakMemoryUow());
        var permissions = await service.GetAsync(7);
        Assert.NotNull(permissions);
        Assert.Equal((int)YetkiTipi.W, permissions.Single(x => x.MenuTanimiId == 47).EtkinYetkiTipiId);
        var child = permissions.Single(x => x.Kod == YetkiKodlari.Finans.PoGir);
        Assert.Equal((int)YetkiTipi.W, child.RolYetkiTipiId);
        Assert.Equal((int)YetkiTipi.N, child.EtkinYetkiTipiId);
    }

    [Fact]
    public async Task RolIptali_AyniServisVeAyniOturumdaSonrakiIstegiEngeller()
    {
        using var context = CreateContext(3, null);
        var service = new RolService(context);
        Assert.True(await service.HasUserPermissionAsync(7, YetkiKodlari.Finans.PoGir, YetkiTipi.W));
        context.KullaniciYetkileri = new OrtakMemorySet<KullaniciYetki>([new()
        { KullaniciId = 7, MenuTanimiId = YetkiKatalogu.Bul(YetkiKodlari.Finans.PoGir)!.Id, IzinVerildi = false }]);
        Assert.False(await service.HasUserPermissionAsync(7, YetkiKodlari.Finans.PoGir, YetkiTipi.W));
    }

    [Fact]
    public async Task KisiselAlanIzni_YazmaEylemineVeyaDigerKodaDonusmez()
    {
        using var context = CreateContext(1, null);
        context.KullaniciYetkileri = new OrtakMemorySet<KullaniciYetki>([new()
        { KullaniciId = 7, MenuTanimiId = YetkiKatalogu.Bul(YetkiKodlari.Finans.BirimFiyatGoruntule)!.Id, IzinVerildi = true }]);
        var service = new RolService(context);
        Assert.True(await service.HasUserPermissionAsync(7, YetkiKodlari.Finans.BirimFiyatGoruntule, YetkiTipi.R));
        Assert.False(await service.HasUserPermissionAsync(7, YetkiKodlari.Finans.BirimFiyatGoruntule, YetkiTipi.W));
        Assert.False(await service.HasUserPermissionAsync(7, YetkiKodlari.Finans.BirimFiyatDegistir, YetkiTipi.W));
    }

    [Fact]
    public async Task AnaParasalIzinOlmadanTumAltAlanlarKapaliKalir()
    {
        var roles = new MemoryRoles();
        foreach (var item in YetkiKatalogu.Tum) roles.Grants[item.Kod] = 3;
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
    public async Task KendiMenuYaniti_KisiselReddinUzerineRolIzniGondermez()
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
        Assert.Equal(1, Assert.Single(result.Value!.MenuAgaci).YetkiTipiId);
    }

    [Fact]
    public async Task RolDetayiVeKullaniciMenusu_KokOkumaAltYazmaEyleminiKapaliGosterir()
    {
        var uow = new OrtakMemoryUow();
        uow.Repo<Rol>().Rows.Add(new() { Id = 7, Ad = "Sınırlı rol" });
        uow.Repo<Kullanici>().Rows.Add(new() { Id = 7, RolId = 7 });
        var poGir = YetkiKatalogu.Bul(YetkiKodlari.Finans.PoGir)!;
        var roles = new MemoryRoles();
        roles.Menus.Add(new MenuTanimi { Id = 47, Kod = YetkiKodlari.Finans.Modul,
            Children = [new MenuTanimi { Id = poGir.Id, Kod = poGir.Kod, ParentId = 47 }] });
        roles.Permissions.AddRange([
            new RolYetki { RolId = 7, MenuTanimiId = 47, YetkiTipiId = (int)YetkiTipi.R },
            new RolYetki { RolId = 7, MenuTanimiId = poGir.Id, YetkiTipiId = (int)YetkiTipi.W }
        ]);
        var detail = await new GetRolDetayQueryHandler(uow, roles).Handle(new() { RolId = 7 }, default);
        Assert.Equal((int)YetkiTipi.N, Assert.Single(Assert.Single(detail.Value!.MenuAgaci).Children).YetkiTipiId);
        uow.Repo<KullaniciYetki>().Rows.Add(new() { KullaniciId = 7, MenuTanimiId = 47, IzinVerildi = true });
        var own = await new GetKullaniciMenuQueryHandler(new OrtakUser(), uow, roles).Handle(new(), default);
        Assert.Equal((int)YetkiTipi.N, Assert.Single(Assert.Single(own.Value!.MenuAgaci).Children).YetkiTipiId);
        uow.Repo<KullaniciYetki>().Rows.Add(new() { KullaniciId = 7, MenuTanimiId = poGir.Id, IzinVerildi = true });
        own = await new GetKullaniciMenuQueryHandler(new OrtakUser(), uow, roles).Handle(new(), default);
        Assert.Equal((int)YetkiTipi.W, Assert.Single(Assert.Single(own.Value!.MenuAgaci).Children).YetkiTipiId);
    }

    [Fact]
    public async Task MenuEndpoint_YetkisizSandikKokunuAltYetkiOlsaDaGizler()
    {
        var mediator = DispatchProxy.Create<IMediator, MenuMediatorProxy>();
        ((MenuMediatorProxy)(object)mediator).Response = Result<RolDetayDto>.Success(new()
        {
            MenuAgaci = [new MenuTreeDto { Id = 5, Kod = "sandik-yonetimi", YetkiTipiId = 1,
                Children = [new MenuTreeDto { Id = 14, Kod = "grid-modulu", YetkiTipiId = 3 }] }]
        });
        var response = Assert.IsType<OkObjectResult>(await new MenuController(mediator).GetKullaniciMenu());
        Assert.Empty(Assert.IsType<List<MenuTreeDto>>(response.Value));
    }

    [Fact]
    public async Task RolGuncelleme_RolYoneticisiSahipOlmadigiKritikIzniRoleEkleyebilir()
    {
        var (uow, roles) = RoleFixture();
        var result = await new RolGuncelleCommandHandler(uow, roles, new OrtakUser()).Handle(new()
        {
            Id = 7, Ad = "Yeni ad", Yetkiler = [
                new() { MenuTanimiId = 47, YetkiTipiId = 3 },
                new() { MenuTanimiId = YetkiKatalogu.Bul(YetkiKodlari.Finans.KaliciSil)!.Id, YetkiTipiId = 3 }
            ]
        }, default);
        Assert.True(result.IsSuccess);
        Assert.Equal("Yeni ad", uow.Repo<Rol>().Rows.Single().Ad);
        Assert.Contains(roles.Permissions, x => x.MenuTanimiId == YetkiKatalogu.Bul(YetkiKodlari.Finans.KaliciSil)!.Id &&
            x.YetkiTipiId == 3);
        Assert.Equal(1, roles.UpdateCount);
        Assert.Single(uow.Repo<YetkiDegisikligi>().Rows);
    }

    [Fact]
    public async Task RolGuncelleme_KokOkumaAltYazmaIzniniKaldirir()
    {
        var (uow, roles) = RoleFixture();
        var result = await new RolGuncelleCommandHandler(uow, roles, new OrtakUser()).Handle(new()
        {
            Id = 7,
            Ad = "Test rolü",
            Yetkiler =
            [
                new() { MenuTanimiId = 46, YetkiTipiId = 2 },
                new() { MenuTanimiId = YetkiKatalogu.Bul(YetkiKodlari.Ambalaj.KayitDuzenle)!.Id, YetkiTipiId = 3 }
            ]
        }, default);

        Assert.True(result.IsSuccess);
        var permission = Assert.Single(roles.Permissions, x => x.YetkiTipiId >= 2);
        Assert.Equal(46, permission.MenuTanimiId);
        Assert.Equal(2, permission.YetkiTipiId);
        Assert.Equal(1, roles.UpdateCount);
        Assert.Single(uow.Repo<YetkiDegisikligi>().Rows);
    }

    [Fact]
    public async Task RolGuncelleme_UstYetkiSonraAcilsaEskiAltYazmaGeriGelmez()
    {
        var (uow, roles) = RoleFixture();
        var poGir = YetkiKatalogu.Bul(YetkiKodlari.Finans.PoGir)!;
        var first = await new RolGuncelleCommandHandler(uow, roles, new OrtakUser()).Handle(new()
        {
            Id = 7, Ad = "Test rolü", Yetkiler = [
                new() { MenuTanimiId = 47, YetkiTipiId = (int)YetkiTipi.R },
                new() { MenuTanimiId = poGir.Id, YetkiTipiId = (int)YetkiTipi.W }
            ]
        }, default);
        Assert.True(first.IsSuccess);
        Assert.Equal((int)YetkiTipi.N, roles.Permissions.Single(x => x.MenuTanimiId == poGir.Id).YetkiTipiId);
        var second = await new RolGuncelleCommandHandler(uow, roles, new OrtakUser()).Handle(new()
        {
            Id = 7, Ad = "Test rolü", Yetkiler = [
                new() { MenuTanimiId = 47, YetkiTipiId = (int)YetkiTipi.W },
                new() { MenuTanimiId = poGir.Id, YetkiTipiId = (int)YetkiTipi.N }
            ]
        }, default);
        Assert.True(second.IsSuccess);
        Assert.Equal((int)YetkiTipi.N, roles.Permissions.Single(x => x.MenuTanimiId == poGir.Id).YetkiTipiId);
    }

    [Theory]
    [InlineData("finans-siparis", YetkiTipi.W)]
    [InlineData("finans-okuma", YetkiTipi.R)]
    public async Task RolSablonu_AltIzinlerineUygunKokDuzeyiVerir(string template, YetkiTipi expectedRoot)
    {
        var (uow, roles) = RoleFixture();
        var result = await new RolOlusturCommandHandler(uow, roles, new OrtakUser()).Handle(new()
        { Ad = "Yeni rol", SablonKodu = template }, default);
        Assert.True(result.IsSuccess);
        Assert.Equal((int)expectedRoot, roles.Permissions.Single(x => x.MenuTanimiId == 47).YetkiTipiId);
    }

    [Fact]
    public async Task RolGuncelleme_RolYoneticisiAmbalajYetkisiOlmadanYalnizKokOkumaIzniniAtayabilir()
    {
        var (uow, roles) = RoleFixture();
        var result = await new RolGuncelleCommandHandler(uow, roles, new OrtakUser()).Handle(new()
        {
            Id = 7,
            Ad = "Test rolü",
            Yetkiler = [new() { MenuTanimiId = 46, YetkiTipiId = 2 }]
        }, default);

        Assert.True(result.IsSuccess);
        var permission = Assert.Single(roles.Permissions);
        Assert.Equal(46, permission.MenuTanimiId);
        Assert.Equal(2, permission.YetkiTipiId);
        Assert.Equal(1, roles.UpdateCount);
        Assert.Single(uow.Repo<YetkiDegisikligi>().Rows);
    }

    [Fact]
    public async Task RolGuncelleme_RolYonetimiSaltOkumaOlanAktorYetkiAtayamaz()
    {
        var (uow, roles) = RoleFixture();
        roles.Grants["rol-yonetimi"] = 2;
        var result = await new RolGuncelleCommandHandler(uow, roles, new OrtakUser()).Handle(new()
        {
            Id = 7, Ad = "Yeni ad", Yetkiler = [new() { MenuTanimiId = YetkiKatalogu.Bul(YetkiKodlari.Ambalaj.KayitDuzenle)!.Id, YetkiTipiId = 3 }]
        }, default);

        Assert.Equal(403, result.StatusCode);
        Assert.Equal("Rol izinlerini değiştirme yetkiniz bulunmuyor.", result.Error!.Message);
        Assert.Equal("Test rolü", uow.Repo<Rol>().Rows.Single().Ad);
        Assert.Equal(0, uow.SaveCount);
        Assert.Equal(0, roles.UpdateCount);
    }

    [Theory]
    [InlineData(99999, 3, "Bilinmeyen izin.")]
    [InlineData(5100, 2, "İşlem ve alan izinlerinde yalnız tanımlı izin seviyesi kullanılabilir.")]
    [InlineData(5100, 4, "Yinelenen veya geçersiz izin.")]
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
        var menuId = YetkiKatalogu.Bul(YetkiKodlari.Ambalaj.KayitDuzenle)!.Id;
        var result = await new RolGuncelleCommandHandler(uow, roles, new OrtakUser()).Handle(new()
        {
            Id = 7, Ad = "Yeni ad", Yetkiler =
            [
                new() { MenuTanimiId = menuId, YetkiTipiId = 3 },
                new() { MenuTanimiId = menuId, YetkiTipiId = 3 }
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
        roles.Permissions.Add(new() { RolId = 7, MenuTanimiId = 47, YetkiTipiId = 3 });
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
    public async Task KisiselIzinServisi_KendiIzniniGuncellemeyiYazmadanReddeder()
    {
        using var context = CreateContext(1, null);
        var roles = new MemoryRoles();
        roles.Grants["kullanicilar"] = 3;
        var uow = new OrtakMemoryUow();
        var service = new KullaniciYetkiService(context, roles, new OrtakUser(), uow);
        Assert.Equal(403, (await service.UpdateAsync(7, [new(5200, true)])).DurumKodu);
        Assert.Equal(403, (await service.RolAtamayiDogrulaAsync(7, 1)).DurumKodu);
        Assert.Equal(0, uow.SaveCount);
    }

    [Fact]
    public async Task RolAtama_DormantAltYazmaIzniniAktordenIstemez_AktifYazmayiIster()
    {
        using var context = CreateContext((int)YetkiTipi.N, null);
        context.Roller = new OrtakMemorySet<Rol>([new() { Id = 77, Ad = "Eski rol" }]);
        var poGir = YetkiKatalogu.Bul(YetkiKodlari.Finans.PoGir)!;
        context.RolYetkileri = new OrtakMemorySet<RolYetki>([
            new() { RolId = 77, MenuTanimiId = 47, YetkiTipiId = (int)YetkiTipi.R },
            new() { RolId = 77, MenuTanimiId = poGir.Id, YetkiTipiId = (int)YetkiTipi.W }
        ]);
        var roles = new MemoryRoles();
        roles.Grants["kullanicilar"] = (int)YetkiTipi.W;
        roles.Grants[YetkiKodlari.Finans.Modul] = (int)YetkiTipi.W;
        var service = new KullaniciYetkiService(context, roles, new OrtakUser(), new OrtakMemoryUow());

        Assert.True((await service.RolAtamayiDogrulaAsync(8, 77)).Basarili);

        context.KullaniciYetkileri = new OrtakMemorySet<KullaniciYetki>([
            new() { KullaniciId = 8, MenuTanimiId = 47, IzinVerildi = true },
            new() { KullaniciId = 8, MenuTanimiId = poGir.Id, IzinVerildi = true }
        ]);
        Assert.Equal(403, (await service.RolAtamayiDogrulaAsync(8, 77)).DurumKodu);
        context.KullaniciYetkileri = new OrtakMemorySet<KullaniciYetki>([]);

        context.RolYetkileri = new OrtakMemorySet<RolYetki>([
            new() { RolId = 77, MenuTanimiId = 47, YetkiTipiId = (int)YetkiTipi.W },
            new() { RolId = 77, MenuTanimiId = poGir.Id, YetkiTipiId = (int)YetkiTipi.W }
        ]);
        Assert.Equal(403, (await service.RolAtamayiDogrulaAsync(8, 77)).DurumKodu);
    }

    [Fact]
    public void KatalogBagimsizdir_SaltOkumaSablonlariYazmaIzniIcermez()
    {
        Assert.Equal(YetkiKatalogu.Tum.Count, YetkiKatalogu.Tum.Select(x => x.Kod).Distinct().Count());
        Assert.DoesNotContain(YetkiKatalogu.Tum, x => x.Kod is "finans-yonetimi" or "ambalaj-uretim-listesi");
        foreach (var template in RolSablonlari.Tum.Where(x => x.Kod is "uretim-goruntuleme" or "finans-okuma" or "finans-rapor"))
            Assert.All(template.IzinKodlari, code => Assert.Equal(YetkiTipi.R, YetkiKatalogu.Bul(code)!.GerekenYetki));
        Assert.DoesNotContain(RolSablonlari.Tum.Single(x => x.Kod == "uretim-personel").IzinKodlari,
            code => YetkiKatalogu.Bul(code)!.Kritik);
    }

    [Fact]
    public void OnayBaglami_GercekOturumuKorumaliBaslatandanAyirirVeScopeBitinceGeriDoner()
    {
        var execution = new ApprovalExecutionContext();
        var http = new DefaultHttpContext { User = new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "99")], "test")) };
        var user = new CurrentUserService(new HttpContextAccessor { HttpContext = http }, execution);
        using (execution.BeginApprovedExecution(7, true))
        {
            Assert.Equal(99, user.UserId);
            Assert.Equal(7, user.IslemKullaniciId);
        }
        Assert.Equal(99, user.IslemKullaniciId);
        Assert.Null(execution.InitiatorUserId);
        using (execution.BeginApprovedExecution(7, false))
            Assert.Equal(99, user.IslemKullaniciId);
    }

    private static AppDbContext CreateContext(int roleLevel, bool? decision)
    {
        var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().Options);
        context.Kullanicilar = new OrtakMemorySet<Kullanici>([new() { Id = 7, RolId = 1 }]);
        var poGir = YetkiKatalogu.Bul(YetkiKodlari.Finans.PoGir)!;
        var kaliciSil = YetkiKatalogu.Bul(YetkiKodlari.Finans.KaliciSil)!;
        var birimFiyat = YetkiKatalogu.Bul(YetkiKodlari.Finans.BirimFiyatGoruntule)!;
        context.MenuTanimlari = new OrtakMemorySet<MenuTanimi>([
            new() { Id = 47, Kod = YetkiKodlari.Finans.Modul },
            new() { Id = poGir.Id, Kod = poGir.Kod, ParentId = 47 },
            new() { Id = kaliciSil.Id, Kod = kaliciSil.Kod, ParentId = 47 },
            new() { Id = birimFiyat.Id, Kod = birimFiyat.Kod, ParentId = 47 }
        ]);
        context.RolYetkileri = new OrtakMemorySet<RolYetki>([
            new() { RolId = 1, MenuTanimiId = 47, YetkiTipiId = (int)YetkiTipi.W },
            new() { RolId = 1, MenuTanimiId = poGir.Id, YetkiTipiId = roleLevel }
        ]);
        context.KullaniciYetkileri = new OrtakMemorySet<KullaniciYetki>(decision.HasValue
            ? [new() { KullaniciId = 7, MenuTanimiId = poGir.Id, IzinVerildi = decision.Value }]
            : []);
        return context;
    }

    [Fact]
    public async Task OnayliFinansKomutu_BaslataninKaldirilmisIzniniOnaylayaninIzniyleAsamaz()
    {
        var roles = new MemoryRoles { AllowedUserId = 99 };
        roles.Grants[YetkiKodlari.Finans.IsIptal] = 3;
        var approval = new ApprovalExecutionContext();
        using var scope = approval.BeginApprovedExecution(7, true);
        var invoked = false;
        var response = await new AuthorizationBehavior<_3K.Application.Features.FinansIslemleri.Commands.FinansIsKaydiIptalCommand, Result>(
            new OrtakUser(99), roles, approvalExecutionContext: approval).Handle(new(), () =>
            { invoked = true; return Task.FromResult(Result.Success()); }, default);
        Assert.Equal(403, response.StatusCode);
        Assert.False(invoked);
    }

    [Fact]
    public async Task OnayliFinansKomutu_BaslatanKimligiOlmadanCalismaz()
    {
        var roles = new MemoryRoles();
        roles.Grants[YetkiKodlari.Finans.IsIptal] = 3;
        var approval = new ApprovalExecutionContext();
        using var scope = approval.BeginApprovedExecution();
        var response = await new AuthorizationBehavior<_3K.Application.Features.FinansIslemleri.Commands.FinansIsKaydiIptalCommand, Result>(
            new OrtakUser(99), roles, approvalExecutionContext: approval).Handle(new(),
            () => throw new Xunit.Sdk.XunitException("Kimlik olmadan handler çalışmamalı"), default);
        Assert.Equal(403, response.StatusCode);
    }

    private static (OrtakMemoryUow, MemoryRoles) RoleFixture()
    {
        var uow = new OrtakMemoryUow();
        uow.Repo<Rol>().Rows.Add(new() { Id = 7, Ad = "Test rolü" });
        uow.Repo<MenuTanimi>().Rows.AddRange([
            new() { Id = 46, Kod = YetkiKodlari.Ambalaj.Listele },
            new() { Id = 47, Kod = YetkiKodlari.Finans.Modul }
        ]);
        foreach (var item in YetkiKatalogu.Tum)
            uow.Repo<MenuTanimi>().Rows.Add(new() { Id = item.Id, Kod = item.Kod, ParentId = item.ParentId });
        var roles = new MemoryRoles();
        roles.Grants["rol-yonetimi"] = 3;
        return (uow, roles);
    }

    [Fact]
    public async Task UretimModuluRootRet_AcikFormIzniyleAsilamaz()
    {
        var roles = new MemoryRoles();
        roles.Grants[YetkiKodlari.Ambalaj.FormGoruntule] = 2;
        var result = await new AuthorizationBehavior<
            _3K.Application.Features.AmbalajIslemleri.Queries.GetAmbalajUretimFormuQuery,
            Result<_3K.Core.Models.AmbalajUretimFormuModel>>(new OrtakUser(), roles).Handle(new(),
            () => throw new Xunit.Sdk.XunitException("Modül erişimi olmadan alt işlem çalışmamalı"), default);
        Assert.Equal(403, result.StatusCode);
    }

    private sealed class MemoryRoles : IRolService
    {
        public int? AllowedUserId { get; init; }
        public Dictionary<string, int> Grants { get; } = [];
        public List<RolYetki> Permissions { get; private set; } = [];
        public List<MenuTanimi> Menus { get; } = [];
        public int UpdateCount { get; private set; }
        public Task<bool> HasUserPermissionAsync(int userId, string code, YetkiTipi required, CancellationToken ct = default)
            => Task.FromResult((!AllowedUserId.HasValue || AllowedUserId == userId) && Grants.GetValueOrDefault(code, 1) >= (int)required);
        public Task<bool> IsAdminAsync(int userId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<List<MenuTanimi>> GetMenuAgaciAsync(CancellationToken ct = default) => Task.FromResult(Menus);
        public Task<List<RolYetki>> GetRolYetkileriAsync(int rolId, CancellationToken ct = default) => Task.FromResult(Permissions);
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

    public class MenuMediatorProxy : DispatchProxy
    {
        public Result<RolDetayDto> Response { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => targetMethod?.Name == nameof(IMediator.Send)
                ? Task.FromResult(Response)
                : throw new NotSupportedException(targetMethod?.Name);
    }
}
