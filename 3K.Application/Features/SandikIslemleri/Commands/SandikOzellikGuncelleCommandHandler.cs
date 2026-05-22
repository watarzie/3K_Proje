using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SandikOzellikGuncelleCommandHandler : IRequestHandler<SandikOzellikGuncelleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;

        public SandikOzellikGuncelleCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(SandikOzellikGuncelleCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<Sandik>();
            var sandik = await repo.GetByIdAsync(request.SandikId);

            if (sandik == null)
                return Result.Failure("Sandık bulunamadı", 404);
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
                    var icerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
                    var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();

                    var icerikler = (await icerikRepo.FindAsync(i => i.SandikId == sandik.Id)).ToList();
                    var cekiSatiriIdler = icerikler
                        .Where(i => i.CekiSatiriId.HasValue)
                        .Select(i => i.CekiSatiriId!.Value)
                        .Distinct()
                        .ToList();

                    var cekiSatirlari = cekiSatiriIdler.Count == 0
                        ? new Dictionary<int, CekiSatiri>()
                        : (await cekiSatiriRepo.FindAsync(c => cekiSatiriIdler.Contains(c.Id))).ToDictionary(c => c.Id);

                    if (!SandikDepoKurali.DepoLokasyonuAtanabilir(sandik, icerikler, cekiSatirlari))
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
