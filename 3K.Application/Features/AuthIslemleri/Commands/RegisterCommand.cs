using MediatR;
using _3K.Application.DTOs;

namespace _3K.Application.Features.AuthIslemleri.Commands
{
    public class RegisterCommand : IRequest<KullaniciDto>
    {
        public string AdSoyad { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Sifre { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }
}
