using FluentValidation;
using _3K.Application.Features.SevkiyatIslemleri.Queries;

namespace _3K.Application.Features.SevkiyatIslemleri.Validators;

public sealed class SevkiyatDonemRaporuQueryValidator : AbstractValidator<SevkiyatDonemRaporuQuery>
{
    public SevkiyatDonemRaporuQueryValidator()
    {
        RuleFor(x => x.Format)
            .Must(f => SevkiyatRaporBicimleri.Coz(f) is not null)
            .WithMessage("Rapor biçimi pdf veya excel olmalıdır.");
        RuleFor(x => x)
            .Must(x => !x.Baslangic.HasValue || !x.Bitis.HasValue || x.Baslangic.Value.Date <= x.Bitis.Value.Date)
            .WithMessage("Başlangıç tarihi bitiş tarihinden sonra olamaz.");
    }
}

public sealed class SevkSeferiRaporuQueryValidator : AbstractValidator<SevkSeferiRaporuQuery>
{
    public SevkSeferiRaporuQueryValidator()
    {
        RuleFor(x => x)
            .Must(x => x.SeferId is > 0 || x.EskiTarih.HasValue)
            .WithMessage("Sefer belirtilmelidir.");
    }
}
