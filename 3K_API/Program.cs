using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using MediatR;
using FluentValidation;
using Serilog;
using Serilog.Events;
using _3K_API.Logging;
using _3K_API.Middleware;
using _3K.Core.Interfaces;
using _3K.Application.Behaviors;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services;

// Npgsql'in DateTime davranışı için eski uyumluluğu aç (Unspecified DateTime hatasını çözer)
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// ======= Serilog Bootstrap (En erken aşamada, uygulama başlamadan loglamayı ayağa kaldır) =======
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

// Parametrik log path — appsettings'ten veya environment variable'dan okunur
var logBasePath = configuration["Serilog:LogBasePath"] ?? @"C:\Logs\3K";
var traceRetainedCount = int.TryParse(configuration["Serilog:TraceRetainedFileCount"], out var tc) ? tc : 168;   // 7 gün x 24 saat
var serviceRetainedCount = int.TryParse(configuration["Serilog:ServiceRetainedFileCount"], out var sc) ? sc : 720; // 30 gün x 24 saat
var traceRetainedDays = int.TryParse(configuration["Serilog:TraceRetainedDays"], out var trd) ? trd : 7;
var serviceRetainedDays = int.TryParse(configuration["Serilog:ServiceRetainedDays"], out var srd) ? srd : 30;

// Minimum seviye overrides konfigürasyondan okunur
var defaultLevel = Enum.TryParse<LogEventLevel>(configuration["Serilog:MinimumLevel:Default"], out var dl) ? dl : LogEventLevel.Information;

var logOutputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{CorrelationId}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";
const string sseStreamPath = "/api/onay/sse-stream";

