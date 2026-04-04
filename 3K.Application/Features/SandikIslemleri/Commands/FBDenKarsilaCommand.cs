using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// İş akışı 11: Eksik ürünü FB transferi ile karşıla.
    /// Roller: Admin, Personel3K
    /// </summary>
    public class FBDenKarsilaCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { nameof(KullaniciRol.Admin), nameof(KullaniciRol.Personel3K) };

        public int CekiSatiriId { get; set; }
        public int ProjeId { get; set; }
        public int KullaniciId { get; set; }
        public string AsilFB { get; set; } = string.Empty;
        public string AlinanFB { get; set; } = string.Empty;
        public int KarsilananAdet { get; set; }
        public string? Aciklama { get; set; }
        public string? IadeDurumu { get; set; }
    }
}
