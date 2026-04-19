using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.OnayIslemleri.Queries
{
    public class OnayKuraliDto
    {
        public int LookupUcKDurumId { get; set; }
        public string IslemAdi { get; set; } = string.Empty;
        public bool OnayGerektirirMi { get; set; }
    }

    public class GetOnayKurallariQuery : IRequest<Result<List<OnayKuraliDto>>>
    {
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

            var list = rules.Select(r => new OnayKuraliDto
            {
                LookupUcKDurumId = r.LookupUcKDurumId,
                IslemAdi = r.LookupUcKDurum?.Deger ?? "Bilinmiyor",
                OnayGerektirirMi = r.OnayGerektirirMi
            }).OrderBy(r => r.LookupUcKDurumId).ToList();

            return Result<List<OnayKuraliDto>>.Success(list);
        }
    }
}
