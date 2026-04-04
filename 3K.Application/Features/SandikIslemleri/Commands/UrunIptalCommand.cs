using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// İş akışı 9: Ürün iptali/pasife çekme.
    /// Roller: Admin, Personel3K
    /// </summary>
    public class UrunIptalCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { nameof(KullaniciRol.Admin), nameof(KullaniciRol.Personel3K) };

        public int CekiSatiriId { get; set; }
        public int ProjeId { get; set; }
        public int KullaniciId { get; set; }
        public string Neden { get; set; } = string.Empty;
    }
}
