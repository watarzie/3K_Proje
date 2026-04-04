using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Queries;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SandikController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SandikController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("proje/{projeId}")]
        public async Task<ActionResult> GetProjeSandiklari(int projeId)
        {
            var result = await _mediator.Send(new GetProjeSandiklariQuery { ProjeId = projeId });
            return result.ToActionResult();
        }

        [HttpGet("{sandikId}/icerik")]
        public async Task<ActionResult> GetSandikIcerik(int sandikId)
        {
            var result = await _mediator.Send(new GetSandikIcerikQuery { SandikId = sandikId });
            return result.ToActionResult();
        }

        [HttpPut("urun-guncelle")]
        public async Task<ActionResult> UrunGuncelle([FromBody] UrunGuncelleCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPut("degistir")]
        public async Task<ActionResult> SandikDegistir([FromBody] FiiliSandikDegistirCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPost("manuel-ekle")]
        public async Task<ActionResult> ManuelUrunEkle([FromBody] ManuelUrunEkleCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPost("iptal")]
        public async Task<ActionResult> UrunIptal([FromBody] UrunIptalCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPost("stoktan-karsila")]
        public async Task<ActionResult> StoktanKarsila([FromBody] StoktanKarsilaCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPost("fbden-karsila")]
        public async Task<ActionResult> FBDenKarsila([FromBody] FBDenKarsilaCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        /// <summary>
        /// Projeye yeni boş sandık ekler — çeki dışı ek sandık ihtiyacı için.
        /// </summary>
        [HttpPost("ekle")]
        public async Task<ActionResult> SandikEkle([FromBody] SandikEkleCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }
    }
}
