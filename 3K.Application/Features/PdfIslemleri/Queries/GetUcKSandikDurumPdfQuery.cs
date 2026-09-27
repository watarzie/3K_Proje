using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetUcKSandikDurumPdfQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest
    {
        public int ProjeId { get; set; }
    }

    public sealed class GetUcKSandikDurumPdfQueryHandler(IUnitOfWork unitOfWork, IPdfService pdfService)
        : IRequestHandler<GetUcKSandikDurumPdfQuery, Result<DosyaIcerigi>>
    {
        public async Task<Result<DosyaIcerigi>> Handle(GetUcKSandikDurumPdfQuery request, CancellationToken cancellationToken)
        {
            var proje = await RaporDosyaAdi.ProjeBulAsync(unitOfWork, request.ProjeId, cancellationToken);
            if (proje == null) return RaporDosyaAdi.ProjeBulunamadi;
            var pdf = await pdfService.UcKSandikDurumRaporuPdfOlusturAsync(request.ProjeId);
            return Result<DosyaIcerigi>.Success(DosyaIcerigi.Pdf(pdf, $"{proje.ProjeNo}_SandikDurumRaporu.pdf"));
        }
    }
}
