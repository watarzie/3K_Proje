using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _3K.Application.Features.PdfIslemleri.Commands;
using _3K.Infrastructure.Data;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PdfController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly AppDbContext _context;

        public PdfController(IMediator mediator, AppDbContext context)
        {
            _mediator = mediator;
            _context = context;
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

            var projeNo = await GetProjeNo(projeId);
            return File(result.Value!, "application/pdf", $"{projeNo}_CekiRaporu.pdf");
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

            var projeNo = await GetProjeNo(projeId);
            return File(result.Value!,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{projeNo}_Ceki.xlsx");
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

            // Sandıktan projeNo'yu çek
            var sandik = await _context.Sandiklar.Include(s => s.Proje).FirstOrDefaultAsync(s => s.Id == sandikId);
            var projeNo = sandik?.Proje?.ProjeNo ?? sandikId.ToString();
            var tipStr = sandik?.Proje?.ProjeTipiId == 3 ? "YedekRaporu" : "SahaRaporu";
            return File(result.Value!, "application/pdf", $"{projeNo}_{tipStr}.pdf");
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

            var proje = await _context.Projeler.FindAsync(projeId);
            var projeNo = proje?.ProjeNo ?? projeId.ToString();
            var tipStr = proje?.ProjeTipiId == 3 ? "YedekRaporu" : "SahaRaporu";
            return File(result.Value!, "application/pdf", $"{projeNo}_{tipStr}.pdf");
        }

        [HttpGet("eksik-urunler/{projeId}")]
        public async Task<IActionResult> EksikUrunlerPdfIndir(int projeId)
        {
            var result = await _mediator.Send(new _3K.Application.Features.PdfIslemleri.Queries.GetEksikUrunlerPdfQuery
            {
                ProjeId = projeId
            });

            if (!result.IsSuccess)
                return result.ToActionResult();

            var projeNo = await GetProjeNo(projeId);
            return File(result.Value!, "application/pdf", $"{projeNo}_EksikRaporu.pdf");
        }

        [HttpGet("stok")]
        public async Task<IActionResult> StokPdfIndir()
        {
            var result = await _mediator.Send(new _3K.Application.Features.PdfIslemleri.Queries.GetStokPdfQuery());

            if (!result.IsSuccess)
                return result.ToActionResult();

            var tarih = DateTime.Now.ToString("yyyyMMdd");
            return File(result.Value!, "application/pdf", $"StokRaporu_{tarih}.pdf");
        }

        private int GetKullaniciId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        private async Task<string> GetProjeNo(int projeId)
        {
            var proje = await _context.Projeler.FindAsync(projeId);
            return proje?.ProjeNo ?? projeId.ToString();
        }
    }
}
