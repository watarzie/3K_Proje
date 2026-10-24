using FluentValidation;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Core.Models;

namespace _3K.Application.Features.ProjeIslemleri.Validators
{
    /// <summary>Geri almada (Kapali false) sebep zorunlu, not sınırlı (<see cref="SandikDurumOlayKurali.GeriAcmaHatasi"/>).</summary>
    public class SandikKapatCommandValidator : AbstractValidator<SandikKapatCommand>
    {
        public SandikKapatCommandValidator()
        {
            RuleFor(x => x.SandikId).GreaterThan(0).WithMessage("Sandık seçilmelidir.");
            RuleFor(x => x)
                .Must(x => SandikDurumOlayKurali.GeriAcmaHatasi(x.AcilmaSebebiId, x.Not) == null)
                .When(x => !x.Kapali)
                .WithMessage(x => SandikDurumOlayKurali.GeriAcmaHatasi(x.AcilmaSebebiId, x.Not)!);
        }
    }
}
