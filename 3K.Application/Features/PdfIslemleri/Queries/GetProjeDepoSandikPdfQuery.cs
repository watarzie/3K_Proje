using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetProjeDepoSandikPdfQuery : IRequest<Result<byte[]>>, ISecuredRequest
    {
        public int ProjeId { get; set; }
    }

    public class GetProjeDepoSandikPdfQueryHandler : IRequestHandler<GetProjeDepoSandikPdfQuery, Result<byte[]>>
    {
        private readonly IPdfService _pdfService;

        public GetProjeDepoSandikPdfQueryHandler(IPdfService pdfService)
        {
            _pdfService = pdfService;
        }

        public async Task<Result<byte[]>> Handle(GetProjeDepoSandikPdfQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var pdf = await _pdfService.ProjeDepoSandikRaporuPdfOlusturAsync(request.ProjeId);
                return Result<byte[]>.Success(pdf);
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure(ex.Message);
            }
        }
    }
}
