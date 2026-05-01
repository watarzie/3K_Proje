using MediatR;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Enums;
using _3K.Application.Common;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SandikSilCommandHandler : IRequestHandler<SandikSilCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ICurrentUserService _currentUserService;

        public SandikSilCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(SandikSilCommand request, CancellationToken cancellationToken)
        {
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandik = await sandikRepo.GetByIdAsync(request.SandikId);

            if (sandik == null)
                return Result.Failure("Sandık bulunamadı.", 404);

            if (sandik.ProjeId != request.ProjeId)
                return Result.Failure("Sandık bu projeye ait değil.");

            // İçinde ürün var mı kontrol et
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var icerikler = await sandikIcerikRepo.FindAsync(x => x.SandikId == sandik.Id);
            if (icerikler.Any())
                return Result.Failure($"Bu sandıkta {icerikler.Count()} ürün bulunuyor. Önce ürünleri silin veya taşıyın.");

            var sandikNo = sandik.SandikNo;

            sandikRepo.Remove(sandik);
            await _unitOfWork.SaveChangesAsync();

            // Hareket kaydı
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "Sandik",
                ReferansId = request.SandikId.ToString(),
                Islem = "Sandık Silindi",
                IslemTipiId = (int)IslemTipi.SandikSilindi,
                Aciklama = $"Sandık {sandikNo} silindi."
            });

            return Result.Success();
        }
    }
}
