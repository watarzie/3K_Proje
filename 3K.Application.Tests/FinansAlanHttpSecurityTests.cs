using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using _3K.Application.Behaviors;
using _3K.Application.Features.FinansIslemleri.Queries;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Services;
using _3K_API.Controllers;

namespace _3K.Application.Tests;

/// <summary>
/// Gerçek Kestrel + JWT + MVC/controller + MediatR authorization + JSON output sınırı.
/// Program/appsettings/worker çalıştırılmaz; veri servisinin sentetik sonuçları kullanılır.
/// PostgreSQL rol/override kalıcılığı ayrı GranularYetkiPostgresTests kapsamındadır.
/// </summary>
public sealed class FinansAlanHttpSecurityTests
{
    [Fact]
    public async Task Http_JwtGerekir_SahteHeaderYetkiVermez_AyniTokenIleIptalAnindaEtkin()
    {
        await using var host = await Host.StartAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.GetAsync("api/finans/is-kayitlari")).StatusCode);
        host.Authenticate();
        host.Client.DefaultRequestHeaders.Add("X-Menu-Kod", "stok");
        host.Roles.Grant("stok", YetkiTipi.W);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("api/finans/is-kayitlari")).StatusCode);
        Assert.Equal(0, host.Data.Calls);

        host.Roles.Grant(YetkiKodlari.Finans.KayitGoruntule);
        host.Roles.Grant(YetkiKodlari.Finans.Modul);
        var json = await host.Json("api/finans/is-kayitlari");
        var item = json["items"]![0]!;
        Assert.Equal("TEST-PROJE", item["projeNo"]!.GetValue<string>());
        Assert.Equal(3m, item["adet"]!.GetValue<decimal>());
        foreach (var field in new[] { "birimFiyat", "netTutar", "toplamTutar", "boy", "en", "yukseklik", "birimM3", "toplamM3", "alanDegerleri", "bilesenler" })
            Assert.Null(item[field]);
        Assert.Equal(1, host.Data.Calls);

        host.Roles.Revoke(YetkiKodlari.Finans.KayitGoruntule);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("api/finans/is-kayitlari")).StatusCode);
        Assert.Equal(1, host.Data.Calls);
    }

    [Fact]
    public async Task Http_ModulReddedilirseTumAltIzinlerVeSahteHeaderApiErisimiSaglamaz()
    {
        await using var host = await Host.StartAsync();
        host.Authenticate();
        host.GrantAll();
        host.Roles.Revoke(YetkiKodlari.Finans.Modul);
        host.Client.DefaultRequestHeaders.Add("X-Menu-Kod", YetkiKodlari.Finans.KayitGoruntule);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("api/finans/is-kayitlari")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("api/finans/belgeler/1/indir")).StatusCode);
        Assert.Equal(0, host.Data.Calls);
    }

    [Theory]
    [InlineData(YetkiKodlari.Finans.ParasalVeriGoruntule, "netTutar")]
    [InlineData(YetkiKodlari.Finans.BirimFiyatGoruntule, "birimFiyat")]
    [InlineData(YetkiKodlari.Finans.TutarGoruntule, "netTutar")]
    [InlineData(YetkiKodlari.Ambalaj.OlcuGoruntule, "boy")]
    [InlineData(YetkiKodlari.Ambalaj.M3Goruntule, "toplamM3")]
    public async Task Http_AltAlanIzniKaldirilinca_YalnizHassasAlanNull_OkulabilirKayitKalir(string permission, string field)
    {
        await using var host = await Host.StartAsync();
        host.Authenticate();
        host.GrantAll();
        var initial = await host.Json("api/finans/is-kayitlari/1");
        Assert.NotNull(initial[field]);
        host.Roles.Revoke(permission);
        var denied = await host.Json("api/finans/is-kayitlari/1");
        Assert.Null(denied[field]);
        Assert.Equal("TEST-PROJE", denied["projeNo"]!.GetValue<string>());
        Assert.Equal(3m, denied["adet"]!.GetValue<decimal>());
        Assert.Null(denied["alanDegerleri"]);
        Assert.Null(denied["bilesenler"]);
    }

    [Theory]
    [InlineData(YetkiKodlari.Finans.GelirGoruntule, "gelir")]
    [InlineData(YetkiKodlari.Finans.GiderGoruntule, "gider")]
    [InlineData(YetkiKodlari.Finans.KarlilikGoruntule, "karOrani")]
    public async Task Http_PaneldeGelirGiderKarBagimsizMaskelenir_SayacSifirlanmaz(string permission, string field)
    {
        await using var host = await Host.StartAsync();
        host.Authenticate();
        host.GrantAll();
        host.Roles.Revoke(permission);
        var json = await host.Json("api/finans/panel?baslangic=2026-01-01&bitis=2026-12-31");
        Assert.Null(json["tutarlar"]![0]![field]);
        Assert.Equal(7, json["siparisBekleyen"]!.GetValue<int>());
        if (field != "karOrani") Assert.Null(json["aylik"]![0]![field]);
    }

    [Fact]
    public async Task Http_ParasalIzinYoksa_LegacyDashboardSayaclariGorunurKalir()
    {
        await using var host = await Host.StartAsync();
        host.Authenticate();
        host.Roles.Grant(YetkiKodlari.Finans.Modul);
        var json = await host.Json("api/finans/dashboard");
        Assert.Equal(7, json["siparisBekleyen"]!.GetValue<int>());
        Assert.Equal(2, json["faturaBekleyen"]!.GetValue<int>());
        Assert.Null(json["buAyGider"]);
        Assert.Null(json["toplamM3"]);
    }

    [Theory]
    [InlineData(YetkiKodlari.Finans.BirimFiyatGoruntule)]
    [InlineData(YetkiKodlari.Ambalaj.OlcuGoruntule)]
    [InlineData(YetkiKodlari.Ambalaj.M3Goruntule)]
    public async Task Http_AuditStringSnapshotHassasIzinEksikkenSizmaz(string permission)
    {
        await using var host = await Host.StartAsync();
        host.Authenticate();
        host.GrantAll();
        host.Roles.Revoke(permission);
        var json = await host.Json("api/finans/denetim");
        var audit = json["items"]![0]!;
        Assert.Null(audit["eskiDeger"]);
        Assert.Null(audit["yeniDeger"]);
        Assert.Null(audit["aciklama"]);
        Assert.Equal("Fiyatlandirma", audit["islem"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("api/finans/raporlar/isler/pdf")]
    [InlineData("api/finans/raporlar/isler/xlsx")]
    [InlineData("api/finans/raporlar/aylik/ayri?yil=2026&ay=9")]
    [InlineData("api/finans/belgeler/1/indir")]
    public async Task Http_HassasDosyaEksikAlanIzniyleUretilmez(string path)
    {
        await using var host = await Host.StartAsync();
        host.Authenticate();
        host.GrantAll();
        host.Roles.Revoke(YetkiKodlari.Ambalaj.OlcuGoruntule);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync(path)).StatusCode);
        Assert.Equal(0, host.Data.Calls);
    }

    [Fact]
    public async Task Http_TamAlanIzinleriBelgeIndirmeyiAcar_AyniTokenleSonradanKapanir()
    {
        await using var host = await Host.StartAsync();
        host.Authenticate();
        host.GrantAll();
        var response = await host.Client.GetAsync("api/finans/belgeler/1/indir");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType!.MediaType);
        Assert.StartsWith("%PDF", await response.Content.ReadAsStringAsync());
        host.Roles.Revoke(YetkiKodlari.Finans.TutarGoruntule);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("api/finans/belgeler/1/indir")).StatusCode);
        Assert.Equal(1, host.Data.Calls);
    }

    [Fact]
    public async Task Http_SarfIzniYoksaHassasBinaryRaporAcilmaz()
    {
        await using var host = await Host.StartAsync();
        host.Authenticate();
        host.GrantAll();
        host.Roles.Revoke(YetkiKodlari.Ambalaj.SarfGoruntule);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("api/finans/raporlar/isler/pdf")).StatusCode);
        Assert.Equal(0, host.Data.Calls);
    }

    [Fact]
    public async Task Http_RaporVerisiIndirmeKapisiDegildir_HassasAlanlarNullOlur()
    {
        await using var host = await Host.StartAsync();
        host.Authenticate();
        host.GrantAll();
        host.Roles.Revoke(YetkiKodlari.Finans.ParasalVeriGoruntule);
        var json = await host.Json("api/finans/raporlar/veri");
        Assert.Equal("TEST-PROJE", json["isler"]![0]!["projeNo"]!.GetValue<string>());
        Assert.Null(json["isler"]![0]!["netTutar"]);
        Assert.Null(json["gelirToplamlari"]);
        Assert.Null(json["giderToplamlari"]);
        Assert.Null(json["netToplamlari"]);
    }

    private sealed class Host : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly SecurityKey _key;
        public HttpClient Client { get; }
        public PermissionStore Roles { get; }
        public FinanceProxy Data { get; }
        private Host(WebApplication app, SecurityKey key, PermissionStore roles, FinanceProxy data)
        {
            _app = app; _key = key; Roles = roles; Data = data;
            var url = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            Client = new HttpClient { BaseAddress = new Uri(url), Timeout = TimeSpan.FromSeconds(20) };
        }
        public static async Task<Host> StartAsync()
        {
            var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
            builder.Configuration.Sources.Clear();
            builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
            var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64));
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = key, ValidIssuer = "isolated-tests", ValidAudience = "isolated-tests",
                    ValidateIssuerSigningKey = true, ValidateIssuer = true, ValidateAudience = true,
                    ValidateLifetime = true, ClockSkew = TimeSpan.Zero
                });
            builder.Services.AddAuthorization();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddScoped<IAlanErisimService, AlanErisimService>();
            var roles = new PermissionStore();
            builder.Services.AddSingleton<IRolService>(roles);
            var finance = DispatchProxy.Create<IFinansService, FinanceProxy>();
            var data = (FinanceProxy)finance;
            builder.Services.AddSingleton(finance);
            builder.Services.AddMediatR(options =>
            {
                options.RegisterServicesFromAssembly(typeof(FinansQueryHandlers).Assembly);
                options.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
            });
            builder.Services.AddControllers().AddApplicationPart(typeof(FinansController).Assembly);
            var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            await app.StartAsync();
            return new Host(app, key, roles, data);
        }
        public void Authenticate()
        {
            var token = new JwtSecurityToken("isolated-tests", "isolated-tests",
                [new Claim(ClaimTypes.NameIdentifier, "7")], expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        }
        public void GrantAll()
        {
            Roles.Grant(YetkiKodlari.Finans.Modul);
            foreach (var permission in YetkiKatalogu.Tum) Roles.Grant(permission.Kod, permission.GerekenYetki);
        }
        public async Task<JsonNode> Json(string path)
        {
            using var response = await Client.GetAsync(path);
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{path}: {(int)response.StatusCode} {body}");
            return JsonNode.Parse(body)!;
        }
        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    private sealed class PermissionStore : IRolService
    {
        private readonly ConcurrentDictionary<string, YetkiTipi> _permissions = new();
        public void Grant(string code, YetkiTipi level = YetkiTipi.R) => _permissions[code] = level;
        public void Revoke(string code) => _permissions.TryRemove(code, out _);
        public Task<bool> HasUserPermissionAsync(int userId, string menuKod, YetkiTipi requiredYetkiTipi, CancellationToken ct = default) =>
            Task.FromResult(userId == 7 && _permissions.TryGetValue(menuKod, out var level) && level >= requiredYetkiTipi);
        public Task<bool> IsAdminAsync(int userId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<List<MenuTanimi>> GetMenuAgaciAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<RolYetki>> GetRolYetkileriAsync(int rolId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task YetkileriGuncelleAsync(int rolId, List<RolYetki> yetkiler, CancellationToken ct = default) => throw new NotSupportedException();
    }

    public class FinanceProxy : DispatchProxy
    {
        public int Calls;
        private static readonly FinansIsKaydiModel Row = new()
        {
            Id = 1, ProjeNo = "TEST-PROJE", IsAdi = "Sentetik kayıt", Adet = 3, Birim = "Adet",
            Boy = 1234, En = 567, Yukseklik = 890, BirimM3 = 1.5m, ToplamM3 = 4.5m,
            BirimFiyat = 777.25m, NetTutar = 2331.75m, KdvTutari = 466.35m, ToplamTutar = 2798.10m,
            ParaBirimi = "EUR", AlanDegerleri = new Dictionary<string, string?> { ["gizli-olcu"] = "1234x567x890" },
            Bilesenler = [new("Kereste", FinansFiyatlandirmaBirimi.Metrekup, 4.5m, 777.25m)]
        };
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            Interlocked.Increment(ref Calls);
            object result = method!.Name switch
            {
                "IsKayitlariAsync" => new FinansSayfaliSonuc<FinansIsKaydiModel> { Items = [Row], TotalCount = 1, PageSize = 25 },
                "IsKaydiGetirAsync" => Row,
                "DashboardAsync" => new FinansDashboardModel { ToplamIs = 20, SiparisBekleyen = 7, FaturaBekleyen = 2, ToplamM3 = 500, BuAyGiderler = [new("EUR", 100, 20, 120)] },
                "PanelAsync" => new FinansPanelModel(new(2026, 1, 1), new(2026, 12, 31), "FinansTarihi",
                    [new("EUR", 1000, 1000, 400, 600, 500, 100, 400, 300, 40, 9000, 2000)],
                    [new("Ocak", "EUR", 500, 100)], [], [], 7, 2, 3, 1, 4),
                "DegisiklikGecmisiAsync" => new FinansSayfaliSonuc<FinansDegisiklikModel>
                    { Items = [new(1, "Is", 1, "Fiyatlandirma", "Snapshot", "{\"birimFiyat\":777.25}", "1234x567x890", "777.25 EUR", new(2026, 9, 19), "Sentetik")], TotalCount = 1, PageSize = 25 },
                "BelgeIndirAsync" => new FinansBelgeIcerikModel(Encoding.ASCII.GetBytes("%PDF-SENTETIK"), "test.pdf", "application/pdf"),
                "RaporVerisiAsync" => new FinansRaporModel { Isler = [Row], GelirToplamlari = [new("EUR", 1000, 200, 1200)], GiderToplamlari = [new("EUR", 100, 20, 120)], NetToplamlari = [new("EUR", 900, 180, 1080)] },
                _ => throw new NotSupportedException("Beklenmeyen sentetik servis çağrısı: " + method.Name)
            };
            return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(method.ReturnType.GenericTypeArguments[0]).Invoke(null, [result]);
        }
    }
}
