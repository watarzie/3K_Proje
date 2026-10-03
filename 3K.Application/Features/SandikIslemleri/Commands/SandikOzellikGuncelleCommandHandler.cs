using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SandikOzellikGuncelleCommandHandler : IRequestHandler<SandikOzellikGuncelleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;

        public SandikOzellikGuncelleCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(SandikOzellikGuncelleCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<Sandik>();
            var sandik = await repo.GetByIdAsync(request.SandikId);

            if (sandik == null)
                return Result.Failure("Sandık bulunamadı", 404);

            if (SandikSevkKilidiHelper.SandikKilitliMi(sandik))
                return Result.Failure(SandikSevkKilidiHelper.SandikKilitliMesaji);

            if (!SandikTipiKurali.Gecerli(request.TipId))
                return Result.Failure(SandikTipiKurali.Mesaj, 400);

            // Manuel lokasyon değişikliği yalnızca kendine ait yetki ve onay
            // akışı üzerinden yapılabilir. İstemci geriye uyumluluk nedeniyle
            // mevcut değeri gönderebilir; yalnızca gerçek değişiklik reddedilir.
            if (request.DepoLokasyonId.HasValue &&
                request.DepoLokasyonId.Value != sandik.DepoLokasyonId)
            {
                return Result.Failure(
                    "Sandık lokasyonu özellik güncelleme ekranından değiştirilemez. Lokasyon Atama işlemini kullanın.",
                    400);
            }

            sandik.Ad = request.SandikIsmi;
            sandik.TipId = request.TipId!.Value;
            sandik.En = request.En;
            sandik.Boy = request.Boy;
            sandik.Yukseklik = request.Yukseklik;
            sandik.NetKg = request.NetKg;
            sandik.GrossKg = request.GrossKg;

            repo.Update(sandik);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }
    }
}
