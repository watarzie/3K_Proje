using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SandikSevkiyatDuzeltmeTamamlaCommand : IRequest<Result>, ISecuredRequest
    {
        public int ProjeId { get; set; }
        public int SandikId { get; set; }
    }

    public class SandikSevkiyatDuzeltmeTamamlaCommandHandler : IRequestHandler<SandikSevkiyatDuzeltmeTamamlaCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;
        private readonly IReadQueryExecutor _readQueries;

        public SandikSevkiyatDuzeltmeTamamlaCommandHandler(
            IUnitOfWork unitOfWork,
            IHareketService hareketService,
            ICurrentUserService currentUserService,
            ISahaTamamlamaService sahaTamamlamaService,
            IReadQueryExecutor readQueries)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _currentUserService = currentUserService;
            _sahaTamamlamaService = sahaTamamlamaService;
            _readQueries = readQueries;
        }

        public async Task<Result> Handle(SandikSevkiyatDuzeltmeTamamlaCommand request, CancellationToken cancellationToken)
        {
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var projeRepo = _unitOfWork.GetRepository<Proje>();

            if (!_currentUserService.UserId.HasValue)
                return Result.Failure("Oturum açmanız gerekiyor.", 401);

            var sandik = await sandikRepo.GetByIdAsync(request.SandikId);
            if (sandik == null || sandik.ProjeId != request.ProjeId)
                return Result.Failure("Sandık bulunamadı veya projeye ait değil.", 404);

            if (sandik.DurumId != (int)SandikDurum.Sevkedildi)
                return Result.Failure("Sadece sevk edilmiş sandıklarda düzeltme tamamlanabilir.");

            if (!sandik.SevkiyatDuzeltmeAcikMi)
                return Result.Failure("Bu sandıkta açık bir sevkiyat düzeltmesi bulunmuyor.");

            var proje = await projeRepo.GetByIdAsync(request.ProjeId);
            if (proje == null)
                return Result.Failure("Proje bulunamadı.", 404);

            var eskiDeger = sandik.SevkiyatDuzeltmeAcikMi;
            sandik.SevkiyatDuzeltmeAcikMi = false;

            if (proje.ProjeTipiId == (int)ProjeTipi.Normal)
            {
                var sandiklar = await _readQueries.ToListAsync(
                    _readQueries.AsNoTracking(sandikRepo.Queryable())
                        .Where(s => s.ProjeId == proje.Id), cancellationToken);
                var sahaDurumu = await _sahaTamamlamaService.GetKaynakSandikSahaAktarimDurumuAsync(
                    sandiklar.Select(s => s.Id), cancellationToken);
                var yeniDurum = await NormalProjeSevkDurumHesaplayici.HesaplaAsync(
                    _unitOfWork, _readQueries, _sahaTamamlamaService,
                    proje.Id, proje.DurumId, sandiklar,
                    sahaDurumu.SahaUzerindenSevkEdilenSandikIds, cancellationToken);

                if (yeniDurum != proje.DurumId)
                {
                    proje.DurumId = yeniDurum;
                    projeRepo.Update(proje);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (proje.ProjeTipiId == (int)ProjeTipi.Saha)
            {
                await _sahaTamamlamaService.SenkronizeKaynakProjelerBySahaSandikIdsAsync(
                    new[] { sandik.Id },
                    cancellationToken);
            }

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId.Value,
                ReferansTipi = "Sandik",
                ReferansId = sandik.Id.ToString(),
                Islem = "Sevkiyat Düzeltmesi Tamamlandı",
                IslemTipiId = null,
                EskiDeger = $"SevkiyatDuzeltmeAcikMi:{eskiDeger}",
                YeniDeger = $"SevkiyatDuzeltmeAcikMi:{sandik.SevkiyatDuzeltmeAcikMi}",
                Aciklama = $"Sandık {sandik.SandikNo} için sevkiyat kaydı korunarak açılan düzeltme tamamlandı. Sandık sevk edilmiş durumda kilitlendi."
            });

            return Result.Success();
        }
    }
}
