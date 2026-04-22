using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    public class ProjeSevkEtCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin };
        // UI'da W yetkisi olanlar tetikleyebilecek, backend'de yetki yönetimi Pipeline üzerinden de yapılıyor.

        public int ProjeId { get; set; }
    }

    public class ProjeSevkEtCommandHandler : IRequestHandler<ProjeSevkEtCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ICurrentUserService _currentUserService;

        public ProjeSevkEtCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(ProjeSevkEtCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<Proje>();
            var proje = await repo.GetByIdAsync(request.ProjeId);

            if (proje == null)
                return Result.Failure("Proje bulunamadı.");

            if (proje.DurumId == (int)ProjeDurum.SevkEdildi)
                return Result.Failure("Proje zaten sevk edilmiş durumda.");

            int eskiDurum = proje.DurumId;
            proje.DurumId = (int)ProjeDurum.SevkEdildi;

            repo.Update(proje);
            await _unitOfWork.SaveChangesAsync();

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = proje.Id,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "Proje",
                ReferansId = proje.Id.ToString(),
                Islem = "Proje Kilitlendi / Sevk Edildi",
                IslemTipiId = null, // Özel işlem tipi yok, ismi görünsün
                EskiDeger = eskiDurum.ToString(),
                YeniDeger = proje.DurumId.ToString(),
                Aciklama = "Proje sevk edildi ve kilitlendi. Üzerinde hiçbir işlem yapılamaz."
            });

            return Result.Success();
        }
    }
}
