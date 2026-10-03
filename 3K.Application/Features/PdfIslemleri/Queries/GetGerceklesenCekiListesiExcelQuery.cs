using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetGerceklesenCekiListesiExcelQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest
    {
        public int ProjeId { get; set; }
    }

    public sealed class GetGerceklesenCekiListesiExcelQueryHandler(IUnitOfWork unitOfWork, IGerceklesenCekiListesiRaporService gerceklesenCekiListesiRaporu)
        : IRequestHandler<GetGerceklesenCekiListesiExcelQuery, Result<DosyaIcerigi>>
    {
        public async Task<Result<DosyaIcerigi>> Handle(GetGerceklesenCekiListesiExcelQuery request, CancellationToken cancellationToken)
        {
            var proje = await RaporDosyaAdi.ProjeBulAsync(unitOfWork, request.ProjeId, cancellationToken);
            if (proje == null) return RaporDosyaAdi.ProjeBulunamadi;
            var excel = await gerceklesenCekiListesiRaporu.GerceklesenCekiListesiRaporuExcelOlusturAsync(request.ProjeId);
            var tipEki = RaporDosyaAdi.YedekMi(proje) ? "_Yedek" : string.Empty;
            return Result<DosyaIcerigi>.Success(
                DosyaIcerigi.Excel(excel, $"{proje.ProjeNo}{tipEki}_GerceklesenCekiListesi.xlsx"));
        }
    }
}
