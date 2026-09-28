using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Application.Features.FinansIslemleri.DTOs;
using _3K.Application.Features.FinansIslemleri.Queries;
using _3K.Core.Models;
using _3K_API.Extensions;

namespace _3K_API.Controllers;

/// <summary>Düzenli işler, gider kategorisi ve kalemleri, ürün/tarifeler ve iş şablonları.</summary>
public sealed class FinansTanimController(IMediator mediator) : FinansControllerBase(mediator)
{
    [HttpGet("duzenli-isler")]
    public async Task<ActionResult> DuzenliIsler(
        [FromQuery] bool sadeceAktif,
        [FromQuery] string? arama,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
        => (await Mediator.Send(new FinansDuzenliIslerQuery
        {
            SadeceAktif = sadeceAktif,
            Arama = arama,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken)).ToActionResult();

    [HttpPost("duzenli-isler")]
    public async Task<ActionResult> DuzenliIsOlustur([FromBody] FinansDuzenliIsKaydetDto model, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansDuzenliIsOlusturCommand { Model = model.DuzenliIsModeli() }, cancellationToken)).ToActionResult();

    [HttpPut("duzenli-isler/{id:int}")]
    public async Task<ActionResult> DuzenliIsGuncelle(int id, [FromBody] FinansDuzenliIsKaydetDto model, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansDuzenliIsGuncelleCommand { Id = id, Model = model.DuzenliIsModeli() }, cancellationToken)).ToActionResult();

    [HttpGet("gider-kategorileri")]
    public async Task<ActionResult> GiderKategorileri([FromQuery] bool sadeceAktif, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansGiderKategorileriQuery { SadeceAktif = sadeceAktif }, cancellationToken)).ToActionResult();

    [HttpPost("gider-kategorileri")]
    public async Task<ActionResult> GiderKategoriOlustur([FromBody] FinansGiderKategoriKaydetModel model, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansGiderKategoriOlusturCommand { Model = model }, cancellationToken)).ToActionResult();

    [HttpPut("gider-kategorileri/{id:int}")]
    public async Task<ActionResult> GiderKategoriGuncelle(int id, [FromBody] FinansGiderKategoriKaydetModel model, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansGiderKategoriGuncelleCommand { Id = id, Model = model }, cancellationToken)).ToActionResult();

    [HttpGet("gider-kalemleri")]
    public async Task<ActionResult> GiderKalemleri([FromQuery] int? kategoriId, [FromQuery] bool sadeceAktif, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansGiderKalemleriQuery { KategoriId = kategoriId, SadeceAktif = sadeceAktif }, cancellationToken)).ToActionResult();

    [HttpPost("gider-kalemleri")]
    public async Task<ActionResult> GiderKalemiOlustur([FromBody] FinansGiderKalemiKaydetModel model, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansGiderKalemiOlusturCommand { Model = model }, cancellationToken)).ToActionResult();

    [HttpPut("gider-kalemleri/{id:int}")]
    public async Task<ActionResult> GiderKalemiGuncelle(int id, [FromBody] FinansGiderKalemiKaydetModel model, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansGiderKalemiGuncelleCommand { Id = id, Model = model }, cancellationToken)).ToActionResult();

    [HttpGet("gider-kutuphanesi/kategoriler")]
    public async Task<ActionResult> GiderKutuphaneKategorileri([FromQuery] bool sadeceAktif, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansGiderKutuphaneKategorileriQuery { SadeceAktif = sadeceAktif }, cancellationToken)).ToActionResult();

    [HttpGet("gider-kutuphanesi/kalemler")]
    public async Task<ActionResult> GiderKutuphaneKalemleri([FromQuery] int? kategoriId, [FromQuery] bool sadeceAktif, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansGiderKutuphaneKalemleriQuery { KategoriId = kategoriId, SadeceAktif = sadeceAktif }, cancellationToken)).ToActionResult();

    [HttpGet("urunler")]
    public async Task<ActionResult> Urunler(
        [FromQuery] bool sadeceAktif,
        [FromQuery] DateTime? tarifeTarihi,
        [FromQuery] string? arama,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
        => (await Mediator.Send(new FinansUrunlerQuery
        {
            SadeceAktif = sadeceAktif,
            TarifeTarihi = tarifeTarihi,
            Arama = arama,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken)).ToActionResult();

    [HttpPost("urunler")]
    public async Task<ActionResult> UrunOlustur([FromBody] FinansUrunKaydetModel model, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansUrunOlusturCommand { Model = model }, cancellationToken)).ToActionResult();

    [HttpPut("urunler/{id:int}")]
    public async Task<ActionResult> UrunGuncelle(int id, [FromBody] FinansUrunKaydetModel model, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansUrunGuncelleCommand { Id = id, Model = model }, cancellationToken)).ToActionResult();

    [HttpDelete("urunler/{id:int}")]
    public async Task<ActionResult> UrunPasiflestir(int id, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansUrunPasiflestirCommand { Id = id }, cancellationToken)).ToActionResult();

    [HttpGet("sablonlar")]
    public async Task<ActionResult> Sablonlar(CancellationToken ct)
        => (await Mediator.Send(new FinansSablonlarQuery(), ct)).ToActionResult();

    [HttpPost("sablonlar")]
    public async Task<ActionResult> SablonOlustur([FromBody] FinansSablonKaydetModel model, CancellationToken ct)
        => (await Mediator.Send(new FinansSablonKaydetCommand { Model = model }, ct)).ToActionResult();

    [HttpPut("sablonlar/{id:int}")]
    public async Task<ActionResult> SablonGuncelle(int id, [FromBody] FinansSablonKaydetModel model, CancellationToken ct)
        => (await Mediator.Send(new FinansSablonKaydetCommand { Id = id, Model = model }, ct)).ToActionResult();
}
