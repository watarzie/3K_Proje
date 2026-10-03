using _3K.Application.Common;
using MediatR;
using _3K.Core.Enums;

namespace _3K.Application.Features.BildirimIslemleri.Commands
{
    public class BildirimAbonelikleriniGuncelleCommand
        : IRequest<Result>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "kullanicilar";
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
        public List<int> CekiYuklendiAliciIdleri { get; set; } = new();
        public List<int> CekiRevizyonuAliciIdleri { get; set; } = new();
    }
}
