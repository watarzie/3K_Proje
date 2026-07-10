using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.DashboardIslemleri.DTOs;

namespace _3K.Application.Features.DashboardIslemleri.Queries
{
    public class DashboardOzetQuery : IRequest<Result<DashboardOzetDto>>, ISecuredRequest
    {
    }

    public class DashboardProjelerQuery : IRequest<Result<DashboardPagedResultDto<DashboardProjeItemDto>>>, ISecuredRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int? ProjeTipiId { get; set; }
    }

    public class DashboardKritikEksiklerQuery : IRequest<Result<DashboardPagedResultDto<DashboardKritikProjeDto>>>, ISecuredRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class DashboardEksikSiralamaQuery : IRequest<Result<DashboardPagedResultDto<DashboardEksikSiralamaDto>>>, ISecuredRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class DashboardSahayaAktarilanSandiklarQuery : IRequest<Result<DashboardPagedResultDto<DashboardSahayaAktarilanSandikDto>>>, ISecuredRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int? ProjeId { get; set; }
    }

    public class DashboardProjeFilterOptionsQuery : IRequest<Result<List<DashboardProjeFilterOptionDto>>>, ISecuredRequest
    {
        public int? ProjeTipiId { get; set; }
        public string? SearchTerm { get; set; }
        public bool SadeceSandikAktarimli { get; set; }
        public int Take { get; set; } = 30;
    }

    public class DashboardProjeSandikDurumQuery : IRequest<Result<DashboardProjeSandikDurumDto>>, ISecuredRequest
    {
        public int ProjeId { get; set; }
    }

    public class DashboardProjeSandiklariDrillDownQuery
        : IRequest<Result<DashboardPagedResultDto<DashboardSandikDrillDownDto>>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "dashboard";
        public int ProjeId { get; set; }
        public int DurumId { get; set; }
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
