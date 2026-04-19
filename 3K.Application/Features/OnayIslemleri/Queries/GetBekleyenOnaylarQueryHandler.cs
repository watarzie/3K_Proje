using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.OnayIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.OnayIslemleri.Queries
{
    public class GetBekleyenOnaylarQueryHandler : IRequestHandler<GetBekleyenOnaylarQuery, Result<List<OnayBekleyenIslemDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetBekleyenOnaylarQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<OnayBekleyenIslemDto>>> Handle(GetBekleyenOnaylarQuery request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<OnayBekleyenIslem>();
            var islemList = await repo.GetAllWithIncludeAsync(o => o.TalepEdenKullanici);
            
            var filteredList = islemList
                .Where(o => o.Durum == Core.Enums.OnayDurumu.Bekliyor)
                .OrderBy(o => o.CreatedDate)
                .Select(o => new OnayBekleyenIslemDto
                {
                    Id = o.Id,
                    IslemAciklamasi = o.IslemAciklamasi,
                    TalepEdenKisi = o.TalepEdenKullanici.AdSoyad,
                    OlusturulmaTarihi = o.CreatedDate,
                    Durum = o.Durum
                }).ToList();

            return Result<List<OnayBekleyenIslemDto>>.Success(filteredList);
        }
    }
}
