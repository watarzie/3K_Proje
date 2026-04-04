using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.PdfIslemleri.Commands;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PdfController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PdfController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("indir/{projeId}")]
        public async Task<IActionResult> Indir(int projeId)
        {
            var kullaniciId = GetKullaniciId();
            var result = await _mediator.Send(new PdfOlusturCommand
            {
                ProjeId = projeId,
                KullaniciId = kullaniciId
            });

            if (!result.IsSuccess)
                return result.ToActionResult();

            return File(result.Value!, "application/pdf", $"Ceki_Proje_{projeId}.pdf");
        }

        [HttpGet("excel/{projeId}")]
        public async Task<IActionResult> ExcelIndir(int projeId)
        {
            var kullaniciId = GetKullaniciId();
            var result = await _mediator.Send(new ExcelOlusturCommand
            {
                ProjeId = projeId,
                KullaniciId = kullaniciId
            });

            if (!result.IsSuccess)
                return result.ToActionResult();

            return File(result.Value!,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Ceki_Proje_{projeId}.xlsx");
        }

        private int GetKullaniciId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }
    }
}
