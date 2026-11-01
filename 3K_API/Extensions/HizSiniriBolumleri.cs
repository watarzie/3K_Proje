using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace _3K_API.Extensions;

/// <summary>
/// Hız sınırı kovaları ve eşikleri. Oturumlu istek kullanıcı Id'si, oturumsuz istek istemci IP'si ile bölümlenir:
/// aynı NAT'ın arkasındaki kullanıcılar birbirinin kotasını tüketmez, oturumsuz yük de IP başına sınırlı kalır.
/// İstemci IP'si proxy arkasında <c>ForwardedHeaders</c> yapılandırmasıyla gerçek istemciye çözülür.
/// </summary>
public static class HizSiniriBolumleri
{
    /// <summary>
    /// Oturumlu kullanıcı başına dakikada genel istek. Bir ekran açılışı ~10–20 istek atar; yoğun kullanımda bile
    /// dakikada birkaç yüz isteği geçmez. Sınır yalnız kaçak döngüleri ve betikli kazımayı durdurur.
    /// </summary>
    public const int GenelKullaniciDakikaLimiti = 600;

    /// <summary>Oturumsuz IP başına dakikada genel istek (giriş sayfası ve süresi dolmuş token istekleri).</summary>
    public const int GenelAnonimDakikaLimiti = 120;

    /// <summary>
    /// PDF/Excel üretimi ve dosya indirme: sunucuda pahalı; kullanıcı başına dakikada 20. Attribute ile değil, MediatR
    /// hattında yanıt tipine (<c>Result&lt;DosyaIcerigi&gt;</c>) göre uygulanır (<c>DisaAktarimHizSiniriBehavior</c>).
    /// </summary>
    public const int DisaAktarimDakikaLimiti = 20;

    public const int GirisDakikaLimiti = 10;
    public const int IkiFaktorDakikaLimiti = 20;

    /// <summary>
    /// IPv4'te tam adres; IPv6'da /64 öneki. Tek bir IPv6 istemcisi kendi /64 bloğunda adres değiştirerek yeni kova
    /// açamaz.
    /// </summary>
    public static string IpAnahtari(HttpContext httpContext)
    {
        var adres = httpContext.Connection.RemoteIpAddress;
        if (adres == null)
            return "ip:bilinmiyor";
        if (adres.IsIPv4MappedToIPv6)
            return "ip:" + adres.MapToIPv4();
        if (adres.AddressFamily != AddressFamily.InterNetworkV6)
            return "ip:" + adres;

        var baytlar = adres.GetAddressBytes();
        Array.Clear(baytlar, 8, 8);
        return "ip:" + new IPAddress(baytlar) + "/64";
    }

    public static RateLimitPartition<string> Genel(HttpContext httpContext) =>
        KullaniciId(httpContext) is { } kullaniciId
            ? DakikalikPencere("kullanici:" + kullaniciId, GenelKullaniciDakikaLimiti)
            : DakikalikPencere(IpAnahtari(httpContext), GenelAnonimDakikaLimiti);

    /// <summary>Giriş ve 2FA adımları oturumdan önce çalışır; yalnız IP ile bölümlenir.</summary>
    public static RateLimitPartition<string> IstemciIpBasina(HttpContext httpContext, int dakikaLimiti) =>
        DakikalikPencere(IpAnahtari(httpContext), dakikaLimiti);

    private static string? KullaniciId(HttpContext httpContext) =>
        httpContext.User.Identity?.IsAuthenticated == true
            ? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;

    private static RateLimitPartition<string> DakikalikPencere(string anahtar, int limit) =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: anahtar,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
}
