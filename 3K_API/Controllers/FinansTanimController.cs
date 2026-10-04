using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Application.Features.FinansIslemleri.DTOs;
using _3K.Application.Features.FinansIslemleri.Queries;
using _3K.Core.Models;
using _3K_API.Extensions;

namespace _3K_API.Controllers;

/// <summary>Düzenli işler, gider kategorisi ve kalemleri, ürün/tarifeler, para birimleri ve finans parametreleri.</summary>
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

    [HttpPost("duzenli-isler/{id:int}/donemler")]
    public async Task<ActionResult> DuzenliIsDonemUret(int id, [FromBody] FinansDuzenliIsDonemUretRequest request, CancellationToken cancellationToken)
        => (await Mediator.Send(new FinansDuzenliIsDonemUretCommand { Id = id, BitisTarihi = request.BitisTarihi }, cancellationToken)).ToActionResult();

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

    [HttpPost("urunler/{id:int}/yeniden-fiyatla")]
    public async Task<ActionResult> UrunYenidenFiyatla(int id, [FromBody] FinansIptalDto model, CancellationToken ct)
        => (await Mediator.Send(new FinansUrunYenidenFiyatlaCommand { Id = id, Aciklama = model.Aciklama }, ct)).ToActionResult();

    [HttpGet("urunler/{urunId:int}/tarifeler/{tarifeId:int}/duzeltme-onizleme")]
    public async Task<ActionResult> TarifeDuzeltmeOnizleme(
        int urunId,
        int tarifeId,
        [FromQuery] decimal birimFiyat,
        [FromQuery] decimal kdvOrani,
        CancellationToken ct)
        => (await Mediator.Send(new FinansTarifeDuzeltmeOnizlemeQuery
        {
            UrunId = urunId,
            TarifeId = tarifeId,
            BirimFiyat = birimFiyat,
            KdvOrani = kdvOrani
        }, ct)).ToActionResult();

    [HttpPost("urunler/{urunId:int}/tarifeler/{tarifeId:int}/duzelt")]
    public async Task<ActionResult> TarifeDuzelt(int urunId, int tarifeId, [FromBody] FinansTarifeDuzeltModel model, CancellationToken ct)
        => (await Mediator.Send(new FinansTarifeDuzeltCommand { UrunId = urunId, TarifeId = tarifeId, Model = model }, ct)).ToActionResult();

    [HttpGet("para-birimleri")]
    public async Task<ActionResult> ParaBirimleri([FromQuery] bool sadeceAktif, CancellationToken ct)
        => (await Mediator.Send(new FinansParaBirimleriQuery { SadeceAktif = sadeceAktif }, ct)).ToActionResult();

    [HttpPost("para-birimleri")]
    public async Task<ActionResult> ParaBirimiOlustur([FromBody] FinansParaBirimiKaydetModel model, CancellationToken ct)
        => (await Mediator.Send(new FinansParaBirimiOlusturCommand { Model = model }, ct)).ToActionResult();

    [HttpPut("para-birimleri/{id:int}")]
    public async Task<ActionResult> ParaBirimiGuncelle(int id, [FromBody] FinansParaBirimiKaydetModel model, CancellationToken ct)
        => (await Mediator.Send(new FinansParaBirimiGuncelleCommand { Id = id, Model = model }, ct)).ToActionResult();

    [HttpGet("parametreler")]
    public async Task<ActionResult> Parametreler(CancellationToken ct)
        => (await Mediator.Send(new FinansParametrelerQuery(), ct)).ToActionResult();

    [HttpPut("parametreler")]
    public async Task<ActionResult> ParametreleriKaydet([FromBody] FinansParametrelerModel model, CancellationToken ct)
        => (await Mediator.Send(new FinansParametreleriKaydetCommand { Model = model }, ct)).ToActionResult();
}
