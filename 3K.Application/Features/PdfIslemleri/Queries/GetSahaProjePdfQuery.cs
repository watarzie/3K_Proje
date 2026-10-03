using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetSahaProjePdfQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest
    {
        public int ProjeId { get; set; }
    }

    public sealed class GetSahaProjePdfQueryHandler(IUnitOfWork unitOfWork, ISahaSandikRaporService sahaSandikRaporu)
        : IRequestHandler<GetSahaProjePdfQuery, Result<DosyaIcerigi>>
    {
        public async Task<Result<DosyaIcerigi>> Handle(GetSahaProjePdfQuery request, CancellationToken cancellationToken)
        {
            var proje = await RaporDosyaAdi.ProjeBulAsync(unitOfWork, request.ProjeId, cancellationToken);
            if (proje == null) return RaporDosyaAdi.ProjeBulunamadi;
            var pdf = await sahaSandikRaporu.SahaProjeSandiklariPdfOlusturAsync(request.ProjeId);
            return Result<DosyaIcerigi>.Success(
                DosyaIcerigi.Pdf(pdf, $"{proje.ProjeNo}_{RaporDosyaAdi.SahaRaporAdi(proje)}.pdf"));
        }
    }
}
