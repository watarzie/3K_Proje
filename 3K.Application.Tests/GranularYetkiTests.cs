using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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

namespace _3K.Application.Tests;

public class GranularYetkiTests
{
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

    [Fact]
    public async Task RolIptali_AyniServisVeAyniOturumdaSonrakiIstegiEngeller()
    {
        using var context = CreateContext(3, null);
        var service = new RolService(context);
        Assert.True(await service.HasUserPermissionAsync(7, YetkiKodlari.Finans.PoGir, YetkiTipi.W));
        context.KullaniciYetkileri = new OrtakMemorySet<KullaniciYetki>([new()
        { KullaniciId = 7, MenuTanimi = new() { Kod = YetkiKodlari.Finans.PoGir }, IzinVerildi = false }]);
        Assert.False(await service.HasUserPermissionAsync(7, YetkiKodlari.Finans.PoGir, YetkiTipi.W));
    }

    [Fact]
    public async Task KisiselAlanIzni_YazmaEylemineVeyaDigerKodaDonusmez()
    {
        using var context = CreateContext(1, null);
        context.KullaniciYetkileri = new OrtakMemorySet<KullaniciYetki>([new()
        { KullaniciId = 7, MenuTanimi = new() { Kod = YetkiKodlari.Finans.BirimFiyatGoruntule }, IzinVerildi = true }]);
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
    public async Task RolGuncelleme_SahipOlunmayanKritikIzniKendineEkleyemez_VeriDegismez()
    {
        var (uow, roles) = RoleFixture();
        var result = await new RolGuncelleCommandHandler(uow, roles, new OrtakUser()).Handle(new()
        {
            Id = 7, Ad = "Yetkisiz yeni ad", Yetkiler = [new() { MenuTanimiId = YetkiKatalogu.Bul(YetkiKodlari.Finans.KaliciSil)!.Id, YetkiTipiId = 3 }]
        }, default);
        Assert.Equal(403, result.StatusCode);
        Assert.Equal("Test rolü", uow.Repo<Rol>().Rows.Single().Ad);
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
    public async Task KisiselIzinServisi_KendiIzniniGuncellemeyiYazmadanReddeder()
    {
        using var context = CreateContext(1, null);
        var roles = new MemoryRoles();
        roles.Grants[YetkiKodlari.YetkiAtama] = 3;
        var uow = new OrtakMemoryUow();
        var service = new KullaniciYetkiService(context, roles, new OrtakUser(), uow);
        Assert.Equal(403, (await service.UpdateAsync(7, [new(5200, true)])).DurumKodu);
        Assert.Equal(403, (await service.RolAtamayiDogrulaAsync(7, 1)).DurumKodu);
        Assert.Equal(0, uow.SaveCount);
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
        context.RolYetkileri = new OrtakMemorySet<RolYetki>([new()
        { RolId = 1, YetkiTipiId = roleLevel, MenuTanimi = new() { Kod = YetkiKodlari.Finans.PoGir } }]);
        context.KullaniciYetkileri = new OrtakMemorySet<KullaniciYetki>(decision.HasValue
            ? [new() { KullaniciId = 7, MenuTanimi = new() { Kod = YetkiKodlari.Finans.PoGir }, IzinVerildi = decision.Value }]
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
        foreach (var item in YetkiKatalogu.Tum)
            uow.Repo<MenuTanimi>().Rows.Add(new() { Id = item.Id, Kod = item.Kod });
        var roles = new MemoryRoles();
        roles.Grants[YetkiKodlari.YetkiAtama] = 3;
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
}
