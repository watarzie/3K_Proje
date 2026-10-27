using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.AmbalajIslemleri.Queries;
using _3K.Application.Features.PdfIslemleri.Queries;
using _3K.Core.Enums;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    /// <summary>Rapor indirme uçları; dosya içeriği ve adı ilgili kullanım senaryosunda üretilir.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PdfController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PdfController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("saha-proje/{projeId}")]
        public async Task<IActionResult> SahaProjePdfIndir(int projeId, CancellationToken cancellationToken) =>
            (await _mediator.Send(new GetSahaProjePdfQuery { ProjeId = projeId }, cancellationToken)).ToFileResult();

        /// <summary>
        /// Saha/Yedek sevke hazır sandık raporu (yalnız kapalı ve ölçü/ağırlığı girilmiş sandıklar); format "pdf" veya
        /// "excel". İzin mevcut Saha/Yedek sandık raporuyla aynıdır.
        /// </summary>
        [HttpGet("saha-proje/{projeId}/sevke-hazir/{format}")]
        public async Task<IActionResult> SevkeHazirSandikRaporuIndir(int projeId, string format, CancellationToken cancellationToken) =>
            (await _mediator.Send(new GetSevkeHazirSandikRaporuQuery { ProjeId = projeId, Format = format }, cancellationToken)).ToFileResult();

        [HttpGet("eksik-urunler/{projeId}")]
        public async Task<IActionResult> EksikUrunlerPdfIndir(int projeId, CancellationToken cancellationToken) =>
            (await _mediator.Send(new GetEksikUrunlerPdfQuery { ProjeId = projeId }, cancellationToken)).ToFileResult();

        [HttpGet("eksik-urunler/{projeId}/excel")]
        public async Task<IActionResult> EksikUrunlerExcelIndir(int projeId, CancellationToken cancellationToken) =>
            (await _mediator.Send(new GetEksikUrunlerExcelQuery { ProjeId = projeId }, cancellationToken)).ToFileResult();

        [HttpPost("eksik-urunler/toplu/pdf")]
        public Task<IActionResult> TopluEksikUrunlerPdfIndir(
            [FromBody] TopluEksikUrunlerRaporuRequest? request, CancellationToken cancellationToken) =>
            TopluEksikUrunlerRaporuIndir(request, EksikUrunlerRaporDosyaTuru.Pdf, cancellationToken);

        [HttpPost("eksik-urunler/toplu/excel")]
        public Task<IActionResult> TopluEksikUrunlerExcelIndir(
            [FromBody] TopluEksikUrunlerRaporuRequest? request, CancellationToken cancellationToken) =>
            TopluEksikUrunlerRaporuIndir(request, EksikUrunlerRaporDosyaTuru.Excel, cancellationToken);

        /// <summary>G4: seçili projelerin Grid eksiği olan ürünleri tek sayfalık talep formu Excel'inde.</summary>
        [HttpPost("talep-formu/toplu/excel")]
        public async Task<IActionResult> TopluTalepFormuExcelIndir(
            [FromBody] TopluEksikUrunlerRaporuRequest? request, CancellationToken cancellationToken) =>
            (await _mediator.Send(new GetTopluTalepFormuQuery
            {
                ProjeIds = (IReadOnlyList<int>?)request?.ProjeIds ?? Array.Empty<int>(),
                ProjeTipi = request?.ProjeTipi ?? ProjeTipi.Normal
            }, cancellationToken)).ToFileResult();

        [HttpGet("gerceklesen-ceki-listesi/{projeId}")]
        public async Task<IActionResult> GerceklesenCekiListesiPdfIndir(int projeId, CancellationToken cancellationToken) =>
            (await _mediator.Send(new GetGerceklesenCekiListesiPdfQuery { ProjeId = projeId }, cancellationToken)).ToFileResult();

        [HttpGet("gerceklesen-ceki-listesi/{projeId}/excel")]
        public async Task<IActionResult> GerceklesenCekiListesiExcelIndir(int projeId, CancellationToken cancellationToken) =>
            (await _mediator.Send(new GetGerceklesenCekiListesiExcelQuery { ProjeId = projeId }, cancellationToken)).ToFileResult();

        /// <summary>
        /// Müşteri gerçekleşen çeki listesi (TR/EN, yalnız sevk edilen sandık ve ürünler); format "pdf" veya "excel".
        /// İzin projenin gerçek tipinin gerçekleşen çeki raporu okumasıdır.
        /// </summary>
        [HttpGet("gerceklesen-ceki-listesi/{projeId}/musteri/{format}")]
        public async Task<IActionResult> MusteriCekiListesiIndir(int projeId, string format, CancellationToken cancellationToken) =>
            (await _mediator.Send(new GetMusteriCekiListesiQuery { ProjeId = projeId, Format = format }, cancellationToken)).ToFileResult();

        [HttpGet("saha-gerceklesen-ceki-listesi/{projeId}")]
        public async Task<IActionResult> SahaGerceklesenCekiListesiPdfIndir(int projeId, CancellationToken cancellationToken) =>
            (await _mediator.Send(new GetSahaGerceklesenCekiListesiPdfQuery { ProjeId = projeId }, cancellationToken)).ToFileResult();

        [HttpGet("saha-gerceklesen-ceki-listesi/{projeId}/excel")]
        public async Task<IActionResult> SahaGerceklesenCekiListesiExcelIndir(int projeId, CancellationToken cancellationToken) =>
            (await _mediator.Send(new GetSahaGerceklesenCekiListesiExcelQuery { ProjeId = projeId }, cancellationToken)).ToFileResult();

        [HttpGet("uck-sandik-durum/{projeId}")]
        public async Task<IActionResult> UcKSandikDurumPdfIndir(int projeId, CancellationToken cancellationToken) =>
            (await _mediator.Send(new GetUcKSandikDurumPdfQuery { ProjeId = projeId }, cancellationToken)).ToFileResult();

        [HttpGet("stok")]
        public async Task<IActionResult> StokPdfIndir([FromQuery] int? durumId, CancellationToken cancellationToken) =>
            (await _mediator.Send(new GetStokPdfQuery { DurumId = durumId }, cancellationToken)).ToFileResult();

        [HttpGet("depo-sandik")]
        public async Task<IActionResult> DepoSandikPdfIndir([FromQuery] int? projeTipiId, CancellationToken cancellationToken) =>
            (await _mediator.Send(new GetDepoSandikPdfQuery { ProjeTipiId = projeTipiId }, cancellationToken)).ToFileResult();

        [HttpGet("depo-sandik/proje/{projeId}")]
        public async Task<IActionResult> ProjeDepoSandikPdfIndir(int projeId, CancellationToken cancellationToken) =>
            (await _mediator.Send(new GetProjeDepoSandikPdfQuery { ProjeId = projeId }, cancellationToken)).ToFileResult();

        [HttpGet("ambalaj-uretim/{projeId:int}")]
        public async Task<IActionResult> AmbalajUretimPdfIndir(
            int projeId,
            [FromQuery] int? tur,
            CancellationToken cancellationToken)
        {
            var ambalajTuru = tur switch
            {
                null => (AmbalajSandikTuru?)null,
                1 => AmbalajSandikTuru.Normal,
                2 => AmbalajSandikTuru.Ilave,
                3 => AmbalajSandikTuru.Ic,
                _ => null
            };
            if (tur.HasValue && ambalajTuru == null)
                return BadRequest(new { message = "Üretim grubu geçersiz." });

            return (await _mediator.Send(new GetAmbalajUretimFormuDosyasiQuery
            {
                ProjeId = projeId,
                Tur = ambalajTuru,
                BagimsizKayitMi = false
            }, cancellationToken)).ToFileResult();
        }

        [HttpGet("ozel-sandik/{tur:int}")]
        public async Task<IActionResult> OzelSandikPdfIndir(int tur, CancellationToken cancellationToken)
        {
            var ambalajTuru = OzelAmbalajTurunuCoz(tur);
            if (ambalajTuru == null) return BadRequest(new { message = "Özel sandık türü geçersiz." });

            return (await _mediator.Send(new GetAmbalajRaporDosyasiQuery { Tur = ambalajTuru.Value }, cancellationToken))
                .ToFileResult();
        }

        [HttpGet("ozel-sandik/{tur:int}/proje/{projeId:int}")]
        public async Task<IActionResult> OzelSandikUretimFormuIndir(int tur, int projeId, CancellationToken cancellationToken)
        {
            var ambalajTuru = OzelAmbalajTurunuCoz(tur);
            if (ambalajTuru == null) return BadRequest(new { message = "Özel sandık türü geçersiz." });

            return (await _mediator.Send(new GetAmbalajUretimFormuDosyasiQuery
            {
                ProjeId = projeId,
                Tur = ambalajTuru,
                BagimsizKayitMi = true
            }, cancellationToken)).ToFileResult();
        }

        private static AmbalajSandikTuru? OzelAmbalajTurunuCoz(int tur) => tur switch
        {
            2 => AmbalajSandikTuru.Ilave,
            3 => AmbalajSandikTuru.Ic,
            4 => AmbalajSandikTuru.Saha,
            5 => AmbalajSandikTuru.Yedek,
            _ => null
        };

        private async Task<IActionResult> TopluEksikUrunlerRaporuIndir(
            TopluEksikUrunlerRaporuRequest? request,
            EksikUrunlerRaporDosyaTuru dosyaTuru,
            CancellationToken cancellationToken) =>
            (await _mediator.Send(new GetTopluEksikUrunlerRaporuQuery
            {
                ProjeIds = (IReadOnlyCollection<int>?)request?.ProjeIds ?? Array.Empty<int>(),
                ProjeTipi = request?.ProjeTipi ?? ProjeTipi.Normal,
                DosyaTuru = dosyaTuru
            }, cancellationToken)).ToFileResult();
    }

    public sealed class TopluEksikUrunlerRaporuRequest
    {
        public List<int> ProjeIds { get; set; } = new();
        public ProjeTipi ProjeTipi { get; set; } = ProjeTipi.Normal;
    }
}
