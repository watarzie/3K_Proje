using Serilog;
using Serilog.Events;

namespace _3K_API.Logging;

/// <summary>
/// Uygulama host'u kurulmadan önce loglamayı başlatır. Yapılandırma host'tan bağımsız
/// okunur; böylece başlangıç hataları da trace/service dosyalarına düşer.
/// </summary>
public static class SerilogBootstrap
{
    public const string SseStreamPath = "/api/onay/sse-stream";

    private const string OutputTemplate =
        "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{CorrelationId}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

    public static string CreateLogger()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
                optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var logBasePath = configuration["Serilog:LogBasePath"] ?? @"C:\Logs\3K";
        var traceRetainedDays = ReadInt(configuration, "Serilog:TraceRetainedDays", 7);
        var serviceRetainedDays = ReadInt(configuration, "Serilog:ServiceRetainedDays", 30);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(ReadLevel(configuration, "Serilog:MinimumLevel:Default", LogEventLevel.Information))
            .MinimumLevel.Override("Microsoft.AspNetCore",
                ReadLevel(configuration, "Serilog:MinimumLevel:Override:Microsoft.AspNetCore", LogEventLevel.Warning))
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore",
                ReadLevel(configuration, "Serilog:MinimumLevel:Override:Microsoft.EntityFrameworkCore", LogEventLevel.Warning))
            .MinimumLevel.Override("System",
                ReadLevel(configuration, "Serilog:MinimumLevel:Override:System", LogEventLevel.Warning))
            .Enrich.FromLogContext()
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .Enrich.WithProperty("Application", "3K_API")
            .WriteTo.Console(outputTemplate: OutputTemplate)
            // Trace kanalı: tüm detay logları, {LogBasePath}/{yyyy-MM}/{dd}/trace-{HH}.txt
            .WriteTo.Logger(traceLogger => traceLogger
                .Filter.ByExcluding(IsSseStreamLogEvent)
                .WriteTo.HourlyFolderFile(
                    basePath: logBasePath,
                    filePrefix: "trace",
                    minimumLevel: LogEventLevel.Verbose,
                    outputTemplate: OutputTemplate,
                    retainedDays: traceRetainedDays))
            // Service kanalı: Warning ve üzeri, {LogBasePath}/{yyyy-MM}/{dd}/service-{HH}.txt
            .WriteTo.HourlyFolderFile(
                basePath: logBasePath,
                filePrefix: "service",
                minimumLevel: LogEventLevel.Warning,
                outputTemplate: OutputTemplate,
                retainedDays: serviceRetainedDays)
            .CreateLogger();

        return logBasePath;
    }

    private static bool IsSseStreamLogEvent(LogEvent logEvent)
    {
        if (logEvent.Properties.TryGetValue("RequestPath", out var requestPath)
            && requestPath.ToString().Contains(SseStreamPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return logEvent.RenderMessage().Contains(SseStreamPath, StringComparison.OrdinalIgnoreCase);
    }

    private static int ReadInt(IConfiguration configuration, string key, int fallback) =>
        int.TryParse(configuration[key], out var value) ? value : fallback;

    private static LogEventLevel ReadLevel(IConfiguration configuration, string key, LogEventLevel fallback) =>
        Enum.TryParse<LogEventLevel>(configuration[key], out var level) ? level : fallback;
}
