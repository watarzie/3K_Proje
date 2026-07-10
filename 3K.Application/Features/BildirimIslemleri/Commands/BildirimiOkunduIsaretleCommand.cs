using _3K.Application.Common;
using MediatR;

namespace _3K.Application.Features.BildirimIslemleri.Commands
{
    public class BildirimiOkunduIsaretleCommand : IRequest<Result>
    {
        public int BildirimId { get; set; }
    }
}
