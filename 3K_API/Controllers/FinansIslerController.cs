using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Application.Features.FinansIslemleri.DTOs;
using _3K.Application.Features.FinansIslemleri.Queries;
using _3K.Core.Models;
using _3K_API.Extensions;

namespace _3K_API.Controllers;

/// <summary>İş kayıtları, aylık iş listesi, özel işler ve fiyatlandırma.</summary>
public sealed class FinansIslerController(IMediator mediator) : FinansControllerBase(mediator)
{
    [HttpGet("is-kayitlari/{id:int}")]
    public async Task<ActionResult> IsKaydi(int id, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansIsKaydiGetirQuery { Id = id }, cancellationToken)).ToActionResult();

    [HttpPost("is-kayitlari/secim")]
    public async Task<ActionResult> IsKayitlariSecim([FromBody] FinansIsKayitlariSecimRequest request, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansIsKayitlariSecimQuery { Ids = request.Ids }, cancellationToken)).ToActionResult();

    [HttpGet("proje-secenekleri")]
    public async Task<ActionResult> ProjeSecenekleri([FromQuery] FinansProjeSecenekRequest request, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansProjeSecenekleriQuery
        {
            Arama = request.Arama,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        }, cancellationToken)).ToActionResult();

    [HttpGet("aylik-isler")]
    public async Task<ActionResult> AylikIsler(
        [FromQuery] int yil,
        [FromQuery] int ay,
        [FromQuery] FinansFilterRequest request,
        CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansAylikIslerQuery { Yil = yil, Ay = ay, Filtre = request.ToModel() }, cancellationToken)).ToActionResult();

    [HttpPost("is-kayitlari/{id:int}/finans-tarihi")]
    public async Task<ActionResult> FinansTarihi(int id, [FromBody] FinansTarihiDegistirModel model, CancellationToken ct)
        => (await Mediator.Send(new FinansTarihiDegistirCommand { Id = id, Model = model }, ct)).ToActionResult();

    [HttpPost("is-kayitlari/{id:int}/fiyatlandirma")]
    public async Task<ActionResult> Fiyatlandirma(int id, [FromBody] FinansFiyatlandirmaModel model, CancellationToken ct)
        => (await Mediator.Send(new FinansFiyatlandirCommand { Id = id, Model = model }, ct)).ToActionResult();

    [HttpPost("is-kayitlari/{id:int}/hacim")]
    public async Task<ActionResult> Hacim(int id, [FromBody] FinansHacimGirDto model, CancellationToken ct)
        => (await Mediator.Send(new FinansHacimGirCommand { Id = id, Model = model.HacimModeli() }, ct)).ToActionResult();

    [HttpPost("is-kayitlari/{id:int}/iptal")]
    public async Task<ActionResult> IsKaydiIptal(int id, [FromBody] FinansIptalDto model, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansIsKaydiIptalCommand { Id = id, Aciklama = model.Aciklama }, cancellationToken)).ToActionResult();

    [HttpPost("ozel-isler")]
    public async Task<ActionResult> OzelIsOlustur([FromBody] FinansOzelIsKaydetDto model, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansIsKaydiOlusturCommand { Model = model.IsKaydiModeli() }, cancellationToken)).ToActionResult();

    [HttpPut("ozel-isler/{id:int}/aylik-deger")]
    public async Task<ActionResult> OzelIsAylikDegerGuncelle(int id, [FromBody] FinansAylikDegerModel model, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansOzelIsAylikDegerGuncelleCommand { Id = id, Model = model }, cancellationToken)).ToActionResult();

    [HttpPost("ozel-isler/{id:int}/iptal")]
    public async Task<ActionResult> OzelIsIptal(int id, [FromBody] FinansIptalDto model, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansIsKaydiIptalCommand { Id = id, Aciklama = model.Aciklama }, cancellationToken)).ToActionResult();

    [HttpPost("ozel-isler/{id:int}/geri-al")]
    public async Task<ActionResult> OzelIsGeriAl(int id, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansIsKaydiGeriAlCommand { Id = id }, cancellationToken)).ToActionResult();
}
