using FluentValidation;
using _3K.Application.Features.FinansIslemleri.Commands;

namespace _3K.Application.Features.FinansIslemleri.Validators;

public sealed class FinansPoOnayGonderCommandValidator : AbstractValidator<FinansPoOnayGonderCommand>
{
    public FinansPoOnayGonderCommandValidator()
    {
        RuleFor(x => x.Model).NotNull();
        RuleFor(x => x.Model.IsKaydiIds)
            .NotEmpty().WithMessage("Onaya gönderilecek en az bir iş seçilmelidir.")
            .Must(x => x.Count <= 2000).WithMessage("Tek işlemde en fazla 2000 iş onaya gönderilebilir.")
            .When(x => x.Model is not null);
        RuleForEach(x => x.Model.IsKaydiIds).GreaterThan(0).When(x => x.Model?.IsKaydiIds is not null);
    }
}

public sealed class FinansPoOnayUygulaCommandValidator : AbstractValidator<FinansPoOnayUygulaCommand>
{
    public FinansPoOnayUygulaCommandValidator()
    {
        RuleFor(x => x.TalepId).GreaterThan(0);
        RuleFor(x => x.OnizlemeHash).NotEmpty().Length(64);
        RuleFor(x => x.ProjeNo).MaximumLength(100);
        RuleFor(x => x.Ozet).MaximumLength(500);
    }
}
