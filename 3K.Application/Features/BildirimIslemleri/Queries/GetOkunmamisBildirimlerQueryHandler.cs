using _3K.Application.Common;
using _3K.Application.Features.BildirimIslemleri.DTOs;
using _3K.Core.Interfaces;
using MediatR;

namespace _3K.Application.Features.BildirimIslemleri.Queries
{
    public class GetOkunmamisBildirimlerQueryHandler
        : IRequestHandler<GetOkunmamisBildirimlerQuery, Result<BildirimOzetDto>>
    {
        private readonly IBildirimRepository _bildirimRepository;
        private readonly ICurrentUserService _currentUserService;

        public GetOkunmamisBildirimlerQueryHandler(
            IBildirimRepository bildirimRepository,
            ICurrentUserService currentUserService)
        {
            _bildirimRepository = bildirimRepository;
            _currentUserService = currentUserService;
        }

        public async Task<Result<BildirimOzetDto>> Handle(
            GetOkunmamisBildirimlerQuery request,
            CancellationToken cancellationToken)
        {
            var kullaniciId = _currentUserService.UserId;
            if (!kullaniciId.HasValue)
                return Result<BildirimOzetDto>.Failure("Kullanıcı bilgisi alınamadı.", 401);

            var limit = Math.Clamp(request.Limit, 1, 50);
            var (bildirimler, toplam) = await _bildirimRepository.GetOkunmamisAsync(
                kullaniciId.Value,
                limit,
                cancellationToken);

            var dto = new BildirimOzetDto
            {
                ToplamOkunmamis = toplam,
                Bildirimler = bildirimler.Select(kullaniciBildirimi => new BildirimDto
                {
                    Id = kullaniciBildirimi.BildirimId,
                    TipId = kullaniciBildirimi.Bildirim.TipId,
                    Baslik = kullaniciBildirimi.Bildirim.Baslik,
                    Mesaj = kullaniciBildirimi.Bildirim.Mesaj,
                    HedefUrl = kullaniciBildirimi.Bildirim.HedefUrl,
                    OlusturulmaTarihi = kullaniciBildirimi.Bildirim.CreatedDate,
                    OkunduMu = kullaniciBildirimi.OkunduMu,
                    OkunmaTarihi = kullaniciBildirimi.OkunmaTarihi,
                    ReferansTipi = kullaniciBildirimi.Bildirim.ReferansTipi,
                    ReferansId = kullaniciBildirimi.Bildirim.ReferansId
                }).ToList()
            };

            return Result<BildirimOzetDto>.Success(dto);
        }
    }
}
