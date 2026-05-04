using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.UcKIslemleri.Commands
{
    /// <summary>
    /// Seçilen ürünleri toplu olarak "Tedarikçiden Geldi" olarak işaretler.
    /// Her ürünün kalan miktarı kadar tedarikçiden karşılanır.
    /// </summary>
    public class UcKTopluTedarikciCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.Personel3K };

        public int ProjeId { get; set; }
        public List<int> CekiSatiriIdler { get; set; } = new();
        public string? Aciklama { get; set; }
    }
}
