using _3K.Application.Common;
using _3K.Application.Features.BildirimIslemleri.DTOs;
using _3K.Core.Interfaces;
using MediatR;

namespace _3K.Application.Features.BildirimIslemleri.Queries
{
    public class GetBildirimDetayiQueryHandler
        : IRequestHandler<GetBildirimDetayiQuery, Result<BildirimDto>>
    {
        private readonly IBildirimRepository _bildirimRepository;
        private readonly ICurrentUserService _currentUserService;

        public GetBildirimDetayiQueryHandler(
            IBildirimRepository bildirimRepository,
            ICurrentUserService currentUserService)
        {
            _bildirimRepository = bildirimRepository;
            _currentUserService = currentUserService;
        }

        public async Task<Result<BildirimDto>> Handle(
            GetBildirimDetayiQuery request,
            CancellationToken cancellationToken)
        {
            var kullaniciId = _currentUserService.UserId;
            if (!kullaniciId.HasValue)
                return Result<BildirimDto>.Failure("Kullanıcı bilgisi alınamadı.", 401);

            var bildirim = await _bildirimRepository.GetDetayAsync(
                request.BildirimId,
                kullaniciId.Value,
                cancellationToken);

            return bildirim == null
                ? Result<BildirimDto>.Failure("Bildirim bulunamadı.", 404)
                : Result<BildirimDto>.Success(bildirim.ToDto());
        }
    }
}
