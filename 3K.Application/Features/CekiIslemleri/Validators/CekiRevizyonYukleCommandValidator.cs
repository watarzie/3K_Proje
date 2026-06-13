using FluentValidation;
using _3K.Application.Features.CekiIslemleri.Commands;

namespace _3K.Application.Features.CekiIslemleri.Validators
{
    public class CekiRevizyonYukleCommandValidator : AbstractValidator<CekiRevizyonYukleCommand>
    {
        public CekiRevizyonYukleCommandValidator()
        {
            RuleFor(x => x.DosyaAdi).NotEmpty().WithMessage("Dosya adı boş olamaz.");
            RuleFor(x => x.ExcelDosya).NotNull().WithMessage("Revizyon Excel dosyası yüklenmeli.");
        }
    }
}
