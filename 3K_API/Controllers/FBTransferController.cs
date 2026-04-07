using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.FBTransferIslemleri.Commands;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FBTransferController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FBTransferController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] FBTransferOlusturCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }
    }
}
