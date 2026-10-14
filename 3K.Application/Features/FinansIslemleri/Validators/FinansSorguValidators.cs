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

public sealed class FinansProjeSandikKirilimiQueryValidator : AbstractValidator<FinansProjeSandikKirilimiQuery>
{
    public FinansProjeSandikKirilimiQueryValidator()
    {
        RuleFor(x => x)
            .Must(x => x.ProjeId > 0 || !string.IsNullOrWhiteSpace(x.ProjeNo))
            .WithMessage("Sandık kırılımı için proje seçilmelidir.");
        RuleFor(x => x)
            .Must(x => x.Yil is null && x.Ay is null ||
                       x.Yil.HasValue && x.Ay.HasValue && FinansTarihKurallari.GecerliDonem(x.Yil.Value, x.Ay.Value))
            .WithMessage("Geçerli bir yıl ve ay seçilmelidir.");
        RuleFor(x => x.ProjeNo).MaximumLength(100);
        RuleFor(x => x.Musteri).MaximumLength(250);
        RuleFor(x => x.Filtre.SandikNo).MaximumLength(100);
    }
}

public sealed class FinansAylikRaporDosyaQueryValidator : AbstractValidator<FinansAylikRaporDosyaQuery>
{
    public FinansAylikRaporDosyaQueryValidator() => RuleFor(x => x)
        .Must(x => FinansTarihKurallari.GecerliDonem(x.Yil, x.Ay))
        .WithMessage("Geçerli bir yıl ve ay seçilmelidir.");
}
