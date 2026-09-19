using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Application.Features.SandikIslemleri.Services;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SandikUrunTasiCommandHandler : IRequestHandler<SandikUrunTasiCommand, Result>
    {
        private const string TransferIslemAnahtariIndex = "IX_SandikUrunTransferleri_IslemAnahtari";

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public SandikUrunTasiCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public async Task<Result> Handle(SandikUrunTasiCommand request, CancellationToken cancellationToken)
        {
            if (_currentUserService.UserId is not int kullaniciId || kullaniciId <= 0)
                return Result.Failure("Taşıma işlemi için geçerli bir kullanıcı oturumu bulunamadı.", 401);

            if (request.TasinanAdet <= 0)
                return Result.Failure("Taşınan miktar 0'dan büyük olmalıdır.");

            if (!SandikUrunTasimaIslemi.MiktarHassasiyetiGecerliMi(request.TasinanAdet))
            {
                return Result.Failure(
                    "Taşınan miktar en fazla 14 tam ve 4 ondalık basamak içerebilir.");
            }

            if (request.IslemAnahtari == Guid.Empty)
                return Result.Failure("İşlem anahtarı zorunludur.");

            var islemAnahtari = request.IslemAnahtari;
            var transferRepo = _unitOfWork.GetRepository<SandikUrunTransferi>();

            var oncekiTransfer = (await transferRepo.FindAsync(t => t.IslemAnahtari == islemAnahtari))
                .SingleOrDefault();

            if (oncekiTransfer != null)
            {
                return SandikUrunTasimaIslemi.TransferTekrariAyniMi(oncekiTransfer, request)
                    ? Result.Success()
                    : Result.Failure("İşlem anahtarı daha önce farklı bir sandık taşımasında kullanılmış.", 409);
            }

            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
                {
                    var sonuc = await new SandikUrunTasimaIslemi(_unitOfWork, _sahaTamamlamaService)
                        .TasiAsync(request, kullaniciId, transactionCancellationToken);
                    if (!sonuc.IsSuccess)
                        throw new SandikTasimaReddedildiException(sonuc);
                    return sonuc;
                }, cancellationToken);
            }
            catch (SandikTasimaReddedildiException ex)
            {
                return ex.Sonuc;
            }
            catch (ConcurrencyConflictException)
            {
                var tekrarSonucu = await SandikUrunTasimaIslemi.KayitliTransferTekrariniDogrulaAsync(
                    transferRepo,
                    islemAnahtari,
                    request);
                if (tekrarSonucu != null)
                    return tekrarSonucu;

                return Result.Failure(
                    "Ürün miktarı başka bir kullanıcı tarafından değiştirildi. Güncel veriyi yükleyip tekrar deneyin.",
                    409);
            }
            catch (UniqueConstraintViolationException ex)
                when (string.Equals(ex.ConstraintName, TransferIslemAnahtariIndex, StringComparison.OrdinalIgnoreCase))
            {
                // Unique ihlali diğer transaction commit edildikten sonra oluşur. Veritabanındaki
                // payload doğrulanmadan başarılı saymak, aynı anahtar/farklı talebi gizleyebilir.
                return await SandikUrunTasimaIslemi.KayitliTransferTekrariniDogrulaAsync(
                           transferRepo,
                           islemAnahtari,
                           request)
                       ?? Result.Failure("Taşıma işlemi eşzamanlı bir istekle çakıştı. Tekrar deneyin.", 409);
            }
            catch (UniqueConstraintViolationException)
            {
                return Result.Failure(
                    "Taşıma sırasında aynı ürün için çakışan bir sandık tahsisi oluştu. Güncel veriyi yükleyip tekrar deneyin.",
                    409);
            }
        }

    }
}
