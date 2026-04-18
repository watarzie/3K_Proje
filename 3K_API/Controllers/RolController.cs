using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.RolIslemleri.Queries;
using _3K.Application.Features.RolIslemleri.Commands;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    /// <summary>
    /// Rol ve Yetki Yönetimi API.
    /// Tüm endpoint'ler sadece Admin yetkisi gerektirir.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RolController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RolController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Tüm rolleri listeler.</summary>
        [HttpGet("liste")]
        public async Task<ActionResult> GetRoller()
        {
            var result = await _mediator.Send(new GetRollerQuery());
            return result.ToActionResult();
        }

        /// <summary>Tek rolün detayını (menü ağacı + yetkiler) getirir.</summary>
        [HttpGet("{id}/detay")]
        public async Task<ActionResult> GetRolDetay(int id)
        {
            var result = await _mediator.Send(new GetRolDetayQuery { RolId = id });
            return result.ToActionResult();
        }

        /// <summary>Yeni rol oluşturur.</summary>
        [HttpPost("olustur")]
        public async Task<ActionResult> RolOlustur([FromBody] RolOlusturCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        /// <summary>Rol adı ve yetkilerini günceller.</summary>
        [HttpPut("guncelle")]
        public async Task<ActionResult> RolGuncelle([FromBody] RolGuncelleCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        /// <summary>Rolü siler.</summary>
        [HttpDelete("{id}/sil")]
        public async Task<ActionResult> RolSil(int id)
        {
            var result = await _mediator.Send(new RolSilCommand { Id = id });
            return result.ToActionResult();
        }
    }
}
