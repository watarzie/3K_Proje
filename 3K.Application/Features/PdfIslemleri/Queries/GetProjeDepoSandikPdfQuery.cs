using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetProjeDepoSandikPdfQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest
    {
        public int ProjeId { get; set; }
    }

    public sealed class GetProjeDepoSandikPdfQueryHandler(IUnitOfWork unitOfWork, IDepoSandikRaporService depoSandikRaporu)
        : IRequestHandler<GetProjeDepoSandikPdfQuery, Result<DosyaIcerigi>>
    {
        public async Task<Result<DosyaIcerigi>> Handle(GetProjeDepoSandikPdfQuery request, CancellationToken cancellationToken)
        {
            var proje = await RaporDosyaAdi.ProjeBulAsync(unitOfWork, request.ProjeId, cancellationToken);
            if (proje == null) return RaporDosyaAdi.ProjeBulunamadi;
            var pdf = await depoSandikRaporu.ProjeDepoSandikRaporuPdfOlusturAsync(request.ProjeId);
            return Result<DosyaIcerigi>.Success(
                DosyaIcerigi.Pdf(pdf, $"{proje.ProjeNo}_DepoSandikRaporu_{RaporDosyaAdi.Tarih()}.pdf"));
        }
    }
}
