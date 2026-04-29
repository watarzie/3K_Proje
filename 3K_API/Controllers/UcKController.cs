using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.UcKIslemleri.Commands;
using _3K.Application.Features.UcKIslemleri.Queries;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UcKController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UcKController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// 3K ürün listesi — proje bazında tüm ürünler ve 3K durumları.
        /// </summary>
        [HttpGet("{projeId}/urunler")]
        public async Task<ActionResult> GetUcKUrunler(int projeId)
        {
            var result = await _mediator.Send(new GetUcKUrunlerQuery { ProjeId = projeId });
            return result.ToActionResult();
        }

        /// <summary>
        /// 3K personeli ürün karşılama durumunu günceller.
        /// </summary>
        [HttpPut("durum-guncelle")]
        public async Task<ActionResult> DurumGuncelle([FromBody] UcKDurumGuncelleCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        /// <summary>
        /// 3K karşılama durumunu sıfırlar — ürünü başlangıç (Bekliyor) durumuna döndürür.
        /// </summary>
        [HttpPut("durum-sifirla")]
        public async Task<ActionResult> DurumSifirla([FromBody] UcKDurumSifirlaCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }
    }
}
