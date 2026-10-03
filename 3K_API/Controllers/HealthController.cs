using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _3K.Core.Interfaces;

namespace _3K_API.Controllers
{
    /// <summary>
    /// Sunucu + veritabanı canlılık kontrolü (UptimeRobot, Grafana vb. için).
    /// Anonimdir; bu yüzden makine adı veya hata ayrıntısı döndürmez.
    /// </summary>
    [ApiController]
    [AllowAnonymous]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IDatabaseHealthProbe _databaseHealthProbe;
        private readonly IWebHostEnvironment _environment;

        public HealthController(IDatabaseHealthProbe databaseHealthProbe, IWebHostEnvironment environment)
        {
            _databaseHealthProbe = databaseHealthProbe;
            _environment = environment;
        }

        /// <summary>GET /api/health — sunucu ve veritabanı durumunu döner.</summary>
        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var database = await _databaseHealthProbe.CheckAsync(cancellationToken);
            var (status, databaseText, statusCode) = database switch
            {
                DatabaseHealth.Connected => ("Healthy", "Connected", 200),
                DatabaseHealth.Unreachable => ("Degraded", "Unreachable", 200),
                _ => ("Unhealthy", "Error", 503)
            };

            return StatusCode(statusCode, new
            {
                Status = status,
                Timestamp = DateTime.UtcNow,
                Environment = _environment.EnvironmentName,
                Database = databaseText
            });
        }
    }
}
