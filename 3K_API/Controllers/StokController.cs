using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.DTOs;
using _3K.Application.Features.StokIslemleri.Commands;
using _3K.Application.Features.StokIslemleri.Queries;

namespace _3K_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StokController : ControllerBase
    {
        private readonly IMediator _mediator;

        public StokController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Stok listesi — opsiyonel malzeme kodu filtresi
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StokKaydiDto>>> GetAll([FromQuery] string? malzemeKodu = null)
        {
            var result = await _mediator.Send(new StokListeleQuery { MalzemeKodu = malzemeKodu });
            return Ok(result);
        }

        /// <summary>
        /// Yeni kalan stok kaydı oluştur
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<StokKaydiDto>> Create([FromBody] StokKaydiOlusturCommand command)
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
        }

        /// <summary>
        /// İş akışı 6: Stoktan eksik karşıla
        /// </summary>
        [HttpPost("karsila")]
        public async Task<ActionResult> Karsila([FromBody] StokKarsilaCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);
                return result ? Ok(new { message = "Stoktan karşılandı." }) : BadRequest(new { message = "İşlem başarısız." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
