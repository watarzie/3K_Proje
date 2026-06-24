using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetSahaGerceklesenCekiListesiPdfQueryHandler : IRequestHandler<GetSahaGerceklesenCekiListesiPdfQuery, Result<byte[]>>
    {
        private readonly IPdfService _pdfService;

        public GetSahaGerceklesenCekiListesiPdfQueryHandler(IPdfService pdfService)
        {
            _pdfService = pdfService;
        }

        public async Task<Result<byte[]>> Handle(GetSahaGerceklesenCekiListesiPdfQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var pdfBytes = await _pdfService.SahaGerceklesenCekiListesiRaporuPdfOlusturAsync(request.ProjeId);
                return Result<byte[]>.Success(pdfBytes);
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"Saha gerçekleşen çeki listesi raporu oluşturulurken hata meydana geldi: {ex.Message}");
            }
        }
    }
}
