using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetSahaGerceklesenCekiListesiExcelQueryHandler : IRequestHandler<GetSahaGerceklesenCekiListesiExcelQuery, Result<byte[]>>
    {
        private readonly IPdfService _pdfService;

        public GetSahaGerceklesenCekiListesiExcelQueryHandler(IPdfService pdfService)
        {
            _pdfService = pdfService;
        }

        public async Task<Result<byte[]>> Handle(GetSahaGerceklesenCekiListesiExcelQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var excelBytes = await _pdfService.SahaGerceklesenCekiListesiRaporuExcelOlusturAsync(request.ProjeId);
                return Result<byte[]>.Success(excelBytes);
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"Saha gerçekleşen çeki listesi Excel raporu oluşturulurken hata meydana geldi: {ex.Message}");
            }
        }
    }
}
