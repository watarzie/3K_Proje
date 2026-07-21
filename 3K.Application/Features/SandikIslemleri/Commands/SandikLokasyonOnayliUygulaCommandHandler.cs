using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.SandikIslemleri.Services;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public sealed class SandikLokasyonOnayliUygulaCommandHandler
        : IRequestHandler<SandikLokasyonOnayliUygulaCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISandikLokasyonGuncellemeService _lokasyonGuncellemeService;
        private readonly ICurrentUserService _currentUserService;

        public SandikLokasyonOnayliUygulaCommandHandler(
            IUnitOfWork unitOfWork,
            ISandikLokasyonGuncellemeService lokasyonGuncellemeService,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _lokasyonGuncellemeService = lokasyonGuncellemeService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<bool>> Handle(
            SandikLokasyonOnayliUygulaCommand request,
            CancellationToken cancellationToken)
        {
            if (!_currentUserService.UserId.HasValue)
                return Result<bool>.Failure("İşlemi gerçekleştiren kullanıcı bilgisi alınamadı.", 401);

            // Hareket servisi kendi SaveChanges çağrılarını yaptığı için toplu
            // lokasyon değişikliği tek transaction altında atomik yürütülür.
            return await _unitOfWork.ExecuteInTransactionAsync(
                transactionCancellationToken =>
                    _lokasyonGuncellemeService.UygulaAsync(
                        request,
                        transactionCancellationToken),
                cancellationToken);
        }
    }
}
