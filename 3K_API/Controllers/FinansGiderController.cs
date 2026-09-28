using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Application.Features.FinansIslemleri.DTOs;
using _3K.Application.Features.FinansIslemleri.Queries;
using _3K_API.Extensions;

namespace _3K_API.Controllers;

/// <summary>Giderler.</summary>
public sealed class FinansGiderController(IMediator mediator) : FinansControllerBase(mediator)
{
    [HttpGet("giderler")]
    public async Task<ActionResult> Giderler([FromQuery] FinansFilterRequest request, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansGiderlerQuery { Filtre = request.ToModel() }, cancellationToken)).ToActionResult();

    [HttpPost("giderler")]
    public async Task<ActionResult> GiderOlustur([FromBody] FinansGiderKaydetDto model, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansGiderOlusturCommand { Model = model.GiderModeli() }, cancellationToken)).ToActionResult();

    [HttpPut("giderler/{id:int}")]
    public async Task<ActionResult> GiderGuncelle(int id, [FromBody] FinansGiderKaydetDto model, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansGiderGuncelleCommand { Id = id, Model = model.GiderModeli() }, cancellationToken)).ToActionResult();

    [HttpPost("giderler/{id:int}/iptal")]
    public async Task<ActionResult> GiderIptal(int id, [FromBody] FinansIptalDto model, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansGiderIptalCommand { Id = id, Aciklama = model.Aciklama }, cancellationToken)).ToActionResult();
}
