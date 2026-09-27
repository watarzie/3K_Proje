using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetSahaSandikPdfQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest
    {
        public int SandikId { get; set; }
    }

    public sealed class GetSahaSandikPdfQueryHandler(IUnitOfWork unitOfWork, IPdfService pdfService)
        : IRequestHandler<GetSahaSandikPdfQuery, Result<DosyaIcerigi>>
    {
        public async Task<Result<DosyaIcerigi>> Handle(GetSahaSandikPdfQuery request, CancellationToken cancellationToken)
        {
            var proje = await RaporDosyaAdi.SandikProjesiBulAsync(unitOfWork, request.SandikId, cancellationToken);
            if (proje == null) return RaporDosyaAdi.SandikBulunamadi;
            var pdf = await pdfService.SahaSandikPdfOlusturAsync(request.SandikId);
            return Result<DosyaIcerigi>.Success(
                DosyaIcerigi.Pdf(pdf, $"{proje.ProjeNo}_{RaporDosyaAdi.SahaRaporAdi(proje)}.pdf"));
        }
    }
}
