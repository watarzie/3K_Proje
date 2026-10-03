using Serilog;
using _3K_API.Extensions;
using _3K_API.Logging;

// Npgsql'in DateTime davranışı için eski uyumluluğu aç (Unspecified DateTime hatasını çözer)
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var logBasePath = SerilogBootstrap.CreateLogger();

try
{
    Log.Information("========== 3K API başlatılıyor ==========");

    var builder = WebApplication.CreateBuilder(args);

    // Hassas bilgiler ayrı dosyadan okunur (Git'e gitmez).
    builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true);
    builder.Host.UseSerilog();

    // Dosya yükleme boyut limiti (50 MB)
    builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 50 * 1024 * 1024);

    builder.Services.AddApi(builder.Configuration, builder.Environment);

    var app = builder.Build();

    app.UseApiPipeline();
    await app.WarmupLookupCacheAsync();

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
