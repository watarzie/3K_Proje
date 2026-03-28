using MediatR;
using _3K.Application.DTOs;

namespace _3K.Application.Features.AuthIslemleri.Commands
{
    public class LoginCommand : IRequest<LoginResultDto>
    {
        public string Email { get; set; } = string.Empty;
        public string Sifre { get; set; } = string.Empty;
    }
}
