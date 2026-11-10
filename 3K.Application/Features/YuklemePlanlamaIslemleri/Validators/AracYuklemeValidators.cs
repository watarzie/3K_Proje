using FluentValidation;
using _3K.Application.Features.YuklemePlanlamaIslemleri.Commands;
using _3K.Application.Features.YuklemePlanlamaIslemleri.Queries;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Features.YuklemePlanlamaIslemleri.Validators;

public sealed class AracYuklemePlanlariQueryValidator : AbstractValidator<AracYuklemePlanlariQuery>
{
    public AracYuklemePlanlariQueryValidator() =>
        RuleFor(x => x.Arama).MaximumLength(100).WithMessage("Arama en fazla 100 karakter olabilir.");
}

/// <summary>Barkod çözme sorgusu: okutulan metin boş ya da aşırı uzun olamaz (sandık barkodu biçimi serviste çözülür).</summary>
public sealed class AracYuklemeCozumleQueryValidator : AbstractValidator<AracYuklemeCozumleQuery>
{
    public AracYuklemeCozumleQueryValidator()
    {
        RuleFor(x => x.OturumId).GreaterThan(0).WithMessage("Yükleme oturumu seçilmelidir.");
        RuleFor(x => x.AracNo).GreaterThan(0).WithMessage("Araç seçilmelidir.");
        RuleFor(x => x.Barkod).NotEmpty().WithMessage("Barkod okutulmalıdır.")
            .MaximumLength(YuklemeOturumuKurali.BarkodEnFazla).WithMessage(YuklemeOturumuKurali.GecersizBarkodMesaji);
    }
}

public sealed class YuklemeOturumuBaslatCommandValidator : AbstractValidator<YuklemeOturumuBaslatCommand>
{
    public YuklemeOturumuBaslatCommandValidator()
    {
        RuleFor(x => x.PlanId).GreaterThan(0).WithMessage("Plan seçilmelidir.");
        RuleFor(x => x.BeklenenRevizyonNo).GreaterThan(0).WithMessage("Plan revizyonu eksik; sayfayı yenileyin.");
    }
}

public sealed class YukuYuklendiIsaretleCommandValidator : AbstractValidator<YukuYuklendiIsaretleCommand>
{
    public YukuYuklendiIsaretleCommandValidator()
    {
        RuleFor(x => x.OturumId).GreaterThan(0).WithMessage("Yükleme oturumu seçilmelidir.");
        RuleFor(x => x.AracNo).GreaterThan(0).WithMessage("Araç seçilmelidir.");
        RuleFor(x => x.YukAnahtari).NotEmpty().WithMessage("Yük seçilmelidir.")
            .MaximumLength(YuklemeOturumuKurali.YukAnahtariEnFazla).WithMessage(YuklemeOturumuKurali.PlandaYokMesaji);
        RuleFor(x => x.Kaynak).IsInEnum().WithMessage("İşaret kaynağı geçersiz.");
        // Barkod kaynağı okutulan metni taşır (yükle eşleşmesi serviste, aynı Core kuralıyla denetlenir).
        RuleFor(x => x.Barkod).NotEmpty().WithMessage("Barkod okutulmalıdır.")
            .MaximumLength(YuklemeOturumuKurali.BarkodEnFazla).WithMessage(YuklemeOturumuKurali.GecersizBarkodMesaji)
            .When(x => x.Kaynak == YuklemeIsaretKaynagi.Barkod);
    }
}

public sealed class YukIsaretiGeriAlCommandValidator : AbstractValidator<YukIsaretiGeriAlCommand>
{
    public YukIsaretiGeriAlCommandValidator()
    {
        RuleFor(x => x.OturumId).GreaterThan(0).WithMessage("Yükleme oturumu seçilmelidir.");
        RuleFor(x => x.IsaretId).GreaterThan(0).WithMessage("Yükleme işareti seçilmelidir.");
        RuleFor(x => x.Gerekce).Must(YuklemeOturumuKurali.GerekceGecerli).WithMessage(YuklemeOturumuKurali.GerekceMesaji);
    }
}

public sealed class YuklemeOturumuKapatCommandValidator : AbstractValidator<YuklemeOturumuKapatCommand>
{
    public YuklemeOturumuKapatCommandValidator()
    {
        RuleFor(x => x.OturumId).GreaterThan(0).WithMessage("Yükleme oturumu seçilmelidir.");
        // İptal her zaman gerekçe ister; tamamlamada gerekçe yazıldıysa geçerli olmalıdır (eksik yükte zorunluluk serviste).
        RuleFor(x => x.Gerekce).Must(YuklemeOturumuKurali.GerekceGecerli).WithMessage(YuklemeOturumuKurali.GerekceMesaji)
            .When(x => x.Iptal || !string.IsNullOrWhiteSpace(x.Gerekce));
    }
}
