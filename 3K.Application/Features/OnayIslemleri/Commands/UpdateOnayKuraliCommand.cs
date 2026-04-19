using MediatR;
using Microsoft.Extensions.Caching.Memory;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.OnayIslemleri.Commands
{
    public class UpdateOnayKuraliCommand : IRequest<Result>
    {
        public int LookupUcKDurumId { get; set; }
        public bool OnayGerektirirMi { get; set; }
    }

    public class UpdateOnayKuraliCommandHandler : IRequestHandler<UpdateOnayKuraliCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;

        public UpdateOnayKuraliCommandHandler(IUnitOfWork unitOfWork, IMemoryCache cache)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
        }

        public async Task<Result> Handle(UpdateOnayKuraliCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<IslemOnayKurali>();
            var rules = await repo.FindAsync(r => r.LookupUcKDurumId == request.LookupUcKDurumId);
            var rule = rules.FirstOrDefault();

            if (rule == null)
            {
                return Result.Failure("Kural bulunamadı.", 404);
            }

            rule.OnayGerektirirMi = request.OnayGerektirirMi;
            repo.Update(rule);
            await _unitOfWork.SaveChangesAsync();

            var lookupRepo = _unitOfWork.GetRepository<LookupUcKDurum>();
            var lookup = await lookupRepo.GetByIdAsync(request.LookupUcKDurumId);

            // Clear the memory cache instantly
            if (lookup != null)
            {
                _cache.Remove($"ApprovalRule_UcK_{lookup.Deger}");
            }

            return Result.Success();
        }
    }
}
