using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetGerceklesenCekiListesiPdfQuery : IRequest<Result<byte[]>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "gerceklesen-ceki-raporu";

        public int ProjeId { get; set; }
    }
}
