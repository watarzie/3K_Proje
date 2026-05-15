using Serilog.Context;

namespace _3K_API.Middleware
{
    /// <summary>
    /// Her HTTP isteğine benzersiz Correlation ID atar.
    /// - İstek "X-Correlation-ID" header'ı ile gelirse onu kullanır.
    /// - Gelmezse yeni GUID üretir.
    /// - Tüm Serilog loglarına "CorrelationId" property olarak eklenir.
    /// - Yanıt header'ına da "X-Correlation-ID" olarak yansıtılır.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private const string CorrelationIdHeader = "X-Correlation-ID";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // İstekte varsa kullan, yoksa üret
            var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
                                ?? Guid.NewGuid().ToString("N")[..12]; // Kısa 12 karakterlik ID

            // Response header'a ekle
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[CorrelationIdHeader] = correlationId;
                return Task.CompletedTask;
            });

            // HttpContext.TraceIdentifier'ı da güncelle (ASP.NET Core built-in)
            context.TraceIdentifier = correlationId;

            // Serilog LogContext'e pushla — tüm loglarda görünür
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await _next(context);
            }
        }
    }
}
