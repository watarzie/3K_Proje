using _3K.Application.Features.BildirimIslemleri.Commands;
using _3K.Application.Features.BildirimIslemleri.Queries;
using _3K_API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _3K_API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/bildirimler")]
    public class BildirimController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BildirimController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetBildirimler(
            [FromQuery] GetBildirimlerQuery query,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return result.ToActionResult();
        }

        [HttpGet("{bildirimId:int}")]
        public async Task<IActionResult> GetBildirimDetayi(
            int bildirimId,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetBildirimDetayiQuery { BildirimId = bildirimId },
                cancellationToken);
            return result.ToActionResult();
        }

        [HttpGet("okunmamis")]
        public async Task<IActionResult> GetOkunmamis(
            [FromQuery] int limit = 20,
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(
                new GetOkunmamisBildirimlerQuery { Limit = limit },
                cancellationToken);
            return result.ToActionResult();
        }

        [HttpPut("{bildirimId:int}/okundu")]
        public async Task<IActionResult> OkunduIsaretle(
            int bildirimId,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new BildirimiOkunduIsaretleCommand { BildirimId = bildirimId },
                cancellationToken);
            return result.ToActionResult();
        }

        [HttpPut("tumunu-okundu")]
        public async Task<IActionResult> TumunuOkunduIsaretle(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new TumBildirimleriOkunduIsaretleCommand(),
                cancellationToken);
            return result.ToActionResult();
        }

        [HttpGet("abonelik-ayarlari")]
        public async Task<IActionResult> GetAbonelikAyarlari()
        {
            var result = await _mediator.Send(new GetBildirimAbonelikAyarlariQuery());
            return result.ToActionResult();
        }

        [HttpPut("abonelik-ayarlari")]
        public async Task<IActionResult> AbonelikAyarlariGuncelle(
            [FromBody] BildirimAbonelikleriniGuncelleCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }
    }
}
