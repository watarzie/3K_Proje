using FluentValidation;
using _3K.Application.Features.AuthIslemleri.Commands;

namespace _3K.Application.Features.AuthIslemleri.Validators
{
    public sealed class IkiFaktorKurulumBaslatCommandValidator
        : AbstractValidator<IkiFaktorKurulumBaslatCommand>
    {
        public IkiFaktorKurulumBaslatCommandValidator()
        {
            RuleFor(x => x.ChallengeToken)
                .NotEmpty().WithMessage("Doğrulama talebi zorunludur.")
                .MaximumLength(200).WithMessage("Doğrulama talebi geçersizdir.");
        }
    }

    public sealed class IkiFaktorKurulumDogrulaCommandValidator
        : AbstractValidator<IkiFaktorKurulumDogrulaCommand>
    {
        public IkiFaktorKurulumDogrulaCommandValidator()
        {
            Include(new ChallengeVeTotpValidator<IkiFaktorKurulumDogrulaCommand>(
                x => x.ChallengeToken,
                x => x.Kod));
        }
    }

    public sealed class IkiFaktorGirisDogrulaCommandValidator
        : AbstractValidator<IkiFaktorGirisDogrulaCommand>
    {
        public IkiFaktorGirisDogrulaCommandValidator()
        {
            Include(new ChallengeVeTotpValidator<IkiFaktorGirisDogrulaCommand>(
                x => x.ChallengeToken,
                x => x.Kod));
        }
    }

    public sealed class IkiFaktorKurtarmaKoduDogrulaCommandValidator
        : AbstractValidator<IkiFaktorKurtarmaKoduDogrulaCommand>
    {
        public IkiFaktorKurtarmaKoduDogrulaCommandValidator()
        {
            RuleFor(x => x.ChallengeToken)
                .NotEmpty().WithMessage("Doğrulama talebi zorunludur.")
                .MaximumLength(200).WithMessage("Doğrulama talebi geçersizdir.");
            RuleFor(x => x.KurtarmaKodu)
                .NotEmpty().WithMessage("Kurtarma kodu zorunludur.")
                .MaximumLength(64).WithMessage("Kurtarma kodu geçersizdir.");
        }
    }

    internal sealed class ChallengeVeTotpValidator<T> : AbstractValidator<T>
    {
        public ChallengeVeTotpValidator(
            System.Linq.Expressions.Expression<Func<T, string>> challenge,
            System.Linq.Expressions.Expression<Func<T, string>> kod)
        {
            RuleFor(challenge)
                .NotEmpty().WithMessage("Doğrulama talebi zorunludur.")
                .MaximumLength(200).WithMessage("Doğrulama talebi geçersizdir.");
            RuleFor(kod)
                .NotEmpty().WithMessage("Doğrulama kodu zorunludur.")
                .Matches("^[0-9]{6}$").WithMessage("Doğrulama kodu 6 haneli olmalıdır.");
        }
    }
}
