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
}
