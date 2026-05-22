using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.LookupIslemleri.Commands
{
    public class DepoLokasyonSilCommandHandler : IRequestHandler<DepoLokasyonSilCommand, Result>
    {
        private static readonly HashSet<int> SistemLokasyonlari = new()
        {
            (int)DepoLokasyon.Belirsiz,
            (int)DepoLokasyon.UcK,
            (int)DepoLokasyon.Seymen,
            (int)DepoLokasyon.Grid
        };

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILookupCacheService _lookupCache;

        public DepoLokasyonSilCommandHandler(IUnitOfWork unitOfWork, ILookupCacheService lookupCache)
        {
            _unitOfWork = unitOfWork;
            _lookupCache = lookupCache;
        }

        public async Task<Result> Handle(DepoLokasyonSilCommand request, CancellationToken cancellationToken)
        {
            if (SistemLokasyonlari.Contains(request.Id))
                return Result.Failure("Sistem depo lokasyonları silinemez.");

            var lokasyonRepo = _unitOfWork.GetRepository<LookupDepoLokasyon>();
            var lokasyon = await lokasyonRepo.GetByIdAsync(request.Id);
            if (lokasyon == null)
                return Result.Failure("Depo lokasyonu bulunamadı.", 404);

            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var kullaniliyor = (await sandikRepo.FindAsync(s => s.DepoLokasyonId == request.Id)).Any();

            if (kullaniliyor)
                return Result.Failure("Bu depoda sandık bulunduğu için silinemez. Önce sandıkları farklı bir depoya taşıyın.");

            lokasyonRepo.Remove(lokasyon);
            await _unitOfWork.SaveChangesAsync();
            await _lookupCache.RefreshAsync<LookupDepoLokasyon>(cancellationToken);

            return Result.Success();
        }
    }
}
