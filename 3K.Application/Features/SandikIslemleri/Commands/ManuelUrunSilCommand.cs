using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// Manuel eklenen ürünü siler (IsManuelEklenen = true ve üzerinde işlem yapılmamış olan).
    /// </summary>
    public class ManuelUrunSilCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.Personel3K };

        /// <summary>Normal proje manuel ürünleri için CekiSatiriId kullanılır.</summary>
        public int? CekiSatiriId { get; set; }
        /// <summary>Saha/Yedek ürünleri için SandikIcerikId kullanılır (CekiSatiri yok).</summary>
        public int? SandikIcerikId { get; set; }
        public int ProjeId { get; set; }
    }
}
