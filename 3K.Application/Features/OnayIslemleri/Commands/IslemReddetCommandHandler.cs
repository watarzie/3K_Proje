using MediatR;
using Microsoft.Extensions.Logging;
using _3K.Application.Common;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.OnayIslemleri.Commands
{
    public class IslemReddetCommandHandler : IRequestHandler<IslemReddetCommand, Result>
    {
        private readonly IOnayIslemRepository _onayRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IOnayYetkiService _onayYetkiService;
        private readonly ISseNotifier _sseNotifier;
        private readonly ILogger<IslemReddetCommandHandler> _logger;

        public IslemReddetCommandHandler(
            IOnayIslemRepository onayRepository,
            ICurrentUserService currentUserService,
            IOnayYetkiService onayYetkiService,
            ISseNotifier sseNotifier,
            ILogger<IslemReddetCommandHandler> logger)
        {
            _onayRepository = onayRepository;
            _currentUserService = currentUserService;
            _onayYetkiService = onayYetkiService;
            _sseNotifier = sseNotifier;
            _logger = logger;
        }

        public async Task<Result> Handle(IslemReddetCommand request, CancellationToken cancellationToken)
        {
            var kullaniciId = _currentUserService.UserId;
            if (!kullaniciId.HasValue)
                return Result.Failure("Kullanıcı bilgisi alınamadı.", 401);

            var islem = await _onayRepository.GetByIdNoTrackingAsync(
                request.OnayBekleyenIslemId,
                cancellationToken);

            if (islem == null)
                return Result.Failure("Onay kaydı bulunamadı.", 404);

            var reddedebilir = await _onayYetkiService.KullaniciIslemOnaylayabilirMiAsync(
                kullaniciId.Value,
                islem.IslemKodu,
                islem.TalepEdenKullaniciId,
                cancellationToken);

            if (!reddedebilir)
                return Result.Failure("Bu işlem tipi için red yetkiniz bulunmuyor.", 403);

            if (islem.Durum != OnayDurumu.Bekliyor)
                return Result.Failure("Bu işlem başka bir kullanıcı tarafından sonuçlandırılmış.", 409);

            var reddedildi = await _onayRepository.ReddetAsync(
                islem.Id,
                kullaniciId.Value,
                TurkeyTime.Now,
                request.RedAciklamasi.Trim(),
                cancellationToken);

            if (!reddedildi)
                return Result.Failure("Bu işlem başka bir kullanıcı tarafından sonuçlandırılmış.", 409);

            try
            {
                await _sseNotifier.BroadcastApprovalUpdateAsync();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Onay kaydı {OnayId} reddedildikten sonra SSE sinyali gönderilemedi.",
                    islem.Id);
            }

            return Result.Success();
        }
    }
}
