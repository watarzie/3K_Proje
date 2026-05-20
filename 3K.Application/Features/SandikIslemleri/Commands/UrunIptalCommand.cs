using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// İş akışı 9: Ürün iptali/pasife çekme.
    /// </summary>
    public class UrunIptalCommand : IRequest<Result>, ISecuredRequest
    {

        public int CekiSatiriId { get; set; }
        public int ProjeId { get; set; }
        public int KullaniciId { get; set; }
        public string Neden { get; set; } = string.Empty;
    }
}
