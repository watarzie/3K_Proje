using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.SandikIslemleri.Queries;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    /// <summary>Sandık açılış/kapanış istatistiği ve Excel çıktısı (sandik-acilis-istatistigi R).</summary>
    [ApiController]
    [Route("api/sandik-istatistik")]
    public class SandikIstatistikController(IMediator mediator) : ControllerBase
    {
        /// <summary>Tarih aralığı, proje ve müşteri süzgeciyle özet, sebep dağılımı ve en çok geri açılan sandıklar.</summary>
        [HttpGet]
        public async Task<ActionResult> Getir([FromQuery] SandikDurumIstatistigiQuery query, CancellationToken cancellationToken) =>
            (await mediator.Send(query, cancellationToken)).ToActionResult();

        /// <summary>Proje bazında sayılar, sayfalı.</summary>
        [HttpGet("projeler")]
        public async Task<ActionResult> Projeler([FromQuery] SandikDurumProjeleriQuery query, CancellationToken cancellationToken) =>
            (await mediator.Send(query, cancellationToken)).ToActionResult();

        /// <summary>Müşteri bazında sayılar, sayfalı.</summary>
        [HttpGet("musteriler")]
        public async Task<ActionResult> Musteriler([FromQuery] SandikDurumMusterileriQuery query, CancellationToken cancellationToken) =>
            (await mediator.Send(query, cancellationToken)).ToActionResult();

        /// <summary>Kişi bazında sayılar, sayfalı.</summary>
        [HttpGet("kisiler")]
        public async Task<ActionResult> Kisiler([FromQuery] SandikDurumKisileriQuery query, CancellationToken cancellationToken) =>
            (await mediator.Send(query, cancellationToken)).ToActionResult();

        /// <summary>Süzgeçteki olaylar en yeniden eskiye, sayfalı.</summary>
        [HttpGet("olaylar")]
        public async Task<ActionResult> Olaylar([FromQuery] SandikDurumOlaylariQuery query, CancellationToken cancellationToken) =>
            (await mediator.Send(query, cancellationToken)).ToActionResult();

        /// <summary>Aynı süzgecin Excel çıktısı.</summary>
        [HttpGet("excel")]
        public async Task<IActionResult> Excel([FromQuery] SandikDurumIstatistigiExcelQuery query, CancellationToken cancellationToken) =>
            (await mediator.Send(query, cancellationToken)).ToFileResult();
    }
}
