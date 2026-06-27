using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.OnayIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.OnayIslemleri.Queries
{
    public class GetBekleyenOnaylarQueryHandler : IRequestHandler<GetBekleyenOnaylarQuery, Result<List<OnayBekleyenIslemDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IOnayYetkiService _onayYetkiService;

        public GetBekleyenOnaylarQueryHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IOnayYetkiService onayYetkiService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _onayYetkiService = onayYetkiService;
        }

        public async Task<Result<List<OnayBekleyenIslemDto>>> Handle(
            GetBekleyenOnaylarQuery request,
            CancellationToken cancellationToken)
        {
            var kullaniciId = _currentUserService.UserId ?? 0;
            var repo = _unitOfWork.GetRepository<OnayBekleyenIslem>();
            var islemList = await repo.GetAllWithIncludeAsync(o => o.TalepEdenKullanici);
            var bekleyenler = islemList
                .Where(o => o.Durum == OnayDurumu.Bekliyor)
                .OrderBy(o => o.CreatedDate)
                .ToList();

            var filteredList = new List<OnayBekleyenIslemDto>();
            foreach (var islem in bekleyenler)
            {
                var onaylayabilir = await _onayYetkiService.KullaniciIslemOnaylayabilirMiAsync(
                    kullaniciId,
                    islem.IslemKodu,
                    islem.TalepEdenKullaniciId,
                    cancellationToken);

                if (!onaylayabilir)
                    continue;

                filteredList.Add(new OnayBekleyenIslemDto
                {
                    Id = islem.Id,
                    IslemKodu = islem.IslemKodu,
                    IslemAciklamasi = islem.IslemAciklamasi,
                    TalepEdenKisi = islem.TalepEdenKullanici.AdSoyad,
                    OlusturulmaTarihi = islem.CreatedDate,
                    Durum = islem.Durum
                });
            }

            return Result<List<OnayBekleyenIslemDto>>.Success(filteredList);
        }
    }
}
