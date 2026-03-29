using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.AuthIslemleri.Commands;
using _3K.Application.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _unitOfWork;

        public AuthController(IMediator mediator, IUnitOfWork unitOfWork)
        {
            _mediator = mediator;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// İlk admin kullanıcısını oluşturur — SADECE veritabanında hiç kullanıcı yokken çalışır.
        /// İlk admin oluşturulduktan sonra bu endpoint devre dışı kalır.
        /// </summary>
        [HttpPost("seed-admin")]
        [AllowAnonymous]
        public async Task<ActionResult<KullaniciDto>> SeedAdmin([FromBody] RegisterCommand command)
        {
            try
            {
                var kullaniciRepo = _unitOfWork.GetRepository<Kullanici>();
                var mevcutKullanicilar = await kullaniciRepo.GetAllAsync();

                if (mevcutKullanicilar.Any())
                {
                    return BadRequest(new { message = "Sistemde zaten kullanıcı mevcut. Bu endpoint sadece ilk kurulumda kullanılabilir." });
                }

                // İlk kullanıcıyı zorla Admin yap
                command.Rol = "Admin";
                var result = await _mediator.Send(command);
                return CreatedAtAction(nameof(SeedAdmin), new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResultDto>> Login([FromBody] LoginCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpPost("register")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<KullaniciDto>> Register([FromBody] RegisterCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);
                return CreatedAtAction(nameof(Register), new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
