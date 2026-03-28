using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.PdfIslemleri.Commands;

namespace _3K_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PdfController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PdfController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// İş akışı 9: PDF oluştur ve indir
        /// </summary>
        [HttpGet("indir/{projeId}")]
        public async Task<IActionResult> Indir(int projeId)
        {
            try
            {
                var kullaniciId = GetKullaniciId();
                var pdfBytes = await _mediator.Send(new PdfOlusturCommand
                {
                    ProjeId = projeId,
                    KullaniciId = kullaniciId
                });

                return File(pdfBytes, "application/pdf", $"Ceki_Proje_{projeId}.pdf");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        private int GetKullaniciId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }
    }
}
