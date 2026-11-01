using FluentValidation;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Application.Features.SevkiyatIslemleri.Validators;

namespace _3K.Application.Features.ProjeIslemleri.Validators;

/// <summary>Tekil sevk: sefer bilgileri toplu sevkle aynı kurallardır (araç plakası zorunlu).</summary>
public sealed class ProjeSevkEtCommandValidator : AbstractValidator<ProjeSevkEtCommand>
{
    public ProjeSevkEtCommandValidator()
    {
        RuleFor(x => x.ProjeId).GreaterThan(0);
        Include(new SevkSeferiBilgisiValidator());
    }
}
