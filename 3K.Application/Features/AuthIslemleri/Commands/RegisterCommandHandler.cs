using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AuthIslemleri.DTOs;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.AuthIslemleri.Commands
{
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<KullaniciDto>>
    {
        private readonly IAuthService _authService;
        private readonly IKullaniciYetkiService _yetkiService;

        public RegisterCommandHandler(IAuthService authService, IKullaniciYetkiService yetkiService)
        {
            _authService = authService;
            _yetkiService = yetkiService;
        }

        public async Task<Result<KullaniciDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var check = await _yetkiService.RolAtamayiDogrulaAsync(null, request.RolId, cancellationToken);
            if (!check.Basarili)
                return Result<KullaniciDto>.Failure(check.Hata!, check.DurumKodu);
            var kullanici = await _authService.RegisterAsync(request.AdSoyad, request.Email, request.Sifre, request.RolId);

            return Result<KullaniciDto>.Success(AuthDtoFactory.Kullanici(kullanici));
        }
    }
}
