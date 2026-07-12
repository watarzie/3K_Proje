using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.OnayIslemleri.DTOs;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.OnayIslemleri.Queries
{
    public class GetBekleyenOnaylarQueryHandler
        : IRequestHandler<GetBekleyenOnaylarQuery, Result<List<OnayBekleyenIslemDto>>>
    {
        private readonly IOnayIslemRepository _onayRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IOnayYetkiService _onayYetkiService;

        public GetBekleyenOnaylarQueryHandler(
            IOnayIslemRepository onayRepository,
            ICurrentUserService currentUserService,
            IOnayYetkiService onayYetkiService)
        {
            _onayRepository = onayRepository;
            _currentUserService = currentUserService;
            _onayYetkiService = onayYetkiService;
        }

        public async Task<Result<List<OnayBekleyenIslemDto>>> Handle(
            GetBekleyenOnaylarQuery request,
            CancellationToken cancellationToken)
        {
            var kullaniciId = _currentUserService.UserId;
            if (!kullaniciId.HasValue)
                return Result<List<OnayBekleyenIslemDto>>.Failure("Kullanıcı bilgisi alınamadı.", 401);

            var erisimKapsami = await _onayYetkiService.GetErisimKapsamiAsync(
                kullaniciId.Value,
                cancellationToken);
            var bekleyenler = await _onayRepository.GetYetkiliBekleyenlerAsync(
                kullaniciId.Value,
                erisimKapsami,
                cancellationToken);

            return Result<List<OnayBekleyenIslemDto>>.Success(
                bekleyenler.Select(islem => new OnayBekleyenIslemDto
                {
                    Id = islem.Id,
                    IslemKodu = islem.IslemKodu,
                    IslemAciklamasi = islem.IslemAciklamasi,
                    TalepEdenKisi = islem.TalepEdenKisi,
                    OlusturulmaTarihi = islem.OlusturulmaTarihi,
                    Durum = islem.Durum
                }).ToList());
        }
    }
}
