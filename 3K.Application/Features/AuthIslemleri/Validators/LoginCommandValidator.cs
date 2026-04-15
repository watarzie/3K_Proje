using FluentValidation;
using _3K.Application.Features.AuthIslemleri.Commands;

namespace _3K.Application.Features.AuthIslemleri.Validators
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage("Email adresi boş olamaz.")
                .EmailAddress().WithMessage("Geçerli bir email formatı girilmeli.");
            RuleFor(x => x.Sifre).NotEmpty().WithMessage("Şifre boş olamaz.")
                .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalı.");
        }
    }
}
