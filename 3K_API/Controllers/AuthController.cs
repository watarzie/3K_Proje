using MediatR;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.AuthIslemleri.Commands;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;
        private readonly IWebHostEnvironment _environment;

        public AuthController(
            IMediator mediator,
            IUnitOfWork unitOfWork,
            IAuthService authService,
            IWebHostEnvironment environment)
        {
            _mediator = mediator;
            _unitOfWork = unitOfWork;
            _authService = authService;
            _environment = environment;
        }

        /// <summary>
        /// İlk admin kullanıcısını oluşturur.
        /// GÜVENLİK: Sadece DB'de hiç kullanıcı yokken çalışır.
        /// MediatR pipeline'ını bypass eder (yetkilendirme gerekmez).
        /// </summary>
        [HttpPost("seed-admin")]
        public async Task<ActionResult> SeedAdmin([FromBody] SeedAdminRequest request)
        {
            // Güvenlik 1: Sistemde zaten kullanıcı varsa engelle
            var kullaniciRepo = _unitOfWork.GetRepository<Kullanici>();
            var mevcutKullanicilar = await kullaniciRepo.GetAllAsync();

            if (mevcutKullanicilar.Any())
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Sistemde zaten kullanıcı mevcut. Bu endpoint sadece ilk kurulumda kullanılabilir."
                });
            }

            // Validasyon
            if (string.IsNullOrWhiteSpace(request.AdSoyad) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Sifre))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "AdSoyad, Email ve Sifre alanları zorunludur."
                });
            }

            // MediatR pipeline bypass — doğrudan AuthService kullan
            // Rol her zaman Admin olarak zorunlu atanır (request'ten alınmaz)
            var kullanici = await _authService.RegisterAsync(
                request.AdSoyad,
                request.Email,
                request.Sifre,
                "Admin"
            );

            return Ok(new
            {
                success = true,
                message = "İlk admin kullanıcısı başarıyla oluşturuldu.",
                data = new
                {
                    kullanici.Id,
                    kullanici.AdSoyad,
                    kullanici.Email,
                    kullanici.Rol,
                    kullanici.BasHarf
                }
            });
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] RegisterCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }
    }

    /// <summary>
    /// Seed Admin için ayrı DTO — RegisterCommand'dan bağımsız (pipeline bypass).
    /// </summary>
    public class SeedAdminRequest
    {
        public string AdSoyad { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Sifre { get; set; } = string.Empty;
    }
}
