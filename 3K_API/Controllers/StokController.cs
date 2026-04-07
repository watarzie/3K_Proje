using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.StokIslemleri.Commands;
using _3K.Application.Features.StokIslemleri.Queries;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StokController : ControllerBase
    {
        private readonly IMediator _mediator;

        public StokController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll([FromQuery] string? malzemeKodu = null)
        {
            var result = await _mediator.Send(new StokListeleQuery { MalzemeKodu = malzemeKodu });
            return result.ToActionResult();
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] StokKaydiOlusturCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPost("karsila")]
        public async Task<ActionResult> Karsila([FromBody] StokKarsilaCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }
    }
}
