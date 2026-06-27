using System.Text.Json;
using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.OnayIslemleri.Commands
{
    public class IslemOnaylaCommandHandler : IRequestHandler<IslemOnaylaCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApprovalExecutionContext _approvalExecutionContext;
        private readonly IOnayYetkiService _onayYetkiService;

        public IslemOnaylaCommandHandler(
            IUnitOfWork unitOfWork,
            IMediator mediator,
            ICurrentUserService currentUserService,
            IApprovalExecutionContext approvalExecutionContext,
            IOnayYetkiService onayYetkiService)
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _currentUserService = currentUserService;
            _approvalExecutionContext = approvalExecutionContext;
            _onayYetkiService = onayYetkiService;
        }

        public async Task<Result> Handle(IslemOnaylaCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<OnayBekleyenIslem>();
            var islem = await repo.GetByIdAsync(request.OnayBekleyenIslemId);

            if (islem == null || islem.Durum != OnayDurumu.Bekliyor)
                return Result.Failure("Geçerli bir onay bekleyen işlem bulunamadı.");

            var kullaniciId = _currentUserService.UserId ?? 0;
            var onaylayabilir = await _onayYetkiService.KullaniciIslemOnaylayabilirMiAsync(
                kullaniciId,
                islem.IslemKodu,
                islem.TalepEdenKullaniciId,
                cancellationToken);

            if (!onaylayabilir)
                return Result.Failure("Bu işlem tipi için onay yetkiniz bulunmuyor.", 403);

            islem.Durum = OnayDurumu.Onaylandi;
            islem.OnaylayanKullaniciId = kullaniciId;
            repo.Update(islem);
            await _unitOfWork.SaveChangesAsync();

            var targetType = Type.GetType(islem.CommandType);
            if (targetType == null)
                return Result.Failure("Orijinal komut tipi sistemde bulunamadı. Sürüm güncellemesi yapılıp sınıf silinmiş olabilir.");

            var originalRequest = JsonSerializer.Deserialize(islem.PayloadJson, targetType);
            if (originalRequest == null)
                return Result.Failure("JSON datası orijinal komuta dönüştürülemedi.");

            using var approvedExecution = _approvalExecutionContext.BeginApprovedExecution();
            var response = await _mediator.Send(originalRequest, cancellationToken);

            var isSuccess = false;
            var errorMsg = "Bilinmeyen asıl işlem hatası.";

            if (response is Result resObj)
            {
                isSuccess = resObj.IsSuccess;
                if (!isSuccess && resObj.Error != null)
                    errorMsg = resObj.Error.Message;
            }
            else
            {
                return Result.Success();
            }

            if (!isSuccess)
            {
                islem.Durum = OnayDurumu.Reddedildi;
                islem.RedAciklamasi = "Komut çalıştırılırken hata: " + errorMsg;
                repo.Update(islem);
                await _unitOfWork.SaveChangesAsync();

                return Result.Failure("İşlem onaylanıp çalıştırıldı fakat asıl komut başarısız oldu: " + errorMsg);
            }

            return Result.Success();
        }
    }
}