bool IsSseStreamLogEvent(LogEvent logEvent)
{
    if (logEvent.Properties.TryGetValue("RequestPath", out var requestPath)
        && requestPath.ToString().Contains(sseStreamPath, StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    return logEvent.RenderMessage().Contains(sseStreamPath, StringComparison.OrdinalIgnoreCase);
}

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(defaultLevel)
    .MinimumLevel.Override("Microsoft.AspNetCore",
        Enum.TryParse<LogEventLevel>(configuration["Serilog:MinimumLevel:Override:Microsoft.AspNetCore"], out var aspLevel) ? aspLevel : LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore",
        Enum.TryParse<LogEventLevel>(configuration["Serilog:MinimumLevel:Override:Microsoft.EntityFrameworkCore"], out var efLevel) ? efLevel : LogEventLevel.Warning)
    .MinimumLevel.Override("System",
        Enum.TryParse<LogEventLevel>(configuration["Serilog:MinimumLevel:Override:System"], out var sysLevel) ? sysLevel : LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("MachineName", System.Environment.MachineName)
    .Enrich.WithProperty("Application", "3K_API")
    // Konsol — Development'ta da görmek için
    .WriteTo.Console(outputTemplate: logOutputTemplate)
    // Trace kanalı: Verbose → Information (tüm detay logları)
    // Klasör yapısı: {LogBasePath}/{yyyy-MM}/{dd}/trace-{HH}.txt
    .WriteTo.Logger(traceLogger => traceLogger
        .Filter.ByExcluding(IsSseStreamLogEvent)
        .WriteTo.HourlyFolderFile(
            basePath: logBasePath,
            filePrefix: "trace",
            minimumLevel: LogEventLevel.Verbose,
            outputTemplate: logOutputTemplate,
            retainedDays: traceRetainedDays))
    // Service kanalı: Warning ve üzeri (hata + kritik loglar)
    // Klasör yapısı: {LogBasePath}/{yyyy-MM}/{dd}/service-{HH}.txt
    .WriteTo.HourlyFolderFile(
        basePath: logBasePath,
        filePrefix: "service",
        minimumLevel: LogEventLevel.Warning,
        outputTemplate: logOutputTemplate,
        retainedDays: serviceRetainedDays)
    .CreateLogger();

try
{
    Log.Information("========== 3K API başlatılıyor ==========");

    var builder = WebApplication.CreateBuilder(args);

    // Hassas bilgileri ayrı dosyadan oku (Git'e gitmez)
    builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true);

    // Serilog'u host'a bağla — tüm ILogger<T> çağrıları artık Serilog'a yönlenir
    builder.Host.UseSerilog();

    // ======= DbContext + AuditInterceptor & ProjectLockInterceptor =======
    builder.Services.AddScoped<AuditInterceptor>();
    builder.Services.AddScoped<_3K.Infrastructure.Data.Interceptors.ProjectLockInterceptor>();

    builder.Services.AddDbContext<AppDbContext>((sp, options) =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
               .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
               .AddInterceptors(
                   sp.GetRequiredService<AuditInterceptor>(),
                   sp.GetRequiredService<_3K.Infrastructure.Data.Interceptors.ProjectLockInterceptor>()
               ));

    // ======= Repository & UnitOfWork =======
    builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IProjeRepository, ProjeRepository>();

    // ======= Services =======
    builder.Services.AddScoped<ICekiService, CekiService>();
    builder.Services.AddScoped<ISandikService, SandikService>();
    builder.Services.AddScoped<IUrunService, UrunService>();
    builder.Services.AddScoped<IStokService, StokService>();
    builder.Services.AddScoped<IHareketService, HareketService>();
    builder.Services.AddScoped<IPdfService, PdfService>();
    builder.Services.AddScoped<IMailService, MailService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IDurumHesaplaService, DurumHesaplaService>();
    builder.Services.AddScoped<ILookupService, _3K.Infrastructure.Services.LookupService>();
    builder.Services.AddScoped<IRolService, _3K.Infrastructure.Services.RolService>();
    builder.Services.AddScoped<IProjectLockService, ProjectLockService>();

    // ======= Current User Service (Pipeline Behavior için) =======
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    // ======= Background Task Queue =======
    builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
    builder.Services.AddHostedService<BackgroundTaskProcessor>();

    // ======= In-Memory Cache + Lookup Cache =======
    builder.Services.AddMemoryCache();
    builder.Services.AddScoped<ILookupCacheService, LookupCacheService>();

    // ======= MediatR + Pipeline Behaviors =======
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(_3K.Application.Features.SandikIslemleri.Commands.FiiliSandikDegistirCommand).Assembly);
        // Pipeline sırası: UnhandledException (en dış) → Authorization → Validation → Caching → Handler
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ApprovalBehavior<,>));
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
    });

    // ======= FluentValidation =======
    builder.Services.AddValidatorsFromAssembly(typeof(_3K.Application.Features.SandikIslemleri.Commands.FiiliSandikDegistirCommand).Assembly);

    // ======= JWT Authentication =======
    var jwtKey = builder.Configuration["JwtSettings:SecretKey"]
        ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
        ?? throw new InvalidOperationException("JWT SecretKey yapılandırılmamış.");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "3K_API",
                ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "3K_Client",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/api/onay/sse-stream"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddSingleton<ISseNotifier, SseNotifier>();
    builder.Services.AddAuthorization();

    // ======= Controllers =======
    builder.Services.AddControllers();

    // ======= Dosya yükleme boyut limiti (50 MB) =======
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = 50 * 1024 * 1024;
    });

    // ======= Swagger =======
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.CustomSchemaIds(type => type.FullName); // Conflict resolution via full namespace
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "3K Sevkiyat / Çeki Takip API",
            Version = "v1",
            Description = "Sevkiyat yönetimi, çeki takibi, sandık operasyonları ve stok yönetimi API'si"
        });

        // JWT Auth tanımı — Http scheme kullanılıyor, Swagger otomatik "Bearer " ekler
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
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // ======= CORS =======
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
            policy.WithOrigins(
                    "https://www.3klojistikdepolama.com",
                    "https://3klojistikdepolama.com"
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());
    });

    var app = builder.Build();

    // ======= Correlation ID — Tüm loglarda istek takibi =======
    app.UseMiddleware<CorrelationIdMiddleware>();

    // ======= Serilog Request Logging — Her HTTP isteğini otomatik loglar =======
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.000} ms";
        options.GetLevel = (httpContext, elapsed, ex) =>
        {
            if (httpContext.Request.Path.StartsWithSegments(sseStreamPath))
                return ex == null ? LogEventLevel.Debug : LogEventLevel.Error;

            if (ex != null) return LogEventLevel.Error;
            if (httpContext.Response.StatusCode >= 500) return LogEventLevel.Error;
            if (httpContext.Response.StatusCode >= 400) return LogEventLevel.Warning;
            if (elapsed > 3000) return LogEventLevel.Warning; // 3 saniyeden uzun süren istekler
            return LogEventLevel.Information;
        };
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "unknown");
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown");
            var user = httpContext.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(user))
                diagnosticContext.Set("UserName", user);
        };
    });

    // ======= Middleware Pipeline =======
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "3K API v1"));
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // ======= Lookup Cache Warmup =======
    using (var scope = app.Services.CreateScope())
    {
        var lookupCache = scope.ServiceProvider.GetRequiredService<ILookupCacheService>();
        await lookupCache.WarmupAsync();
    }

    Log.Information("========== 3K API başarıyla başlatıldı. Log path: {LogBasePath} ==========", logBasePath);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "3K API başlatılırken kritik hata oluştu!");
    throw;
}
finally
{
    // Uygulama kapanırken tüm logların diske yazıldığından emin ol
    Log.Information("========== 3K API kapatılıyor ==========");
    Log.CloseAndFlush();
}

