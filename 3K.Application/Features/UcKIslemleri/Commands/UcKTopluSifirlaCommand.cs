using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.UcKIslemleri.Commands
{
    /// <summary>
    /// Seçili ürünlerin 3K durumlarını toplu olarak sıfırlar (geri alır).
    /// </summary>
    public class UcKTopluSifirlaCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.Personel3K };
        public string? RequiredMenuKod => "3k-modulu";

        public int ProjeId { get; set; }
        public List<int> CekiSatiriIdler { get; set; } = new();
        public string? Aciklama { get; set; }
    }
}
