using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.LookupIslemleri.Queries;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    /// <summary>
    /// Dinamik Lookup/Parametre endpoint'i.
    /// Entity adı query string ile gönderilir, reflection ile resolve edilip veriler döner.
    /// 
    /// Kullanım: GET /api/lookup?entity=LookupProjeDurum&amp;entity=LookupSandikDurum
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class LookupController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LookupController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Bir veya birden fazla lookup tablosunun verilerini döner.
        /// </summary>
        /// <param name="entity">Lookup sınıf adları (birden fazla gönderilebilir).</param>
        /// <returns>{ "LookupProjeDurum": [{id, anahtar, deger}, ...], ... }</returns>
        [HttpGet]
        public async Task<ActionResult> GetLookups([FromQuery] List<string> entity)
        {
            if (entity == null || entity.Count == 0)
            {
                return BadRequest(new { success = false, message = "En az bir 'entity' parametresi gereklidir." });
            }

            var result = await _mediator.Send(new GetLookupsQuery { Entities = entity });
            return result.ToActionResult();
        }
    }
}
