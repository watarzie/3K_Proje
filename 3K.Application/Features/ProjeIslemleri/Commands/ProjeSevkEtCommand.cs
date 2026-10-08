using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.Services;
using _3K.Core.Interfaces;
using _3K.Core.Helpers;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    public class ProjeSevkEtCommand : IRequest<Result>, ISecuredRequest
    {
        // UI'da W yetkisi olanlar tetikleyebilecek, backend'de yetki yönetimi Pipeline üzerinden de yapılıyor.

        public int ProjeId { get; set; }
        public DateTime? SevkTarihi { get; set; }
        public List<int>? SandikIds { get; set; }
        public string? Aciklama { get; set; }
        public string? AracPlaka { get; set; }
    }

    public class ProjeSevkEtCommandHandler : IRequestHandler<ProjeSevkEtCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;
        private readonly IReadQueryExecutor _readQueries;

        public ProjeSevkEtCommandHandler(
            IUnitOfWork unitOfWork,
            IHareketService hareketService,
            ICurrentUserService currentUserService,
            ISahaTamamlamaService sahaTamamlamaService,
            IReadQueryExecutor readQueries)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _currentUserService = currentUserService;
            _sahaTamamlamaService = sahaTamamlamaService;
            _readQueries = readQueries;
        }

        public async Task<Result> Handle(ProjeSevkEtCommand request, CancellationToken cancellationToken)
        {
            if (!_currentUserService.UserId.HasValue)
                return Result.Failure("Oturum acmaniz gerekiyor.", 401);

            var islem = new ProjeSevkIslemi(_unitOfWork, _hareketService, _sahaTamamlamaService, _readQueries);
            var kontrol = await islem.HazirlaAsync(request.ProjeId, request.SandikIds, cancellationToken);
            if (kontrol.Hata != null)
                return kontrol.Hata;

            var kullaniciId = _currentUserService.UserId.Value;
            return await _unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
            {
                // Tekil sevk de tek projelik bir sefer açar; istatistikler tek kaynaktan okunur.
                await islem.UygulaAsync(kontrol.Hazirlik!, request.SevkTarihi ?? TurkeyTime.Now, request.Aciklama,
                    request.AracPlaka, null, kullaniciId, null, transactionCancellationToken);
                return Result.Success();
            }, cancellationToken);
        }
    }
}
