using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetUcKSandikDurumPdfQuery : IRequest<Result<byte[]>>, ISecuredRequest
    {
        public int ProjeId { get; set; }
    }

    public class GetUcKSandikDurumPdfQueryHandler : IRequestHandler<GetUcKSandikDurumPdfQuery, Result<byte[]>>
    {
        private readonly IPdfService _pdfService;

        public GetUcKSandikDurumPdfQueryHandler(IPdfService pdfService)
        {
            _pdfService = pdfService;
        }

        public async Task<Result<byte[]>> Handle(GetUcKSandikDurumPdfQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var pdfBytes = await _pdfService.UcKSandikDurumRaporuPdfOlusturAsync(request.ProjeId);
                return Result<byte[]>.Success(pdfBytes);
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"3K sandık durum raporu oluşturulurken hata meydana geldi: {ex.Message}");
            }
        }
    }
}
