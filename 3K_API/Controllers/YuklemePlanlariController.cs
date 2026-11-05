using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.YuklemePlanlamaIslemleri.Commands;
using _3K.Application.Features.YuklemePlanlamaIslemleri.Queries;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    /// <summary>Yükleme planları (yukleme-planlari: R görüntüleme, W oluşturma ve kaydetme).</summary>
    [Authorize]
    [ApiController]
    [Route("api/yukleme-planlari")]
    public sealed class YuklemePlanlariController(IMediator mediator) : ControllerBase
    {
        /// <summary>Taşıma aileleri, yük türleri ve lokasyonlar.</summary>
        [HttpGet("tanimlar")]
        public async Task<ActionResult> Tanimlar(CancellationToken ct) =>
            (await mediator.Send(new YuklemePlaniTanimlariQuery(), ct)).ToActionResult();

        /// <summary>Plana eklenebilecek projeler, sayfalı.</summary>
        [HttpGet("proje-adaylari")]
        public async Task<ActionResult> ProjeAdaylari([FromQuery] PlanProjeAdaylariQuery query, CancellationToken ct) =>
            (await mediator.Send(query, ct)).ToActionResult();

        /// <summary>Planlar, en son kaydedilen önce, sayfalı.</summary>
        [HttpGet]
        public async Task<ActionResult> Listele([FromQuery] YuklemePlanlariQuery query, CancellationToken ct) =>
            (await mediator.Send(query, ct)).ToActionResult();

        /// <summary>Planın son revizyonu, yük listesi ve kaynak farkları.</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult> Getir(int id, CancellationToken ct) =>
            (await mediator.Send(new YuklemePlaniGetirQuery { PlanId = id }, ct)).ToActionResult();

        /// <summary>Yeni plan ve ilk revizyonu.</summary>
        [HttpPost]
        public async Task<ActionResult> Olustur([FromBody] YuklemePlaniOlusturCommand command, CancellationToken ct) =>
            (await mediator.Send(command, ct)).ToActionResult();

        /// <summary>Yeni revizyon olarak kaydeder; arada başka biri kaydettiyse 409.</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult> Kaydet(int id, [FromBody] YuklemePlaniKaydetCommand command, CancellationToken ct)
        {
            command.PlanId = id;
            return (await mediator.Send(command, ct)).ToActionResult();
        }

        /// <summary>Otomatik yerleşim: sonuç yeni revizyon olarak yazılır; arada başka biri kaydettiyse 409.</summary>
        [HttpPost("{id:int}/hesapla")]
        public async Task<ActionResult> Hesapla(int id, [FromBody] YuklemePlaniHesaplaCommand command, CancellationToken ct)
        {
            command.PlanId = id;
            return (await mediator.Send(command, ct)).ToActionResult();
        }

        /// <summary>Kaynak farklarını uygulayan yeni revizyon.</summary>
        [HttpPost("{id:int}/kaynaktan-guncelle")]
        public async Task<ActionResult> KaynaktanGuncelle(int id, [FromBody] YuklemePlaniKaynaktanGuncelleCommand command, CancellationToken ct)
        {
            command.PlanId = id;
            return (await mediator.Send(command, ct)).ToActionResult();
        }
    }
}
