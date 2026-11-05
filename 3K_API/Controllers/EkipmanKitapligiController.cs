using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.YuklemePlanlamaIslemleri.Commands;
using _3K.Application.Features.YuklemePlanlamaIslemleri.Queries;
using _3K.Core.Models;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    /// <summary>
    /// Ekipman kitaplığı (ekipman-kitapligi: R görüntüleme ve Excel indirme, W içe aktarma; ekipman-dogrulama: W
    /// doğrulama).
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/ekipman-kitapligi")]
    public sealed class EkipmanKitapligiController(IMediator mediator) : ControllerBase
    {
        // Gövde, dosya sınırı (10 MB) ve form başlıkları kadar; daha büyüğü tamponlanmadan reddedilir.
        private const long DosyaGovdeSiniri = EkipmanKitapligiKurali.DosyaEnFazlaBayt + 1024 * 1024;

        /// <summary>Taşıma aileleri ve tipleri.</summary>
        [HttpGet("aileler")]
        public async Task<ActionResult> Aileler(CancellationToken ct) =>
            (await mediator.Send(new EkipmanAileleriQuery(), ct)).ToActionResult();

        /// <summary>Konfigürasyonlar, sayfalı.</summary>
        [HttpGet("konfigurasyonlar")]
        public async Task<ActionResult> Konfigurasyonlar([FromQuery] EkipmanKonfigurasyonlariQuery query, CancellationToken ct) =>
            (await mediator.Send(query, ct)).ToActionResult();

        /// <summary>Konfigürasyon ayrıntısı, teknik verisi ve revizyonları.</summary>
        [HttpGet("konfigurasyonlar/{id:int}")]
        public async Task<ActionResult> Konfigurasyon(int id, CancellationToken ct) =>
            (await mediator.Send(new EkipmanKonfigurasyonGetirQuery { Id = id }, ct)).ToActionResult();

        /// <summary>Son revizyonu doğrulanmış işaretler.</summary>
        [HttpPost("konfigurasyonlar/{id:int}/dogrula")]
        public async Task<ActionResult> Dogrula(int id, [FromBody] EkipmanDogrulaCommand command, CancellationToken ct)
        {
            command.KonfigurasyonId = id;
            return (await mediator.Send(command, ct)).ToActionResult();
        }

        /// <summary>Boş içe aktarım şablonu.</summary>
        [HttpGet("sablon")]
        public async Task<IActionResult> Sablon(CancellationToken ct) =>
            (await mediator.Send(new EkipmanSablonuQuery(), ct)).ToFileResult();

        /// <summary>Kitaplığın etkin konfigürasyonları şablon biçiminde.</summary>
        [HttpGet("disa-aktar")]
        public async Task<IActionResult> DisaAktar(CancellationToken ct) =>
            (await mediator.Send(new EkipmanDisaAktarQuery(), ct)).ToFileResult();

        /// <summary>İçe aktarım önizlemesi: denetler, yazmaz.</summary>
        [HttpPost("ice-aktar/onizle")]
        [RequestSizeLimit(DosyaGovdeSiniri)]
        public async Task<ActionResult> IceAktarOnizle(IFormFile dosya, CancellationToken ct)
        {
            if (dosya == null || dosya.Length == 0)
                return BadRequest(new { message = "Dosya seçilmedi." });

            await using var akis = dosya.OpenReadStream();
            return (await mediator.Send(new EkipmanIceAktarOnizleQuery
            {
                Dosya = akis,
                DosyaAdi = dosya.FileName,
                DosyaBoyutu = dosya.Length
            }, ct)).ToActionResult();
        }

        /// <summary>İçe aktarım: sorun yoksa hepsini yazar, sorun varsa hiçbirini yazmaz.</summary>
        [HttpPost("ice-aktar")]
        [RequestSizeLimit(DosyaGovdeSiniri)]
        public async Task<ActionResult> IceAktar(IFormFile dosya, CancellationToken ct)
        {
            if (dosya == null || dosya.Length == 0)
                return BadRequest(new { message = "Dosya seçilmedi." });

            await using var akis = dosya.OpenReadStream();
            return (await mediator.Send(new EkipmanIceAktarCommand
            {
                Dosya = akis,
                DosyaAdi = dosya.FileName,
                DosyaBoyutu = dosya.Length
            }, ct)).ToActionResult();
        }
    }
}
