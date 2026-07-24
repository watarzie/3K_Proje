using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _3K.Application.Features.PdfIslemleri.Commands;
using _3K.Core.Enums;
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

            var sandikBilgisi = await _context.Sandiklar
                .AsNoTracking()
                .Where(s => s.Id == sandikId)
                .Select(s => new
                {
                    s.Proje.ProjeNo,
                    s.Proje.ProjeTipiId
                })
                .SingleOrDefaultAsync();

            if (sandikBilgisi == null)
                return NotFound(new { message = "Sandık bulunamadı." });

            var projeNo = sandikBilgisi.ProjeNo;
            var tipStr = sandikBilgisi.ProjeTipiId == (int)ProjeTipi.Yedek ? "YedekRaporu" : "SahaRaporu";
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
            var tipStr = proje?.ProjeTipiId == (int)ProjeTipi.Yedek ? "YedekRaporu" : "SahaRaporu";
            return File(result.Value!, "application/pdf", $"{projeNo}_{tipStr}.pdf");
        }

        [HttpGet("eksik-urunler/{projeId}")]
        public async Task<IActionResult> EksikUrunlerPdfIndir(
            int projeId,
            CancellationToken cancellationToken)
        {
            var projeBilgisi = await GetProjeDosyaBilgisi(projeId, cancellationToken);
            var result = await _mediator.Send(new _3K.Application.Features.PdfIslemleri.Queries.GetEksikUrunlerPdfQuery
            {
                ProjeId = projeId,
                ProjeTipi = (ProjeTipi)projeBilgisi.ProjeTipiId
            }, cancellationToken);

            if (!result.IsSuccess)
                return result.ToActionResult();

            var raporAdi = projeBilgisi.ProjeTipiId == (int)ProjeTipi.Saha ? "SevkSonrasiEksikRaporu" : "EksikRaporu";
            return File(result.Value!, "application/pdf", $"{projeBilgisi.ProjeNo}_{raporAdi}.pdf");
        }

        [HttpGet("eksik-urunler/{projeId}/excel")]
        public async Task<IActionResult> EksikUrunlerExcelIndir(
            int projeId,
            CancellationToken cancellationToken)
        {
            var projeBilgisi = await GetProjeDosyaBilgisi(projeId, cancellationToken);
            var result = await _mediator.Send(new _3K.Application.Features.PdfIslemleri.Queries.GetEksikUrunlerExcelQuery
            {
                ProjeId = projeId,
                ProjeTipi = (ProjeTipi)projeBilgisi.ProjeTipiId
            }, cancellationToken);

            if (!result.IsSuccess)
                return result.ToActionResult();

            var raporAdi = projeBilgisi.ProjeTipiId == (int)ProjeTipi.Saha ? "SevkSonrasiEksikRaporu" : "EksikRaporu";
            return File(result.Value!,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{projeBilgisi.ProjeNo}_{raporAdi}.xlsx");
        }

        [HttpPost("eksik-urunler/toplu/pdf")]
        public Task<IActionResult> TopluEksikUrunlerPdfIndir(
            [FromBody] TopluEksikUrunlerRaporuRequest? request,
            CancellationToken cancellationToken)
        {
            return TopluEksikUrunlerRaporuIndir(
                request,
                _3K.Application.Features.PdfIslemleri.Queries.EksikUrunlerRaporDosyaTuru.Pdf,
                cancellationToken);
        }

        [HttpPost("eksik-urunler/toplu/excel")]
        public Task<IActionResult> TopluEksikUrunlerExcelIndir(
            [FromBody] TopluEksikUrunlerRaporuRequest? request,
            CancellationToken cancellationToken)
        {
            return TopluEksikUrunlerRaporuIndir(
                request,
                _3K.Application.Features.PdfIslemleri.Queries.EksikUrunlerRaporDosyaTuru.Excel,
                cancellationToken);
        }

        [HttpGet("gerceklesen-ceki-listesi/{projeId}")]
        public async Task<IActionResult> GerceklesenCekiListesiPdfIndir(int projeId)
        {
            var result = await _mediator.Send(new _3K.Application.Features.PdfIslemleri.Queries.GetGerceklesenCekiListesiPdfQuery
            {
                ProjeId = projeId
            });

            if (!result.IsSuccess)
                return result.ToActionResult();

            var projeBilgisi = await GetProjeDosyaBilgisi(projeId);
            var tipEki = projeBilgisi.ProjeTipiId == (int)ProjeTipi.Yedek ? "_Yedek" : string.Empty;
            return File(result.Value!, "application/pdf", $"{projeBilgisi.ProjeNo}{tipEki}_GerceklesenCekiListesi.pdf");
        }

        [HttpGet("gerceklesen-ceki-listesi/{projeId}/excel")]
        public async Task<IActionResult> GerceklesenCekiListesiExcelIndir(int projeId)
        {
            var result = await _mediator.Send(new _3K.Application.Features.PdfIslemleri.Queries.GetGerceklesenCekiListesiExcelQuery
            {
                ProjeId = projeId
            });

            if (!result.IsSuccess)
                return result.ToActionResult();

            var projeBilgisi = await GetProjeDosyaBilgisi(projeId);
            var tipEki = projeBilgisi.ProjeTipiId == (int)ProjeTipi.Yedek ? "_Yedek" : string.Empty;
            return File(result.Value!,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{projeBilgisi.ProjeNo}{tipEki}_GerceklesenCekiListesi.xlsx");
        }

        [HttpGet("saha-gerceklesen-ceki-listesi/{projeId}")]
        public async Task<IActionResult> SahaGerceklesenCekiListesiPdfIndir(int projeId)
        {
            var result = await _mediator.Send(new _3K.Application.Features.PdfIslemleri.Queries.GetSahaGerceklesenCekiListesiPdfQuery
            {
                ProjeId = projeId
            });

            if (!result.IsSuccess)
                return result.ToActionResult();

            var projeNo = await GetProjeNo(projeId);
            return File(result.Value!, "application/pdf", $"{projeNo}_SahaGerceklesenCekiListesi.pdf");
        }

        [HttpGet("saha-gerceklesen-ceki-listesi/{projeId}/excel")]
        public async Task<IActionResult> SahaGerceklesenCekiListesiExcelIndir(int projeId)
        {
            var result = await _mediator.Send(new _3K.Application.Features.PdfIslemleri.Queries.GetSahaGerceklesenCekiListesiExcelQuery
            {
                ProjeId = projeId
            });

            if (!result.IsSuccess)
                return result.ToActionResult();

            var projeNo = await GetProjeNo(projeId);
            return File(result.Value!,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{projeNo}_SahaGerceklesenCekiListesi.xlsx");
        }

        [HttpGet("uck-sandik-durum/{projeId}")]
        public async Task<IActionResult> UcKSandikDurumPdfIndir(int projeId)
        {
            var result = await _mediator.Send(new _3K.Application.Features.PdfIslemleri.Queries.GetUcKSandikDurumPdfQuery
            {
                ProjeId = projeId
            });

            if (!result.IsSuccess)
                return result.ToActionResult();

            var projeNo = await GetProjeNo(projeId);
            return File(result.Value!, "application/pdf", $"{projeNo}_SandikDurumRaporu.pdf");
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

        [HttpGet("depo-sandik")]
        public async Task<IActionResult> DepoSandikPdfIndir([FromQuery] int? projeTipiId = null)
        {
            var result = await _mediator.Send(new _3K.Application.Features.PdfIslemleri.Queries.GetDepoSandikPdfQuery
            {
                ProjeTipiId = projeTipiId
            });

            if (!result.IsSuccess)
                return result.ToActionResult();

            var tarih = DateTime.Now.ToString("yyyyMMdd");
            var kapsam = projeTipiId switch
            {
                1 => "_Normal",
                2 => "_Saha",
                3 => "_Yedek",
                _ => string.Empty
            };
            return File(result.Value!, "application/pdf", $"DepoSandikRaporu{kapsam}_{tarih}.pdf");
        }

        [HttpGet("depo-sandik/proje/{projeId}")]
        public async Task<IActionResult> ProjeDepoSandikPdfIndir(int projeId)
        {
            var result = await _mediator.Send(new _3K.Application.Features.PdfIslemleri.Queries.GetProjeDepoSandikPdfQuery
            {
                ProjeId = projeId
            });

            if (!result.IsSuccess)
                return result.ToActionResult();

            var tarih = DateTime.Now.ToString("yyyyMMdd");
            var projeNo = await GetProjeNo(projeId);
            return File(result.Value!, "application/pdf", $"{projeNo}_DepoSandikRaporu_{tarih}.pdf");
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

        private async Task<(string ProjeNo, int ProjeTipiId)> GetProjeDosyaBilgisi(
            int projeId,
            CancellationToken cancellationToken = default)
        {
            var proje = await _context.Projeler
                .AsNoTracking()
                .Where(p => p.Id == projeId)
                .Select(p => new { p.ProjeNo, p.ProjeTipiId })
                .SingleOrDefaultAsync(cancellationToken);

            return proje == null
                ? (projeId.ToString(), 0)
                : (proje.ProjeNo, proje.ProjeTipiId);
        }

        private async Task<IActionResult> TopluEksikUrunlerRaporuIndir(
            TopluEksikUrunlerRaporuRequest? request,
            _3K.Application.Features.PdfIslemleri.Queries.EksikUrunlerRaporDosyaTuru dosyaTuru,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new _3K.Application.Features.PdfIslemleri.Queries.GetTopluEksikUrunlerRaporuQuery
                {
                    ProjeIds = (IReadOnlyCollection<int>?)request?.ProjeIds ?? Array.Empty<int>(),
                    DosyaTuru = dosyaTuru
                },
                cancellationToken);

            if (!result.IsSuccess)
                return result.ToActionResult();

            var format = dosyaTuru == _3K.Application.Features.PdfIslemleri.Queries.EksikUrunlerRaporDosyaTuru.Pdf
                ? "PDF"
                : "Excel";
            var tarih = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return File(
                result.Value!,
                "application/zip",
                $"TopluEksikRaporlari_{format}_{tarih}.zip");
        }
    }

    public sealed class TopluEksikUrunlerRaporuRequest
    {
        public List<int> ProjeIds { get; set; } = new();
    }
}
