using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using MediatR;

namespace _3K.Application.Features.BildirimIslemleri.Commands
{
    public class TumBildirimleriOkunduIsaretleCommandHandler
        : IRequestHandler<TumBildirimleriOkunduIsaretleCommand, Result>
    {
        private readonly IBildirimRepository _bildirimRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISseNotifier _sseNotifier;

        public TumBildirimleriOkunduIsaretleCommandHandler(
            IBildirimRepository bildirimRepository,
            ICurrentUserService currentUserService,
            ISseNotifier sseNotifier)
        {
            _bildirimRepository = bildirimRepository;
            _currentUserService = currentUserService;
            _sseNotifier = sseNotifier;
        }

        public async Task<Result> Handle(TumBildirimleriOkunduIsaretleCommand request, CancellationToken cancellationToken)
        {
            var kullaniciId = _currentUserService.UserId;
            if (!kullaniciId.HasValue)
                return Result.Failure("Kullanıcı bilgisi alınamadı.", 401);

            var guncellenenBildirimSayisi = await _bildirimRepository.TumOkunmamisBildirimleriOkunduIsaretleAsync(
                kullaniciId.Value,
                TurkeyTime.Now,
                cancellationToken);

            if (guncellenenBildirimSayisi > 0)
            {
                await _sseNotifier.NotifyUsersAsync(
                    new[] { kullaniciId.Value },
                    SseOlaylari.BildirimGuncellendi);
            }

            return Result.Success();
        }
    }
}
