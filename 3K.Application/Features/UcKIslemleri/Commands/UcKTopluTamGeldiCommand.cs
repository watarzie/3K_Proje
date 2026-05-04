using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.UcKIslemleri.Commands
{
    /// <summary>
    /// Seçilen ürünleri toplu olarak "Sevk Adeti Tam Geldi" olarak işaretler.
    /// </summary>
    public class UcKTopluTamGeldiCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.Personel3K };

        public int ProjeId { get; set; }
        public List<int> CekiSatiriIdler { get; set; } = new();
        public string? Aciklama { get; set; }
    }
}
