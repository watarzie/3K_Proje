using MediatR;
using _3K.Application.Common;
using _3K.Core.Models;

namespace _3K.Application.Features.CekiIslemleri.Commands
{
    public class CekiRevizyonOnizleCommand : IRequest<Result<CekiRevizyonOnizlemeSonuc>>, ISecuredRequest, IRequiresMenuPermission
    {
        public Stream ExcelDosya { get; set; } = null!;
        public string DosyaAdi { get; set; } = string.Empty;
        public string RequiredMenuKod => "ceki-revizyon-yukle";
    }
}
