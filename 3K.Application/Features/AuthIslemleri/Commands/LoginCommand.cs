using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AuthIslemleri.DTOs;

namespace _3K.Application.Features.AuthIslemleri.Commands
{
    /// <summary>
    /// Login - herkes erişebilir, ISecuredRequest yok.
    /// </summary>
    public class LoginCommand : IRequest<Result<LoginResultDto>>
    {
        public string Email { get; set; } = string.Empty;
        public string Sifre { get; set; } = string.Empty;
    }
}
