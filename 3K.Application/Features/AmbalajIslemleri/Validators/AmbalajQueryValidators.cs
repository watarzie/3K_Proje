using FluentValidation;
using _3K.Application.Features.AmbalajIslemleri.Queries;
using _3K.Core.Enums;

namespace _3K.Application.Features.AmbalajIslemleri.Validators
{
    public sealed class GetAmbalajPlanlamaProjeleriQueryValidator
        : AbstractValidator<GetAmbalajPlanlamaProjeleriQuery>
    {
        public GetAmbalajPlanlamaProjeleriQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.ProjeTipiId).InclusiveBetween(1, 3).When(x => x.ProjeTipiId.HasValue);
            RuleFor(x => x.Grup).InclusiveBetween(1, 3);
            RuleFor(x => x.Arama).MaximumLength(200);
        }
    }

    public sealed class GetAmbalajBagimsizSandiklarQueryValidator
        : AbstractValidator<GetAmbalajBagimsizSandiklarQuery>
    {
        public GetAmbalajBagimsizSandiklarQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.Tur).Must(x => x is 2 or 3 or 4 or 5)
                .When(x => x.Tur.HasValue)
                .WithMessage("Özel sandık türü geçersiz.");
            RuleFor(x => x.Arama).MaximumLength(200);
        }
    }

    public sealed class GetAmbalajProjeSandikSecenekleriQueryValidator
        : AbstractValidator<GetAmbalajProjeSandikSecenekleriQuery>
    {
        public GetAmbalajProjeSandikSecenekleriQueryValidator()
        {
            RuleFor(x => x.ProjeId).GreaterThan(0);
        }
    }

    public sealed class GetAmbalajRaporDosyasiQueryValidator : AbstractValidator<GetAmbalajRaporDosyasiQuery>
    {
        public GetAmbalajRaporDosyasiQueryValidator()
        {
            RuleFor(x => x.Tur)
                .Must(x => x is AmbalajSandikTuru.Ilave or AmbalajSandikTuru.Ic or AmbalajSandikTuru.Saha or AmbalajSandikTuru.Yedek)
                .WithMessage("Özel sandık türü geçersiz.");
        }
    }

    public sealed class GetAmbalajUretimFormuDosyasiQueryValidator : AbstractValidator<GetAmbalajUretimFormuDosyasiQuery>
    {
        public GetAmbalajUretimFormuDosyasiQueryValidator()
        {
            RuleFor(x => x.ProjeId).GreaterThan(0);
            RuleFor(x => x.Tur).IsInEnum().When(x => x.Tur.HasValue);
        }
    }
}
