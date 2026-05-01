using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    public class ProjeKilidiAcCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin };

        public int ProjeId { get; set; }
    }

    public class ProjeKilidiAcCommandHandler : IRequestHandler<ProjeKilidiAcCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ICurrentUserService _currentUserService;

        public ProjeKilidiAcCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(ProjeKilidiAcCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<Proje>();
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var proje = await repo.GetByIdAsync(request.ProjeId);

            if (proje == null)
                return Result.Failure("Proje bulunamadı.");

            if (proje.DurumId != (int)ProjeDurum.SevkEdildi)
                return Result.Failure("Proje sevk edilmediği için kilidi açılamaz.");

            int eskiDurum = proje.DurumId;
            proje.DurumId = (int)ProjeDurum.Hazirlaniyor; // Kilidi açınca Hazırlanıyor durumuna geçsin

            repo.Update(proje);

            // ===== Sandıkları sevk öncesi durumlarına geri döndür =====
            var sandiklar = await sandikRepo.FindAsync(s => s.ProjeId == request.ProjeId);
            int geriDondurulenSandik = 0;
            foreach (var sandik in sandiklar)
            {
                if (sandik.DurumId == (int)SandikDurum.Sevkedildi)
                {
                    // Sevk öncesi durum varsa ona dön, yoksa Hazır'a dön
                    sandik.DurumId = sandik.SevkOncesiDurumId ?? (int)SandikDurum.Hazir;
                    sandik.SevkOncesiDurumId = null; // Temizle
                    sandikRepo.Update(sandik);
                    geriDondurulenSandik++;
                }
            }

            await _unitOfWork.SaveChangesAsync();

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = proje.Id,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "Proje",
                ReferansId = proje.Id.ToString(),
                Islem = "Proje Kilidi Açıldı",
                IslemTipiId = null,
                EskiDeger = eskiDurum.ToString(),
                YeniDeger = proje.DurumId.ToString(),
                Aciklama = $"Proje kilidi Admin tarafından açıldı. {geriDondurulenSandik} sandık eski durumuna döndürüldü."
            });

            return Result.Success();
        }
    }
}
