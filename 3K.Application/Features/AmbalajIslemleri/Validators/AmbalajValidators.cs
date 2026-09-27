using FluentValidation;
using _3K.Application.Features.AmbalajIslemleri.Commands;

namespace _3K.Application.Features.AmbalajIslemleri.Validators
{
    public sealed class AmbalajUretimDurumuGuncelleCommandValidator : AbstractValidator<AmbalajUretimDurumuGuncelleCommand>
    {
        public AmbalajUretimDurumuGuncelleCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Durum).IsInEnum();
            RuleFor(x => x.Aciklama).MaximumLength(1000);
        }
    }
}
