using _3K.Application.Common;
using _3K.Application.Features.BildirimIslemleri.DTOs;
using MediatR;

namespace _3K.Application.Features.BildirimIslemleri.Queries
{
    public class GetOkunmamisBildirimlerQuery : IRequest<Result<BildirimOzetDto>>
    {
        public int Limit { get; set; } = 20;
    }
}
