using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SandikOzellikGuncelleCommandHandler : IRequestHandler<SandikOzellikGuncelleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISandikService _sandikService;

        public SandikOzellikGuncelleCommandHandler(
            IUnitOfWork unitOfWork,
            ISandikService sandikService)
        {
            _unitOfWork = unitOfWork;
            _sandikService = sandikService;
        }

        public async Task<Result> Handle(SandikOzellikGuncelleCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<Sandik>();
            var sandik = await repo.GetByIdAsync(request.SandikId);

            if (sandik == null)
                return Result.Failure("Sandık bulunamadı", 404);

            if (SandikSevkKilidiHelper.SandikKilitliMi(sandik))
                return Result.Failure(SandikSevkKilidiHelper.SandikKilitliMesaji);

            sandik.Ad = request.SandikIsmi;
            sandik.En = request.En;
            sandik.Boy = request.Boy;
            sandik.Yukseklik = request.Yukseklik;
            sandik.NetKg = request.NetKg;
            sandik.GrossKg = request.GrossKg;
            if (request.DepoLokasyonId.HasValue)
            {
                if (request.DepoLokasyonId.Value != sandik.DepoLokasyonId &&
                    !SandikDepoKurali.BelirsizLokasyonMu(request.DepoLokasyonId.Value))
                {
                    var etkinIceriklerBySandik = await _sandikService
                        .GetEtkinSandikIcerikleriAsync(new[] { sandik.Id }, cancellationToken);
                    var etkinIcerikler = etkinIceriklerBySandik
                        .GetValueOrDefault(sandik.Id) ?? Array.Empty<SandikIcerik>();

                    if (!SandikDepoKurali.DepoLokasyonuAtanabilir(sandik, etkinIcerikler))
                    {
                        return Result.Failure(SandikDepoKurali.LokasyonAtamaUyariMesaji);
                    }
                }

                sandik.DepoLokasyonId = request.DepoLokasyonId.Value;
            }

            repo.Update(sandik);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }
    }
}
