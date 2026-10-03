using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetGerceklesenCekiListesiPdfQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest
    {
        public int ProjeId { get; set; }
    }

    public sealed class GetGerceklesenCekiListesiPdfQueryHandler(IUnitOfWork unitOfWork, IGerceklesenCekiListesiRaporService gerceklesenCekiListesiRaporu)
        : IRequestHandler<GetGerceklesenCekiListesiPdfQuery, Result<DosyaIcerigi>>
    {
        public async Task<Result<DosyaIcerigi>> Handle(GetGerceklesenCekiListesiPdfQuery request, CancellationToken cancellationToken)
        {
            var proje = await RaporDosyaAdi.ProjeBulAsync(unitOfWork, request.ProjeId, cancellationToken);
            if (proje == null) return RaporDosyaAdi.ProjeBulunamadi;
            var pdf = await gerceklesenCekiListesiRaporu.GerceklesenCekiListesiRaporuPdfOlusturAsync(request.ProjeId);
            var tipEki = RaporDosyaAdi.YedekMi(proje) ? "_Yedek" : string.Empty;
            return Result<DosyaIcerigi>.Success(
                DosyaIcerigi.Pdf(pdf, $"{proje.ProjeNo}{tipEki}_GerceklesenCekiListesi.pdf"));
        }
    }
}
