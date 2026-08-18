using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.KullaniciIslemleri.Commands
{
    public sealed class KullaniciIkiFaktorSifirlaCommand
        : IRequest<Result<bool>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "kullanicilar";
        public int KullaniciId { get; set; }
    }
}
