using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetSahaSandikPdfQueryHandler : IRequestHandler<GetSahaSandikPdfQuery, Result<byte[]>>
    {
        private readonly IPdfService _pdfService;

        public GetSahaSandikPdfQueryHandler(IPdfService pdfService)
        {
            _pdfService = pdfService;
        }

        public async Task<Result<byte[]>> Handle(GetSahaSandikPdfQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var pdfBytes = await _pdfService.SahaSandikPdfOlusturAsync(request.SandikId);
                return Result<byte[]>.Success(pdfBytes);
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"PDF oluşturulurken hata meydana geldi: {ex.Message}");
            }
        }
    }
}
