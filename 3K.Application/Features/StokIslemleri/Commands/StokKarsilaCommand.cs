using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.StokIslemleri.Commands
{
    /// <summary>
    /// İş akışı 6: Stoktan eksik karşılama.
    /// Roller: Admin, Personel3K
    /// </summary>
    public class StokKarsilaCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.Personel3K };

        public int CekiSatiriId { get; set; }
        public int StokKaydiId { get; set; }
        public int Miktar { get; set; }
        public int KullaniciId { get; set; }
        public int ProjeId { get; set; }
    }
}
