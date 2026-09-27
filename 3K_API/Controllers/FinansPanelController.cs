using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.FinansIslemleri.Queries;
using _3K.Core.Helpers;
using _3K_API.Extensions;

namespace _3K_API.Controllers;

/// <summary>Panel, hareketler, genel arama, yaşlandırma ve denetim geçmişi.</summary>
public sealed class FinansPanelController(IMediator mediator) : FinansControllerBase(mediator)
{
    [HttpGet("panel")]
    public async Task<ActionResult> Panel([FromQuery] DateTime baslangic, [FromQuery] DateTime bitis, CancellationToken ct)
        => (await Mediator.Send(new FinansPanelQuery { Baslangic = baslangic, Bitis = bitis }, ct)).ToActionResult();

    [HttpGet("hareketler")]
    public async Task<ActionResult> Hareketler([FromQuery] FinansFilterRequest request, [FromQuery] string? tur, CancellationToken ct)
        => (await Mediator.Send(new FinansHareketlerQuery { Filtre = request.ToModel(), Tur = tur }, ct)).ToActionResult();

    [HttpGet("genel-arama")]
    public async Task<ActionResult> GenelArama([FromQuery] FinansFilterRequest request, CancellationToken ct)
        => (await Mediator.Send(new FinansGenelAramaQuery { Filtre = request.ToModel() }, ct)).ToActionResult();

    [HttpGet("yaslandirma")]
    public async Task<ActionResult> Yaslandirma([FromQuery] FinansFilterRequest request, [FromQuery] DateTime? referansTarihi, [FromQuery] int minimumGun, CancellationToken ct)
        => (await Mediator.Send(new FinansYaslandirmaQuery { Filtre = request.ToModel(), ReferansTarihi = referansTarihi ?? TurkeyTime.Now.Date, MinimumGun = minimumGun }, ct)).ToActionResult();

    [HttpGet("denetim")]
    public async Task<ActionResult> Denetim(
        [FromQuery] string? varlikTuru,
        [FromQuery] int? varlikId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
        => (await Mediator.Send(new FinansDegisiklikGecmisiQuery
        {
            VarlikTuru = varlikTuru,
            VarlikId = varlikId,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken)).ToActionResult();
}
