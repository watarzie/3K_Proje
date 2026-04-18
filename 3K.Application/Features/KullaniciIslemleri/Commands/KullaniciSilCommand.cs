using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.KullaniciIslemleri.Commands
{
    public class KullaniciSilCommand : IRequest<Result<bool>>
    {
        public int Id { get; set; }
    }
}
