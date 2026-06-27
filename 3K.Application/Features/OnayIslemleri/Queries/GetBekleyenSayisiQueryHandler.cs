using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.OnayIslemleri.Queries
{
    public class GetBekleyenSayisiQueryHandler : IRequestHandler<GetBekleyenSayisiQuery, Result<int>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IOnayYetkiService _onayYetkiService;

        public GetBekleyenSayisiQueryHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IOnayYetkiService onayYetkiService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _onayYetkiService = onayYetkiService;
        }

        public async Task<Result<int>> Handle(GetBekleyenSayisiQuery request, CancellationToken cancellationToken)
        {
            var kullaniciId = _currentUserService.UserId ?? 0;
            var repo = _unitOfWork.GetRepository<OnayBekleyenIslem>();
            var bekleyenler = await repo.FindAsync(o => o.Durum == OnayDurumu.Bekliyor);

            var count = 0;
            foreach (var islem in bekleyenler)
            {
                var onaylayabilir = await _onayYetkiService.KullaniciIslemOnaylayabilirMiAsync(
                    kullaniciId,
                    islem.IslemKodu,
                    islem.TalepEdenKullaniciId,
                    cancellationToken);

                if (onaylayabilir)
                    count++;
            }

            return Result<int>.Success(count);
        }
    }
}
