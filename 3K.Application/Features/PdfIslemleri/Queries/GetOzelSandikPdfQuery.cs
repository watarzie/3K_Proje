using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetOzelSandikPdfQuery : IRequest<Result<byte[]>>, ISecuredRequest
    {
        public int Tur { get; set; }
        public int? ProjeId { get; set; }
        public bool UretimFormu { get; set; }
    }

    public class GetOzelSandikPdfQueryHandler : IRequestHandler<GetOzelSandikPdfQuery, Result<byte[]>>
    {
        private readonly IPdfService _pdfService;

        public GetOzelSandikPdfQueryHandler(IPdfService pdfService)
        {
            _pdfService = pdfService;
        }

        public async Task<Result<byte[]>> Handle(GetOzelSandikPdfQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var pdf = request.UretimFormu && request.ProjeId.HasValue
                    ? await _pdfService.OzelSandikUretimFormuPdfOlusturAsync(request.Tur, request.ProjeId.Value)
                    : await _pdfService.OzelSandikRaporuPdfOlusturAsync(request.Tur, request.ProjeId);
                return Result<byte[]>.Success(pdf);
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure(ex.Message);
            }
        }
    }
}
