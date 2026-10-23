using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.GridIslemleri.DTOs;
using _3K.Core.Constants;
using _3K.Core.Enums;

namespace _3K.Application.Features.GridIslemleri.Queries
{
    public class GetGridIsListesiQuery
        : IRequest<Result<GridIsListesiDto>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => YetkiKodlari.Sandik.GridIsListesi;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? IsTipi { get; set; }
        public int? ProjeId { get; set; }
        public bool SadeceBugun { get; set; }
    }
}
