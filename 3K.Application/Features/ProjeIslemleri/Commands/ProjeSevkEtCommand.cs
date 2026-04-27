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
            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();

            var proje = await projeRepo.GetByIdAsync(request.ProjeId);

            if (proje == null)
                return Result.Failure("Proje bulunamadı.");

            if (proje.DurumId == (int)ProjeDurum.SevkEdildi)
                return Result.Failure("Proje zaten sevk edilmiş durumda.");

            int eskiDurum = proje.DurumId;
            proje.DurumId = (int)ProjeDurum.SevkEdildi;
            proje.GerceklesenSevkTarihi = DateTime.UtcNow.AddHours(3); // Türkiye saati
            projeRepo.Update(proje);

            // ===== Tüm sandıkları "Sevk Edildi" durumuna geçir =====
            var sandiklar = await sandikRepo.FindAsync(s => s.ProjeId == request.ProjeId);
            int sevkEdilenSandikSayisi = 0;
            foreach (var sandik in sandiklar)
            {
                if (sandik.DurumId != (int)SandikDurum.Sevkedildi)
                {
                    sandik.DurumId = (int)SandikDurum.Sevkedildi;
                    sandikRepo.Update(sandik);
                    sevkEdilenSandikSayisi++;
                }
            }

            await _unitOfWork.SaveChangesAsync();

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = proje.Id,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "Proje",
                ReferansId = proje.Id.ToString(),
                Islem = "Proje Sevk Edildi",
                IslemTipiId = (int)IslemTipi.ProjeSevkEdildi,
                EskiDeger = eskiDurum.ToString(),
                YeniDeger = proje.DurumId.ToString(),
                Aciklama = $"Proje sevk edildi ve kilitlendi. {sevkEdilenSandikSayisi} sandık sevk edildi."
            });

            return Result.Success();
        }
    }
}

