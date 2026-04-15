using FluentValidation;
using _3K.Application.Features.CekiIslemleri.Commands;

namespace _3K.Application.Features.CekiIslemleri.Validators
{
    public class CekiYukleCommandValidator : AbstractValidator<CekiYukleCommand>
    {
        public CekiYukleCommandValidator()
        {
            RuleFor(x => x.DosyaAdi).NotEmpty().WithMessage("Dosya adı boş olamaz.");
            RuleFor(x => x.ExcelDosya).NotNull().WithMessage("Excel dosyası yüklenmeli.");
        }
    }
}
