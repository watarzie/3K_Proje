using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.UcKIslemleri.Commands
{
    /// <summary>
    /// Madde 5: Toplu 3K Durum Güncelleme (şimdilik sadece TamGeldi).
    /// Birden fazla CekiSatiri'ni tek seferde "Tam Geldi" olarak işaretler.
    /// </summary>
    public class TopluDurumGuncelleCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.Personel3K };

        public int ProjeId { get; set; }
        public List<int> CekiSatiriIdler { get; set; } = new();
        public string? Aciklama { get; set; }
    }
}
