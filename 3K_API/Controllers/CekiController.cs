using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.DTOs;
using _3K.Application.Features.CekiIslemleri.Commands;
using _3K.Application.Features.CekiIslemleri.Queries;

namespace _3K_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CekiController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CekiController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// İş akışı 2: Excel çeki dosyasını yükle (Proje FB NO'ya göre otomatik bulunur/oluşturulur)
        /// </summary>
        [HttpPost("yukle")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CekiYuklemeResultDto>> Yukle(IFormFile dosya)
        {
            if (dosya == null || dosya.Length == 0)
                return BadRequest(new { message = "Dosya seçilmedi." });

            try
            {
                using var stream = dosya.OpenReadStream();
                var command = new CekiYukleCommand
                {
                    ExcelDosya = stream,
                    DosyaAdi = dosya.FileName,
                    KullaniciId = GetKullaniciId()
                };

                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, details = ex.InnerException?.Message });
            }
        }

        [HttpGet("satirlar/{cekiId}")]
        public async Task<ActionResult<IEnumerable<CekiSatiriDto>>> GetSatirlari(int cekiId)
        {
            var result = await _mediator.Send(new CekiSatirlariQuery { CekiId = cekiId });
            return Ok(result);
        }

        private int GetKullaniciId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }
    }
}
