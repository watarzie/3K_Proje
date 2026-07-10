using _3K.Application.Features.DashboardIslemleri.Queries;
using _3K.Core.Enums;
using FluentValidation;

namespace _3K.Application.Features.DashboardIslemleri.Validators
{
    public class DashboardProjeSandiklariDrillDownQueryValidator
        : AbstractValidator<DashboardProjeSandiklariDrillDownQuery>
    {
        public DashboardProjeSandiklariDrillDownQueryValidator()
        {
            RuleFor(query => query.ProjeId)
                .GreaterThan(0)
                .WithMessage("Geçerli bir proje seçilmelidir.");

            RuleFor(query => query.DurumId)
                .Must(durumId => Enum.IsDefined(typeof(SandikDurum), durumId))
                .WithMessage("Geçersiz sandık durumu.");

            RuleFor(query => query.SearchTerm)
                .MaximumLength(100)
                .When(query => !string.IsNullOrWhiteSpace(query.SearchTerm));

            RuleFor(query => query.Page)
                .InclusiveBetween(1, 1_000_000);

            RuleFor(query => query.PageSize)
                .InclusiveBetween(1, 100);
        }
    }
}
