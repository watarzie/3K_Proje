using FluentValidation;
using _3K.Application.Features.KullaniciIslemleri.Commands;

namespace _3K.Application.Features.KullaniciIslemleri.Validators
{
    public sealed class KullaniciIkiFaktorZorunluluguGuncelleCommandValidator
        : AbstractValidator<KullaniciIkiFaktorZorunluluguGuncelleCommand>
    {
        public KullaniciIkiFaktorZorunluluguGuncelleCommandValidator()
        {
            RuleFor(x => x.KullaniciId)
                .GreaterThan(0)
                .WithMessage("Geçerli bir kullanıcı seçilmelidir.");

            RuleFor(x => x.ZorunluMu)
                .NotNull()
                .WithMessage("ZorunluMu alanı zorunludur.");
        }
    }
}
