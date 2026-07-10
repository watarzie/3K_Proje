using _3K.Application.Common;
using _3K.Application.Features.BildirimIslemleri.DTOs;
using MediatR;

namespace _3K.Application.Features.BildirimIslemleri.Queries
{
    public class GetBildirimDetayiQuery : IRequest<Result<BildirimDto>>
    {
        public int BildirimId { get; set; }
    }
}
