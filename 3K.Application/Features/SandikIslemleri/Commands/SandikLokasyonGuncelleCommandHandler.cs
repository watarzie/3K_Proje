using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SandikLokasyonGuncelleCommandHandler : IRequestHandler<SandikLokasyonGuncelleCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IHareketService _hareketService;
        private readonly IRolService _rolService;

        public SandikLokasyonGuncelleCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IHareketService hareketService, IRolService rolService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _hareketService = hareketService;
            _rolService = rolService;
        }

        public async Task<Result<bool>> Handle(SandikLokasyonGuncelleCommand request, CancellationToken cancellationToken)
        {
            if (!_currentUserService.IsAuthenticated) return Result<bool>.Failure("Oturum açmanız gerekiyor.");
            
            bool hasPermission = await _rolService.HasPermissionAsync(_currentUserService.Roles, "sandik-yonetimi", "W", cancellationToken);
            if (!hasPermission) return Result<bool>.Failure("Lokasyon güncelleme yetkiniz bulunmuyor.", 403);

            if (request.SandikIds == null || !request.SandikIds.Any())
            {
                return Result<bool>.Failure("Güncellenecek sandık seçilmedi.");
            }

            var repo = _unitOfWork.GetRepository<Sandik>();
            var sandiklar = await repo.FindAsync(s => request.SandikIds.Contains(s.Id));

            if (!sandiklar.Any())
            {
                return Result<bool>.Failure("Sandıklar bulunamadı.");
            }

            foreach (var sandik in sandiklar)
            {
                var eskiLokasyon = sandik.DepoLokasyonId;
                sandik.DepoLokasyonId = request.DepoLokasyonId;
                repo.Update(sandik);

                await _hareketService.HareketKaydetAsync(new HareketGecmisi
                {
                    ProjeId = sandik.ProjeId,
                    KullaniciId = _currentUserService.UserId ?? 0,
                    ReferansTipi = "Sandik",
                    ReferansId = sandik.Id.ToString(),
                    Islem = "Lokasyon Güncelleme",
                    EskiDeger = eskiLokasyon.ToString(),
                    YeniDeger = sandik.DepoLokasyonId.ToString(),
                    Aciklama = $"Sandık lokasyonu '{eskiLokasyon}' değerinden '{sandik.DepoLokasyonId}' olarak değiştirildi."
                });
            }

            await _unitOfWork.SaveChangesAsync();
            return Result<bool>.Success(true);
        }
    }
}
