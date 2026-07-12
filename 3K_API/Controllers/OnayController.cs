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
        private readonly ICurrentUserService _currentUserService;

        public OnayController(
            IMediator mediator,
            ISseNotifier sseNotifier,
            ICurrentUserService currentUserService)
        {
            _mediator = mediator;
            _sseNotifier = sseNotifier;
            _currentUserService = currentUserService;
        }

        [HttpGet("sse-stream")]
        public async Task SseStream()
        {
            var kullaniciId = _currentUserService.UserId;
            if (!kullaniciId.HasValue)
            {
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await _sseNotifier.SubscribeAsync(HttpContext, kullaniciId.Value);
        }

        [HttpGet]
        public async Task<IActionResult> GetBekleyenler()
        {
            var result = await _mediator.Send(new GetBekleyenOnaylarQuery());
            return result.ToActionResult();
        }

        [HttpGet("gecmis")]
        public async Task<IActionResult> GetGecmis(
            [FromQuery] GetOnayGecmisiQuery query,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return result.ToActionResult();
        }

        [HttpGet("gecmis/{id:int}")]
        public async Task<IActionResult> GetGecmisDetayi(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetOnayGecmisiDetayiQuery { Id = id },
                cancellationToken);
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
            return result.ToActionResult();
        }

        [HttpPost("reddet")]
        public async Task<IActionResult> Reddet([FromBody] IslemReddetCommand command)
        {
            var result = await _mediator.Send(command);
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

        [HttpPut("kural-guncelle")]
        public async Task<IActionResult> KuralGuncelle([FromBody] UpdateOnayKuraliCommand command)
        {
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
