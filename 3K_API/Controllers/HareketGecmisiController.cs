using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.HareketGecmisiIslemleri.Queries;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HareketGecmisiController : ControllerBase
    {
        private readonly IMediator _mediator;

        public HareketGecmisiController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("proje/{projeId}")]
        public async Task<ActionResult> GetByProje(int projeId)
        {
            var result = await _mediator.Send(new GetProjeHareketleriQuery { ProjeId = projeId });
            return result.ToActionResult();
        }
    }
}
