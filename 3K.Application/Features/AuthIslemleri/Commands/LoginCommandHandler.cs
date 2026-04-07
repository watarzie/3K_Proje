using MediatR;
using _3K.Application.Common;
using _3K.Application.DTOs;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.AuthIslemleri.Commands
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResultDto>>
    {
        private readonly IAuthService _authService;

        public LoginCommandHandler(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<Result<LoginResultDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var kullanici = await _authService.GetKullaniciByEmailAsync(request.Email);
            if (kullanici == null)
                return Result<LoginResultDto>.Failure("Geçersiz email veya şifre.", 401);

            var token = await _authService.LoginAsync(request.Email, request.Sifre);

            return Result<LoginResultDto>.Success(new LoginResultDto
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
            });
        }
    }
}
