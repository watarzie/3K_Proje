using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.GridIslemleri.Commands;
using _3K.Application.Features.GridIslemleri.Queries;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GridController : ControllerBase
    {
        private readonly IMediator _mediator;

        public GridController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Grid ürün listesi — proje bazında tüm ürünler ve durumları.
        /// </summary>
        [HttpGet("{projeId}/urunler")]
        public async Task<ActionResult> GetGridUrunler(int projeId)
        {
            var result = await _mediator.Send(new GetGridUrunlerQuery { ProjeId = projeId });
            return result.ToActionResult();
        }

        /// <summary>
        /// Grid personeli ürün durumunu günceller.
        /// </summary>
        [HttpPut("durum-guncelle")]
        public async Task<ActionResult> DurumGuncelle([FromBody] GridDurumGuncelleCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        /// <summary>
        /// Grid durumunu sıfırlar — ürünü çeki yüklendiğindeki ham durumuna döndürür.
        /// </summary>
        [HttpPut("durum-sifirla")]
        public async Task<ActionResult> DurumSifirla([FromBody] GridDurumSifirlaCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        /// <summary>
        /// Birden fazla ürünü tek seferde SevkEdildi yapar.
        /// </summary>
        [HttpPost("toplu-sevk")]
        public async Task<ActionResult> TopluSevk([FromBody] GridTopluSevkCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }
    }
}
