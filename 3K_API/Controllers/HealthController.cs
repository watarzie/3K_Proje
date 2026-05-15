using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _3K.Infrastructure.Data;

namespace _3K_API.Controllers
{
    /// <summary>
    /// Health Check endpoint — Sunucu + Veritabanı canlılık kontrolü.
    /// Monitoring araçları (UptimeRobot, Grafana, Azure Monitor vb.) için.
    /// [AllowAnonymous] — JWT gerektirmez.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HealthController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET /api/health
        /// Sunucu ve veritabanı durumunu döner.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = new HealthCheckResult
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                MachineName = Environment.MachineName,
                Environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
            };

            // Veritabanı bağlantısını kontrol et
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                result.Database = canConnect ? "Connected" : "Unreachable";

                if (!canConnect)
                    result.Status = "Degraded";
            }
            catch (Exception ex)
            {
                result.Database = $"Error: {ex.Message}";
                result.Status = "Unhealthy";
            }

            var statusCode = result.Status switch
            {
                "Healthy" => 200,
                "Degraded" => 200,
                _ => 503
            };

            return StatusCode(statusCode, result);
        }

        private class HealthCheckResult
        {
            public string Status { get; set; } = "Unknown";
            public DateTime Timestamp { get; set; }
            public string MachineName { get; set; } = string.Empty;
            public string Environment { get; set; } = string.Empty;
            public string Database { get; set; } = "Unknown";
        }
    }
}
