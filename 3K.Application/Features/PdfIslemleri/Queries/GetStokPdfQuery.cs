using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetStokPdfQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest { }

    public sealed class GetStokPdfQueryHandler(IStokRaporService stokRaporu)
        : IRequestHandler<GetStokPdfQuery, Result<DosyaIcerigi>>
    {
        public async Task<Result<DosyaIcerigi>> Handle(GetStokPdfQuery request, CancellationToken cancellationToken)
        {
            var pdf = await stokRaporu.StokRaporuPdfOlusturAsync();
            return Result<DosyaIcerigi>.Success(DosyaIcerigi.Pdf(pdf, $"StokRaporu_{RaporDosyaAdi.Tarih()}.pdf"));
        }
    }
}
