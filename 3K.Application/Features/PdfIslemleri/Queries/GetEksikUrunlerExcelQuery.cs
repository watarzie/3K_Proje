using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetEksikUrunlerExcelQuery : IRequest<Result<byte[]>>, ISecuredRequest
    {
        public int ProjeId { get; set; }
    }
}
