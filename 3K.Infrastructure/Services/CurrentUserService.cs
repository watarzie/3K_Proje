using System.Security.Claims;
using _3K.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace _3K.Infrastructure.Services
{
    /// <summary>
    /// JWT Token'dan mevcut kullanıcı bilgilerini çıkaran servis.
    /// IHttpContextAccessor üzerinden ClaimsPrincipal'e erişir.
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? UserId
        {
            get
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)
                    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub");

                return userIdClaim != null && int.TryParse(userIdClaim.Value, out var id) ? id : null;
            }
        }

        public bool IsAuthenticated =>
            _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        public string? MenuKod
        {
            get
            {
                var value = _httpContextAccessor.HttpContext?.Request.Headers["X-Menu-Kod"].FirstOrDefault();
                return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            }
        }
    }
}
