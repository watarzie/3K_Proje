using System.Text.Json;
using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Enums;

namespace _3K.Application.Features.OnayIslemleri.Commands
{
    public class IslemOnaylaCommandHandler : IRequestHandler<IslemOnaylaCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUserService;

        public IslemOnaylaCommandHandler(IUnitOfWork unitOfWork, IMediator mediator, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(IslemOnaylaCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<OnayBekleyenIslem>();
            var islem = await repo.GetByIdAsync(request.OnayBekleyenIslemId);

            if (islem == null || islem.Durum != OnayDurumu.Bekliyor)
                return Result.Failure("Geçerli bir onay bekleyen işlem bulunamadı.");

            // 1. Durumu Onaylandı yap.
            islem.Durum = OnayDurumu.Onaylandi;
            islem.OnaylayanKullaniciId = _currentUserService.UserId ?? 0;
            repo.Update(islem);
            await _unitOfWork.SaveChangesAsync(); // Update state first, so if execution fails it's still logged? Or execution is part of transaction?
            
            // 2. JSON Payload'u deserialize et.
            var targetType = Type.GetType(islem.CommandType);
            if (targetType == null)
                return Result.Failure("Orijinal komut tipi sistemde bulunamadı. Sürüm güncellemesi yapılıp sınıf silinmiş olabilir.");

            var originalRequest = JsonSerializer.Deserialize(islem.PayloadJson, targetType);
            if (originalRequest == null)
                return Result.Failure("JSON datası orijinal komuta dönüştürülemedi.");

            // 3. Asıl komutu MediatR ile çalıştır! (ApprovalBehavior Admin olarak geçtiğimiz için izin verecek)
            var response = await _mediator.Send(originalRequest, cancellationToken);
            
            // response could be Result or Result<T>. We check if it was successful.
            bool isSuccess = false;
            string errorMsg = "Bilinmeyen asıl işlem hatası.";

            if (response is Result resObj)
            {
                isSuccess = resObj.IsSuccess;
                if (!isSuccess && resObj.Error != null)
                    errorMsg = resObj.Error.Message;
            }
            else
            {
                // Unlikely, but if it doesn't inherit from Result
                return Result.Success();
            }

            if (!isSuccess)
            {
                // Asıl komut başarısız olduysa, onay statesini de geri alabiliriz ya da hatayı dönebiliriz.
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
