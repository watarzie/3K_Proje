using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Application.Features.FinansIslemleri.Queries;
using _3K.Core.Models;
using _3K_API.Extensions;

namespace _3K_API.Controllers;

/// <summary>PDF belgeleri ve kalıcı silme.</summary>
public sealed class FinansBelgeController(IMediator mediator) : FinansControllerBase(mediator)
{
    [HttpGet("belgeler")]
    public async Task<ActionResult> Belgeler([FromQuery] string hedefTuru, [FromQuery] int hedefId, CancellationToken ct)
        => (await Mediator.Send(new FinansBelgelerQuery { HedefTuru = hedefTuru, HedefId = hedefId }, ct)).ToActionResult();

    [HttpPost("belgeler")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<ActionResult> BelgeYukle([FromForm] string hedefTuru, [FromForm] int hedefId, [FromForm] IFormFile dosya, CancellationToken ct)
    {
        if (dosya.Length > 10 * 1024 * 1024) return BadRequest(new { message = "PDF en fazla 10 MB olabilir." });
        using var buffer = new MemoryStream();
        await dosya.CopyToAsync(buffer, ct);
        return (await Mediator.Send(new FinansBelgeYukleCommand { Model = new(hedefTuru, hedefId, dosya.FileName, buffer.ToArray()) }, ct)).ToActionResult();
    }

    [EnableRateLimiting(ApiServiceCollectionExtensions.DisaAktarimRateLimitPolicy)]
    [HttpGet("belgeler/{id:int}/indir")]
    public async Task<ActionResult> BelgeIndir(int id, CancellationToken ct)
    {
        var result = await Mediator.Send(new FinansBelgeIndirQuery { Id = id }, ct);
        return result.ToFileResult();
    }

    [HttpGet("kalici-silme/onizleme")]
    public async Task<ActionResult> SilmeOnizleme([FromQuery] string varlikTuru, [FromQuery] int id, CancellationToken ct)
        => (await Mediator.Send(new FinansKaliciSilOnizlemeQuery { VarlikTuru = varlikTuru, Id = id }, ct)).ToActionResult();

    [HttpPost("kalici-silme")]
    public async Task<ActionResult> KaliciSil([FromBody] FinansKaliciSilModel model, CancellationToken ct)
        => (await Mediator.Send(new FinansKaliciSilCommand { Model = model }, ct)).ToActionResult();
}
