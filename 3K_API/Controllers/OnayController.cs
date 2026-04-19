using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.OnayIslemleri.Commands;
using _3K.Application.Features.OnayIslemleri.Queries;
using _3K.Core.Interfaces;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OnayController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ISseNotifier _sseNotifier;

        public OnayController(IMediator mediator, ISseNotifier sseNotifier)
        {
            _mediator = mediator;
            _sseNotifier = sseNotifier;
        }

        [HttpGet("sse-stream")]
        public async Task SseStream()
        {
            await _sseNotifier.SubscribeAsync(HttpContext);
        }

        [HttpGet]
        public async Task<IActionResult> GetBekleyenler()
        {
            var result = await _mediator.Send(new GetBekleyenOnaylarQuery());
            return result.ToActionResult();
        }

        [HttpGet("bekleyen-sayisi")]
        public async Task<IActionResult> GetBekleyenSayisi()
        {
            var result = await _mediator.Send(new GetBekleyenSayisiQuery());
            return result.ToActionResult();
        }

        [HttpPost("onayla")]
        public async Task<IActionResult> Onayla([FromBody] IslemOnaylaCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsSuccess) await _sseNotifier.BroadcastApprovalUpdateAsync();
            return result.ToActionResult();
        }

        [HttpPost("reddet")]
        public async Task<IActionResult> Reddet([FromBody] IslemReddetCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsSuccess) await _sseNotifier.BroadcastApprovalUpdateAsync();
            return result.ToActionResult();
        }

        [HttpPut("kural-guncelle/{lookupUcKDurumId}")]
        public async Task<IActionResult> KuralGuncelle(int lookupUcKDurumId, [FromBody] bool onayGerektirirMi)
        {
            var command = new UpdateOnayKuraliCommand
            {
                LookupUcKDurumId = lookupUcKDurumId,
                OnayGerektirirMi = onayGerektirirMi
            };
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpGet("kurallar")]
        public async Task<IActionResult> GetKurallar()
        {
            var result = await _mediator.Send(new GetOnayKurallariQuery());
            return result.ToActionResult();
        }
    }
}
