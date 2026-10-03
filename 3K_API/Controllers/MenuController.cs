using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.RolIslemleri.Queries;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    /// <summary>
    /// Oturum sahibinin erişebildiği menü ağacı (sidebar ve route guard için).
    /// Yetkisiz (N) düğümler kullanım senaryosunda çıkarılır; istemciye hiç gönderilmez.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class MenuController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MenuController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("kullanici-menu")]
        public async Task<ActionResult> GetKullaniciMenu(CancellationToken cancellationToken) =>
            (await _mediator.Send(new GetKullaniciMenuQuery(), cancellationToken)).ToActionResult();
    }
}
