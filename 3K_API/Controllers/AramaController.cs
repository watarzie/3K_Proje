using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.AramaIslemleri;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    /// <summary>Üst çubuktaki genel arama: projeler, ürünler (barkod) ve sandıklar; her grup kullanıcının iznine göre.</summary>
    [ApiController]
    [Route("api/arama")]
    public class AramaController(IMediator mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult> Ara([FromQuery] GenelAramaQuery query, CancellationToken cancellationToken) =>
            (await mediator.Send(query, cancellationToken)).ToActionResult();
    }
}
