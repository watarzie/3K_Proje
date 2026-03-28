using MediatR;
using _3K.Application.DTOs;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.AuthIslemleri.Commands
{
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, KullaniciDto>
    {
        private readonly IAuthService _authService;

        public RegisterCommandHandler(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<KullaniciDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var kullanici = await _authService.RegisterAsync(request.AdSoyad, request.Email, request.Sifre, request.Rol);

            return new KullaniciDto
            {
                Id = kullanici.Id,
                AdSoyad = kullanici.AdSoyad,
                BasHarf = kullanici.BasHarf,
                Rol = kullanici.Rol.ToString(),
                Email = kullanici.Email
            };
        }
    }
}
