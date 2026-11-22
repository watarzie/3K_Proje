using FluentValidation;
using _3K.Application.Features.CekiIslemleri.Commands;

namespace _3K.Application.Features.CekiIslemleri.Validators
{
    public class CekiYukleCommandValidator : AbstractValidator<CekiYukleCommand>
    {
        public CekiYukleCommandValidator()
        {
            // İstemcinin gönderdiği ad güvenilmezdir; yedek ve revizyon yüklemesiyle aynı sınırlar uygulanır.
            RuleFor(x => x.DosyaAdi)
                .NotEmpty().WithMessage("Dosya adı boş olamaz.")
                .MaximumLength(255).WithMessage("Dosya adı 255 karakterden uzun olamaz.")
                .Must(dosyaAdi => string.Equals(
                    Path.GetExtension(dosyaAdi),
                    ".xlsx",
                    StringComparison.OrdinalIgnoreCase))
                .WithMessage("Çeki yalnızca .xlsx formatında yüklenebilir.");
            RuleFor(x => x.ExcelDosya).NotNull().WithMessage("Excel dosyası yüklenmeli.");
        }
    }
}
