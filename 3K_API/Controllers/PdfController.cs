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

        [HttpGet("{projeId}/indir")]
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

        [HttpGet("{projeId}/excel")]
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

        [HttpGet("saha-sandik/{sandikId}")]
        public async Task<IActionResult> SahaSandikPdfIndir(int sandikId)
        {
            var result = await _mediator.Send(new _3K.Application.Features.PdfIslemleri.Queries.GetSahaSandikPdfQuery
            {
                SandikId = sandikId
            });

            if (!result.IsSuccess)
                return result.ToActionResult();

            return File(result.Value!, "application/pdf", $"SahaSandikRaporu_{sandikId}.pdf");
        }

        [HttpGet("saha-proje/{projeId}")]
        public async Task<IActionResult> SahaProjePdfIndir(int projeId)
        {
            var result = await _mediator.Send(new _3K.Application.Features.PdfIslemleri.Queries.GetSahaProjePdfQuery
            {
                ProjeId = projeId
            });

            if (!result.IsSuccess)
                return result.ToActionResult();

            return File(result.Value!, "application/pdf", $"SahaProje_TopluRapor_{projeId}.pdf");
        }

        private int GetKullaniciId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }
    }
}
