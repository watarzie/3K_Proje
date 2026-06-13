using FluentValidation;
using _3K.Application.Features.CekiIslemleri.Commands;

namespace _3K.Application.Features.CekiIslemleri.Validators
{
    public class CekiRevizyonOnizleCommandValidator : AbstractValidator<CekiRevizyonOnizleCommand>
    {
        public CekiRevizyonOnizleCommandValidator()
        {
            RuleFor(x => x.DosyaAdi).NotEmpty().WithMessage("Dosya adı boş olamaz.");
            RuleFor(x => x.ExcelDosya).NotNull().WithMessage("Revizyon Excel dosyası yüklenmeli.");
        }
    }
}
