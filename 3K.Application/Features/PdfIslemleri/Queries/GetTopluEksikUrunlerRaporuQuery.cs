using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public enum EksikUrunlerRaporDosyaTuru
    {
        Pdf = 1,
        Excel = 2
    }

    /// <summary>
    /// Seçili projelerin eksik raporlarını tek ZIP'te döner. ProjeTipi raporun kapsamıdır;
    /// yetki her projenin veritabanındaki gerçek tipinden ayrıca doğrulanır.
    /// </summary>
    public sealed class GetTopluEksikUrunlerRaporuQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest
    {
        public ProjeTipi ProjeTipi { get; init; } = ProjeTipi.Normal;

        public IReadOnlyCollection<int> ProjeIds { get; init; } = Array.Empty<int>();

        public EksikUrunlerRaporDosyaTuru DosyaTuru { get; init; }
    }
}
