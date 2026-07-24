using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetEksikUrunlerPdfQueryHandler : IRequestHandler<GetEksikUrunlerPdfQuery, Result<byte[]>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPdfService _pdfService;

        public GetEksikUrunlerPdfQueryHandler(
            IUnitOfWork unitOfWork,
            IPdfService pdfService)
        {
            _unitOfWork = unitOfWork;
            _pdfService = pdfService;
        }

        public async Task<Result<byte[]>> Handle(GetEksikUrunlerPdfQuery request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!EksikUrunlerRaporYetkisi.GecerliMi(request.ProjeTipi))
                    return Result<byte[]>.Failure("Geçersiz proje tipi için eksik raporu alınamaz.", 403);

                var proje = await _unitOfWork.GetRepository<Proje>().GetByIdAsync(request.ProjeId);
                cancellationToken.ThrowIfCancellationRequested();

                if (proje == null)
                    return Result<byte[]>.Failure("Proje bulunamadı.", 404);

                if (proje.ProjeTipiId != (int)request.ProjeTipi)
                    return Result<byte[]>.Failure("Eksik raporu yetki kapsamı projenin güncel tipiyle eşleşmiyor.", 403);

                var pdfBytes = await _pdfService.EksikUrunlerRaporuPdfOlusturAsync(request.ProjeId);
                cancellationToken.ThrowIfCancellationRequested();

                return Result<byte[]>.Success(pdfBytes);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"Eksik ürünler raporu oluşturulurken hata meydana geldi: {ex.Message}");
            }
        }
    }
}
