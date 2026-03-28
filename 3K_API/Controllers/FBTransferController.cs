using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.DTOs;
using _3K.Application.Features.FBTransferIslemleri.Commands;

namespace _3K_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FBTransferController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FBTransferController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// İş akışı 5: FB arası malzeme transferi
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<FBTransferResultDto>> Create([FromBody] FBTransferOlusturCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);
                return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
