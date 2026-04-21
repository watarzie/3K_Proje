using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Enums;

namespace _3K.Application.Features.OnayIslemleri.Commands
{
    public class IslemReddetCommandHandler : IRequestHandler<IslemReddetCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public IslemReddetCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(IslemReddetCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<OnayBekleyenIslem>();
            var islem = await repo.GetByIdAsync(request.OnayBekleyenIslemId);

            if (islem == null || islem.Durum != OnayDurumu.Bekliyor)
                return Result.Failure("Geçerli bir onay bekleyen işlem bulunamadı.");

            islem.Durum = OnayDurumu.Reddedildi;
            islem.RedAciklamasi = request.RedAciklamasi;
            islem.OnaylayanKullaniciId = _currentUserService.UserId ?? 0;
            
            repo.Update(islem);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }
    }
}
