using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AuthIslemleri.DTOs;
using _3K.Core.Enums;

namespace _3K.Application.Features.KullaniciIslemleri.Commands
{
    /// <summary>
    /// Kullanıcı bilgilerini (ad, rol) günceller.
    /// </summary>
    public class KullaniciGuncelleCommand
        : IRequest<Result<KullaniciDto>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "kullanicilar";
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
        public int Id { get; set; }
        public string AdSoyad { get; set; } = string.Empty;
        public int RolId { get; set; }
    }
}
