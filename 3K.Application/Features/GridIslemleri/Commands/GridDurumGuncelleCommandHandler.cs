using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.GridIslemleri.Commands
{
    public class GridDurumGuncelleCommandHandler : IRequestHandler<GridDurumGuncelleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDurumHesaplaService _durumHesaplaService;
        private readonly IHareketService _hareketService;

        public GridDurumGuncelleCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IDurumHesaplaService durumHesaplaService,
            IHareketService hareketService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _durumHesaplaService = durumHesaplaService;
            _hareketService = hareketService;
        }

        public async Task<Result> Handle(GridDurumGuncelleCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var satir = await repo.GetByIdAsync(request.CekiSatiriId);

            if (satir == null)
                return Result.Failure("Ürün bulunamadı.", 404);

            var eskiDurum = satir.GridDurumu;

            // Grid alanlarını güncelle
            satir.GridDurumu = request.YeniDurum;
            satir.GridPersonelId = _currentUserService.UserId;
            satir.GridNotu = request.Not;

            if (request.YeniDurum == "SevkEdildi" || request.YeniDurum == "KismiSevkEdildi")
            {
                satir.GridSevkMiktari = request.SevkMiktari ?? satir.IstenenAdet;
                satir.GridSevkTarihi = DateTime.UtcNow;
            }

            // Genel durumu otomatik hesapla
            satir.Durum = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumu, satir.UcKDurumu);

            repo.Update(satir);
            await _unitOfWork.SaveChangesAsync();

            // Hareket kaydı
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "CekiSatiri",
                ReferansId = satir.Id.ToString(),
                Islem = "Grid Durum Güncellendi",
                EskiDeger = eskiDurum,
                YeniDeger = request.YeniDurum,
                Aciklama = request.Not
            });

            return Result.Success();
        }
    }
}
