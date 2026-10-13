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

    public sealed class AmbalajOzelSandikFinansCommandValidator : AbstractValidator<AmbalajOzelSandikFinansCommand>
    {
        public AmbalajOzelSandikFinansCommandValidator()
        {
            RuleFor(x => x.SandikId).GreaterThan(0);
            RuleFor(x => x.FinansTarihi!.Value).Must(Core.Models.FinansTarihKurallari.GecerliTarih)
                .WithMessage("Geçerli bir finans tarihi girin.")
                .When(x => x.FinansTarihi.HasValue);
            RuleFor(x => x.FinansTarihi).Null().When(x => !x.Aktar)
                .WithMessage("Finans tarihi yalnız finansa aktarırken girilir.");
            RuleFor(x => x.Gerekce).MaximumLength(1000);
        }
    }
}
