using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.DashboardIslemleri.Queries;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DashboardController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("ozet")]
        public async Task<ActionResult> GetOzet()
        {
            var result = await _mediator.Send(new DashboardOzetQuery());
            return result.ToActionResult();
        }

        [HttpGet("projeler")]
        public async Task<ActionResult> GetProjeler(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? projeTipiId = null)
        {
            var result = await _mediator.Send(new DashboardProjelerQuery
            {
                Page = page,
                PageSize = pageSize,
                ProjeTipiId = projeTipiId
            });
            return result.ToActionResult();
        }

        [HttpGet("kritik-eksikler")]
        public async Task<ActionResult> GetKritikEksikler([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _mediator.Send(new DashboardKritikEksiklerQuery { Page = page, PageSize = pageSize });
            return result.ToActionResult();
        }

        [HttpGet("eksik-siralama")]
        public async Task<ActionResult> GetEksikSiralama([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _mediator.Send(new DashboardEksikSiralamaQuery { Page = page, PageSize = pageSize });
            return result.ToActionResult();
        }
    }
}
