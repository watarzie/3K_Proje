using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.KullaniciIslemleri.Commands
{
    /// <summary>
    /// Admin tarafından kullanıcı şifresi değiştirme.
    /// Eski şifre sorulmaz — admin yetkisiyle doğrudan atanır.
    /// </summary>
    public class KullaniciSifreDegistirCommand : IRequest<Result<bool>>
    {
        public int KullaniciId { get; set; }
        public string YeniSifre { get; set; } = string.Empty;
    }
}
