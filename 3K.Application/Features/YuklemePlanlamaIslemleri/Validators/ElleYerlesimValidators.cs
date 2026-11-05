using FluentValidation;
using _3K.Application.Features.YuklemePlanlamaIslemleri.Commands;
using _3K.Core.Models;

namespace _3K.Application.Features.YuklemePlanlamaIslemleri.Validators;

/// <summary>Elle yerleşimin ortak kuralları: araç sayısı, yük sayısı ve koordinat aralığı.</summary>
internal sealed class ElleYerlesimIstegiValidator : AbstractValidator<IElleYerlesimIstegi>
{
    private static readonly string KoordinatMesaji = $"Konumlar ±{YuklemePlaniKurali.ElleKoordinatSiniriMm} mm içinde olmalıdır.";

    public ElleYerlesimIstegiValidator()
    {
        RuleFor(x => x.Araclar).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Araç listesi eksik.")
            .Must(a => a.Count <= YuklemePlaniKurali.ElleAracEnFazla).WithMessage($"Bir planda en fazla {YuklemePlaniKurali.ElleAracEnFazla} araç olabilir.")
            .Must(a => a.Sum(x => x?.Konumlar?.Count ?? 0) <= YuklemePlaniKurali.EnFazlaYuk).WithMessage($"Bir planda en fazla {YuklemePlaniKurali.EnFazlaYuk} yük olabilir.");
        // Null eleman (ör. "araclar": [null]) 500 değil 400 olmalı; alt kurallar null elemanda çalışmaz.
        RuleForEach(x => x.Araclar).NotNull().WithMessage("Araç bilgisi eksik.").ChildRules(a =>
        {
            a.RuleFor(x => x.KonfigurasyonId).GreaterThan(0).WithMessage("Araç ekipmanı seçilmelidir.");
            a.RuleForEach(x => x.Konumlar).NotNull().WithMessage("Yük konumu eksik.").ChildRules(k =>
            {
                k.RuleFor(x => x.YukAnahtari).NotEmpty().MaximumLength(64).WithMessage("Yük anahtarı geçersiz.");
                k.RuleFor(x => x.XMm).InclusiveBetween(-YuklemePlaniKurali.ElleKoordinatSiniriMm, YuklemePlaniKurali.ElleKoordinatSiniriMm).WithMessage(KoordinatMesaji);
                k.RuleFor(x => x.YMm).InclusiveBetween(-YuklemePlaniKurali.ElleKoordinatSiniriMm, YuklemePlaniKurali.ElleKoordinatSiniriMm).WithMessage(KoordinatMesaji);
                k.RuleFor(x => x.ZMm).InclusiveBetween(0, YuklemePlaniKurali.ElleKoordinatSiniriMm).WithMessage(KoordinatMesaji);
            });
        });
    }
}

/// <summary>Elle yerleşim denetimi isteğinin doğrulaması.</summary>
public sealed class YuklemePlaniElleDenetleQueryValidator : AbstractValidator<YuklemePlaniElleDenetleQuery>
{
    public YuklemePlaniElleDenetleQueryValidator()
    {
        RuleFor(x => x.PlanId).GreaterThan(0).WithMessage("Plan seçilmelidir.");
        Include(new ElleYerlesimIstegiValidator());
    }
}

/// <summary>Elle yerleşim kaydının doğrulaması (beklenen revizyon zorunlu).</summary>
public sealed class YuklemePlaniElleKaydetCommandValidator : AbstractValidator<YuklemePlaniElleKaydetCommand>
{
    public YuklemePlaniElleKaydetCommandValidator()
    {
        RuleFor(x => x.PlanId).GreaterThan(0).WithMessage("Plan seçilmelidir.");
        RuleFor(x => x.BeklenenRevizyonNo).GreaterThan(0).WithMessage("Planın revizyon numarası eksik; sayfayı yenileyin.");
        Include(new ElleYerlesimIstegiValidator());
    }
}
