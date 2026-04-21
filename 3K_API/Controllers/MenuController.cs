using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.RolIslemleri.Queries;
using _3K.Application.Features.RolIslemleri.DTOs;

namespace _3K_API.Controllers
{
    /// <summary>
    /// Kullanıcıya ait menü ağacını döner.
    /// JWT'deki RolId claim'i kullanılarak backend'de filtrelenir.
    /// Frontend bu bilgiyi sidebar ve route guard için kullanır.
    /// GÜVENLİK: Yetkisiz (N) menüler asla frontend'e gönderilmez.
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

        /// <summary>
        /// Giriş yapmış kullanıcının rolüne göre yetkili menü ağacını döner.
        /// Yetkisiz (N) menüler filtrelenir — frontend'e hiç gönderilmez.
        /// </summary>
        [HttpGet("kullanici-menu")]
        public async Task<ActionResult> GetKullaniciMenu()
        {
            var rolIdClaim = User.FindFirst("RolId")?.Value;
            if (string.IsNullOrEmpty(rolIdClaim) || !int.TryParse(rolIdClaim, out var rolId))
                return Unauthorized(new { message = "Geçersiz oturum." });

            var result = await _mediator.Send(new GetRolDetayQuery { RolId = rolId });
            if (!result.IsSuccess)
                return BadRequest(new { message = result.Error });

            // Backend'de filtreleme: sadece yetkili (W veya R) menüleri gönder
            var filteredMenu = FilterTree(result.Value!.MenuAgaci);

            return Ok(filteredMenu);
        }

        /// <summary>
        /// Recursive olarak yetkisiz (N) menüleri çıkarır.
        /// </summary>
        private List<MenuTreeDto> FilterTree(List<MenuTreeDto> nodes)
        {
            return nodes
                .Where(n => n.YetkiTipiId >= 2) // 2=R, 3=W — exclude 1=N
                .Select(n => new MenuTreeDto
                {
                    Id = n.Id,
                    Kod = n.Kod,
                    LabelKey = n.LabelKey,
                    Icon = n.Icon,
                    Route = n.Route,
                    Sira = n.Sira,
                    YetkiTipiId = n.YetkiTipiId,
                    YetkiTipiMetni = n.YetkiTipiMetni,
                    Children = n.Children != null ? FilterTree(n.Children) : new()
                })
                .ToList();
        }
    }
}
