using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AuthIslemleri.DTOs;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.AuthIslemleri.Commands
{
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<KullaniciDto>>
    {
        private readonly IAuthService _authService;
        private readonly IRolAtamaService _rolAtama;

        public RegisterCommandHandler(IAuthService authService, IRolAtamaService rolAtama)
        {
            _authService = authService;
            _rolAtama = rolAtama;
        }

        public async Task<Result<KullaniciDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var check = await _rolAtama.RolAtamayiDogrulaAsync(null, request.RolId, cancellationToken);
            if (!check.Basarili)
                return Result<KullaniciDto>.Failure(check.Hata!, check.DurumKodu);
            var kullanici = await _authService.RegisterAsync(request.AdSoyad, request.Email, request.Sifre, request.RolId);

            return Result<KullaniciDto>.Success(AuthDtoFactory.Kullanici(kullanici));
        }
    }
}
