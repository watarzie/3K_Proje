using FluentValidation;
using _3K.Application.Features.CekiIslemleri.Commands;

namespace _3K.Application.Features.CekiIslemleri.Validators
{
    public sealed class CekiRevizyonOnayliUygulaCommandValidator
        : AbstractValidator<CekiRevizyonOnayliUygulaCommand>
    {
        public CekiRevizyonOnayliUygulaCommandValidator()
        {
            RuleFor(command => command.TalepId)
                .GreaterThan(0)
                .WithMessage("Revizyon talep bilgisi geçersiz.");

            RuleFor(command => command.ProjeId)
                .GreaterThan(0)
                .WithMessage("Revizyon proje bilgisi geçersiz.");
        }
    }
}
