using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Application.Features.FinansIslemleri.DTOs;
using _3K.Application.Features.FinansIslemleri.Queries;
using _3K.Core.Models;
using _3K_API.Extensions;

namespace _3K_API.Controllers;

/// <summary>Sipariş (PO) ve faturalar.</summary>
public sealed class FinansSiparisFaturaController(IMediator mediator) : FinansControllerBase(mediator)
{
    [HttpGet("siparisler")]
    public async Task<ActionResult> Siparisler([FromQuery] FinansFilterRequest request, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansSiparislerQuery { Filtre = request.ToModel() }, cancellationToken)).ToActionResult();

    [HttpGet("siparis-gruplari")]
    public async Task<ActionResult> SiparisGruplari([FromQuery] FinansSiparisGrupRequest request, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansSiparisGruplariQuery { Filtre = request.ToModel() }, cancellationToken)).ToActionResult();

    /// <summary>Seçilen işleri PO öncesi onaya gönderir; kural açıksa 202 (Onay Merkezi'ne düştü).</summary>
    [HttpPost("po-onay-talepleri")]
    public async Task<ActionResult> PoOnayinaGonder([FromBody] FinansPoOnayGonderModel model, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansPoOnayGonderCommand { Model = model }, cancellationToken)).ToActionResult();

    [HttpGet("siparisler/{id:int}")]
    public async Task<ActionResult> Siparis(int id, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansSiparisGetirQuery { Id = id }, cancellationToken)).ToActionResult();

    [HttpPost("siparisler")]
    public async Task<ActionResult> SiparisOlustur([FromBody] FinansSiparisOlusturModel model, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansSiparisOlusturCommand { Model = model }, cancellationToken)).ToActionResult();

    [HttpPost("siparisler/{id:int}/kalemler")]
    public async Task<ActionResult> SiparisKalemEkle(int id, [FromBody] FinansSiparisKalemEkleModel model, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansSiparisKalemEkleCommand { Id = id, Model = model }, cancellationToken)).ToActionResult();

    [HttpPut("siparisler/{id:int}")]
    public async Task<ActionResult> SiparisGuncelle(int id, [FromBody] FinansSiparisGuncelleModel model, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansSiparisGuncelleCommand { Id = id, Model = model }, cancellationToken)).ToActionResult();

    [HttpPost("siparisler/{id:int}/iptal")]
    public async Task<ActionResult> SiparisIptal(int id, [FromBody] FinansIptalDto model, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansSiparisIptalCommand { Id = id, Aciklama = model.Aciklama }, cancellationToken)).ToActionResult();

    [HttpGet("faturalar")]
    public async Task<ActionResult> Faturalar([FromQuery] FinansFilterRequest request, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansFaturalarQuery { Filtre = request.ToModel() }, cancellationToken)).ToActionResult();

    [HttpGet("faturalar/{id:int}")]
    public async Task<ActionResult> Fatura(int id, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansFaturaGetirQuery { Id = id }, cancellationToken)).ToActionResult();

    [HttpPost("faturalar")]
    public async Task<ActionResult> FaturaOlustur([FromBody] FinansFaturaOlusturModel model, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansFaturaOlusturCommand { Model = model }, cancellationToken)).ToActionResult();

    [HttpPut("faturalar/{id:int}")]
    public async Task<ActionResult> FaturaGuncelle(int id, [FromBody] FinansFaturaGuncelleModel model, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansFaturaGuncelleCommand { Id = id, Model = model }, cancellationToken)).ToActionResult();

    [HttpPost("faturalar/{id:int}/iptal")]
    public async Task<ActionResult> FaturaIptal(int id, [FromBody] FinansIptalDto model, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansFaturaIptalCommand { Id = id, Aciklama = model.Aciklama }, cancellationToken)).ToActionResult();

    [HttpPut("faturalar/{id:int}/odeme")]
    public async Task<ActionResult> FaturaOdeme(int id, [FromBody] FinansOdemeKaydetModel model, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansFaturaOdemeCommand { Id = id, Model = model }, cancellationToken)).ToActionResult();

    [HttpGet("vade-uyarilari")]
    public async Task<ActionResult> VadeUyarilari([FromQuery] int enFazlaKalem = 20, CancellationToken cancellationToken = default)
        => (await Mediator.Send(new FinansVadeUyarilariQuery { EnFazlaKalem = enFazlaKalem }, cancellationToken)).ToActionResult();
}
