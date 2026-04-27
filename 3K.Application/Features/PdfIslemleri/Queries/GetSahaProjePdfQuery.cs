using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetSahaProjePdfQuery : IRequest<Result<byte[]>>
    {
        public int ProjeId { get; set; }
    }
}
