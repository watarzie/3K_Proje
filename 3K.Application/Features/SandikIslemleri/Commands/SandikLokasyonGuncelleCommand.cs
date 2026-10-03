using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SandikLokasyonGuncelleCommand
        : IRequest<Result<bool>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "sandik-lokasyon-atama";
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
        public List<int> SandikIds { get; set; } = new();
        public int DepoLokasyonId { get; set; }
    }
}
