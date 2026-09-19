using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.CekiIslemleri.Commands;
using _3K.Application.Features.CekiIslemleri.Queries;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CekiController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CekiController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("yukle")]
        public async Task<ActionResult> Yukle(IFormFile dosya)
        {
            if (dosya == null || dosya.Length == 0)
                return BadRequest(new { message = "Dosya seçilmedi." });

            using var stream = dosya.OpenReadStream();
            var command = new CekiYukleCommand
            {
                ExcelDosya = stream,
                DosyaAdi = dosya.FileName,
                KullaniciId = GetKullaniciId()
            };

            var result = await _mediator.Send(command, HttpContext.RequestAborted);
            return result.ToActionResult();
        }

        /// <summary>
        /// Yedek proje formatındaki .xlsx çekisini yükler.
        /// Proje numarası yeni formatta B1, eski formatta C1 hücresinden okunur;
        /// tüm ürünler 1 numaralı sandığa tahsis edilir.
        /// </summary>
        [Authorize]
        [HttpPost("yedek-yukle")]
        public async Task<ActionResult> YedekYukle(IFormFile dosya)
        {
            if (dosya == null || dosya.Length == 0)
                return BadRequest(new { message = "Dosya seçilmedi." });

            using var stream = dosya.OpenReadStream();
            var command = new YedekCekiYukleCommand
            {
                ExcelDosya = stream,
                DosyaAdi = dosya.FileName,
                KullaniciId = GetKullaniciId()
            };

            var result = await _mediator.Send(command, HttpContext.RequestAborted);
            return result.ToActionResult();
        }

        [HttpPost("revizyon-yukle")]
        public async Task<ActionResult> RevizyonYukle(IFormFile dosya)
        {
            if (dosya == null || dosya.Length == 0)
                return BadRequest(new { message = "Dosya seçilmedi." });

            using var stream = dosya.OpenReadStream();
            var command = new CekiRevizyonYukleCommand
            {
                ExcelDosya = stream,
                DosyaAdi = dosya.FileName,
                KullaniciId = GetKullaniciId()
            };

            var result = await _mediator.Send(command, HttpContext.RequestAborted);
            return result.ToActionResult();
        }

        [HttpPost("revizyon-onizle")]
        public async Task<ActionResult> RevizyonOnizle(IFormFile dosya)
        {
            if (dosya == null || dosya.Length == 0)
                return BadRequest(new { message = "Dosya seçilmedi." });

            using var stream = dosya.OpenReadStream();
            var command = new CekiRevizyonOnizleCommand
            {
                ExcelDosya = stream,
                DosyaAdi = dosya.FileName
            };

            var result = await _mediator.Send(command, HttpContext.RequestAborted);
            return result.ToActionResult();
        }

        [HttpGet("{cekiId}/satirlar")]
        public async Task<ActionResult> GetSatirlari(int cekiId)
        {
            var result = await _mediator.Send(new CekiSatirlariQuery { CekiId = cekiId });
            return result.ToActionResult();
        }

        [Authorize]
        [HttpGet("revizyon-gecmisi/{projeId:int}")]
        public async Task<ActionResult> RevizyonGecmisi(int projeId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _mediator.Send(new GetCekiRevizyonGecmisiQuery { ProjeId = projeId, PageNumber = pageNumber, PageSize = pageSize }, HttpContext.RequestAborted);
            return result.ToActionResult();
        }

        [Authorize]
        [HttpGet("revizyon-gecmisi/{projeId:int}/{kaynak}/{kayitId:int}")]
        public async Task<ActionResult> RevizyonGecmisiDetay(int projeId, string kaynak, int kayitId)
        {
            var result = await _mediator.Send(new GetCekiRevizyonGecmisiDetayQuery { ProjeId = projeId, Kaynak = kaynak, KayitId = kayitId }, HttpContext.RequestAborted);
            return result.ToActionResult();
        }

        [Authorize]
        [HttpGet("revizyon-gecmisi/{projeId:int}/{kaynak}/{kayitId:int}/dosya")]
        public async Task<ActionResult> RevizyonDosya(int projeId, string kaynak, int kayitId)
        {
            var result = await _mediator.Send(new GetCekiRevizyonDosyaQuery { ProjeId = projeId, Kaynak = kaynak, KayitId = kayitId }, HttpContext.RequestAborted);
            if (!result.IsSuccess) return result.ToActionResult();
            return File(result.Value!.Icerik, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.Value.DosyaAdi);
        }

        private int GetKullaniciId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }
    }
}
