using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
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
/// Finans yetkisi yalnız sekme düzeyindedir ve yanıtlarda alan maskelemesi yoktur: sekmeyi okuyabilen kullanıcı
/// tutar, fiyat, ölçü ve m³ dahil bütün alanları görür. PostgreSQL rol kalıcılığı ayrı YetkiPostgresTests kapsamındadır.
/// </summary>
public sealed class FinansAlanHttpSecurityTests
{
    [Fact]
    public async Task Http_JwtGerekir_SahteHeaderYetkiVermez_AyniTokenIleIptalAnindaEtkin()
    {
        await using var host = await Host.StartAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.GetAsync("api/finans/genel-arama")).StatusCode);
        host.Authenticate();
        host.Client.DefaultRequestHeaders.Add("X-Menu-Kod", "stok");
        host.Roles.Grant("stok", YetkiTipi.W);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("api/finans/genel-arama")).StatusCode);
        Assert.Equal(0, host.Data.Calls);

        host.Roles.Grant(YetkiKodlari.Finans.SekmeIsTakibi);
        host.Roles.Grant(YetkiKodlari.Finans.Modul);
        var json = await host.Json("api/finans/genel-arama");
        var item = json["items"]![0]!;
        Assert.Equal("TEST-PROJE", item["projeNo"]!.GetValue<string>());
        Assert.Equal(3m, item["adet"]!.GetValue<decimal>());
        // Sekme okuması bütün alanları gösterir: alan maskelemesi yoktur.
        foreach (var field in new[] { "birimFiyat", "netTutar", "toplamTutar", "boy", "en", "yukseklik", "birimM3", "toplamM3", "bilesenler" })
            Assert.NotNull(item[field]);
        Assert.Equal(1, host.Data.Calls);

        host.Roles.Revoke(YetkiKodlari.Finans.SekmeIsTakibi);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("api/finans/genel-arama")).StatusCode);
        Assert.Equal(1, host.Data.Calls);
    }

    [Fact]
    public async Task Http_ModulReddedilirseTumSekmeIzinleriVeSahteHeaderApiErisimiSaglamaz()
    {
        await using var host = await Host.StartAsync();
        host.Authenticate();
        host.GrantAll();
        host.Roles.Revoke(YetkiKodlari.Finans.Modul);
        host.Client.DefaultRequestHeaders.Add("X-Menu-Kod", YetkiKodlari.Finans.SekmeIsTakibi);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("api/finans/genel-arama")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("api/finans/belgeler/1/indir")).StatusCode);
        Assert.Equal(0, host.Data.Calls);
    }

    /// <summary>
    /// Alan maskelemesi kaldırıldı: yalnız İş Takibi sekmesini okuyabilen kullanıcı (hiçbir Ambalaj ölçü/m³ izni olmadan)
    /// kayıt detayında fiyat, tutar, ölçü, m³ ve bileşenleri görür.
    /// </summary>
    [Fact]
    public async Task Http_YalnizSekmeOkumasiylaFiyatTutarOlcuVeM3Gorunur()
    {
        await using var host = await Host.StartAsync();
        host.Authenticate();
        host.Roles.Grant(YetkiKodlari.Finans.Modul);
        host.Roles.Grant(YetkiKodlari.Finans.SekmeIsTakibi);

        var kayit = await host.Json("api/finans/is-kayitlari/1");
        Assert.Equal(777.25m, kayit["birimFiyat"]!.GetValue<decimal>());
        Assert.Equal(2331.75m, kayit["netTutar"]!.GetValue<decimal>());
        Assert.Equal(2798.10m, kayit["toplamTutar"]!.GetValue<decimal>());
        Assert.Equal(1234m, kayit["boy"]!.GetValue<decimal>());
        Assert.Equal(567m, kayit["en"]!.GetValue<decimal>());
        Assert.Equal(890m, kayit["yukseklik"]!.GetValue<decimal>());
        Assert.Equal(4.5m, kayit["toplamM3"]!.GetValue<decimal>());
        Assert.Equal(3m, kayit["ahsapM3"]!.GetValue<decimal>());
        Assert.Equal(1.5m, kayit["kontraM3"]!.GetValue<decimal>());
        Assert.Single(kayit["bilesenler"]!.AsArray());
        Assert.Equal(1, host.Data.Calls);
    }

    /// <summary>Panel sekmesinin okuması gelir, gider ve kârlılığı gösterir; sayaçlar da açıktır.</summary>
    [Fact]
    public async Task Http_PanelSekmesiOkumasiGelirGiderKarGosterir()
    {
        await using var host = await Host.StartAsync();
        host.Authenticate();
        host.Roles.Grant(YetkiKodlari.Finans.Modul);
        host.Roles.Grant(YetkiKodlari.Finans.SekmePanel);
        var json = await host.Json("api/finans/panel?baslangic=2026-01-01&bitis=2026-12-31");
        var tutar = json["tutarlar"]![0]!;
        Assert.Equal((500m, 100m, 40m), (tutar["gelir"]!.GetValue<decimal>(), tutar["gider"]!.GetValue<decimal>(), tutar["karOrani"]!.GetValue<decimal>()));
        Assert.Equal((500m, 100m), (json["aylik"]![0]!["gelir"]!.GetValue<decimal>(), json["aylik"]![0]!["gider"]!.GetValue<decimal>()));
        Assert.Equal(7, json["siparisBekleyen"]!.GetValue<int>());
    }

    /// <summary>Değişiklik geçmişindeki eski/yeni değer metinleri, geçmişi açan tek bir sekmenin okumasıyla görünür.</summary>
    [Fact]
    public async Task Http_DenetimKaydiEskiYeniDegerleriSekmeOkumasiylaGorunur()
    {
        await using var host = await Host.StartAsync();
        host.Authenticate();
        host.Roles.Grant(YetkiKodlari.Finans.Modul);
        host.Roles.Grant(YetkiKodlari.Finans.SekmeGiderler);
        var audit = (await host.Json("api/finans/denetim"))["items"]![0]!;
        Assert.Equal("{\"birimFiyat\":777.25}", audit["eskiDeger"]!.GetValue<string>());
        Assert.Equal("1234x567x890", audit["yeniDeger"]!.GetValue<string>());
        Assert.Equal("777.25 EUR", audit["aciklama"]!.GetValue<string>());
        Assert.Equal("Fiyatlandirma", audit["islem"]!.GetValue<string>());
    }

    /// <summary>
    /// Dosyalar yalnız ilgili sekmenin okumasını ister; ayrı alan, Excel/PDF veya Ambalaj ölçü/m³/sarf izni gerekmez.
    /// Sekme izni yoksa (başka sekmenin yazması olsa da) 403 ve servis çağrılmaz.
    /// </summary>
    [Theory]
    [InlineData("api/finans/raporlar/isler/pdf", YetkiKodlari.Finans.SekmeRaporlar)]
    [InlineData("api/finans/raporlar/isler/xlsx", YetkiKodlari.Finans.SekmeRaporlar)]
    [InlineData("api/finans/belgeler/1/indir", YetkiKodlari.Finans.SekmeGiderler)]
    public async Task Http_DosyaYalnizSekmeOkumasiylaUretilir(string path, string sekme)
    {
        await using var host = await Host.StartAsync();
        host.Authenticate();
        host.Roles.Grant(YetkiKodlari.Finans.Modul);
        host.Roles.Grant(YetkiKodlari.Finans.SekmeTanimlar, YetkiTipi.W);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync(path)).StatusCode);
        Assert.Equal(0, host.Data.Calls);

        host.Roles.Grant(sekme);
        using var response = await host.Client.GetAsync(path);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"{path}: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        Assert.NotEmpty(await response.Content.ReadAsByteArrayAsync());
        Assert.Equal(1, host.Data.Calls);
    }

    /// <summary>
    /// Sekme kapalıysa işlem 403; sekmeye özel okuma uçları sekme izni ister, birden çok sekmenin kullandığı uçlar
    /// (genel arama, kayıt detayı) kümedeki herhangi bir sekmenin okumasıyla açılır.
    /// </summary>
    [Fact]
    public async Task Http_SekmeKapaliysaIslemVeSekmeyeOzelOkumaReddedilir()
    {
        await using var host = await Host.StartAsync();
        host.Authenticate();
        host.GrantAll();
        var govde = new { poNumarasi = "PO-1", siparisTarihi = "2026-09-01", kalemler = new[] { new { isKaydiId = 1, adet = 0, m3 = 0, netTutar = 777.25m } } };

        // PO açma PO'lar alt sekmesinin (PO/Fatura sekmesi tavanıyla), İş Takibi veya Münferit İşler sekmesinin yazmasını ister.
        host.Roles.Revoke(YetkiKodlari.Finans.SekmePoFatura);
        host.Roles.Revoke(YetkiKodlari.Finans.SekmeIsTakibi);
        host.Roles.Revoke(YetkiKodlari.Finans.SekmeMunferitIsler);
        using (var po = await host.Client.PostAsync("api/finans/siparisler", new StringContent(
                   System.Text.Json.JsonSerializer.Serialize(govde), Encoding.UTF8, "application/json")))
            Assert.Equal(HttpStatusCode.Forbidden, po.StatusCode);
        host.Roles.Grant(YetkiKodlari.Finans.SekmePoFatura);
        using (var salt = await host.Client.PostAsync("api/finans/siparisler", new StringContent(
                   System.Text.Json.JsonSerializer.Serialize(govde), Encoding.UTF8, "application/json")))
            Assert.Equal(HttpStatusCode.Forbidden, salt.StatusCode);
        Assert.Equal(0, host.Data.Calls);

        host.Roles.Revoke(YetkiKodlari.Finans.SekmePanel);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("api/finans/panel?baslangic=2026-01-01&bitis=2026-12-31")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("api/finans/aylik-isler?yil=2026&ay=9")).StatusCode);
        Assert.Equal(0, host.Data.Calls);
        await host.Json("api/finans/genel-arama");
        await host.Json("api/finans/is-kayitlari/1");
        Assert.Equal(2, host.Data.Calls);
    }

    /// <summary>Vade uyarısı kendi okuma iznini ister; bütün sekmelerin yazma izni yetmez.</summary>
    [Fact]
    public async Task Http_VadeUyarisiKendiIzniOlmadanReddedilir()
    {
        await using var host = await Host.StartAsync();
        host.Authenticate();
        host.GrantAll();
        host.Roles.Revoke(YetkiKodlari.Finans.VadeUyarisi);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("api/finans/vade-uyarilari")).StatusCode);
        Assert.Equal(0, host.Data.Calls);
    }

    /// <summary>
    /// Münferit iş tarifeden fiyatlanır: sekmenin yazma izni yeter, ayrı fiyat izni yoktur; sekme okuması yalnız listeyi
    /// (fiyatıyla) açar, oluşturmayı açmaz.
    /// </summary>
    [Fact]
    public async Task Http_MunferitIs_SekmeYazmasiYeter_SekmeOkumasiListeyiFiyatlaGosterir()
    {
        await using var host = await Host.StartAsync();
        host.Authenticate();
        host.Roles.Grant(YetkiKodlari.Finans.Modul, YetkiTipi.W);
        host.Roles.Grant(YetkiKodlari.Finans.SekmeMunferitIsler, YetkiTipi.W);
        var govde = new { finansUrunId = 3, isAdi = "Kanat kasası", musteri = "GE Vernova", adet = 2, birim = "Adet", isTarihi = "2026-10-06" };

        var olusan = await host.PostJson("api/finans/munferit-isler", govde);
        Assert.Equal(777.25m, olusan["birimFiyat"]!.GetValue<decimal>());
        Assert.Equal("TEST-PROJE", olusan["projeNo"]!.GetValue<string>());

        host.Roles.Grant(YetkiKodlari.Finans.SekmeMunferitIsler);
        using (var salt = await host.Client.PostAsync("api/finans/munferit-isler", new StringContent(
                   System.Text.Json.JsonSerializer.Serialize(govde), Encoding.UTF8, "application/json")))
            Assert.Equal(HttpStatusCode.Forbidden, salt.StatusCode);
        Assert.Equal(777.25m, (await host.Json("api/finans/munferit-isler"))["items"]![0]!["birimFiyat"]!.GetValue<decimal>());

        host.Roles.Revoke(YetkiKodlari.Finans.SekmeMunferitIsler);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("api/finans/munferit-isler")).StatusCode);
        Assert.Equal(2, host.Data.Calls);
    }

    [Fact]
    public async Task Http_BelgeIndirmeSekmeIzniyleAcilir_AyniTokenleSonradanKapanir()
    {
        await using var host = await Host.StartAsync();
        host.Authenticate();
        host.Roles.Grant(YetkiKodlari.Finans.Modul);
        host.Roles.Grant(YetkiKodlari.Finans.SekmePoFatura);
        var response = await host.Client.GetAsync("api/finans/belgeler/1/indir");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType!.MediaType);
        Assert.StartsWith("%PDF", await response.Content.ReadAsStringAsync());
        host.Roles.Revoke(YetkiKodlari.Finans.SekmePoFatura);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("api/finans/belgeler/1/indir")).StatusCode);
        Assert.Equal(1, host.Data.Calls);
    }

    /// <summary>
    /// Oluşturma yanıtı sahte sıfır üretmez; yalnız PO/Fatura sekmesi ve PO'lar alt sekmesi yazması olan kullanıcı da birim
    /// fiyatı görür. Alt sekme izni olmadan sekme yazması PO açmaz.
    /// </summary>
    [Fact]
    public async Task Http_OlusturmaYanitiSahteSifirUretmez_SekmeYazmasiylaFiyatGorunur()
    {
        await using var host = await Host.StartAsync();
        host.Authenticate();
        host.GrantAll();
        var govde = new { poNumarasi = "PO-1", siparisTarihi = "2026-09-01", kalemler = new[] { new { isKaydiId = 1, adet = 0, m3 = 0, netTutar = 777.25m } } };

        var tam = await host.PostJson("api/finans/siparisler", govde);
        Assert.Equal(777.25m, tam["kalemler"]![0]!["birimFiyat"]!.GetValue<decimal>());
        Assert.Equal("EUR", tam["kalemler"]![0]!["paraBirimi"]!.GetValue<string>());
        Assert.Equal(932.70m, tam["tutarlar"]![0]!["toplamTutar"]!.GetValue<decimal>());

        await using var sekmeli = await Host.StartAsync();
        sekmeli.Authenticate();
        sekmeli.Roles.Grant(YetkiKodlari.Finans.Modul, YetkiTipi.W);
        sekmeli.Roles.Grant(YetkiKodlari.Finans.SekmePoFatura, YetkiTipi.W);
        // Sekmenin W'si PO'lar alt sekmesinin iznini kendiliğinden vermez; Faturalar alt sekmesi de PO açmaz.
        sekmeli.Roles.Grant(YetkiKodlari.Finans.SekmePoFaturaFatura, YetkiTipi.W);
        using (var altSekmesiz = await sekmeli.Client.PostAsync("api/finans/siparisler", new StringContent(
                   System.Text.Json.JsonSerializer.Serialize(govde), Encoding.UTF8, "application/json")))
            Assert.Equal(HttpStatusCode.Forbidden, altSekmesiz.StatusCode);
        Assert.Equal(0, sekmeli.Data.Calls);
        sekmeli.Roles.Grant(YetkiKodlari.Finans.SekmePoFaturaPo, YetkiTipi.W);
        var yanit = await sekmeli.PostJson("api/finans/siparisler", govde);
        Assert.Equal(777.25m, yanit["kalemler"]![0]!["birimFiyat"]!.GetValue<decimal>());
        Assert.Equal(777.25m, yanit["kalemler"]![0]!["netTutar"]!.GetValue<decimal>());
        Assert.Equal(932.70m, yanit["tutarlar"]![0]!["toplamTutar"]!.GetValue<decimal>());
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
            var roles = new PermissionStore();
            builder.Services.AddSingleton<IRolService>(roles);
            var finance = DispatchProxy.Create<ITumFinansServisleri, FinanceProxy>();
            var data = (FinanceProxy)finance;
            foreach (var servis in typeof(ITumFinansServisleri).GetInterfaces())
                builder.Services.AddSingleton(servis, finance);
            builder.Services.AddMediatR(options =>
            {
                options.RegisterServicesFromAssembly(typeof(FinansPanelQueryHandlers).Assembly);
                options.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
            });
            builder.Services.AddControllers().AddApplicationPart(typeof(FinansPanelController).Assembly);
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
            foreach (var kod in typeof(YetkiKodlari.Finans).GetFields().Concat(typeof(YetkiKodlari.Ambalaj).GetFields())
                         .Where(f => f.IsLiteral).Select(f => (string)f.GetRawConstantValue()!))
                Roles.Grant(kod, YetkiTipi.W);
        }
        public async Task<JsonNode> Json(string path)
        {
            using var response = await Client.GetAsync(path);
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{path}: {(int)response.StatusCode} {body}");
            return JsonNode.Parse(body)!;
        }
        public async Task<JsonNode> PostJson(string path, object body)
        {
            using var response = await Client.PostAsync(path, new StringContent(
                System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
            var text = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{path}: {(int)response.StatusCode} {text}");
            return JsonNode.Parse(text)!;
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
        // Gerçek değerlendirici gibi üst düğüm alt kodların tavanıdır: finans kökü → sekme düğümü / vade uyarısı → alt sekme.
        public Task<bool> HasUserPermissionAsync(int userId, string menuKod, YetkiTipi requiredYetkiTipi, CancellationToken ct = default) =>
            Task.FromResult(userId == 7 && Seviye(menuKod) >= requiredYetkiTipi);
        private YetkiTipi Seviye(string kod)
        {
            var kendi = _permissions.TryGetValue(kod, out var level) ? level : YetkiTipi.N;
            var ust = kod == YetkiKodlari.Ambalaj.Listele ? null
                : kod.StartsWith("finans-") ? FinansSekmeAgaci.Ust(kod)
                : kod.StartsWith("ambalaj-") ? YetkiKodlari.Ambalaj.Listele : null;
            if (ust == null) return kendi;
            var ustSeviye = Seviye(ust);
            return kendi < ustSeviye ? kendi : ustSeviye;
        }
        public Task<List<MenuTanimi>> GetMenuAgaciAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<RolYetki>> GetRolYetkileriAsync(int rolId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task YetkileriGuncelleAsync(int rolId, List<RolYetki> yetkiler, CancellationToken ct = default) => throw new NotSupportedException();
    }

    /// <summary>Tek sentetik vekil bütün finans alan arayüzlerini karşılar; çağrı sayısı ortaktır.</summary>
    public interface ITumFinansServisleri :
        IFinansPanelService, IFinansIsKaydiService, IFinansAylikOzetService, IFinansSiparisFaturaService,
        IFinansGiderService, IFinansGiderTanimService, IFinansUrunService, IFinansParametreService,
        IFinansDuzenliIsService, IFinansBelgeService, IFinansKaliciSilmeService, IFinansRaporVerisiService,
        IFinansRaporService
    {
    }

    public class FinanceProxy : DispatchProxy
    {
        public int Calls;
        private static readonly FinansIsKaydiModel Row = new()
        {
            Id = 1, ProjeNo = "TEST-PROJE", IsAdi = "Sentetik kayıt", Adet = 3, Birim = "Adet",
            Boy = 1234, En = 567, Yukseklik = 890, BirimM3 = 1.5m, ToplamM3 = 4.5m, AhsapM3 = 3m, KontraM3 = 1.5m,
            BirimFiyat = 777.25m, NetTutar = 2331.75m, KdvTutari = 466.35m, ToplamTutar = 2798.10m,
            ParaBirimi = "EUR",
            Bilesenler = [new("Kereste", FinansFiyatlandirmaBirimi.Metrekup, 4.5m, 777.25m)]
        };
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            Interlocked.Increment(ref Calls);
            object result = method!.Name switch
            {
                "GenelAramaAsync" or "MunferitIslerAsync" => new FinansSayfaliSonuc<FinansIsKaydiModel> { Items = [Row], TotalCount = 1, PageSize = 25 },
                "MunferitIsOlusturAsync" => Row,
                "IsKaydiGetirAsync" => Row,
                "PanelAsync" => new FinansPanelModel(new(2026, 1, 1), new(2026, 12, 31), "FinansTarihi",
                    [new("EUR", 1000, 1000, 400, 600, 500, 100, 400, 300, 40, 9000, 2000)],
                    [new("Ocak", "EUR", 500, 100)], [], [], 7, 2, 3, 1, 4),
                "DegisiklikGecmisiAsync" => new FinansSayfaliSonuc<FinansDegisiklikModel>
                    { Items = [new(1, "Is", 1, "Fiyatlandirma", "Snapshot", "{\"birimFiyat\":777.25}", "1234x567x890", "777.25 EUR", new(2026, 9, 19), "Sentetik")], TotalCount = 1, PageSize = 25 },
                "SiparisOlusturAsync" => new FinansSiparisModel
                {
                    Id = 9, PoNumarasi = "PO-1", Tutarlar = [new("EUR", 777.25m, 155.45m, 932.70m)],
                    Kalemler = [new FinansSiparisKalemiModel { Id = 1, IsKaydiId = 1, BirimFiyat = 777.25m, ParaBirimi = "EUR", NetTutar = 777.25m, KdvTutari = 155.45m, ToplamTutar = 932.70m }]
                },
                "BelgeIndirAsync" => new DosyaIcerigi(Encoding.ASCII.GetBytes("%PDF-SENTETIK"), "application/pdf", "test.pdf"),
                "IslerPdfAsync" or "IslerExcelAsync" => Encoding.ASCII.GetBytes("SENTETIK-RAPOR"),
                _ => throw new NotSupportedException("Beklenmeyen sentetik servis çağrısı: " + method.Name)
            };
            return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(method.ReturnType.GenericTypeArguments[0]).Invoke(null, [result]);
        }
    }
}
