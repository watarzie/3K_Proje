using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// Tekil sandık sevk komutu. Sandığı "Sevk Edildi" durumuna geçirir.
    /// </summary>
    public class SandikSevkEtCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin };

        public int ProjeId { get; set; }
        public int SandikId { get; set; }
    }

    public class SandikSevkEtCommandHandler : IRequestHandler<SandikSevkEtCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ICurrentUserService _currentUserService;

        public SandikSevkEtCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(SandikSevkEtCommand request, CancellationToken cancellationToken)
        {
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandik = await sandikRepo.GetByIdAsync(request.SandikId);

            if (sandik == null || sandik.ProjeId != request.ProjeId)
                return Result.Failure("Sandık bulunamadı veya projeye ait değil.", 404);

            if (sandik.DurumId == (int)SandikDurum.Sevkedildi)
                return Result.Failure("Sandık zaten sevk edilmiş durumda.");

            int eskiDurum = sandik.DurumId;
            sandik.DurumId = (int)SandikDurum.Sevkedildi;
            sandikRepo.Update(sandik);

            await _unitOfWork.SaveChangesAsync();

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "Sandik",
                ReferansId = sandik.Id.ToString(),
                Islem = "Sandık Sevk Edildi",
                IslemTipiId = (int)IslemTipi.SandikSevkEdildi,
                EskiDeger = eskiDurum.ToString(),
                YeniDeger = sandik.DurumId.ToString(),
                Aciklama = $"Sandık {sandik.SandikNo} sevk edildi."
            });

            return Result.Success();
        }
    }
}
