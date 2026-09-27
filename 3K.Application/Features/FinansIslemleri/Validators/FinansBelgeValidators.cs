using FluentValidation;
using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Validators;

public sealed class FinansKaliciSilCommandValidator : AbstractValidator<FinansKaliciSilCommand>
{
    public FinansKaliciSilCommandValidator() =>
        RuleFor(x => x.Model).NotNull().DependentRules(() => RuleFor(x => x.Model.Aciklama).Must(FinansGerekceKurali.Gecerli).WithMessage(FinansGerekceKurali.Mesaj));
}
