using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.YuklemePlanlamaIslemleri.Commands;
using _3K.Application.Features.YuklemePlanlamaIslemleri.Queries;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    /// <summary>
    /// Araç yükleme (saha): plan revizyonuna sabit yükleme oturumu ve barkodla "yüklendi" işaretleme
    /// (arac-yukleme: R izleme, W oturum ve işaretleme). Sandık durumu ve sevk bu uçlardan değişmez.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/arac-yukleme")]
    public sealed class AracYuklemeController(IMediator mediator) : ControllerBase
    {
        /// <summary>Yerleşimi olan ya da yüklemesi süren planlar, sayfalı.</summary>
        [HttpGet("planlar")]
        public async Task<ActionResult> Planlar([FromQuery] AracYuklemePlanlariQuery query, CancellationToken ct) =>
            (await mediator.Send(query, ct)).ToActionResult();

        /// <summary>Planın yükleme durumu: oturum ve araçların ilerlemesi.</summary>
        [HttpGet("planlar/{planId:int}")]
        public async Task<ActionResult> Durum(int planId, CancellationToken ct) =>
            (await mediator.Send(new AracYuklemeDurumuQuery { PlanId = planId }, ct)).ToActionResult();

        /// <summary>Planın son revizyonuyla yükleme oturumu başlatır; süren oturum varsa 409.</summary>
        [HttpPost("planlar/{planId:int}/oturum")]
        public async Task<ActionResult> Baslat(int planId, [FromBody] YuklemeOturumuBaslatCommand command, CancellationToken ct)
        {
            command.PlanId = planId;
            return (await mediator.Send(command, ct)).ToActionResult();
        }

        /// <summary>Oturumdaki aracın yerleşimi, işaretleri ve sıradaki adım.</summary>
        [HttpGet("oturumlar/{oturumId:int}/araclar/{aracNo:int}")]
        public async Task<ActionResult> Arac(int oturumId, int aracNo, CancellationToken ct) =>
            (await mediator.Send(new AracYuklemeAracQuery { OturumId = oturumId, AracNo = aracNo }, ct)).ToActionResult();

        /// <summary>Okutulan sandık barkodunu çözer (durum değiştirmez).</summary>
        [HttpGet("oturumlar/{oturumId:int}/cozumle")]
        public async Task<ActionResult> Cozumle(int oturumId, [FromQuery] string? barkod, [FromQuery] int aracNo, CancellationToken ct) =>
            (await mediator.Send(new AracYuklemeCozumleQuery { OturumId = oturumId, Barkod = barkod, AracNo = aracNo }, ct)).ToActionResult();

        /// <summary>Yükü yüklendi işaretler; zaten yüklüyse yeni kayıt yazmaz.</summary>
        [HttpPost("oturumlar/{oturumId:int}/yuklemeler")]
        public async Task<ActionResult> Isaretle(int oturumId, [FromBody] YukuYuklendiIsaretleCommand command, CancellationToken ct)
        {
            command.OturumId = oturumId;
            return (await mediator.Send(command, ct)).ToActionResult();
        }

        /// <summary>Yükleme işaretini gerekçeyle geri alır.</summary>
        [HttpPost("oturumlar/{oturumId:int}/yuklemeler/{isaretId:int}/geri-al")]
        public async Task<ActionResult> GeriAl(int oturumId, int isaretId, [FromBody] YukIsaretiGeriAlCommand command, CancellationToken ct)
        {
            command.OturumId = oturumId;
            command.IsaretId = isaretId;
            return (await mediator.Send(command, ct)).ToActionResult();
        }

        /// <summary>Oturumu tamamlar; yüklenmemiş yük varsa gerekçe ister.</summary>
        [HttpPost("oturumlar/{oturumId:int}/tamamla")]
        public async Task<ActionResult> Tamamla(int oturumId, [FromBody] YuklemeOturumuKapatCommand command, CancellationToken ct)
        {
            command.OturumId = oturumId;
            command.Iptal = false;
            return (await mediator.Send(command, ct)).ToActionResult();
        }

        /// <summary>Oturumu gerekçeyle iptal eder.</summary>
        [HttpPost("oturumlar/{oturumId:int}/iptal")]
        public async Task<ActionResult> Iptal(int oturumId, [FromBody] YuklemeOturumuKapatCommand command, CancellationToken ct)
        {
            command.OturumId = oturumId;
            command.Iptal = true;
            return (await mediator.Send(command, ct)).ToActionResult();
        }
    }
}
