using FluentValidation;
using _3K.Application.Features.SevkiyatIslemleri.Commands;
using _3K.Core.Helpers;
using _3K.Core.Models;

namespace _3K.Application.Features.SevkiyatIslemleri.Validators;

/// <summary>
/// Toplu ve tekil sevkin ortak sefer kuralları: araç plakası zorunlu, plaka/şoför/açıklama uzunlukları ve sevk tarihi
/// aralığı. İstek doğrulayıcıları bu kuralları <c>Include</c> ile alır; kural iki yerde ayrı yazılmaz.
/// </summary>
public sealed class SevkSeferiBilgisiValidator : AbstractValidator<ISevkSeferiBilgisi>
{
    public SevkSeferiBilgisiValidator()
    {
        RuleFor(x => x.AracPlaka)
            .Must(p => AracPlakaKurali.Normalize(p) is not null).WithMessage("Araç plakası zorunludur.")
            .Must(p => (p ?? string.Empty).Trim().Length <= AracPlakaKurali.EnFazlaUzunluk)
            .WithMessage($"Araç plakası en fazla {AracPlakaKurali.EnFazlaUzunluk} karakter olabilir.");
        RuleFor(x => x.DorsePlaka).MaximumLength(AracPlakaKurali.EnFazlaUzunluk);
        RuleFor(x => x.Sofor).MaximumLength(SevkSeferiKurali.SoforEnFazla);
        RuleFor(x => x.Aciklama).MaximumLength(SevkSeferiKurali.AciklamaEnFazla);
        RuleFor(x => x.SeferSevkTarihi)
            .Must(t => t is null || (t >= TurkeyTime.Now.Date.AddYears(-2) && t <= TurkeyTime.Now.Date.AddDays(8)))
            .WithMessage("Sevk tarihi son iki yıl içinde olmalı ve bir haftadan ileri olmamalıdır.");
    }
}
