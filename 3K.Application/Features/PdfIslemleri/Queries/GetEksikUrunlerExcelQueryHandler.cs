using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetEksikUrunlerExcelQueryHandler : IRequestHandler<GetEksikUrunlerExcelQuery, Result<byte[]>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPdfService _pdfService;
        private readonly ICurrentUserService _currentUser;
        private readonly IRolService _rolService;

        public GetEksikUrunlerExcelQueryHandler(
            IUnitOfWork unitOfWork,
            IPdfService pdfService,
            ICurrentUserService currentUser,
            IRolService rolService)
        {
            _unitOfWork = unitOfWork;
            _pdfService = pdfService;
            _currentUser = currentUser;
            _rolService = rolService;
        }

        public async Task<Result<byte[]>> Handle(GetEksikUrunlerExcelQuery request, CancellationToken cancellationToken)
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

                if (!await EksikUrunlerRaporYetkisi.YetkiliMiAsync(
                    (ProjeTipi)proje.ProjeTipiId, _currentUser, _rolService, cancellationToken))
                    return Result<byte[]>.Failure("Bu projenin eksik raporunu okuma yetkiniz bulunmuyor.", 403);

                var excelBytes = await _pdfService.EksikUrunlerRaporuExcelOlusturAsync(request.ProjeId, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                return Result<byte[]>.Success(excelBytes);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"Eksik ürünler Excel raporu oluşturulurken hata meydana geldi: {ex.Message}");
            }
        }
    }
}
