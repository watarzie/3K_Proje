using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Entities;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    public class SandikKapatCommandHandler : IRequestHandler<SandikKapatCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ICurrentUserService _currentUserService;

        public SandikKapatCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<bool>> Handle(SandikKapatCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<Sandik>();
            var sandik = await repo.GetByIdAsync(request.SandikId);

            var eskiDurumId = sandik.DurumId;

            if (sandik == null)
                return Result<bool>.Failure("Sandık bulunamadı.", 404);
            
            int yeniDurumId = request.Kapali ? (int)SandikDurum.Hazir : (int)SandikDurum.Hazirlaniyor;
            
            if (sandik.DurumId != yeniDurumId)
            {
                sandik.DurumId = yeniDurumId;
                repo.Update(sandik);
                await _unitOfWork.SaveChangesAsync();

                var eskiDurumMetni = Enum.GetName(typeof(SandikDurum), eskiDurumId) ?? eskiDurumId.ToString();
                var yeniDurumMetni = Enum.GetName(typeof(SandikDurum), yeniDurumId) ?? yeniDurumId.ToString();
                var islemMetni = request.Kapali ? "Sandık Manuel Kapatma" : "Sandık Geri Açıldı";

                await _hareketService.HareketKaydetAsync(new HareketGecmisi
                {
                    ProjeId = sandik.ProjeId,
                    KullaniciId = _currentUserService.UserId ?? 0,
                    ReferansTipi = "Sandik",
                    ReferansId = sandik.Id.ToString(),
                    Islem = islemMetni,
                    IslemTipiId = request.Kapali ? (int)IslemTipi.SandikKapatildi : null,
                    EskiDeger = eskiDurumMetni,
                    YeniDeger = yeniDurumMetni,
                    Aciklama = request.Kapali ? $"Sandık yetkili tarafından manuel olarak kapatıldı." : $"Sandık yetkili tarafından tekrar hazırlanıyor konumuna getirildi."
                });
            }

            return Result<bool>.Success(true);
        }
    }
}
