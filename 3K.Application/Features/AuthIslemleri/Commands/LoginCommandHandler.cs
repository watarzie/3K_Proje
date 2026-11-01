using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AuthIslemleri.DTOs;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.AuthIslemleri.Commands
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResultDto>>
    {
        private readonly IAuthService _authService;
        private readonly IIkiFaktorService _ikiFaktorService;

        public LoginCommandHandler(
            IAuthService authService,
            IIkiFaktorService ikiFaktorService)
        {
            _authService = authService;
            _ikiFaktorService = ikiFaktorService;
        }

        public async Task<Result<LoginResultDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var dogrulama = await _authService.ValidateCredentialsAsync(
                request.Email,
                request.Sifre,
                cancellationToken);

            // Hatalı parola, bilinmeyen e-posta ve kilitli hesap aynı yanıtı alır: hesabın varlığı sızmaz.
            if (dogrulama is not { Durum: GirisDogrulamaDurumu.Basarili, Kullanici: { } kullanici })
                return Result<LoginResultDto>.Failure(GirisKilidiKurali.GenelHataMesaji, 401);

            if (!kullanici.IkiFaktorZorunluMu)
            {
                var ayarDurumlari = await _ikiFaktorService.AyarDurumlariniGetirAsync(
                    new[] { kullanici.Id },
                    cancellationToken);
                ayarDurumlari.TryGetValue(kullanici.Id, out var ayarDurumu);
                var token = _authService.GenerateAccessToken(kullanici, ikiFaktorDogrulandi: false);
                return Result<LoginResultDto>.Success(
                    AuthDtoFactory.Authenticated(
                        kullanici,
                        token,
                        ikiFaktorDurumu: ayarDurumu));
            }

            var ayarEtkin = await _ikiFaktorService.AyarEtkinMiAsync(
                kullanici.Id,
                cancellationToken);
            var amac = ayarEtkin
                ? IkiFaktorTalepAmaci.Giris
                : IkiFaktorTalepAmaci.Kurulum;
            var talep = await _ikiFaktorService.TalepOlusturAsync(
                kullanici.Id,
                amac,
                request.BeniHatirla,
                cancellationToken);

            return Result<LoginResultDto>.Success(new LoginResultDto
            {
                NextStep = ayarEtkin
                    ? LoginNextSteps.TwoFactorRequired
                    : LoginNextSteps.TwoFactorSetupRequired,
                ChallengeToken = talep.TalepTokeni,
                ExpiresInSeconds = talep.GecerlilikSuresiSaniye
            });
        }
    }
}
