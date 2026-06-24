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
        public async Task<ActionResult> GetAll(
            [FromQuery] int? projeTipiId = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] bool? isSevkEdilen = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 15)
        {
            var result = await _mediator.Send(new ProjeListeleQuery
            {
                ProjeTipiId = projeTipiId,
                SearchTerm = searchTerm,
                IsSevkEdilen = isSevkEdilen,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
            return result.ToActionResult();
        }

        /// <summary>
        /// Dropdown'lar için hafif proje listesi — Include yok, sadece Id/ProjeNo/Musteri.
        /// </summary>
        [HttpGet("dropdown")]
        public async Task<ActionResult> GetDropdown(
            [FromQuery] int? projeTipiId = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] bool? isSevkEdilen = null,
            [FromQuery] int take = 50,
            [FromQuery] List<int>? includeIds = null)
        {
            var result = await _mediator.Send(new ProjeDropdownQuery
            {
                ProjeTipiId = projeTipiId,
                SearchTerm = searchTerm,
                IsSevkEdilen = isSevkEdilen,
                Take = take,
                IncludeIds = includeIds ?? []
            });
            return result.ToActionResult();
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] ProjeOlusturCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPost("eksiklerden-saha-olustur")]
        public async Task<ActionResult> EksiklerdenSahaOlustur([FromBody] EksiklerdenSahaProjesiOlusturCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPost("sandiklardan-saha-olustur")]
        public async Task<ActionResult> SandiklardanSahaOlustur([FromBody] SandiklardanSahaProjesiOlusturCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPost("saha-aktarim-geri-al")]
        public async Task<ActionResult> SahaAktarimGeriAl([FromBody] SahaAktarimGeriAlCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpGet("{id}/saha-aktarimlari")]
        public async Task<ActionResult> GetSahaAktarimlari(int id)
        {
            var result = await _mediator.Send(new GetSahaAktarimlariQuery { ProjeId = id });
            return result.ToActionResult();
        }

        /// <summary>
        /// Sandık Kapat/Aç
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
        public async Task<ActionResult> SevkEt(int id, [FromBody] SevkEtRequest? request = null)
        {
            var result = await _mediator.Send(new ProjeSevkEtCommand
            {
                ProjeId = id,
                SevkTarihi = request?.SevkTarihi,
                SandikIds = request?.SandikIds,
                Aciklama = request?.Aciklama,
                AracPlaka = request?.AracPlaka
            });
            return result.ToActionResult();
        }

        [HttpGet("{id}/sevkiyatlar")]
        public async Task<ActionResult> GetSevkiyatlar(int id)
        {
            var result = await _mediator.Send(new GetProjeSevkiyatlariQuery { ProjeId = id });
            return result.ToActionResult();
        }

        /// <summary>
        /// Proje kilidini açar (Devam durumuna çeker)
        /// </summary>
        [HttpPost("{id}/kilidi-ac")]
        public async Task<ActionResult> KilidiAc(int id, [FromBody] KilitAcRequest? request = null)
        {
            var result = await _mediator.Send(new ProjeKilidiAcCommand
            {
                ProjeId = id,
                ProjeNo = request?.ProjeNo,
                KilitAcmaTipiId = request?.KilitAcmaTipiId ?? (int)_3K.Core.Enums.SevkiyatKilitAcmaTipi.SevkiyatGeriAlinarakAc,
                Aciklama = request?.Aciklama
            });
            return result.ToActionResult();
        }

        /// <summary>
        /// Projenin Planlanan Sevk Tarihini günceller
        /// </summary>
        [HttpPut("sevk-tarihi-guncelle")]
        public async Task<ActionResult> SevkTarihiGuncelle([FromBody] ProjeSevkTarihiGuncelleCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }
        /// <summary>
        /// Projeyi ve tüm alt verilerini (sandık, ürün, çeki vb.) siler
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _mediator.Send(new ProjeSilCommand { ProjeId = id });
            return result.ToActionResult();
        }
    }

    public record SevkEtRequest
    {
        public DateTime? SevkTarihi { get; init; }
        public List<int>? SandikIds { get; init; }
        public string? Aciklama { get; init; }
        public string? AracPlaka { get; init; }
    }

    public record KilitAcRequest
    {
        public int? KilitAcmaTipiId { get; init; }
        public string? ProjeNo { get; init; }
        public string? Aciklama { get; init; }
    }
}
