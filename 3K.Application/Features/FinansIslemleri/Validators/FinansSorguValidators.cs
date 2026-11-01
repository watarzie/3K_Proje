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

/// <summary>Finansal durum raporu panelle aynı tarih aralığı kuralını uygular (geçersiz aralık 400).</summary>
public sealed class FinansFinansalDurumRaporuDosyaQueryValidator : AbstractValidator<FinansFinansalDurumRaporuDosyaQuery>
{
    public FinansFinansalDurumRaporuDosyaQueryValidator() => RuleFor(x => x.Filtre)
        .Must(FinansalDurumAraligiGecerli)
        .WithMessage("Geçerli ve en fazla on yıllık tarih aralığı seçin.");

    internal static bool FinansalDurumAraligiGecerli(FinansListeFiltre filtre)
    {
        var (baslangic, bitis) = FinansTarihKurallari.FinansalDurumDonemi(filtre.Baslangic, filtre.Bitis);
        return FinansTarihKurallari.GecerliAralik(baslangic, bitis);
    }
}

/// <summary>Finansal durum önizlemesi dosyayla aynı tarih aralığı kuralını uygular; diğer türler servis kuralıyla denetlenir.</summary>
public sealed class FinansRaporOnizlemeQueryValidator : AbstractValidator<FinansRaporOnizlemeQuery>
{
    public FinansRaporOnizlemeQueryValidator() => RuleFor(x => x.Filtre)
        .Must(FinansFinansalDurumRaporuDosyaQueryValidator.FinansalDurumAraligiGecerli)
        .When(x => string.Equals(x.Tur?.Trim(), "finansal-durum", StringComparison.OrdinalIgnoreCase))
        .WithMessage("Geçerli ve en fazla on yıllık tarih aralığı seçin.");
}

public sealed class FinansAylikRaporDosyaQueryValidator : AbstractValidator<FinansAylikRaporDosyaQuery>
{
    public FinansAylikRaporDosyaQueryValidator() => RuleFor(x => x)
        .Must(x => FinansTarihKurallari.GecerliDonem(x.Yil, x.Ay))
        .WithMessage("Geçerli bir yıl ve ay seçilmelidir.");
}
