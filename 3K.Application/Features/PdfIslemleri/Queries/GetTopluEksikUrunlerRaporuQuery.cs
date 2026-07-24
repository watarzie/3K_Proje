using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public enum EksikUrunlerRaporDosyaTuru
    {
        Pdf = 1,
        Excel = 2
    }

    public sealed class GetTopluEksikUrunlerRaporuQuery
        : IRequest<Result<byte[]>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "eksik-raporu";

        public IReadOnlyCollection<int> ProjeIds { get; init; } = Array.Empty<int>();

        public EksikUrunlerRaporDosyaTuru DosyaTuru { get; init; }
    }
}
