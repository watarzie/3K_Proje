using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using _3K_API.Controllers;
using _3K_API.Extensions;

namespace _3K.Application.Tests;

/// <summary>
/// API hız sınırları: genel sınır oturumlu kullanıcıyı Id'siyle, oturumsuz isteği IP ile bölümler; dosya üreten uçlar
/// ayrıca dışa aktarım sınırındadır; proxy arkasında istemci IP'si yalnız güvenilir proxy zincirinden çözülür.
/// </summary>
public sealed class HizSiniriTests
{
    private const string Politika = ApiServiceCollectionExtensions.DisaAktarimRateLimitPolicy;

    [Fact]
    public void Anahtar_OturumluKullaniciIdIle_OturumsuzIpIleBolumlenir()
    {
        Assert.Equal("kullanici:7", HizSiniriBolumleri.KullaniciVeyaIpAnahtari(Istek("10.0.0.1", kullaniciId: 7)));
        Assert.Equal("kullanici:8", HizSiniriBolumleri.KullaniciVeyaIpAnahtari(Istek("10.0.0.1", kullaniciId: 8)));
        Assert.Equal("ip:10.0.0.1", HizSiniriBolumleri.KullaniciVeyaIpAnahtari(Istek("10.0.0.1")));
        // Kimliği doğrulanmamış (ör. süresi dolmuş token) kimlik IP kovasına düşer.
        var dogrulanmamis = Istek("10.0.0.2");
        dogrulanmamis.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "7")]));
        Assert.Equal("ip:10.0.0.2", HizSiniriBolumleri.KullaniciVeyaIpAnahtari(dogrulanmamis));
    }

    [Fact]
    public void Anahtar_Ipv6Istemci64OnekiyleBolumlenir()
    {
        Assert.Equal("ip:2001:db8:1:2::/64", HizSiniriBolumleri.IpAnahtari(Istek("2001:db8:1:2:aaaa::1")));
        Assert.Equal("ip:2001:db8:1:2::/64", HizSiniriBolumleri.IpAnahtari(Istek("2001:db8:1:2:bbbb::9")));
        Assert.Equal("ip:2001:db8:1:3::/64", HizSiniriBolumleri.IpAnahtari(Istek("2001:db8:1:3::1")));
        Assert.Equal("ip:10.0.0.1", HizSiniriBolumleri.IpAnahtari(Istek("::ffff:10.0.0.1")));
    }

    [Fact]
    public void Genel_KullaniciBasina600_AyniIpdekiBaskaKullaniciEtkilenmez_AnonimIpBasina120()
    {
        using var genel = PartitionedRateLimiter.Create<HttpContext, string>(HizSiniriBolumleri.Genel);
        var ali = Istek("10.0.0.1", kullaniciId: 1);
        var veli = Istek("10.0.0.1", kullaniciId: 2);

        Assert.Equal(HizSiniriBolumleri.GenelKullaniciDakikaLimiti, Tuket(genel, ali));
        Assert.False(genel.AttemptAcquire(ali).IsAcquired);
        Assert.True(genel.AttemptAcquire(veli).IsAcquired);

        // Oturumsuz istek IP kovasındadır; aynı IP'deki oturumlu kullanıcıların kotasını tüketmez.
        var anonim = Istek("10.0.0.1");
        Assert.Equal(HizSiniriBolumleri.GenelAnonimDakikaLimiti, Tuket(genel, anonim));
        Assert.True(genel.AttemptAcquire(Istek("10.0.0.9")).IsAcquired);
        Assert.True(genel.AttemptAcquire(veli).IsAcquired);
    }

    [Fact]
    public void DisaAktarim_KullaniciBasina20()
    {
        using var disaAktarim = PartitionedRateLimiter.Create<HttpContext, string>(HizSiniriBolumleri.DisaAktarim);
        Assert.Equal(HizSiniriBolumleri.DisaAktarimDakikaLimiti, Tuket(disaAktarim, Istek("10.0.0.1", kullaniciId: 1)));
        Assert.True(disaAktarim.AttemptAcquire(Istek("10.0.0.1", kullaniciId: 2)).IsAcquired);
    }

    [Fact]
    public void GirisVeIkiFaktor_IpBasinaBolumlenir()
    {
        using var giris = PartitionedRateLimiter.Create<HttpContext, string>(c =>
            HizSiniriBolumleri.IstemciIpBasina(c, HizSiniriBolumleri.GirisDakikaLimiti));
        Assert.Equal(10, Tuket(giris, Istek("10.0.0.1")));
        Assert.True(giris.AttemptAcquire(Istek("10.0.0.2")).IsAcquired);
    }

    [Theory]
    [InlineData(ApiServiceCollectionExtensions.LoginRateLimitPolicy, "doğrulama")]
    [InlineData(ApiServiceCollectionExtensions.TwoFactorRateLimitPolicy, "doğrulama")]
    [InlineData(Politika, "rapor veya dosya")]
    [InlineData(null, "Çok fazla istek")]
    public void Ret_MesajiTurkceVePolitikayaGore(string? politika, string beklenen)
    {
        var istek = Istek("10.0.0.1");
        var metadata = politika == null ? new EndpointMetadataCollection() : new EndpointMetadataCollection(new EnableRateLimitingAttribute(politika));
        istek.SetEndpoint(new Endpoint(null, metadata, "uc"));
        Assert.Contains(beklenen, ApiServiceCollectionExtensions.HizSiniriMesaji(istek));
    }

    /// <summary>
    /// PDF/Excel üreten ve dosya indiren her uç dışa aktarım sınırındadır (PdfController bütünüyle). Yeni rapor ucu bu
    /// adlandırmaya uyduğu sürece sınırsız kalamaz.
    /// </summary>
    [Fact]
    public void DosyaUclari_DisaAktarimPolitikasinda_GirisUclariKendiPolitikasinda()
    {
        var dosyaRotasi = new Regex(@"(excel|\{format\}|/dosya$|/indir$|rapor$)", RegexOptions.IgnoreCase);
        var eylemler = typeof(PdfController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly), (t, m) => (Tip: t, Metot: m))
            .Select(x => (x.Tip, x.Metot, Rota: x.Metot.GetCustomAttributes<HttpMethodAttribute>().SingleOrDefault()?.Template))
            .Where(x => x.Rota != null)
            .ToList();

        string? PolitikaAdi((Type Tip, MethodInfo Metot, string? Rota) e) =>
            (e.Metot.GetCustomAttribute<EnableRateLimitingAttribute>() ?? e.Tip.GetCustomAttribute<EnableRateLimitingAttribute>())?.PolicyName;

        var dosyaUclari = eylemler.Where(e => e.Tip == typeof(PdfController) || dosyaRotasi.IsMatch(e.Rota!)).ToList();
        Assert.True(dosyaUclari.Count >= 30, $"Beklenenden az dosya ucu bulundu: {dosyaUclari.Count}");
        Assert.Empty(dosyaUclari.Where(e => PolitikaAdi(e) != Politika).Select(e => $"{e.Tip.Name}.{e.Metot.Name}"));

        var auth = eylemler.Where(e => e.Tip == typeof(AuthController)).ToDictionary(e => e.Metot.Name, PolitikaAdi);
        Assert.Equal(ApiServiceCollectionExtensions.LoginRateLimitPolicy, auth[nameof(AuthController.Login)]);
    }

    [Fact]
    public void Proxy_CloudflareVeNginxArkasindaGercekIstemciIpsiCozulur_SahteBaslikEtkisizdir()
    {
        var options = ForwardedOptions(UygulamaAyarlari());
        const string cloudflare = "162.158.10.20";

        // İstemci → Cloudflare → nginx (aynı makine) → API. İstemcinin kendi yazdığı sahte girdi en solda kalır.
        Assert.Equal("203.0.113.7", Cozumle(options, "127.0.0.1", $"1.2.3.4, 203.0.113.7, {cloudflare}"));
        Assert.Equal("203.0.113.7", Cozumle(options, "127.0.0.1", $"203.0.113.7, 2606:4700::1"));
        // Cloudflare'i atlayıp doğrudan nginx'e gelen istek: zincirdeki ilk güvenilmeyen adres (saldırgan) istemcidir.
        Assert.Equal("198.51.100.9", Cozumle(options, "127.0.0.1", "1.2.3.4, 198.51.100.9"));
        // Güvenilmeyen kaynaktan gelen başlık hiç dikkate alınmaz.
        Assert.Equal("198.51.100.9", Cozumle(options, "198.51.100.9", "1.2.3.4"));
    }

    [Fact]
    public void Proxy_GecersizAdresAcilistaDurdurur()
    {
        var ayarlar = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:KnownNetworks:0"] = "162.158.0.0/99"
        }).Build();
        var hata = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddApiForwardedHeaders(ayarlar));
        Assert.Contains("KnownNetworks", hata.Message);
    }

    private static int Tuket(PartitionedRateLimiter<HttpContext> limiter, HttpContext istek)
    {
        var sayi = 0;
        while (sayi < 10_000 && limiter.AttemptAcquire(istek).IsAcquired)
            sayi++;
        return sayi;
    }

    private static DefaultHttpContext Istek(string ip, int? kullaniciId = null)
    {
        var istek = new DefaultHttpContext();
        istek.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        if (kullaniciId != null)
            istek.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, kullaniciId.Value.ToString())], authenticationType: "Bearer"));
        return istek;
    }

    private static ForwardedHeadersOptions ForwardedOptions(IConfiguration ayarlar) =>
        new ServiceCollection().AddApiForwardedHeaders(ayarlar).BuildServiceProvider()
            .GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

    private static string Cozumle(ForwardedHeadersOptions options, string kaynak, string xForwardedFor)
    {
        var istek = new DefaultHttpContext();
        istek.Connection.RemoteIpAddress = IPAddress.Parse(kaynak);
        istek.Request.Headers["X-Forwarded-For"] = xForwardedFor;
        new ForwardedHeadersMiddleware(_ => Task.CompletedTask, NullLoggerFactory.Instance, Options.Create(options))
            .ApplyForwarders(istek);
        return istek.Connection.RemoteIpAddress!.ToString();
    }

    /// <summary>Depodaki gerçek <c>3K_API/appsettings.json</c>: canlıya giden aralıklar ayrıştırılabilir olmalı.</summary>
    private static IConfiguration UygulamaAyarlari()
    {
        var dizin = new DirectoryInfo(AppContext.BaseDirectory);
        while (dizin != null && !File.Exists(Path.Combine(dizin.FullName, "3K_API", "appsettings.json")))
            dizin = dizin.Parent;
        Assert.NotNull(dizin);
        return new ConfigurationBuilder().AddJsonFile(Path.Combine(dizin.FullName, "3K_API", "appsettings.json")).Build();
    }
}
