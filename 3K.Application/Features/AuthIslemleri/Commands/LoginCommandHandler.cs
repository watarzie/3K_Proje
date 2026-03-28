using MediatR;
using _3K.Application.DTOs;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.AuthIslemleri.Commands
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResultDto>
    {
        private readonly IAuthService _authService;

        public LoginCommandHandler(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<LoginResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var token = await _authService.LoginAsync(request.Email, request.Sifre);
            var kullanici = await _authService.GetKullaniciByEmailAsync(request.Email);

            if (kullanici == null)
                throw new UnauthorizedAccessException("Geçersiz email veya şifre.");

            return new LoginResultDto
            {
                Token = token,
                Kullanici = new KullaniciDto
                {
                    Id = kullanici.Id,
                    AdSoyad = kullanici.AdSoyad,
                    BasHarf = kullanici.BasHarf,
                    Rol = kullanici.Rol.ToString(),
                    Email = kullanici.Email
                }
            };
        }
    }
}
