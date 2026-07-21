using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.SandikIslemleri.Services;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SandikLokasyonGuncelleCommandHandler : IRequestHandler<SandikLokasyonGuncelleCommand, Result<bool>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ISandikLokasyonGuncellemeService _lokasyonGuncellemeService;
        private readonly IMediator _mediator;

        public SandikLokasyonGuncelleCommandHandler(
            ICurrentUserService currentUserService,
            ISandikLokasyonGuncellemeService lokasyonGuncellemeService,
            IMediator mediator)
        {
            _currentUserService = currentUserService;
            _lokasyonGuncellemeService = lokasyonGuncellemeService;
            _mediator = mediator;
        }

        public async Task<Result<bool>> Handle(SandikLokasyonGuncelleCommand request, CancellationToken cancellationToken)
        {
            if (!_currentUserService.IsAuthenticated)
                return Result<bool>.Failure("Oturum açmanız gerekiyor.", 401);

            var planSonucu = await _lokasyonGuncellemeService.PlanlaAsync(
                request.SandikIds,
                request.DepoLokasyonId,
                cancellationToken);

            if (!planSonucu.IsSuccess || planSonucu.Value == null)
                return BasarisizSonucuDonustur(planSonucu);

            // Seçilen sandıkların tamamı zaten hedef lokasyondaysa ne veri
            // değişikliği ne de gereksiz bir onay/audit kaydı oluşturulur.
            if (planSonucu.Value.Kalemler.Count == 0)
                return Result<bool>.Success(true);

            // Ham HTTP isteği onay kuyruğuna yazılmaz. Sunucunun doğrulayıp
            // zenginleştirdiği değişmez plan mevcut onay motoruna gönderilir.
            return await _mediator.Send(planSonucu.Value, cancellationToken);
        }

        private static Result<bool> BasarisizSonucuDonustur(
            Result<SandikLokasyonOnayliUygulaCommand> sonuc)
        {
            var hata = sonuc.Error;
            if (hata?.Issues != null)
                return Result<bool>.Failure(hata.Message, sonuc.StatusCode, hata.Issues);

            return Result<bool>.Failure(
                hata?.Message ?? "Lokasyon güncelleme planı oluşturulamadı.",
                sonuc.StatusCode);
        }
    }
}
