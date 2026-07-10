using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using MediatR;

namespace _3K.Application.Features.BildirimIslemleri.Commands
{
    public class BildirimiOkunduIsaretleCommandHandler : IRequestHandler<BildirimiOkunduIsaretleCommand, Result>
    {
        private readonly IBildirimRepository _bildirimRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISseNotifier _sseNotifier;

        public BildirimiOkunduIsaretleCommandHandler(
            IBildirimRepository bildirimRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            ISseNotifier sseNotifier)
        {
            _bildirimRepository = bildirimRepository;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _sseNotifier = sseNotifier;
        }

        public async Task<Result> Handle(BildirimiOkunduIsaretleCommand request, CancellationToken cancellationToken)
        {
            var kullaniciId = _currentUserService.UserId;
            if (!kullaniciId.HasValue)
                return Result.Failure("Kullanıcı bilgisi alınamadı.", 401);

            var kullaniciBildirimi = await _bildirimRepository.GetKullaniciBildirimiAsync(
                request.BildirimId,
                kullaniciId.Value,
                cancellationToken);

            if (kullaniciBildirimi == null)
                return Result.Failure("Bildirim bulunamadı.", 404);

            if (!kullaniciBildirimi.OkunduMu)
            {
                kullaniciBildirimi.OkunduMu = true;
                kullaniciBildirimi.OkunmaTarihi = TurkeyTime.Now;
                await _unitOfWork.SaveChangesAsync();

                await _sseNotifier.NotifyUsersAsync(
                    new[] { kullaniciId.Value },
                    SseOlaylari.BildirimGuncellendi);
            }

            return Result.Success();
        }
    }
}
