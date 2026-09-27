using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetDepoSandikPdfQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest
    {
        public int? ProjeTipiId { get; set; }
    }

    public sealed class GetDepoSandikPdfQueryHandler(IPdfService pdfService)
        : IRequestHandler<GetDepoSandikPdfQuery, Result<DosyaIcerigi>>
    {
        public async Task<Result<DosyaIcerigi>> Handle(GetDepoSandikPdfQuery request, CancellationToken cancellationToken)
        {
            var pdf = await pdfService.DepoSandikRaporuPdfOlusturAsync(request.ProjeTipiId);
            var kapsam = request.ProjeTipiId switch
            {
                (int)ProjeTipi.Normal => "_Normal",
                (int)ProjeTipi.Saha => "_Saha",
                (int)ProjeTipi.Yedek => "_Yedek",
                _ => string.Empty
            };
            return Result<DosyaIcerigi>.Success(
                DosyaIcerigi.Pdf(pdf, $"DepoSandikRaporu{kapsam}_{RaporDosyaAdi.Tarih()}.pdf"));
        }
    }
}
