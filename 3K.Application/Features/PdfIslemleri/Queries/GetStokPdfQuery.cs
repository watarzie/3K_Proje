using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetStokPdfQuery : IRequest<Result<byte[]>> { }

    public class GetStokPdfQueryHandler : IRequestHandler<GetStokPdfQuery, Result<byte[]>>
    {
        private readonly IPdfService _pdfService;

        public GetStokPdfQueryHandler(IPdfService pdfService)
        {
            _pdfService = pdfService;
        }

        public async Task<Result<byte[]>> Handle(GetStokPdfQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var pdf = await _pdfService.StokRaporuPdfOlusturAsync();
                return Result<byte[]>.Success(pdf);
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure(ex.Message);
            }
        }
    }
}
