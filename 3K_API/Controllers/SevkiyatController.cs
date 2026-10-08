using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.SevkiyatIslemleri.Commands;
using _3K.Application.Features.SevkiyatIslemleri.Queries;
using _3K_API.Extensions;

namespace _3K_API.Controllers;

/// <summary>Çok projeli toplu sevk (tır seferi), sefer listesi ve sevkiyat istatistikleri.</summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class SevkiyatController(IMediator mediator) : ControllerBase
{
    /// <summary>Seçilen projelerin seçilen sandıklarını tek seferde sevk eder; hepsi ya da hiçbiri.</summary>
    [HttpPost("toplu-sevk")]
    public async Task<ActionResult> TopluSevk([FromBody] TopluSevkEtCommand command, CancellationToken cancellationToken)
        => (await mediator.Send(command, cancellationToken)).ToActionResult();

    [HttpGet("seferler")]
    public async Task<ActionResult> Seferler(
        [FromQuery] DateTime? baslangic,
        [FromQuery] DateTime? bitis,
        [FromQuery] string? plaka,
        [FromQuery] string? projeNo,
        [FromQuery] int? projeTipiId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
        => (await mediator.Send(new SevkSeferleriQuery
        {
            Baslangic = baslangic,
            Bitis = bitis,
            Plaka = plaka,
            ProjeNo = projeNo,
            ProjeTipiId = projeTipiId,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken)).ToActionResult();

    [HttpGet("seferler/{id:int}/detay")]
    public async Task<ActionResult> SeferDetay(int id, CancellationToken cancellationToken)
        => (await mediator.Send(new SevkSeferiDetayQuery { SeferId = id }, cancellationToken)).ToActionResult();

    /// <summary>Sefere bağlı olmayan eski sevkiyatların sanal seferi: plaka + sevk günü.</summary>
    [HttpGet("seferler/eski-detay")]
    public async Task<ActionResult> EskiSeferDetay([FromQuery] string? plaka, [FromQuery] DateTime tarih, CancellationToken cancellationToken)
        => (await mediator.Send(new SevkSeferiDetayQuery { EskiPlaka = plaka, EskiTarih = tarih }, cancellationToken)).ToActionResult();

    [HttpGet("istatistik")]
    public async Task<ActionResult> Istatistik([FromQuery] DateTime? baslangic, [FromQuery] DateTime? bitis, CancellationToken cancellationToken)
        => (await mediator.Send(new SevkiyatIstatistikQuery { Baslangic = baslangic, Bitis = bitis }, cancellationToken)).ToActionResult();
}
