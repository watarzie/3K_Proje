using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AuthIslemleri.DTOs;

namespace _3K.Application.Features.KullaniciIslemleri.Commands
{
    /// <summary>
    /// Kullanıcı bilgilerini (ad, rol) günceller.
    /// </summary>
    public class KullaniciGuncelleCommand : IRequest<Result<KullaniciDto>>
    {
        public int Id { get; set; }
        public string AdSoyad { get; set; } = string.Empty;
        public int RolId { get; set; }
    }
}
