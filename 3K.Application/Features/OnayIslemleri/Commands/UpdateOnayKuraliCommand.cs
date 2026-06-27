using MediatR;
using Microsoft.Extensions.Caching.Memory;
using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.OnayIslemleri.Commands
{
    public class UpdateOnayKuraliCommand : IRequest<Result>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "onay-kurallari-yonet";
        public int? LookupUcKDurumId { get; set; }
        public string? IslemKodu { get; set; }
        public bool OnayGerektirirMi { get; set; }
        public List<int>? YetkiliRolIdleri { get; set; }
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
            var islemKodu = ResolveIslemKodu(request);

            if (string.IsNullOrWhiteSpace(islemKodu))
            {
                return Result.Failure("İşlem kodu belirlenemedi.", 400);
            }

            if (request.LookupUcKDurumId.HasValue)
            {
                var repo = _unitOfWork.GetRepository<IslemOnayKurali>();
                var rules = await repo.FindAsync(r => r.LookupUcKDurumId == request.LookupUcKDurumId.Value);
                var rule = rules.FirstOrDefault();

                if (rule == null)
                {
                    return Result.Failure("Kural bulunamadı.", 404);
                }

                rule.OnayGerektirirMi = request.OnayGerektirirMi;
                repo.Update(rule);

                _cache.Remove($"ApprovalRule_UcK_{request.LookupUcKDurumId.Value}");

                var lookupRepo = _unitOfWork.GetRepository<LookupUcKDurum>();
                var lookup = await lookupRepo.GetByIdAsync(request.LookupUcKDurumId.Value);

                // Clear legacy string-keyed cache entries from older runtime versions too.
                if (lookup != null)
                {
                    _cache.Remove($"ApprovalRule_UcK_{lookup.Deger}");
                }
            }

            if (request.YetkiliRolIdleri != null)
            {
                var yetkiResult = await YetkiliRolleriGuncelleAsync(islemKodu, request.YetkiliRolIdleri);
                if (!yetkiResult.IsSuccess)
                {
                    return yetkiResult;
                }
            }

            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        private static string ResolveIslemKodu(UpdateOnayKuraliCommand request)
        {
            if (!string.IsNullOrWhiteSpace(request.IslemKodu))
                return request.IslemKodu.Trim();

            return request.LookupUcKDurumId.HasValue
                ? OnayIslemKodlari.FromUcKDurumId(request.LookupUcKDurumId.Value)
                : string.Empty;
        }

        private async Task<Result> YetkiliRolleriGuncelleAsync(string islemKodu, IEnumerable<int> rolIdleri)
        {
            var distinctRolIdleri = rolIdleri
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            var rolRepo = _unitOfWork.GetRepository<Rol>();
            var mevcutRolIdleri = (await rolRepo.GetAllAsync())
                .Select(r => r.Id)
                .ToHashSet();

            if (distinctRolIdleri.Any(id => !mevcutRolIdleri.Contains(id)))
            {
                return Result.Failure("Geçersiz rol seçimi yapıldı.", 400);
            }

            var yetkiRepo = _unitOfWork.GetRepository<OnayIslemYetki>();
            var mevcutYetkiler = await yetkiRepo.FindAsync(y => y.IslemKodu == islemKodu);

            foreach (var yetki in mevcutYetkiler)
            {
                yetkiRepo.Remove(yetki);
            }

            foreach (var rolId in distinctRolIdleri)
            {
                await yetkiRepo.AddAsync(new OnayIslemYetki
                {
                    IslemKodu = islemKodu,
                    RolId = rolId
                });
            }

            return Result.Success();
        }
    }
}
