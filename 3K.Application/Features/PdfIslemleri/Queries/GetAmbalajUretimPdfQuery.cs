using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetAmbalajUretimPdfQuery : IRequest<Result<byte[]>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "ambalaj-uretim-listesi";

        public int ProjeId { get; set; }
        public int? Tur { get; set; }
    }

    public class GetAmbalajUretimPdfQueryHandler : IRequestHandler<GetAmbalajUretimPdfQuery, Result<byte[]>>
    {
        private readonly IPdfService _pdfService;

        public GetAmbalajUretimPdfQueryHandler(IPdfService pdfService)
        {
            _pdfService = pdfService;
        }

        public async Task<Result<byte[]>> Handle(GetAmbalajUretimPdfQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var pdf = await _pdfService.AmbalajUretimRaporuPdfOlusturAsync(request.ProjeId, request.Tur);
                return Result<byte[]>.Success(pdf);
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure(ex.Message);
            }
        }
    }
}