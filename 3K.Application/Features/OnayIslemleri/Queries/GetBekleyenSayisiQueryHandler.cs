using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.OnayIslemleri.Queries
{
    public class GetBekleyenSayisiQueryHandler : IRequestHandler<GetBekleyenSayisiQuery, Result<int>>
    {
        private readonly IOnayIslemRepository _onayRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IOnayYetkiService _onayYetkiService;

        public GetBekleyenSayisiQueryHandler(
            IOnayIslemRepository onayRepository,
            ICurrentUserService currentUserService,
            IOnayYetkiService onayYetkiService)
        {
            _onayRepository = onayRepository;
            _currentUserService = currentUserService;
            _onayYetkiService = onayYetkiService;
        }

        public async Task<Result<int>> Handle(
            GetBekleyenSayisiQuery request,
            CancellationToken cancellationToken)
        {
            var kullaniciId = _currentUserService.UserId;
            if (!kullaniciId.HasValue)
                return Result<int>.Failure("Kullanıcı bilgisi alınamadı.", 401);

            var erisimKapsami = await _onayYetkiService.GetErisimKapsamiAsync(
                kullaniciId.Value,
                cancellationToken);
            var sayi = await _onayRepository.GetYetkiliBekleyenSayisiAsync(
                kullaniciId.Value,
                erisimKapsami,
                cancellationToken);

            return Result<int>.Success(sayi);
        }
    }
}
