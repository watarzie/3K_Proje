using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetSahaGerceklesenCekiListesiPdfQuery : IRequest<Result<byte[]>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "saha-gerceklesen-ceki-raporu";

        public int ProjeId { get; set; }
    }
}
