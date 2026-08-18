using FluentValidation;
using _3K.Application.Features.KullaniciIslemleri.Commands;

namespace _3K.Application.Features.KullaniciIslemleri.Validators
{
    public sealed class KullaniciIkiFaktorSifirlaCommandValidator
        : AbstractValidator<KullaniciIkiFaktorSifirlaCommand>
    {
        public KullaniciIkiFaktorSifirlaCommandValidator()
        {
            RuleFor(x => x.KullaniciId)
                .GreaterThan(0).WithMessage("Geçerli bir kullanıcı seçilmelidir.");
        }
    }
}
