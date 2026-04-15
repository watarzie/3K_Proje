using FluentValidation;
using _3K.Application.Features.UcKIslemleri.Commands;

namespace _3K.Application.Features.UcKIslemleri.Validators
{
    public class UcKDurumGuncelleCommandValidator : AbstractValidator<UcKDurumGuncelleCommand>
    {
        public UcKDurumGuncelleCommandValidator()
        {
            RuleFor(x => x.CekiSatiriId).GreaterThan(0).WithMessage("Geçerli bir ürün ID belirtilmeli.");
            RuleFor(x => x.ProjeId).GreaterThan(0).WithMessage("Geçerli bir proje ID belirtilmeli.");
            RuleFor(x => x.KarsilamaTipi).NotEmpty().WithMessage("Karşılama tipi belirtilmeli.");
        }
    }
}
