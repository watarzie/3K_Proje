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

        public AuthController(IMediator mediator, IUnitOfWork unitOfWork)
        {
            _mediator = mediator;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// İlk admin kullanıcısını oluşturur — SADECE veritabanında hiç kullanıcı yokken çalışır.
        /// </summary>
        [HttpPost("seed-admin")]
        public async Task<ActionResult> SeedAdmin([FromBody] RegisterCommand command)
        {
            var kullaniciRepo = _unitOfWork.GetRepository<Kullanici>();
            var mevcutKullanicilar = await kullaniciRepo.GetAllAsync();

            if (mevcutKullanicilar.Any())
                return BadRequest(new { message = "Sistemde zaten kullanıcı mevcut. Bu endpoint sadece ilk kurulumda kullanılabilir." });

            command.Rol = "Admin";
            var result = await _mediator.Send(command);
            return result.ToActionResult();
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
}
