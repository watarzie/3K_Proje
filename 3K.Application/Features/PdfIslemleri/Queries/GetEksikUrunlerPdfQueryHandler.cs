using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetEksikUrunlerPdfQueryHandler : IRequestHandler<GetEksikUrunlerPdfQuery, Result<byte[]>>
    {
        private readonly IPdfService _pdfService;

        public GetEksikUrunlerPdfQueryHandler(IPdfService pdfService)
        {
            _pdfService = pdfService;
        }

        public async Task<Result<byte[]>> Handle(GetEksikUrunlerPdfQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var pdfBytes = await _pdfService.EksikUrunlerRaporuPdfOlusturAsync(request.ProjeId);
                return Result<byte[]>.Success(pdfBytes);
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"Eksik ürünler raporu oluşturulurken hata meydana geldi: {ex.Message}");
            }
        }
    }
}
