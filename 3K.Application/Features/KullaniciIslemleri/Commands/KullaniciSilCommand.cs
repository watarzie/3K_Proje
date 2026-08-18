using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.KullaniciIslemleri.Commands
{
    public class KullaniciSilCommand
        : IRequest<Result<bool>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "kullanicilar";
        public int Id { get; set; }
    }
}
