using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.DTOs;
using _3K.Application.Features.KullaniciIslemleri.Queries;

namespace _3K_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class KullaniciController : ControllerBase
    {
        private readonly IMediator _mediator;

        public KullaniciController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<KullaniciDto>>> GetAll()
        {
            var result = await _mediator.Send(new KullaniciListeleQuery());
            return Ok(result);
        }
    }
}
