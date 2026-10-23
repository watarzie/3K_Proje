using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using _3K_API.Logging;
using _3K_API.Services;
using _3K.Application;
using _3K.Infrastructure;

namespace _3K_API.Extensions;

/// <summary>HTTP katmanına özgü servis kayıtları (kimlik doğrulama, CORS, rate limit, Swagger, worker'lar).</summary>
public static class ApiServiceCollectionExtensions
{
    public const string LoginRateLimitPolicy = "auth-login";
    public const string TwoFactorRateLimitPolicy = "two-factor";
    public const string CorsPolicy = "AllowAll";

    /// <summary>Uygulamanın bütün servis kayıtları; kompozisyon kökü (Program) yalnız bunu çağırır.</summary>
    public static IServiceCollection AddApi(
        this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment) =>
        services
            .AddApplication()
            .AddInfrastructure(configuration)
            .AddApiAuthentication(configuration)
            .AddApiRateLimiting()
            .AddApiSwagger()
            .AddApiCors(environment)
            .AddTwoFactorDataProtection(configuration, environment)
            .AddAmbalajKaynakSenkronizasyonWorker(configuration)
            .AddApiControllers();

    public static IServiceCollection AddApiAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtKey = configuration["JwtSettings:SecretKey"]
            ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
            ?? throw new InvalidOperationException("JWT SecretKey yapılandırılmamış.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["JwtSettings:Issuer"] ?? "3K_API",
                    ValidAudience = configuration["JwtSettings:Audience"] ?? "3K_Client",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
                options.Events = new JwtBearerEvents
                {
                    // EventSource header taşıyamadığı için SSE akışı token'ı query string'den alır.
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken) &&
                            context.HttpContext.Request.Path.StartsWithSegments(SerilogBootstrap.SseStreamPath))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                    // Oturumsuz istek de uygulamanın standart hata gövdesini alır.
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new { message = "Oturum açmanız gerekiyor." });
                    }
                };
            });

        // Varsayılan kapalı: [AllowAnonymous] işaretlenmeyen her endpoint oturum ister.
        // İşlem yetkisi ayrıca MediatR AuthorizationBehavior'da menü izinleriyle doğrulanır.
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }

    /// <summary>
    /// Controller'lar; model bağlama hataları da <c>{ message, issues }</c> gövdesiyle döner.
    /// </summary>
    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        // Controller'lar DI'dan çözülür; böylece bağımlılıkları da başlangıç doğrulamasına girer.
        services.AddControllers()
            .AddApplicationPart(typeof(ApiServiceCollectionExtensions).Assembly)
            .AddControllersAsServices()
            .ConfigureApiBehaviorOptions(options =>
            options.InvalidModelStateResponseFactory = context =>
            {
                var issues = context.ModelState
                    .Where(entry => entry.Value is { Errors.Count: > 0 })
                    .ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value!.Errors
                            .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Geçersiz değer." : error.ErrorMessage)
                            .ToArray());
                return new BadRequestObjectResult(new { message = "İstek verisi geçersiz.", issues });
            });

        return services;
    }

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        // Reverse proxy arkasında istemci IP'si X-Forwarded-For'dan okunur; yalnız
        // varsayılan güvenilir proxy'ler (loopback) dikkate alınır.
        services.Configure<ForwardedHeadersOptions>(options =>
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { message = "Çok fazla doğrulama isteği gönderildi. Lütfen kısa bir süre sonra tekrar deneyin." },
                    cancellationToken);
            };

            options.AddPolicy(LoginRateLimitPolicy, httpContext => PerClientIp(httpContext, permitLimit: 10));
            options.AddPolicy(TwoFactorRateLimitPolicy, httpContext => PerClientIp(httpContext, permitLimit: 20));
        });

        return services;
    }

    public static IServiceCollection AddApiSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.CustomSchemaIds(type => type.FullName);
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "3K Sevkiyat / Çeki Takip API",
                Version = "v1",
                Description = "Sevkiyat yönetimi, çeki takibi, sandık operasyonları ve stok yönetimi API'si"
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Login'den dönen token'ı buraya yapıştırın (Bearer öneki otomatik eklenir)",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    public static IServiceCollection AddApiCors(this IServiceCollection services, IWebHostEnvironment environment)
    {
        string[] origins = environment.IsDevelopment()
            ? ["http://localhost:4200", "https://localhost:4200"]
            : ["https://www.3klojistikdepolama.com", "https://3klojistikdepolama.com"];

        services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
            policy.WithOrigins(origins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                // Hata bildiriminde gösterilen takip kodu ve indirilen dosyanın sunucudaki adı; farklı kökenden
                // çalışan ön yüz bu başlıkları okuyabilsin.
                .WithExposedHeaders("X-Correlation-ID", "Content-Disposition")));

        return services;
    }

    /// <summary>2FA secret'larını koruyan anahtar halkası; sunucu yeniden başlasa da kalıcı olmalıdır.</summary>
    public static IServiceCollection AddTwoFactorDataProtection(
        this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var dataProtection = services.AddDataProtection().SetApplicationName("3K.IkiFaktor");
        var keysPath = configuration["TwoFactor:DataProtectionKeysPath"];
        if (!string.IsNullOrWhiteSpace(keysPath))
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }
        else if (!environment.IsDevelopment())
        {
            Log.Warning(
                "TwoFactor:DataProtectionKeysPath yapılandırılmamış. Kalıcı ve yedekli key-ring hazırlanmadan production kullanıcılarında 2FA zorunluluğunu açmayın.");
        }

        return services;
    }

    /// <summary>Kaynak Sandık/Proje değişikliklerini üretim kayıtlarına aktaran kalıcı kuyruk işleyicisi.</summary>
    public static IServiceCollection AddAmbalajKaynakSenkronizasyonWorker(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AmbalajKaynakSenkronizasyonOptions>(
            configuration.GetSection(AmbalajKaynakSenkronizasyonOptions.SectionName));
        services.AddSingleton<AmbalajKaynakSenkronizasyonKuyrukIsleyici>();
        services.AddSingleton<AmbalajKaynakDegisikligiDinleyici>();
        services.AddHostedService<AmbalajKaynakSenkronizasyonBackgroundService>();

        return services;
    }

    private static RateLimitPartition<string> PerClientIp(HttpContext httpContext, int permitLimit) =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
}
