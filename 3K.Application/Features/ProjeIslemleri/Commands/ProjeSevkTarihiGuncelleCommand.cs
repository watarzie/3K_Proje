using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    public class ProjeSevkTarihiGuncelleCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin };

        public int ProjeId { get; set; }
        public DateTime? PlanlananSevkTarihi { get; set; }
    }

    public class ProjeSevkTarihiGuncelleCommandHandler : IRequestHandler<ProjeSevkTarihiGuncelleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ICurrentUserService _currentUserService;

        public ProjeSevkTarihiGuncelleCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(ProjeSevkTarihiGuncelleCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<Proje>();
            var proje = await repo.GetByIdAsync(request.ProjeId);

            if (proje == null)
                return Result.Failure("Proje bulunamadı.");

            if (proje.DurumId == (int)ProjeDurum.SevkEdildi)
                return Result.Failure("Proje sevk edilmiş durumda. Tarih güncellenemez.");

            var eskiTarih = proje.PlanlananSevkTarihi;
            proje.PlanlananSevkTarihi = request.PlanlananSevkTarihi;
            
            repo.Update(proje);
            await _unitOfWork.SaveChangesAsync();

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = proje.Id,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "Proje",
                ReferansId = proje.Id.ToString(),
                Islem = "Sevk Tarihi Güncellendi",
                IslemTipiId = null,
                EskiDeger = eskiTarih?.ToString("dd.MM.yyyy") ?? "Yok",
                YeniDeger = request.PlanlananSevkTarihi?.ToString("dd.MM.yyyy") ?? "Yok",
                Aciklama = "Projenin planlanan sevk tarihi güncellendi."
            });

            return Result.Success();
        }
    }
}
