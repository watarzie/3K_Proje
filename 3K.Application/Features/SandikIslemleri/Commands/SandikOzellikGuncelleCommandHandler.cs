using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Services;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SandikOzellikGuncelleCommandHandler : IRequestHandler<SandikOzellikGuncelleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly IAmbalajKaynakSenkronizasyonService _ambalajSenkronizasyonu;
        private readonly ICurrentUserService _currentUser;

        public SandikOzellikGuncelleCommandHandler(
            IUnitOfWork unitOfWork,
            IHareketService hareketService,
            IAmbalajKaynakSenkronizasyonService ambalajSenkronizasyonu,
            ICurrentUserService currentUser)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _ambalajSenkronizasyonu = ambalajSenkronizasyonu;
            _currentUser = currentUser;
        }

        // Numara değişikliği bağlı kayıtları ve Ambalaj senkronunu da yazar; ret durumunda hiçbiri kalmaz.
        public Task<Result> Handle(SandikOzellikGuncelleCommand request, CancellationToken cancellationToken) =>
            _unitOfWork.ExecuteAtomicAsync(token => GuncelleAsync(request, token), cancellationToken);

        private async Task<Result> GuncelleAsync(SandikOzellikGuncelleCommand request, CancellationToken cancellationToken)
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

            // Numara değişikliği hiçbir alan yazılmadan denetlenir; ret durumunda sandık olduğu gibi kalır.
            var numaraDegisiyor = SandikNoDegisikligiKurali.DegisiyorMu(sandik.SandikNo, request.SandikNo);
            var numaraIslemi = new SandikNoDegistirmeIslemi(_unitOfWork, _hareketService, _ambalajSenkronizasyonu, _currentUser);
            if (numaraDegisiyor)
            {
                var denetim = numaraIslemi.Denetle(sandik, request.SandikNo!);
                if (!denetim.IsSuccess)
                    return denetim;
            }

            sandik.Ad = request.SandikIsmi;
            sandik.TipId = request.TipId!.Value;
            sandik.En = request.En;
            sandik.Boy = request.Boy;
            sandik.Yukseklik = request.Yukseklik;
            sandik.NetKg = request.NetKg;
            sandik.GrossKg = request.GrossKg;
            repo.Update(sandik);

            // Numara öteki alanlardan sonra değişir ki Ambalaj senkronu sandığın son halini görsün.
            if (numaraDegisiyor)
            {
                var numara = await numaraIslemi.UygulaAsync(sandik, request.SandikNo!, cancellationToken);
                if (!numara.IsSuccess)
                    return numara;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
