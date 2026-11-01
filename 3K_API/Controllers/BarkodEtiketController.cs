using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.SandikIslemleri.Queries;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    /// <summary>
    /// Barkod etiketi (barkod-etiket R): proje seçimi, projenin sandık/ürün listeleri (sayfalı) ve seçilenlerin Grid
    /// etiket dosyası (PDF veya Godex EZPL). Veri değiştirmez.
    /// </summary>
    [ApiController]
    [Route("api/barkod-etiket")]
    public class BarkodEtiketController(IMediator mediator) : ControllerBase
    {
        [HttpGet("projeler")]
        public async Task<ActionResult> Projeler([FromQuery] BarkodEtiketProjeleriQuery query, CancellationToken cancellationToken) =>
            (await mediator.Send(query, cancellationToken)).ToActionResult();

        [HttpGet("projeler/{projeId:int}")]
        public async Task<ActionResult> Proje(int projeId, CancellationToken cancellationToken) =>
            (await mediator.Send(new BarkodEtiketProjeQuery { ProjeId = projeId }, cancellationToken)).ToActionResult();

        [HttpGet("projeler/{projeId:int}/sandiklar")]
        public async Task<ActionResult> Sandiklar(int projeId, [FromQuery] BarkodEtiketSandiklariQuery query, CancellationToken cancellationToken)
        {
            query.ProjeId = projeId;
            return (await mediator.Send(query, cancellationToken)).ToActionResult();
        }

        [HttpGet("projeler/{projeId:int}/urunler")]
        public async Task<ActionResult> Urunler(int projeId, [FromQuery] BarkodEtiketUrunleriQuery query, CancellationToken cancellationToken)
        {
            query.ProjeId = projeId;
            return (await mediator.Send(query, cancellationToken)).ToActionResult();
        }

        /// <summary>Seçilen sandıkların etiketleri; Bicim 1 = PDF, 2 = EZPL (.prn).</summary>
        [HttpPost("projeler/{projeId:int}/sandik-etiketleri")]
        public async Task<IActionResult> SandikEtiketleri(int projeId, [FromBody] SandikEtiketiOlusturQuery query, CancellationToken cancellationToken)
        {
            query.ProjeId = projeId;
            return (await mediator.Send(query, cancellationToken)).ToFileResult();
        }

        /// <summary>Seçilen ürünlerin etiketleri; Bicim 1 = PDF, 2 = EZPL (.prn).</summary>
        [HttpPost("projeler/{projeId:int}/urun-etiketleri")]
        public async Task<IActionResult> UrunEtiketleri(int projeId, [FromBody] UrunEtiketiOlusturQuery query, CancellationToken cancellationToken)
        {
            query.ProjeId = projeId;
            return (await mediator.Send(query, cancellationToken)).ToFileResult();
        }
    }
}
