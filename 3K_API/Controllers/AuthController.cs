using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.AuthIslemleri.Commands;
using _3K.Core.Interfaces;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IAuthService _authService;

        public AuthController(IMediator mediator, IAuthService authService)
        {
            _mediator = mediator;
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting(ApiServiceCollectionExtensions.LoginRateLimitPolicy)]
        public async Task<ActionResult> Login([FromBody] LoginCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPost("2fa/setup/start")]
        [AllowAnonymous]
        [EnableRateLimiting(ApiServiceCollectionExtensions.TwoFactorRateLimitPolicy)]
        public async Task<ActionResult> IkiFaktorKurulumBaslat(
            [FromBody] IkiFaktorKurulumBaslatCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPost("2fa/setup/confirm")]
        [AllowAnonymous]
        [EnableRateLimiting(ApiServiceCollectionExtensions.TwoFactorRateLimitPolicy)]
        public async Task<ActionResult> IkiFaktorKurulumDogrula(
            [FromBody] IkiFaktorKurulumDogrulaCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPost("2fa/verify")]
        [AllowAnonymous]
        [EnableRateLimiting(ApiServiceCollectionExtensions.TwoFactorRateLimitPolicy)]
        public async Task<ActionResult> IkiFaktorGirisDogrula(
            [FromBody] IkiFaktorGirisDogrulaCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPost("2fa/recovery/verify")]
        [AllowAnonymous]
        [EnableRateLimiting(ApiServiceCollectionExtensions.TwoFactorRateLimitPolicy)]
        public async Task<ActionResult> IkiFaktorKurtarmaKoduDogrula(
            [FromBody] IkiFaktorKurtarmaKoduDogrulaCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        /// <summary>
        /// Mevcut geçerli token ile yeni token üretir (silent refresh).
        /// Frontend 401 aldığında bu endpoint'i çağırır.
        /// </summary>
        [HttpPost("refresh-token")]
        [Authorize]
        public async Task<ActionResult> RefreshToken()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Geçersiz token." });

            try
            {
                var ikiFaktorDogrulandi = string.Equals(
                    User.FindFirst("mfa")?.Value,
                    "true",
                    StringComparison.OrdinalIgnoreCase);
                var newToken = await _authService.RefreshTokenAsync(
                    userId,
                    ikiFaktorDogrulandi,
                    HttpContext.RequestAborted);
                return Ok(new { token = newToken });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Oturum yenilenemedi. Lütfen tekrar giriş yapın." });
            }
        }
    }
}
