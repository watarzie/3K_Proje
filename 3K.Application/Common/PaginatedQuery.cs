using MediatR;

namespace _3K.Application.Common
{
    public abstract class PaginatedQuery<TResponse> : IRequest<TResponse>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
