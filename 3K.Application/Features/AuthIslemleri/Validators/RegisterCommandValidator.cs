using FluentValidation;
using _3K.Application.Features.AuthIslemleri.Commands;

namespace _3K.Application.Features.AuthIslemleri.Validators
{
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.AdSoyad).NotEmpty().WithMessage("Ad Soyad boş olamaz.");
            RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Geçerli bir email adresi girilmeli.");
            RuleFor(x => x.Sifre).NotEmpty().MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalı.");
            RuleFor(x => x.Rol).NotEmpty().WithMessage("Rol belirtilmeli.");
        }
    }
}
