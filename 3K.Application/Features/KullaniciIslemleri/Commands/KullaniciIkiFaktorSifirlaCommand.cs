using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.KullaniciIslemleri.Commands
{
    public sealed class KullaniciIkiFaktorSifirlaCommand
        : IRequest<Result<bool>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "kullanicilar";
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
        public int KullaniciId { get; set; }
    }
}
