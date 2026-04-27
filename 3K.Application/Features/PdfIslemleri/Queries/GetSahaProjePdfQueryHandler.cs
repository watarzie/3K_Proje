using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetSahaProjePdfQueryHandler : IRequestHandler<GetSahaProjePdfQuery, Result<byte[]>>
    {
        private readonly IPdfService _pdfService;

        public GetSahaProjePdfQueryHandler(IPdfService pdfService)
        {
            _pdfService = pdfService;
        }

        public async Task<Result<byte[]>> Handle(GetSahaProjePdfQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var pdfBytes = await _pdfService.SahaProjeSandiklariPdfOlusturAsync(request.ProjeId);
                return Result<byte[]>.Success(pdfBytes);
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"Toplu PDF oluşturulurken hata meydana geldi: {ex.Message}");
            }
        }
    }
}
