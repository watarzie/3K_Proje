using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    /// <summary>
    /// Seçili projelerin talep formu Excel'i (G4), tek sayfada. ProjeTipi seçimin kapsamıdır; her projenin Grid
    /// okuma izni veritabanındaki gerçek tipinden doğrulanır. Salt okunurdur, süreç durumunu değiştirmez.
    /// </summary>
    public sealed class GetTopluTalepFormuQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest
    {
        public ProjeTipi ProjeTipi { get; init; } = ProjeTipi.Normal;

        public IReadOnlyList<int> ProjeIds { get; init; } = Array.Empty<int>();
    }
}
