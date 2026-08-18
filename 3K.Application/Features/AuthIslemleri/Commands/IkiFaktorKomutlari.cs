using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AuthIslemleri.DTOs;

namespace _3K.Application.Features.AuthIslemleri.Commands
{
    public sealed class IkiFaktorKurulumBaslatCommand
        : IRequest<Result<IkiFaktorKurulumDto>>
    {
        public string ChallengeToken { get; set; } = string.Empty;
    }

    public sealed class IkiFaktorKurulumDogrulaCommand
        : IRequest<Result<LoginResultDto>>
    {
        public string ChallengeToken { get; set; } = string.Empty;
        public string Kod { get; set; } = string.Empty;
    }

    public sealed class IkiFaktorGirisDogrulaCommand
        : IRequest<Result<LoginResultDto>>
    {
        public string ChallengeToken { get; set; } = string.Empty;
        public string Kod { get; set; } = string.Empty;
    }

    public sealed class IkiFaktorKurtarmaKoduDogrulaCommand
        : IRequest<Result<LoginResultDto>>
    {
        public string ChallengeToken { get; set; } = string.Empty;
        public string KurtarmaKodu { get; set; } = string.Empty;
    }
}
