using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetEksikUrunlerExcelQuery
        : IRequest<Result<byte[]>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => EksikUrunlerRaporYetkisi.GetMenuKod(ProjeTipi);

        public int ProjeId { get; set; }

        public ProjeTipi ProjeTipi { get; set; }
    }
}
