using MediatR;
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

            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpGet("satirlar/{cekiId}")]
        public async Task<ActionResult> GetSatirlari(int cekiId)
        {
            var result = await _mediator.Send(new CekiSatirlariQuery { CekiId = cekiId });
            return result.ToActionResult();
        }

        private int GetKullaniciId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }
    }
}
