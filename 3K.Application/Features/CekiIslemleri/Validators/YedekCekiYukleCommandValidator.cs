using FluentValidation;
using _3K.Application.Features.CekiIslemleri.Commands;

namespace _3K.Application.Features.CekiIslemleri.Validators;

public sealed class YedekCekiYukleCommandValidator : AbstractValidator<YedekCekiYukleCommand>
{
    public YedekCekiYukleCommandValidator()
    {
        RuleFor(command => command.DosyaAdi)
            .NotEmpty().WithMessage("Dosya adı boş olamaz.")
            .MaximumLength(255).WithMessage("Dosya adı 255 karakterden uzun olamaz.")
            .Must(dosyaAdi => string.Equals(
                Path.GetExtension(dosyaAdi),
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
            .WithMessage("Yedek çeki yalnızca .xlsx formatında yüklenebilir.");

        RuleFor(command => command.ExcelDosya)
            .NotNull().WithMessage("Excel dosyası yüklenmelidir.")
            .Must(stream => stream is { CanRead: true })
            .WithMessage("Yüklenen Excel dosyası okunamıyor.");

        RuleFor(command => command.KullaniciId)
            .GreaterThan(0)
            .WithMessage("Geçerli kullanıcı bilgisi bulunamadı.");
    }
}
