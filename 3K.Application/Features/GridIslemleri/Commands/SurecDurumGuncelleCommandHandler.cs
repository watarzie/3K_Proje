using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.GridIslemleri.Commands
{
    public class SurecDurumGuncelleCommandHandler : IRequestHandler<SurecDurumGuncelleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILookupCacheService _lookupCache;

        public SurecDurumGuncelleCommandHandler(
            IUnitOfWork unitOfWork,
            IHareketService hareketService,
            ICurrentUserService currentUserService,
            ILookupCacheService lookupCache)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _currentUserService = currentUserService;
            _lookupCache = lookupCache;
        }

        public async Task<Result> Handle(SurecDurumGuncelleCommand request, CancellationToken cancellationToken)
        {
            if (request.CekiSatiriIdler == null || !request.CekiSatiriIdler.Any())
                return Result.Failure("En az bir ürün seçilmelidir.");

            // Lookup doğrulama
            var durumMetni = _lookupCache.GetDeger<LookupSurecDurum>(request.SurecDurumId);
            if (string.IsNullOrEmpty(durumMetni))
                return Result.Failure("Geçersiz Süreç durumu.");

            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var satirlar = (await repo.FindAsync(cs =>
                request.CekiSatiriIdler.Contains(cs.Id) && cs.Ceki.ProjeId == request.ProjeId))
                .ToList();

            if (!satirlar.Any())
                return Result.Failure("Belirtilen ürünler bulunamadı.", 404);

            foreach (var satir in satirlar)
            {
                var eskiDurum = satir.SurecDurumId.HasValue
                    ? _lookupCache.GetDeger<LookupSurecDurum>(satir.SurecDurumId.Value)
                    : "Belirtilmemiş";

                satir.SurecDurumId = request.SurecDurumId;
                repo.Update(satir);

                // Hareket geçmişi
                await _hareketService.HareketKaydetAsync(new HareketGecmisi
                {
                    ProjeId = request.ProjeId,
                    ReferansTipi = "CekiSatiri",
                    ReferansId = satir.Id.ToString(),
                    Islem = "Süreç Durumu Güncellendi",
                    IslemTipiId = (int)IslemTipi.UrunGuncellendi,
                    KullaniciId = _currentUserService.UserId ?? 0,
                    EskiDeger = eskiDurum,
                    YeniDeger = durumMetni,
                    Aciklama = $"Süreç: {eskiDurum} → {durumMetni}"
                });
            }

            await _unitOfWork.SaveChangesAsync();
            return Result.Success();
        }
    }
}
