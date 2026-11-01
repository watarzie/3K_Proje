using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.SandikIslemleri.Queries;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    /// <summary>Barkodla ürün arama: bir barkodun bütün projelerdeki satırları ve raporu (barkod-urun-arama R).</summary>
    [ApiController]
    [Route("api/barkod-arama")]
    public class BarkodAramaController(IMediator mediator) : ControllerBase
    {
        /// <summary>Barkodun bütün projelerdeki satırları; varsayılan yalnız eksikler, eksikler önce.</summary>
        [HttpGet]
        public async Task<ActionResult> Ara([FromQuery] BarkodUrunAraQuery query, CancellationToken cancellationToken) =>
            (await mediator.Send(query, cancellationToken)).ToActionResult();

        /// <summary>Aynı aramanın raporu; format "pdf" veya "excel". Her satırda proje no.</summary>
        [HttpGet("rapor/{format}")]
        public async Task<IActionResult> Rapor(string format, [FromQuery] BarkodUrunAramaRaporuQuery query, CancellationToken cancellationToken)
        {
            query.Format = format;
            return (await mediator.Send(query, cancellationToken)).ToFileResult();
        }
    }
}
