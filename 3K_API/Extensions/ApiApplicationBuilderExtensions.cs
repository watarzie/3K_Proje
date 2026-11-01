using Serilog;
using Serilog.Events;
using _3K_API.Logging;
using _3K_API.Middleware;
using _3K.Core.Interfaces;

namespace _3K_API.Extensions;

/// <summary>HTTP isteği işleme hattı; sıra güvenlik açısından önemlidir.</summary>
public static class ApiApplicationBuilderExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseApiRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "3K API v1"));
        }
        else
        {
            app.UseHttpsRedirection();
        }

        app.UseCors(ApiServiceCollectionExtensions.CorsPolicy);
        // Hız sınırı kimlik doğrulamadan sonra çalışır: genel sınır oturumlu kullanıcıyı Id'siyle bölümler.
        // Yetkilendirmeden önce çalışır: oturumsuz istek yükü de IP başına sınırlanır.
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

    public static async Task WarmupLookupCacheAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ILookupCacheService>().WarmupAsync();
    }

    private static void UseApiRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.000} ms";
            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                if (httpContext.Request.Path.StartsWithSegments(SerilogBootstrap.SseStreamPath))
                    return ex == null ? LogEventLevel.Debug : LogEventLevel.Error;

                if (ex != null || httpContext.Response.StatusCode >= 500) return LogEventLevel.Error;
                if (httpContext.Response.StatusCode >= 400) return LogEventLevel.Warning;
                return elapsed > 3000 ? LogEventLevel.Warning : LogEventLevel.Information;
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
    }
}
