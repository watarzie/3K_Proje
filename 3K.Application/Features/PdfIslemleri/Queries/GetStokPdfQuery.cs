using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    /// <summary>Stok raporu; <see cref="DurumId"/> verilirse yalnız o sekmenin (Aktif / Tükendi) kayıtları basılır.</summary>
    public class GetStokPdfQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest
    {
        public int? DurumId { get; set; }
    }

    public sealed class GetStokPdfQueryHandler(IStokRaporService stokRaporu)
        : IRequestHandler<GetStokPdfQuery, Result<DosyaIcerigi>>
    {
        public async Task<Result<DosyaIcerigi>> Handle(GetStokPdfQuery request, CancellationToken cancellationToken)
        {
            if (!StokListeFiltresi.GecerliDurum(request.DurumId))
                return Result<DosyaIcerigi>.Failure("Geçersiz stok durumu.");
            var pdf = await stokRaporu.StokRaporuPdfOlusturAsync(request.DurumId);
            return Result<DosyaIcerigi>.Success(DosyaIcerigi.Pdf(pdf, $"StokRaporu_{RaporDosyaAdi.Tarih()}.pdf"));
        }
    }
}
