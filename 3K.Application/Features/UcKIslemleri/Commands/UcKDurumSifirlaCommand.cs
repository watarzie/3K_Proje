using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.UcKIslemleri.Commands
{
    /// <summary>
    /// 3K personeli tarafından yapılan karşılama seçimini sıfırlar (geri alır).
    /// Ürünü ham/başlangıç durumuna döndürür.
    /// </summary>
    public class UcKDurumSifirlaCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.Personel3K };

        public int CekiSatiriId { get; set; }
        public int ProjeId { get; set; }
        public string? Aciklama { get; set; }
    }
}
