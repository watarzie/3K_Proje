using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetEksikUrunlerExcelQueryHandler : IRequestHandler<GetEksikUrunlerExcelQuery, Result<byte[]>>
    {
        private readonly IPdfService _pdfService;

        public GetEksikUrunlerExcelQueryHandler(IPdfService pdfService)
        {
            _pdfService = pdfService;
        }

        public async Task<Result<byte[]>> Handle(GetEksikUrunlerExcelQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var excelBytes = await _pdfService.EksikUrunlerRaporuExcelOlusturAsync(request.ProjeId);
                return Result<byte[]>.Success(excelBytes);
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"Eksik ürünler Excel raporu oluşturulurken hata meydana geldi: {ex.Message}");
            }
        }
    }
}
