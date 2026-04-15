using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// İş akışı 10: Eksik ürünü stoktan karşıla.
    /// Roller: Admin, Personel3K
    /// </summary>
    public class StoktanKarsilaCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.Personel3K };

        public int CekiSatiriId { get; set; }
        public int StokKaydiId { get; set; }
        public int KullaniciId { get; set; }
        public int ProjeId { get; set; }
        public int KarsilananAdet { get; set; }
    }
}
