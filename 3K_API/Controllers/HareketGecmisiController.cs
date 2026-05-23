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

        [HttpGet("{projeId}/gecmis")]
        public async Task<ActionResult> GetByProje(
            int projeId,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int? islemTipiId = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 15)
        {
            var result = await _mediator.Send(new GetProjeHareketleriQuery
            {
                ProjeId = projeId,
                SearchTerm = searchTerm,
                IslemTipiId = islemTipiId,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
            return result.ToActionResult();
        }
    }
}
