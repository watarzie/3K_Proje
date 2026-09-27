using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetSahaGerceklesenCekiListesiExcelQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest
    {
        public int ProjeId { get; set; }
    }

    public sealed class GetSahaGerceklesenCekiListesiExcelQueryHandler(IUnitOfWork unitOfWork, IPdfService pdfService)
        : IRequestHandler<GetSahaGerceklesenCekiListesiExcelQuery, Result<DosyaIcerigi>>
    {
        public async Task<Result<DosyaIcerigi>> Handle(GetSahaGerceklesenCekiListesiExcelQuery request, CancellationToken cancellationToken)
        {
            var proje = await RaporDosyaAdi.ProjeBulAsync(unitOfWork, request.ProjeId, cancellationToken);
            if (proje == null) return RaporDosyaAdi.ProjeBulunamadi;
            var excel = await pdfService.SahaGerceklesenCekiListesiRaporuExcelOlusturAsync(request.ProjeId);
            return Result<DosyaIcerigi>.Success(
                DosyaIcerigi.Excel(excel, $"{proje.ProjeNo}_SahaGerceklesenCekiListesi.xlsx"));
        }
    }
}
