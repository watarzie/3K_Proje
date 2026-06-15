using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.StokIslemleri.Commands
{
    public class StokKaydiSilCommand : IRequest<Result>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "stok-sil";
        public int Id { get; set; }
    }
}
