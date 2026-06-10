using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SandikKilidiAcCommand : IRequest<Result>, ISecuredRequest
    {
        public int ProjeId { get; set; }
        public int SandikId { get; set; }
        public string? Aciklama { get; set; }
    }

    public class SandikKilidiAcCommandHandler : IRequestHandler<SandikKilidiAcCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ICurrentUserService _currentUserService;

        public SandikKilidiAcCommandHandler(
            IUnitOfWork unitOfWork,
            IHareketService hareketService,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(SandikKilidiAcCommand request, CancellationToken cancellationToken)
        {
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var sevkiyatSandikRepo = _unitOfWork.GetRepository<SevkiyatSandik>();

            var sandik = await sandikRepo.GetByIdAsync(request.SandikId);
            if (sandik == null || sandik.ProjeId != request.ProjeId)
                return Result.Failure("Sandık bulunamadı veya projeye ait değil.", 404);

            if (sandik.DurumId != (int)SandikDurum.Sevkedildi)
                return Result.Failure("Sandık sevk edilmiş durumda olmadığı için kilidi açılamaz.");

            var proje = await projeRepo.GetByIdAsync(request.ProjeId);
            if (proje == null)
                return Result.Failure("Proje bulunamadı.", 404);

            var eskiSandikDurum = sandik.DurumId;
            var eskiProjeDurum = proje.DurumId;
            var yeniSandikDurum = sandik.SevkOncesiDurumId ?? (int)SandikDurum.Kapandi;

            sandik.DurumId = yeniSandikDurum;
            sandik.SevkOncesiDurumId = null;
            sandikRepo.Update(sandik);

            var sevkiyatBaglari = (await sevkiyatSandikRepo.FindAsync(ss => ss.SandikId == sandik.Id)).ToList();
            foreach (var sevkiyatBag in sevkiyatBaglari)
                sevkiyatSandikRepo.Remove(sevkiyatBag);

            var sandiklar = (await sandikRepo.FindAsync(s => s.ProjeId == request.ProjeId)).ToList();
            var guncelSandik = sandiklar.FirstOrDefault(s => s.Id == sandik.Id);
            if (guncelSandik != null)
            {
                guncelSandik.DurumId = yeniSandikDurum;
                guncelSandik.SevkOncesiDurumId = null;
            }

            proje.DurumId = ProjeSevkDurumHelper.Hesapla(sandiklar, proje.DurumId);
            if (sandiklar.All(s => s.DurumId != (int)SandikDurum.Sevkedildi))
                proje.GerceklesenSevkTarihi = null;
            projeRepo.Update(proje);

            await _unitOfWork.SaveChangesAsync();

            var aciklamaNotu = string.IsNullOrWhiteSpace(request.Aciklama)
                ? "Açıklama yok"
                : request.Aciklama.Trim();

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "Sandik",
                ReferansId = sandik.Id.ToString(),
                Islem = "Sandık Kilidi Açıldı",
                IslemTipiId = null,
                EskiDeger = $"SandikDurum:{eskiSandikDurum}, ProjeDurum:{eskiProjeDurum}",
                YeniDeger = $"SandikDurum:{sandik.DurumId}, ProjeDurum:{proje.DurumId}",
                Aciklama = $"Sandık {sandik.SandikNo} sevk kilidi açıldı. Not: {aciklamaNotu}"
            });

            return Result.Success();
        }
    }
}
