using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.KullaniciIslemleri.Commands
{
    /// <summary>
    /// Admin tarafından kullanıcı şifresi değiştirme.
    /// Eski şifre sorulmaz — admin yetkisiyle doğrudan atanır.
    /// </summary>
    public class KullaniciSifreDegistirCommand
        : IRequest<Result<bool>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "kullanicilar";
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
        public int KullaniciId { get; set; }
        public string YeniSifre { get; set; } = string.Empty;
    }
}
