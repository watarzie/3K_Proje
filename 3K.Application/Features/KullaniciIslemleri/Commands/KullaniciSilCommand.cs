using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.KullaniciIslemleri.Commands
{
    public class KullaniciSilCommand
        : IRequest<Result<bool>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "kullanicilar";
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
        public int Id { get; set; }
    }
}
