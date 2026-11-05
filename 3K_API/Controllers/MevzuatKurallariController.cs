using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.YuklemePlanlamaIslemleri.Queries;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    /// <summary>Mevzuat kural setleri ve kuralları (mevzuat-kurallari R, salt okunur).</summary>
    [Authorize]
    [ApiController]
    [Route("api/mevzuat-kurallari")]
    public sealed class MevzuatKurallariController(IMediator mediator) : ControllerBase
    {
        [HttpGet("kural-setleri")]
        public async Task<ActionResult> KuralSetleri(CancellationToken ct) =>
            (await mediator.Send(new MevzuatKuralSetleriQuery(), ct)).ToActionResult();

        [HttpGet("kural-setleri/{id:int}")]
        public async Task<ActionResult> KuralSeti(int id, CancellationToken ct) =>
            (await mediator.Send(new MevzuatKuralSetiGetirQuery { Id = id }, ct)).ToActionResult();
    }
}
