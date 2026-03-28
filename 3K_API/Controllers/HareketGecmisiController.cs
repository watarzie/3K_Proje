using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.DTOs;
using _3K.Application.Features.HareketGecmisiIslemleri.Queries;

namespace _3K_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HareketGecmisiController : ControllerBase
    {
        private readonly IMediator _mediator;

        public HareketGecmisiController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// İş akışı 11: Proje hareket geçmişini görüntüle
        /// </summary>
        [HttpGet("proje/{projeId}")]
        public async Task<ActionResult<IEnumerable<HareketGecmisiDto>>> GetByProje(int projeId)
        {
            var result = await _mediator.Send(new GetProjeHareketleriQuery { ProjeId = projeId });
            return Ok(result);
        }
    }
}
