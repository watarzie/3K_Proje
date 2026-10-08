using FluentValidation;
using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Validators;

public sealed class FinansIsKaydiKaydetModelValidator : AbstractValidator<FinansIsKaydiKaydetModel>
{
    public FinansIsKaydiKaydetModelValidator()
    {
        RuleFor(x => x.IsTuru).IsInEnum();
        RuleFor(x => x.IsAdi).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Aciklama).MaximumLength(2000);
        RuleFor(x => x.TalepEdenKisi).MaximumLength(200);
        RuleFor(x => x.TalepEdenBolum).MaximumLength(200);
        RuleFor(x => x.Adet).GreaterThan(0);
        RuleFor(x => x.Birim).NotEmpty().MaximumLength(30);
        RuleFor(x => x.BirimM3).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AhsapM3).InclusiveBetween(0, FinansHacimKurallari.EnFazlaM3).When(x => x.AhsapM3.HasValue);
        RuleFor(x => x.KontraM3).InclusiveBetween(0, FinansHacimKurallari.EnFazlaM3).When(x => x.KontraM3.HasValue);
        RuleFor(x => x).Must(x => FinansHacimKurallari.Bilesik(x.AhsapM3, x.KontraM3) > 0)
            .When(x => x.HesaplamaYontemi == _3K.Core.Enums.FinansHesaplamaYontemi.Metrekup)
            .WithMessage("m³ yönteminde ahşap veya kontra m³ girilmelidir.");
        RuleFor(x => x.UretimTarihi).Must(FinansTarihKurallari.GecerliTarih).WithMessage("Geçerli bir üretim tarihi girilmelidir.");
        RuleFor(x => x.FinansDonemi).Must(FinansTarihKurallari.GecerliTarih).WithMessage("Geçerli bir finans dönemi girilmelidir.");
        RuleFor(x => x.ManuelProjeNo).MaximumLength(100);
        RuleFor(x => x.ManuelProjeAdi).MaximumLength(250);
        RuleFor(x => x.Musteri).MaximumLength(250);
        RuleFor(x => x.SandikNo).MaximumLength(100);
        RuleFor(x => x.SandikAdi).MaximumLength(250);
        RuleFor(x => x.SandikTipi).MaximumLength(100);
        RuleFor(x => x.OzelIsTuru).MaximumLength(150);
        RuleFor(x => x.HesaplamaYontemi).IsInEnum().When(x => x.HesaplamaYontemi.HasValue);
        RuleFor(x => x.RaporGrubu).MaximumLength(150);
        RuleFor(x => x.ParaBirimi).Must(x => x is null || FinansValidationRules.Currency(x)).WithMessage("Para birimi üç karakter olmalıdır.");
        RuleFor(x => x.ManuelBirimFiyat).GreaterThanOrEqualTo(0).When(x => x.ManuelBirimFiyat.HasValue);
        RuleFor(x => x.KdvOrani).InclusiveBetween(0, 100).When(x => x.KdvOrani.HasValue);
        RuleFor(x => x).Must(x => !(x.ProjeId.HasValue && !string.IsNullOrWhiteSpace(x.ManuelProjeNo)))
            .WithMessage("Sistem projesi ile manuel proje aynı anda seçilemez.");
    }
}

public sealed class FinansMunferitIsKaydetModelValidator : AbstractValidator<FinansMunferitIsKaydetModel>
{
    public FinansMunferitIsKaydetModelValidator()
    {
        RuleFor(x => x.FinansUrunId).GreaterThan(0).WithMessage("İş/ürün tanımı seçilmelidir.");
        RuleFor(x => x.IsAdi).NotEmpty().MaximumLength(250);
        RuleFor(x => x.OzelIsTuru).MaximumLength(150);
        RuleFor(x => x.Musteri).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Adet).GreaterThan(0);
        RuleFor(x => x.Birim).NotEmpty().MaximumLength(30);
        RuleFor(x => x.AhsapM3).InclusiveBetween(0, FinansHacimKurallari.EnFazlaM3).When(x => x.AhsapM3.HasValue);
        RuleFor(x => x.KontraM3).InclusiveBetween(0, FinansHacimKurallari.EnFazlaM3).When(x => x.KontraM3.HasValue);
        RuleFor(x => x.IsTarihi).Must(FinansTarihKurallari.GecerliTarih).WithMessage("Geçerli bir iş tarihi girilmelidir.");
        RuleFor(x => x.Aciklama).MaximumLength(2000);
        RuleFor(x => x.TalepEdenKisi).MaximumLength(200);
        RuleFor(x => x.TalepEdenBolum).MaximumLength(200);
    }
}

public sealed class FinansMunferitIsOlusturCommandValidator : AbstractValidator<FinansMunferitIsOlusturCommand>
{
    public FinansMunferitIsOlusturCommandValidator() => RuleFor(x => x.Model).NotNull().SetValidator(new FinansMunferitIsKaydetModelValidator());
}

public sealed class FinansIsKaydiOlusturCommandValidator : AbstractValidator<FinansIsKaydiOlusturCommand>
{
    public FinansIsKaydiOlusturCommandValidator() => RuleFor(x => x.Model).NotNull().SetValidator(new FinansIsKaydiKaydetModelValidator());
}

public sealed class FinansIsKaydiIptalCommandValidator :
    AbstractValidator<FinansIsKaydiIptalCommand>
{
    public FinansIsKaydiIptalCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Aciklama).Must(FinansGerekceKurali.Gecerli).WithMessage(FinansGerekceKurali.Mesaj);
    }
}

public sealed class FinansTarihiDegistirCommandValidator : AbstractValidator<FinansTarihiDegistirCommand>
{
    public FinansTarihiDegistirCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).NotNull().DependentRules(() =>
        {
            RuleFor(x => x.Model.FinansTarihi).Must(FinansTarihKurallari.GecerliTarih).WithMessage("Geçerli finans tarihi girin.");
            RuleFor(x => x.Model.Aciklama).Must(FinansGerekceKurali.Gecerli).WithMessage(FinansGerekceKurali.Mesaj);
        });
    }
}

public sealed class FinansHacimGirCommandValidator : AbstractValidator<FinansHacimGirCommand>
{
    public FinansHacimGirCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).NotNull().DependentRules(() =>
        {
            RuleFor(x => x.Model)
                .Must(x => FinansHacimKurallari.GecerliManuelHacim(x.AhsapM3, x.KontraM3))
                .WithMessage("Ahşap ve kontra m³ sıfır veya pozitif olmalı; en az biri sıfırdan büyük olmalıdır.");
            RuleFor(x => x.Model.Aciklama).Must(FinansGerekceKurali.Gecerli).WithMessage(FinansGerekceKurali.Mesaj);
        });
    }
}

public sealed class FinansFiyatlandirCommandValidator : AbstractValidator<FinansFiyatlandirCommand>
{
    public FinansFiyatlandirCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).NotNull().DependentRules(() => RuleFor(x => x.Model.Aciklama).Must(FinansGerekceKurali.Gecerli).WithMessage(FinansGerekceKurali.Mesaj));
    }
}
