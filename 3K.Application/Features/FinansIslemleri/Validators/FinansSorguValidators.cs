using FluentValidation;
using _3K.Application.Features.FinansIslemleri.Queries;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Validators;

public sealed class FinansPanelQueryValidator : AbstractValidator<FinansPanelQuery>
{
    public FinansPanelQueryValidator() => RuleFor(x => x)
        .Must(x => FinansTarihKurallari.GecerliAralik(x.Baslangic, x.Bitis))
        .WithMessage("Geçerli ve en fazla on yıllık tarih aralığı seçin.");
}

public sealed class FinansAylikIslerQueryValidator : AbstractValidator<FinansAylikIslerQuery>
{
    public FinansAylikIslerQueryValidator() => RuleFor(x => x)
        .Must(x => FinansTarihKurallari.GecerliDonem(x.Yil, x.Ay))
        .WithMessage("Geçerli bir yıl ve ay seçilmelidir.");
}

public sealed class FinansAylikRaporDosyaQueryValidator : AbstractValidator<FinansAylikRaporDosyaQuery>
{
    public FinansAylikRaporDosyaQueryValidator() => RuleFor(x => x)
        .Must(x => FinansTarihKurallari.GecerliDonem(x.Yil, x.Ay))
        .WithMessage("Geçerli bir yıl ve ay seçilmelidir.");
}
