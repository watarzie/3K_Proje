using FluentValidation;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Core.Models;

namespace _3K.Application.Features.ProjeIslemleri.Validators
{
    public class ProjeFotografBaglantisiGuncelleCommandValidator : AbstractValidator<ProjeFotografBaglantisiGuncelleCommand>
    {
        public ProjeFotografBaglantisiGuncelleCommandValidator()
        {
            RuleFor(x => x.ProjeId).GreaterThan(0).WithMessage("Proje seçilmelidir.");
            RuleFor(x => x.Baglanti)
                .Must(ProjeFotografBaglantisiKurali.Gecerli)
                .WithMessage($"Geçerli bir web adresi girin (ör. https://ornek.com/proje); en fazla {ProjeFotografBaglantisiKurali.EnFazlaUzunluk} karakter.");
        }
    }
}
