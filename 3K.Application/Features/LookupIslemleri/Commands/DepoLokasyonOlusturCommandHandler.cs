using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.LookupIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.LookupIslemleri.Commands
{
    public class DepoLokasyonOlusturCommandHandler : IRequestHandler<DepoLokasyonOlusturCommand, Result<LookupItemDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILookupCacheService _lookupCache;

        public DepoLokasyonOlusturCommandHandler(IUnitOfWork unitOfWork, ILookupCacheService lookupCache)
        {
            _unitOfWork = unitOfWork;
            _lookupCache = lookupCache;
        }

        public async Task<Result<LookupItemDto>> Handle(DepoLokasyonOlusturCommand request, CancellationToken cancellationToken)
        {
            var deger = (request.Deger ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(deger))
                return Result<LookupItemDto>.Failure("Depo adı boş olamaz.");

            if (deger.Length > 80)
                return Result<LookupItemDto>.Failure("Depo adı en fazla 80 karakter olabilir.");

            var repo = _unitOfWork.GetRepository<LookupDepoLokasyon>();
            var normalized = deger.ToLower();
            var mevcutLokasyonlar = (await repo.FindAsync(x => true)).ToList();
            var exists = mevcutLokasyonlar.Any(x => x.Deger.ToLower() == normalized);

            if (exists)
                return Result<LookupItemDto>.Failure("Bu depo lokasyonu zaten tanımlı.");

            var nextAnahtar = mevcutLokasyonlar.Count > 0
                ? mevcutLokasyonlar.Max(x => x.Anahtar)
                : 0;

            var lokasyon = new LookupDepoLokasyon
            {
                Anahtar = nextAnahtar + 1,
                Deger = deger
            };

            await repo.AddAsync(lokasyon);
            await _unitOfWork.SaveChangesAsync();
            await _lookupCache.RefreshAsync<LookupDepoLokasyon>(cancellationToken);

            return Result<LookupItemDto>.Success(new LookupItemDto
            {
                Id = lokasyon.Id,
                Anahtar = lokasyon.Anahtar,
                Deger = lokasyon.Deger
            }, 201);
        }
    }
}
