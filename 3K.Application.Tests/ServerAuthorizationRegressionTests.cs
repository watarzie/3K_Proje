using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using _3K.Application.Behaviors;
using _3K.Application.Common;
using _3K.Application.Features.AuthIslemleri.Commands;
using _3K.Application.Features.GridIslemleri.Commands;
using _3K.Application.Features.GridIslemleri.Queries;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Application.Features.RolIslemleri.Commands;
using _3K.Application.Features.RolIslemleri.Queries;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Queries;
using _3K.Application.Features.UcKIslemleri.Commands;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

public class ServerAuthorizationRegressionTests
{
    [Fact]
    public void ButunKorumaliOperasyonlar_SunucudaTanimlidir()
    {
        var secured = typeof(ISecuredRequest).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IBaseRequest).IsAssignableFrom(t))
            .OrderBy(t => t.FullName).ToArray();
        Assert.NotEmpty(secured);
        var missing = secured.Where(t =>
        {
            var request = Activator.CreateInstance(t)!;
            return !RequestMenuPermissionResolver.HasServerDefinition(request) &&
                !MenuAuthorizationExceptions.IsPublic(request) &&
                !MenuAuthorizationExceptions.IsOwnAuthenticatedData(request) &&
                !MenuAuthorizationExceptions.IsServerInternal(request);
        });
        Assert.Empty(missing.Select(t => t.FullName));
    }

    [Theory]
    [InlineData("grid-modulu", YetkiTipi.W, false)]
    [InlineData("stok", YetkiTipi.W, false)]
    [InlineData("rol-yonetimi", YetkiTipi.R, false)]
    [InlineData("rol-yonetimi", YetkiTipi.W, true)]
    public async Task RolCrud_SahteHeaderBaskaModulYazmaYetkisiniTasiyamaz(string grantedMenu, YetkiTipi permission, bool allowed)
    {
        var fixture = new Fixture(grantedMenu, permission, header: grantedMenu);
        Assert.Equal(allowed, (await fixture.Run(new RolSilCommand { Id = 4 })).IsSuccess);
        Assert.Equal(allowed, (await fixture.Run(new RolOlusturCommand())).IsSuccess);
        Assert.Equal(allowed, (await fixture.Run(new RolGuncelleCommand())).IsSuccess);
        Assert.All(fixture.Roles.Calls, x => Assert.Equal(("rol-yonetimi", YetkiTipi.W), x));
    }

    [Fact]
    public async Task TanimlanmamisKorumaliIstek_HeaderYetkisiOlsaBileKapaliKalir()
    {
        var fixture = new Fixture("grid-modulu", YetkiTipi.W);
        var result = await fixture.Run(new UnknownCommand());
        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.StatusCode);
        Assert.Equal(0, fixture.Executed);
        Assert.Empty(fixture.Roles.Calls);
    }

    [Fact]
    public async Task SecuredIsaretiUnutulanYeniOperasyon_KendiligindenPublicOlmaz()
    {
        var fixture = new Fixture("grid-modulu", YetkiTipi.W);
        Assert.Equal(403, (await fixture.Run(new UnmarkedCommand())).StatusCode);
        Assert.Equal(0, fixture.Executed);
    }

    [Fact]
    public async Task NullTopluSecim_ValidatorOncesinde500Uretmez()
    {
        var fixture = new Fixture("3k-modulu", YetkiTipi.W);
        fixture.Seed(ProjeTipi.Normal);
        Assert.Equal(403, (await fixture.Run(new UcKTopluTamGeldiCommand { ProjeId = 10, Secimler = null! })).StatusCode);
        Assert.Equal(403, (await fixture.Run(new UcKTopluTamGeldiCommand { ProjeId = 10, Secimler = [null!] })).StatusCode);
        Assert.Equal(0, fixture.Executed);
    }

    [Theory]
    [InlineData(ProjeTipi.Normal, "grid-modulu")]
    [InlineData(ProjeTipi.Saha, "saha-grid-modulu")]
    [InlineData(ProjeTipi.Yedek, "yedek-grid-modulu")]
    public async Task GridGercekProjeTipininYetkisiyleCalisir(ProjeTipi type, string expectedMenu)
    {
        var fixture = new Fixture(expectedMenu, YetkiTipi.W, header: "rol-yonetimi");
        fixture.Seed(type);
        var result = await fixture.Run(new GridDurumGuncelleCommand { ProjeId = 10, CekiSatiriId = 30 });
        Assert.True(result.IsSuccess);
        Assert.Equal([(expectedMenu, YetkiTipi.W)], fixture.Roles.Calls);
    }

    [Theory]
    [InlineData(ProjeTipi.Saha)]
    [InlineData(ProjeTipi.Yedek)]
    public async Task NormalGridYetkisi_SahaYedekSatiriDegistiremez(ProjeTipi type)
    {
        var fixture = new Fixture("grid-modulu", YetkiTipi.W);
        fixture.Seed(type);
        Assert.Equal(403, (await fixture.Run(new GridDurumGuncelleCommand { ProjeId = 10, CekiSatiriId = 30 })).StatusCode);
        Assert.Equal(0, fixture.Executed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BaskaProjeninSatiriVeyaSandigi_GovdedeYetkiliProjeYazilarakAcilemez(bool crate)
    {
        var fixture = new Fixture("grid-modulu", YetkiTipi.W);
        fixture.Seed(ProjeTipi.Saha);
        fixture.Uow.Repo<Proje>().Rows.Add(new() { Id = 11, ProjeTipiId = (int)ProjeTipi.Normal });
        var result = crate
            ? await fixture.Run(new GetGridUrunlerQuery { ProjeId = 11, SandikId = 40 })
            : await fixture.Run(new GridDurumGuncelleCommand { ProjeId = 11, CekiSatiriId = 30 });
        Assert.Equal(403, result.StatusCode);
        Assert.Equal(0, fixture.Executed);
    }

    [Theory]
    [InlineData("saha-sandiklar")]
    [InlineData("saha-3k-modulu")]
    public async Task OrtakSandikOperasyonu_IzinliEkranlardanHerhangiBiriniKabulEder(string menu)
    {
        var fixture = new Fixture(menu, YetkiTipi.W);
        fixture.Seed(ProjeTipi.Saha);
        Assert.True((await fixture.Run(new SandikOzellikGuncelleCommand { SandikId = 40 })).IsSuccess);
        Assert.True((await fixture.Run(new GetSandikIcerikQuery { SandikId = 40 })).IsSuccess);
    }

    [Fact]
    public async Task OrtakSandik_YalnizOkumaYazmaIsleminiAcamaz()
    {
        var fixture = new Fixture("saha-sandiklar", YetkiTipi.R);
        fixture.Seed(ProjeTipi.Saha);
        Assert.True((await fixture.Run(new GetSandikIcerikQuery { SandikId = 40 })).IsSuccess);
        Assert.Equal(403, (await fixture.Run(new SandikOzellikGuncelleCommand { SandikId = 40 })).StatusCode);
    }

    [Fact]
    public async Task CokluProjeIcerigi_HerGercekProjeIcinYetkiIster()
    {
        var fixture = new Fixture("sandik-yonetimi", YetkiTipi.W);
        fixture.Seed(ProjeTipi.Normal);
        fixture.Uow.Repo<Proje>().Rows.Add(new() { Id = 11, ProjeTipiId = (int)ProjeTipi.Saha });
        fixture.Uow.Repo<Sandik>().Rows.Add(new() { Id = 41, ProjeId = 11 });
        Assert.Equal(403, (await fixture.Run(new CratesCommand([40, 41]))).StatusCode);
        Assert.Equal(0, fixture.Executed);
    }

    [Fact]
    public async Task SahaEksikKaynakSecimi_NormalKaynakSatiriniHedefProjeyeZorlamaz()
    {
        var fixture = new Fixture("saha-sandiklar", YetkiTipi.W);
        fixture.Seed(ProjeTipi.Saha);
        fixture.Uow.Repo<Proje>().Rows.Add(new() { Id = 11, ProjeTipiId = (int)ProjeTipi.Normal });
        fixture.Uow.Repo<Ceki>().Rows.Add(new() { Id = 21, ProjeId = 11 });
        fixture.Uow.Repo<CekiSatiri>().Rows.Add(new() { Id = 31, CekiId = 21 });
        Assert.True((await fixture.Run(new SahaYedekMalzemeEkleCommand { ProjeId = 10, SandikId = 40, CekiSatiriId = 31 })).IsSuccess);
    }

    [Fact]
    public async Task OnayBaglami_YalnizOnayOperasyonunuYurutur_RolGuncellemeyiAtlatamaz()
    {
        var fixture = new Fixture("islem-onay-merkezi", YetkiTipi.W);
        using var execution = fixture.Approval.BeginApprovedExecution();
        Assert.True((await fixture.Run(new ProjeKilidiAcCommand { ProjeId = 10 })).IsSuccess);
        Assert.Equal(403, (await fixture.Run(new RolSilCommand { Id = 4 })).StatusCode);
    }

    [Fact]
    public async Task PublicLogin_MenuYetkisiGerektirmez()
    {
        var fixture = new Fixture("", YetkiTipi.N, authenticated: false);
        Assert.True((await fixture.Run(new LoginCommand())).IsSuccess);
        Assert.Empty(fixture.Roles.Calls);
    }

    [Fact]
    public async Task KendiMenuOkumasi_RolYonetimiIzniIstemez_AncakDbdekiKendiRolunuKullanir()
    {
        var uow = new OrtakMemoryUow();
        uow.Repo<Kullanici>().Rows.Add(new() { Id = 7, RolId = 8 });
        uow.Repo<Rol>().Rows.AddRange([new() { Id = 1, Ad = "Admin" }, new() { Id = 8, Ad = "Okuyucu" }]);
        var roles = new Roles("", YetkiTipi.N);
        var result = await new GetKullaniciMenuQueryHandler(new OrtakUser(7), uow, roles)
            .Handle(new GetKullaniciMenuQuery(), default);
        Assert.True(result.IsSuccess);
        Assert.Equal(8, result.Value!.Id);
        Assert.Empty(roles.Calls);
        var denied = await new GetKullaniciMenuQueryHandler(new OrtakUser(7, false), uow, roles)
            .Handle(new GetKullaniciMenuQuery(), default);
        Assert.Equal(401, denied.StatusCode);
    }

    [Theory]
    [InlineData(7, 2, true)]
    [InlineData(8, 2, false)]
    [InlineData(7, 3, false)]
    public async Task TamTasimaSonrasiSilinenKaynak_YalnizAyniKullanicininAyniIstekTekrarinaIzinVerir(int user, decimal amount, bool expected)
    {
        var fixture = new Fixture("sandik-yonetimi", YetkiTipi.W);
        fixture.Seed(ProjeTipi.Normal);
        fixture.Uow.Repo<SandikIcerik>().Rows.Clear();
        var key = Guid.NewGuid();
        fixture.Uow.Repo<SandikUrunTransferi>().Rows.Add(new()
        {
            Id = 1, ProjeId = 10, KaynakSandikIcerikId = 50, HedefSandikId = 40,
            Miktar = 2, IslemAnahtari = key, KullaniciId = user
        });
        var result = await fixture.Run(new SandikUrunTasiCommand
        { ProjeId = 10, KaynakSandikIcerikId = 50, HedefSandikId = 40, TasinanAdet = amount, IslemAnahtari = key });
        Assert.Equal(expected, result.IsSuccess);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AcikAnyAllSemantigi_SaglanmayanKosuluSessizceDusurmez(bool any)
    {
        var fixture = new Fixture("ikinci", YetkiTipi.W);
        Assert.Equal(any, (await fixture.Run(new MultipleCommand(any))).IsSuccess);
    }

    [Fact]
    public async Task KayitIliskisiSorgusu_PostgreSqlSqlineCevrilebilir_BaglantiAcmaz()
    {
        using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=authorization_translation_only;Username=unused;Password=unused")
            .Options);
        using var uow = new UnitOfWork(context, NullLogger<UnitOfWork>.Instance);
        var executor = new SqlInspectionExecutor();
        var resolver = new RequestMenuPermissionResolver(uow, executor);
        await resolver.ResolveAsync(new GridDurumGuncelleCommand { ProjeId = 10, CekiSatiriId = 30 }, default);
        Assert.Single(executor.Sql);
        Assert.Contains("JOIN", executor.Sql[0]);
        Assert.Contains("CekiSatirlari", executor.Sql[0]);
    }

    private sealed class Fixture
    {
        public OrtakMemoryUow Uow { get; } = new();
        public Roles Roles { get; }
        public ApprovalExecutionContext Approval { get; } = new();
        private readonly CurrentUserService _user;
        public int Executed { get; private set; }
        public Fixture(string menu, YetkiTipi permission, string? header = "grid-modulu", bool authenticated = true)
        {
            Roles = new(menu, permission);
            var context = new DefaultHttpContext();
            context.Request.Headers["X-Menu-Kod"] = header;
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "7")], authenticated ? "test" : null));
            _user = new CurrentUserService(new HttpContextAccessor { HttpContext = context });
        }
        public void Seed(ProjeTipi type)
        {
            Uow.Repo<Proje>().Rows.Add(new() { Id = 10, ProjeTipiId = (int)type });
            Uow.Repo<Ceki>().Rows.Add(new() { Id = 20, ProjeId = 10 });
            Uow.Repo<CekiSatiri>().Rows.Add(new() { Id = 30, CekiId = 20 });
            Uow.Repo<Sandik>().Rows.Add(new() { Id = 40, ProjeId = 10 });
            Uow.Repo<SandikIcerik>().Rows.Add(new() { Id = 50, SandikId = 40, CekiSatiriId = 30 });
        }
        public async Task<Result> Run<T>(T request) where T : notnull
        {
            // Yetki behavior'ı handler'a geçişten önce çalışır; response tipi bu testte ortak Result'tur.
            return await new AuthorizationBehavior<T, Result>(_user, Roles,
                new RequestMenuPermissionResolver(Uow, new ReadExecutor(), _user), Approval)
                .Handle(request, () => { Executed++; return Task.FromResult(Result.Success()); }, default);
        }
    }

    private sealed record UnknownCommand : IRequest<Result>, ISecuredRequest;
    private sealed record UnmarkedCommand : IRequest<Result>;
    private sealed record CratesCommand(IReadOnlyCollection<int> PermissionSandikIds)
        : IRequest<Result>, ISecuredRequest, IRequiresSandikMenuPermission
    {
        public ProjectMenuOperation PermissionOperation => ProjectMenuOperation.CrateWrite;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
    }
    private sealed record MultipleCommand(bool Any) : IRequest<Result>, ISecuredRequest, IRequiresMenuPermissions
    {
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions => [new("ilk", YetkiTipi.R), new("ikinci", YetkiTipi.W)];
        public MenuPermissionMatch PermissionMatch => Any ? MenuPermissionMatch.Any : MenuPermissionMatch.All;
    }
    private sealed class Roles(string grantedMenu, YetkiTipi granted) : IRolService
    {
        public List<(string Menu, YetkiTipi Yetki)> Calls { get; } = [];
        public Task<bool> HasUserPermissionAsync(int user, string menu, YetkiTipi required, CancellationToken ct = default)
        { Calls.Add((menu, required)); return Task.FromResult(menu == grantedMenu && granted >= required); }
        public Task<bool> IsAdminAsync(int userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<MenuTanimi>> GetMenuAgaciAsync(CancellationToken ct = default) => Task.FromResult(new List<MenuTanimi>());
        public Task<List<RolYetki>> GetRolYetkileriAsync(int rolId, CancellationToken ct = default) => Task.FromResult(new List<RolYetki>());
        public Task YetkileriGuncelleAsync(int rolId, List<RolYetki> yetkiler, CancellationToken ct = default) => throw new NotSupportedException();
    }
    private class ReadExecutor : IReadQueryExecutor
    {
        public IQueryable<T> AsNoTracking<T>(IQueryable<T> query) where T : class => query;
        public Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken ct = default) => Task.FromResult(query.Count());
        public virtual Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken ct = default) => Task.FromResult(query.ToList());
    }
    private sealed class SqlInspectionExecutor : ReadExecutor
    {
        public List<string> Sql { get; } = [];
        public override Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken ct = default)
        { Sql.Add(query.ToQueryString()); return Task.FromResult(new List<T>()); }
    }
}
