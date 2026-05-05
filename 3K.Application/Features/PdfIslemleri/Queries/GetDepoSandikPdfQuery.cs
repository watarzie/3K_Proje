using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetDepoSandikPdfQuery : IRequest<Result<byte[]>>
    {
        public int? ProjeTipiId { get; set; }
    }

    public class GetDepoSandikPdfQueryHandler : IRequestHandler<GetDepoSandikPdfQuery, Result<byte[]>>
    {
        private readonly IPdfService _pdfService;

        public GetDepoSandikPdfQueryHandler(IPdfService pdfService)
        {
            _pdfService = pdfService;
        }

        public async Task<Result<byte[]>> Handle(GetDepoSandikPdfQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var pdf = await _pdfService.DepoSandikRaporuPdfOlusturAsync(request.ProjeTipiId);
                return Result<byte[]>.Success(pdf);
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure(ex.Message);
            }
        }
    }
}
