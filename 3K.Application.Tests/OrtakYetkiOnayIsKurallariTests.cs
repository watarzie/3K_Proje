using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using _3K.Application.Behaviors;
using _3K.Application.Common;
using _3K.Application.Features.OnayIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Application.Features.UcKIslemleri.Commands;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

public class OrtakYetkiOnayIsKurallariTests
{
    [Theory]
    [InlineData(false, 7)]
    [InlineData(true, null)]
    public async Task GuvenliIstek_OturumVeyaKimlikYoksaHandlerCalismaz(bool authenticated, int? userId)
    {
        var rol = new RolFake();
        var next = false;
        var result = await new AuthorizationBehavior<OrtakQuery, Result>(new OrtakUser(userId, authenticated), rol)
            .Handle(new OrtakQuery(), () => { next = true; return Task.FromResult(Result.Success()); }, default);
        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.StatusCode);
        Assert.False(next);
        Assert.Empty(rol.Calls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GuvenliIstek_MenuBaglamiYoksaReddedilir(string? menu)
    {
        var result = await new AuthorizationBehavior<OrtakQuery, Result>(new OrtakUser(MenuKod: menu), new RolFake())
            .Handle(new OrtakQuery(), () => throw new Xunit.Sdk.XunitException("Handler must not run"), default);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task QueryOkuma_CommandYazmaVeSabitMenuyuKullanir()
    {
        var rol = new RolFake();
        await new AuthorizationBehavior<SabitQuery, Result>(new OrtakUser(MenuKod: "sahte-menu"), rol)
            .Handle(new SabitQuery(), () => Task.FromResult(Result.Success()), default);
        await new AuthorizationBehavior<SabitCommand, Result>(new OrtakUser(MenuKod: "baska-menu"), rol)
            .Handle(new SabitCommand(), () => Task.FromResult(Result.Success()), default);
        Assert.Equal([(7, "grid-modulu", YetkiTipi.R), (7, "sabit", YetkiTipi.W)], rol.Calls);
    }

    [Fact]
    public async Task CokluMenuIstek_TumGereksinimlerSaglanmadanCalismaz()
    {
        var rol = new RolFake { DeniedMenu = "ikinci" };
        var result = await new AuthorizationBehavior<CokluCommand, Result>(new OrtakUser(), rol)
            .Handle(new CokluCommand(), () => throw new Xunit.Sdk.XunitException("Handler must not run"), default);
        Assert.Equal(403, result.StatusCode);
        Assert.Equal(2, rol.Calls.Count);
        Assert.Equal("ikinci", rol.Calls[1].Menu);
    }

    [Theory]
    [InlineData(1, "Admin", YetkiTipi.N, YetkiTipi.R, false)]
    [InlineData(8, "Personel", YetkiTipi.R, YetkiTipi.R, true)]
    [InlineData(8, "Personel", YetkiTipi.R, YetkiTipi.W, false)]
    [InlineData(8, "Personel", YetkiTipi.W, YetkiTipi.R, true)]
    [InlineData(8, "Personel", YetkiTipi.W, YetkiTipi.W, true)]
    public async Task MenuYetkisi_RolKaydindanGelir_AdminBypassYok(int roleId, string roleName, YetkiTipi granted, YetkiTipi required, bool allowed)
    {
        using var context = Context(roleId, roleName, granted);
        var service = new RolService(context);
        Assert.Equal(allowed, await service.HasUserPermissionAsync(7, "grid-modulu", required));
        Assert.False(await service.HasUserPermissionAsync(7, "diger-menu", required));
        Assert.False(await service.HasUserPermissionAsync(999, "grid-modulu", required));
        Assert.False(await service.HasUserPermissionAsync(7, " ", required));
    }

    [Theory]
    [InlineData(1, "BaskaAd", true, true)]
    [InlineData(42, "aDmIn", true, true)]
    [InlineData(8, "Personel", true, false)]
    [InlineData(8, "Sistem Yönetici", false, false)]
    public async Task OnayYetkisi_MenuYazmaYetkisindenAyridirVeKendiTalebiKuraliVardir(int roleId, string roleName, bool configured, bool admin)
    {
        using var context = Context(roleId, roleName, YetkiTipi.W);
        var codes = new List<OnayIslemYetki>();
        if (configured) codes.Add(new() { Id = 1, RolId = roleId, IslemKodu = "UCK_TEST" });
        codes.Add(new() { Id = 2, RolId = 999, IslemKodu = "DIGER" });
        context.OnayIslemYetkileri = new OrtakMemorySet<OnayIslemYetki>(codes);
        var service = new OnayYetkiService(context);
        Assert.True(await new RolService(context).HasUserPermissionAsync(7, "grid-modulu", YetkiTipi.W));
        Assert.Equal(admin || configured, await service.KullaniciIslemOnaylayabilirMiAsync(7, " UCK_TEST ", 9));
        Assert.Equal(admin, await service.KullaniciIslemOnaylayabilirMiAsync(7, "UCK_TEST", 7));
        Assert.Equal(admin, await service.KullaniciIslemOnaylayabilirMiAsync(7, "DIGER", 9));
        Assert.False(await service.KullaniciIslemOnaylayabilirMiAsync(0, "UCK_TEST", 9));
        Assert.False(await service.KullaniciIslemOnaylayabilirMiAsync(999, "UCK_TEST", 9));
    }

    [Fact]
    public async Task OnayVarsayilanlari_OperasyonYoksaKuyruk_UcKDurumYoksaDogrudan()
    {
        using var data = new ApprovalFixture();
        var operation = await data.Run(new AyarliCommand());
        Assert.Equal(StatusConstants.ActionQueuedForApproval, operation.StatusCode);
        Assert.Equal(0, data.Executed);
        Assert.Single(data.Uow.Repo<OnayBekleyenIslem>().Rows);
        var ucK = await data.Run(new UcKDurumGuncelleCommand { KarsilamaTipiId = (int)UcKDurum.TedarikcidenGeldi });
        Assert.True(ucK.IsSuccess);
        Assert.Equal(1, data.Executed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task YapilandirilmisOperasyonOnayi_KuralDegeriniIzler(bool require)
    {
        using var data = new ApprovalFixture();
        data.Uow.Repo<OnayOperasyonKurali>().Rows.Add(new() { IslemKodu = "TEST_OPERATION", OnayGerektirirMi = require });
        var result = await data.Run(new AyarliCommand());
        Assert.True(result.IsSuccess);
        Assert.Equal(require ? 0 : 1, data.Executed);
        Assert.Equal(require ? 1 : 0, data.Uow.Repo<OnayBekleyenIslem>().Rows.Count);
    }

    [Fact]
    public async Task UcKOnayKuyrugu_FizikselHandleriCalistirmazVeKomutBaglaminiSaklar()
    {
        using var data = new ApprovalFixture();
        data.Uow.Repo<IslemOnayKurali>().Rows.Add(new() { LookupUcKDurumId = (int)UcKDurum.ProjedenKarsilandi, OnayGerektirirMi = true });
        var command = new UcKDurumGuncelleCommand { ProjeId = 50, CekiSatiriId = 70, GelenAdet = 1.2501m, KarsilamaTipiId = (int)UcKDurum.ProjedenKarsilandi };
        var result = await data.Run(command);
        Assert.Equal(StatusConstants.ActionQueuedForApproval, result.StatusCode);
        Assert.Equal(0, data.Executed);
        var pending = Assert.Single(data.Uow.Repo<OnayBekleyenIslem>().Rows);
        Assert.Equal(command.GetApprovalOperationCode(), pending.IslemKodu);
        Assert.Equal(7, pending.TalepEdenKullaniciId);
        Assert.Equal(50, pending.ProjeId);
        Assert.Equal(70, pending.ReferansId);
        Assert.Equal(OnayDurumu.Bekliyor, pending.Durum);
        Assert.Equal(OnayCalistirmaDurumu.Bekliyor, pending.CalistirmaDurumu);
        Assert.Contains("1.2501", pending.PayloadJson);
        Assert.Equal(1, data.Notifier.Count);
    }

    [Fact]
    public async Task OnayCache_YonetimGuncellemesiyleTemizlenirVeYeniKuralIsler()
    {
        using var data = new ApprovalFixture();
        var rule = new IslemOnayKurali { Id = 1, LookupUcKDurumId = (int)UcKDurum.TedarikcidenGeldi, OnayGerektirirMi = false };
        data.Uow.Repo<IslemOnayKurali>().Rows.Add(rule);
        var command = new UcKDurumGuncelleCommand { KarsilamaTipiId = rule.LookupUcKDurumId };
        await data.Run(command);
        rule.OnayGerektirirMi = true; // Sadece DB değeri değiştiğinde bellek önbelleği henüz değişmez.
        await data.Run(command);
        Assert.Equal(2, data.Executed);
        Assert.Equal(1, data.Uow.Repo<IslemOnayKurali>().FindCount);
        var update = await new UpdateOnayKuraliCommandHandler(data.Uow, data.Cache).Handle(new()
        { LookupUcKDurumId = rule.LookupUcKDurumId, OnayGerektirirMi = true }, default);
        Assert.True(update.IsSuccess);
        Assert.Equal(StatusConstants.ActionQueuedForApproval, (await data.Run(command)).StatusCode);
        Assert.Equal(2, data.Executed);
    }

    [Fact]
    public async Task OnayliKomutTekrarKuyrugaAlinmaz_AltHandlerIsKuraliYineCalisir()
    {
        using var data = new ApprovalFixture();
        data.Downstream = Result.Failure("stok artık yetersiz", 409);
        using (data.Execution.BeginApprovedExecution())
        {
            var result = await data.Run(new AyarliCommand());
            Assert.Equal(409, result.StatusCode);
            Assert.Equal("stok artık yetersiz", result.Error!.Message);
        }
        Assert.False(data.Execution.IsExecutingApprovedCommand);
        Assert.Empty(data.Uow.Repo<OnayBekleyenIslem>().Rows);
        Assert.Equal(1, data.Executed);
    }

    [Fact]
    public async Task KilitAcmaHerZamanOnayIster_TopluTedarikciTekliKuraliOtomatikDevralmaz()
    {
        using var data = new ApprovalFixture();
        Assert.Equal(StatusConstants.ActionQueuedForApproval, (await data.Run(new SandikKilidiAcCommand())).StatusCode);
        data.Uow.Repo<IslemOnayKurali>().Rows.Add(new() { LookupUcKDurumId = (int)UcKDurum.TedarikcidenGeldi, OnayGerektirirMi = true });
        Assert.True((await data.Run(new UcKTopluTedarikciCommand())).IsSuccess);
        Assert.Equal(1, data.Executed);
        Assert.Single(data.Uow.Repo<OnayBekleyenIslem>().Rows);
    }

    private static AppDbContext Context(int roleId, string roleName, YetkiTipi granted)
    {
        var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().Options);
        context.Kullanicilar = new OrtakMemorySet<Kullanici>([new() { Id = 7, RolId = roleId, Rol = new() { Id = roleId, Ad = roleName } }]);
        context.RolYetkileri = new OrtakMemorySet<RolYetki>([new() { RolId = roleId, MenuTanimi = new() { Kod = "grid-modulu" }, YetkiTipiId = (int)granted }]);
        context.OnayIslemYetkileri = new OrtakMemorySet<OnayIslemYetki>([]);
        return context;
    }

    private sealed record OrtakQuery : IRequest<Result>, ISecuredRequest;
    private sealed record SabitCommand : IRequest<Result>, ISecuredRequest, IRequiresMenuPermission { public string RequiredMenuKod => "sabit"; }
    private sealed record SabitQuery : IRequest<Result>, ISecuredRequest, IRequiresMenuPermission { public string RequiredMenuKod => "grid-modulu"; }
    private sealed record CokluCommand : IRequest<Result>, ISecuredRequest, IRequiresMenuPermissions
    { public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions => [new("ilk", YetkiTipi.R), new("ikinci", YetkiTipi.W)]; }
    public sealed record AyarliCommand : IRequest<Result>, IConfigurableApproval
    { public string GetApprovalOperationCode() => "TEST_OPERATION"; public string GetApprovalDescription() => "Test"; }

    private sealed class RolFake : IRolService
    {
        public string? DeniedMenu { get; init; }
        public List<(int UserId, string Menu, YetkiTipi Yetki)> Calls { get; } = [];
        public Task<bool> HasUserPermissionAsync(int user, string menu, YetkiTipi permission, CancellationToken token = default)
        { Calls.Add((user, menu, permission)); return Task.FromResult(menu != DeniedMenu); }
        public Task<bool> IsAdminAsync(int user, CancellationToken token = default) => throw new NotSupportedException();
        public Task<List<MenuTanimi>> GetMenuAgaciAsync(CancellationToken token = default) => throw new NotSupportedException();
        public Task<List<RolYetki>> GetRolYetkileriAsync(int id, CancellationToken token = default) => throw new NotSupportedException();
        public Task YetkileriGuncelleAsync(int id, List<RolYetki> rows, CancellationToken token = default) => throw new NotSupportedException();
    }

    private sealed class ApprovalFixture : IDisposable
    {
        public OrtakMemoryUow Uow { get; } = new();
        public MemoryCache Cache { get; } = new(new MemoryCacheOptions());
        public ApprovalExecutionContext Execution { get; } = new();
        public NotifyFake Notifier { get; } = new();
        public int Executed { get; private set; }
        public Result Downstream { get; set; } = Result.Success();
        public Task<Result> Run<T>(T command) where T : IRequest<Result> =>
            new ApprovalBehavior<T, Result>(new OrtakUser(), Uow, Notifier, Cache, Execution, NullLogger<ApprovalBehavior<T, Result>>.Instance)
            .Handle(command, () => { Executed++; return Task.FromResult(Downstream); }, default);
        public void Dispose() { Cache.Dispose(); Uow.Dispose(); }
    }
    private sealed class NotifyFake : ISseNotifier
    {
        public int Count { get; private set; }
        public Task BroadcastApprovalUpdateAsync() { Count++; return Task.CompletedTask; }
        public Task SubscribeAsync(object context, int userId) => throw new NotSupportedException();
        public Task NotifyUsersAsync(IEnumerable<int> users, string name, string data = "refresh") => throw new NotSupportedException();
    }
}
