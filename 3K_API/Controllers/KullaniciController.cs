using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.KullaniciIslemleri.Queries;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KullaniciController : ControllerBase
    {
        private readonly IMediator _mediator;

        public KullaniciController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            var result = await _mediator.Send(new KullaniciListeleQuery());
            return result.ToActionResult();
        }
    }
}
