using FluentValidation;
using _3K.Application.Features.SevkiyatIslemleri.Commands;
using _3K.Core.Helpers;
using _3K.Core.Models;

namespace _3K.Application.Features.SevkiyatIslemleri.Validators;

public sealed class TopluSevkEtCommandValidator : AbstractValidator<TopluSevkEtCommand>
{
    public TopluSevkEtCommandValidator()
    {
        RuleFor(x => x.IslemAnahtari).NotEmpty().WithMessage("İşlem anahtarı zorunludur.");
        RuleFor(x => x.AracPlaka)
            .Must(p => AracPlakaKurali.Normalize(p) is not null).WithMessage("Araç plakası zorunludur.")
            .Must(p => (p ?? string.Empty).Trim().Length <= AracPlakaKurali.EnFazlaUzunluk)
            .WithMessage($"Araç plakası en fazla {AracPlakaKurali.EnFazlaUzunluk} karakter olabilir.");
        RuleFor(x => x.DorsePlaka).MaximumLength(AracPlakaKurali.EnFazlaUzunluk);
        RuleFor(x => x.Sofor).MaximumLength(SevkSeferiKurali.SoforEnFazla);
        RuleFor(x => x.Aciklama).MaximumLength(SevkSeferiKurali.AciklamaEnFazla);
        RuleFor(x => x.SevkTarihi)
            .Must(t => t >= TurkeyTime.Now.Date.AddYears(-2) && t <= TurkeyTime.Now.Date.AddDays(8))
            .WithMessage("Sevk tarihi son iki yıl içinde olmalı ve bir haftadan ileri olmamalıdır.");
        RuleFor(x => x.Projeler)
            .NotEmpty().WithMessage("En az bir proje seçilmelidir.")
            .Must(p => p.Count <= SevkSeferiKurali.EnFazlaProje)
            .WithMessage($"Bir seferde en fazla {SevkSeferiKurali.EnFazlaProje} proje sevk edilebilir.")
            .Must(p => p.Select(x => x.ProjeId).Distinct().Count() == p.Count)
            .WithMessage("Aynı proje birden fazla kez seçilemez.");
        RuleForEach(x => x.Projeler).ChildRules(proje =>
        {
            proje.RuleFor(p => p.ProjeId).GreaterThan(0);
            proje.RuleFor(p => p.SandikIds).NotEmpty().WithMessage("Her proje için en az bir sandık seçilmelidir.");
            proje.RuleForEach(p => p.SandikIds).GreaterThan(0);
        });
        RuleFor(x => x.Projeler)
            .Must(p => p.Sum(x => x.SandikIds.Count) <= SevkSeferiKurali.EnFazlaSandik)
            .WithMessage($"Bir seferde en fazla {SevkSeferiKurali.EnFazlaSandik} sandık sevk edilebilir.")
            .Must(p => p.SelectMany(x => x.SandikIds).Distinct().Count() == p.Sum(x => x.SandikIds.Count))
            .WithMessage("Aynı sandık birden fazla kez seçilemez.");
    }
}
