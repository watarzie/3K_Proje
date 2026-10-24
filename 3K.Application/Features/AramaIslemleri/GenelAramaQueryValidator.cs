using FluentValidation;

namespace _3K.Application.Features.AramaIslemleri;

public sealed class GenelAramaQueryValidator : AbstractValidator<GenelAramaQuery>
{
    public GenelAramaQueryValidator()
    {
        RuleFor(x => x.Q).Must(GenelAramaKurali.Gecerli)
            .WithMessage($"Aramak için en az {GenelAramaKurali.EnAzUzunluk}, en fazla {GenelAramaKurali.EnFazlaUzunluk} karakter yazın.");
        RuleFor(x => x.Limit).InclusiveBetween(1, GenelAramaKurali.EnFazlaLimit)
            .WithMessage($"Sonuç sayısı 1 ile {GenelAramaKurali.EnFazlaLimit} arasında olmalıdır.");
    }
}
