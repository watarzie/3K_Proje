using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetSahaSandikPdfQuery : IRequest<Result<byte[]>>, ISecuredRequest
    {
        public int SandikId { get; set; }
    }
}
