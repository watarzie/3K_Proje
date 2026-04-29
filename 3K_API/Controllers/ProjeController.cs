using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Application.Features.ProjeIslemleri.Queries;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjeController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProjeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll([FromQuery] int? projeTipiId = null)
        {
            var result = await _mediator.Send(new ProjeListeleQuery { ProjeTipiId = projeTipiId });
            return result.ToActionResult();
        }

        /// <summary>
        /// Dropdown'lar için hafif proje listesi — Include yok, sadece Id/ProjeNo/Musteri.
        /// </summary>
        [HttpGet("dropdown")]
        public async Task<ActionResult> GetDropdown()
        {
            var result = await _mediator.Send(new ProjeDropdownQuery());
            return result.ToActionResult();
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] ProjeOlusturCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        /// <summary>
        /// Sandık Kapat/Aç — Sadece Admin
        /// </summary>
        [HttpPut("sandik-kapat")]
        public async Task<ActionResult> SandikKapat([FromBody] SandikKapatCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }
        /// <summary>
        /// Projeyi kilitler (Sevk Edildi durumuna çeker) — Sadece yetkililer
        /// </summary>
        [HttpPost("{id}/sevk-et")]
        public async Task<ActionResult> SevkEt(int id)
        {
            var result = await _mediator.Send(new ProjeSevkEtCommand { ProjeId = id });
            return result.ToActionResult();
        }

        /// <summary>
        /// Proje kilidini açar (Devam durumuna çeker) — Sadece Admin
        /// </summary>
        [HttpPost("{id}/kilidi-ac")]
        public async Task<ActionResult> KilidiAc(int id)
        {
            var result = await _mediator.Send(new ProjeKilidiAcCommand { ProjeId = id });
            return result.ToActionResult();
        }

        /// <summary>
        /// Projenin Planlanan Sevk Tarihini günceller — Sadece Admin
        /// </summary>
        [HttpPut("sevk-tarihi-guncelle")]
        public async Task<ActionResult> SevkTarihiGuncelle([FromBody] ProjeSevkTarihiGuncelleCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }
    }
}
