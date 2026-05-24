using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetGerceklesenCekiListesiExcelQueryHandler : IRequestHandler<GetGerceklesenCekiListesiExcelQuery, Result<byte[]>>
    {
        private readonly IPdfService _pdfService;

        public GetGerceklesenCekiListesiExcelQueryHandler(IPdfService pdfService)
        {
            _pdfService = pdfService;
        }

        public async Task<Result<byte[]>> Handle(GetGerceklesenCekiListesiExcelQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var excelBytes = await _pdfService.GerceklesenCekiListesiRaporuExcelOlusturAsync(request.ProjeId);
                return Result<byte[]>.Success(excelBytes);
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"Gerçekleşen çeki listesi Excel raporu oluşturulurken hata meydana geldi: {ex.Message}");
            }
        }
    }
}
