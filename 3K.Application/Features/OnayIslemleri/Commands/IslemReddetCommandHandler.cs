using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.OnayIslemleri.Commands
{
    public class IslemReddetCommandHandler : IRequestHandler<IslemReddetCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IOnayYetkiService _onayYetkiService;

        public IslemReddetCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IOnayYetkiService onayYetkiService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _onayYetkiService = onayYetkiService;
        }

        public async Task<Result> Handle(IslemReddetCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<OnayBekleyenIslem>();
            var islem = await repo.GetByIdAsync(request.OnayBekleyenIslemId);

            if (islem == null || islem.Durum != OnayDurumu.Bekliyor)
                return Result.Failure("Geçerli bir onay bekleyen işlem bulunamadı.");

            var kullaniciId = _currentUserService.UserId ?? 0;
            var reddedebilir = await _onayYetkiService.KullaniciIslemOnaylayabilirMiAsync(
                kullaniciId,
                islem.IslemKodu,
                islem.TalepEdenKullaniciId,
                cancellationToken);

            if (!reddedebilir)
                return Result.Failure("Bu işlem tipi için red yetkiniz bulunmuyor.", 403);

            islem.Durum = OnayDurumu.Reddedildi;
            islem.RedAciklamasi = request.RedAciklamasi;
            islem.OnaylayanKullaniciId = kullaniciId;

            repo.Update(islem);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }
    }
}
