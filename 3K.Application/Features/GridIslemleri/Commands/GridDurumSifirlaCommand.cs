using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.GridIslemleri.Commands
{
    /// <summary>
    /// Grid personeli tarafından yapılan durum seçimini sıfırlar (geri alır).
    /// Ürünü çeki yüklendiğindeki ham/başlangıç durumuna döndürür.
    /// </summary>
    public class GridDurumSifirlaCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.PersonelGrid };

        public int CekiSatiriId { get; set; }
        public int ProjeId { get; set; }
        public string? Aciklama { get; set; }
    }
}
