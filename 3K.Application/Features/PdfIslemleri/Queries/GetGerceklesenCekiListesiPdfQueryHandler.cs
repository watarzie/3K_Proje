using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetGerceklesenCekiListesiPdfQueryHandler : IRequestHandler<GetGerceklesenCekiListesiPdfQuery, Result<byte[]>>
    {
        private readonly IPdfService _pdfService;

        public GetGerceklesenCekiListesiPdfQueryHandler(IPdfService pdfService)
        {
            _pdfService = pdfService;
        }

        public async Task<Result<byte[]>> Handle(GetGerceklesenCekiListesiPdfQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var pdfBytes = await _pdfService.GerceklesenCekiListesiRaporuPdfOlusturAsync(request.ProjeId);
                return Result<byte[]>.Success(pdfBytes);
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"Gerceklesen ceki listesi raporu olusturulurken hata meydana geldi: {ex.Message}");
            }
        }
    }
}
