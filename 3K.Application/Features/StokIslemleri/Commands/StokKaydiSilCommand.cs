using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.StokIslemleri.Commands
{
    public class StokKaydiSilCommand : IRequest<Result>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "stok-sil";
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
        public int Id { get; set; }
    }
}
