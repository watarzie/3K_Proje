using MediatR;
using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.OnayIslemleri.Queries
{
    public class OnayKuraliDto
    {
        public int? LookupUcKDurumId { get; set; }
        public string IslemKodu { get; set; } = string.Empty;
        public string IslemAdi { get; set; } = string.Empty;
        public bool OnayGerektirirMi { get; set; }
        public bool OnayGerektirirMiDegistirilebilir { get; set; } = true;
        public List<int> YetkiliRolIdleri { get; set; } = new();
    }

    public class GetOnayKurallariQuery : IRequest<Result<List<OnayKuraliDto>>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "onay-kurallari-yonet";
    }

    public class GetOnayKurallariQueryHandler : IRequestHandler<GetOnayKurallariQuery, Result<List<OnayKuraliDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetOnayKurallariQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<OnayKuraliDto>>> Handle(GetOnayKurallariQuery request, CancellationToken cancellationToken)
        {
            var ruleRepo = _unitOfWork.GetRepository<IslemOnayKurali>();
            var rules = await ruleRepo.GetAllWithIncludeAsync(r => r.LookupUcKDurum);

            var yetkiRepo = _unitOfWork.GetRepository<OnayIslemYetki>();
            var yetkiler = await yetkiRepo.GetAllAsync();
            var rolMap = yetkiler
                .GroupBy(y => y.IslemKodu)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(y => y.RolId).Distinct().OrderBy(id => id).ToList());

            var list = rules
                .Select(r =>
                {
                    var islemKodu = OnayIslemKodlari.FromUcKDurumId(r.LookupUcKDurumId);

                    return new OnayKuraliDto
                    {
                        LookupUcKDurumId = r.LookupUcKDurumId,
                        IslemKodu = islemKodu,
                        IslemAdi = r.LookupUcKDurum?.Deger ?? OnayIslemKodlari.DisplayName(islemKodu),
                        OnayGerektirirMi = r.OnayGerektirirMi,
                        OnayGerektirirMiDegistirilebilir = true,
                        YetkiliRolIdleri = rolMap.TryGetValue(islemKodu, out var rolIdleri) ? rolIdleri : new List<int>()
                    };
                })
                .OrderBy(r => r.LookupUcKDurumId)
                .ToList();

            foreach (var sabitIslem in OnayIslemKodlari.SabitOnayIslemleri)
            {
                list.Add(new OnayKuraliDto
                {
                    LookupUcKDurumId = null,
                    IslemKodu = sabitIslem.IslemKodu,
                    IslemAdi = sabitIslem.IslemAdi,
                    OnayGerektirirMi = true,
                    OnayGerektirirMiDegistirilebilir = false,
                    YetkiliRolIdleri = rolMap.TryGetValue(sabitIslem.IslemKodu, out var rolIdleri)
                        ? rolIdleri
                        : new List<int>()
                });
            }

            var bilinenKodlar = list
                .Select(k => k.IslemKodu)
                .Where(kod => !string.IsNullOrWhiteSpace(kod))
                .ToHashSet();

            var bekleyenRepo = _unitOfWork.GetRepository<OnayBekleyenIslem>();
            var bekleyenIslemler = await bekleyenRepo.GetAllAsync();
            var ekstraKodlar = yetkiler
                .Select(y => y.IslemKodu)
                .Concat(bekleyenIslemler.Select(i => i.IslemKodu))
                .Where(kod => !string.IsNullOrWhiteSpace(kod) && !bilinenKodlar.Contains(kod))
                .Distinct()
                .OrderBy(kod => kod);

            foreach (var ekstraKod in ekstraKodlar)
            {
                list.Add(new OnayKuraliDto
                {
                    LookupUcKDurumId = null,
                    IslemKodu = ekstraKod,
                    IslemAdi = OnayIslemKodlari.DisplayName(ekstraKod),
                    OnayGerektirirMi = true,
                    OnayGerektirirMiDegistirilebilir = false,
                    YetkiliRolIdleri = rolMap.TryGetValue(ekstraKod, out var rolIdleri)
                        ? rolIdleri
                        : new List<int>()
                });
            }

            return Result<List<OnayKuraliDto>>.Success(list);
        }
    }
}
