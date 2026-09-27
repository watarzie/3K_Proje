using FluentValidation;
using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Validators;

public sealed class FinansDuzenliIsKaydetModelValidator : AbstractValidator<FinansDuzenliIsKaydetModel>
{
    public FinansDuzenliIsKaydetModelValidator()
    {
        RuleFor(x => x.IsAdi).NotEmpty().MaximumLength(250);
        RuleFor(x => x.IsTuru).IsInEnum();
        RuleFor(x => x.ManuelProjeNo).MaximumLength(100);
        RuleFor(x => x.ManuelProjeAdi).MaximumLength(250);
        RuleFor(x => x.Musteri).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Aciklama).MaximumLength(2000);
        RuleFor(x => x.BaslangicTarihi).Must(FinansTarihKurallari.GecerliTarih);
        RuleFor(x => x.BitisTarihi).GreaterThanOrEqualTo(x => x.BaslangicTarihi).When(x => x.BitisTarihi.HasValue);
        RuleFor(x => x.OlusturmaGunu).InclusiveBetween(1, 31);
        RuleFor(x => x.Miktar).GreaterThan(0);
        RuleFor(x => x.Birim).NotEmpty().MaximumLength(30);
        RuleFor(x => x.BirimFiyat).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ParaBirimi).Must(FinansValidationRules.Currency);
        RuleFor(x => x.KdvOrani).InclusiveBetween(0, 100);
        RuleFor(x => x.OzelIsTuru).MaximumLength(150);
        RuleFor(x => x.HesaplamaYontemi).IsInEnum();
        RuleFor(x => x.RaporGrubu).NotEmpty().MaximumLength(150);
        RuleFor(x => x).Must(x => !(x.ProjeId.HasValue && !string.IsNullOrWhiteSpace(x.ManuelProjeNo)))
            .WithMessage("Sistem projesi ile manuel proje aynı anda seçilemez.");
    }
}

public sealed class FinansDuzenliIsOlusturCommandValidator : AbstractValidator<FinansDuzenliIsOlusturCommand>
{
    public FinansDuzenliIsOlusturCommandValidator() => RuleFor(x => x.Model).NotNull().SetValidator(new FinansDuzenliIsKaydetModelValidator());
}

public sealed class FinansDuzenliIsGuncelleCommandValidator : AbstractValidator<FinansDuzenliIsGuncelleCommand>
{
    public FinansDuzenliIsGuncelleCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).NotNull().SetValidator(new FinansDuzenliIsKaydetModelValidator());
    }
}

public sealed class FinansGiderKategoriKaydetModelValidator : AbstractValidator<FinansGiderKategoriKaydetModel>
{
    public FinansGiderKategoriKaydetModelValidator() => RuleFor(x => x.Ad).NotEmpty().MaximumLength(150);
}

public sealed class FinansGiderKategoriOlusturCommandValidator : AbstractValidator<FinansGiderKategoriOlusturCommand>
{
    public FinansGiderKategoriOlusturCommandValidator() => RuleFor(x => x.Model).NotNull().SetValidator(new FinansGiderKategoriKaydetModelValidator());
}

public sealed class FinansGiderKategoriGuncelleCommandValidator : AbstractValidator<FinansGiderKategoriGuncelleCommand>
{
    public FinansGiderKategoriGuncelleCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).NotNull().SetValidator(new FinansGiderKategoriKaydetModelValidator());
    }
}

public sealed class FinansGiderKalemiKaydetModelValidator : AbstractValidator<FinansGiderKalemiKaydetModel>
{
    public FinansGiderKalemiKaydetModelValidator()
    {
        RuleFor(x => x.KategoriId).GreaterThan(0);
        RuleFor(x => x.Kod).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Ad).NotEmpty().MaximumLength(150);
        RuleFor(x => x.VarsayilanFirmaVeyaKisi).MaximumLength(250);
        RuleFor(x => x.VarsayilanMiktar).GreaterThan(0).When(x => x.VarsayilanMiktar.HasValue);
        RuleFor(x => x.VarsayilanBirim).MaximumLength(30);
        RuleFor(x => x.VarsayilanBirimFiyat).GreaterThanOrEqualTo(0).When(x => x.VarsayilanBirimFiyat.HasValue);
        RuleFor(x => x.VarsayilanParaBirimi)
            .Must(x => x is null || FinansValidationRules.Currency(x))
            .WithMessage("Varsayılan para birimi üç karakter olmalıdır.");
        RuleFor(x => x.VarsayilanKdvOrani).InclusiveBetween(0, 100).When(x => x.VarsayilanKdvOrani.HasValue);
    }
}

public sealed class FinansGiderKalemiOlusturCommandValidator : AbstractValidator<FinansGiderKalemiOlusturCommand>
{
    public FinansGiderKalemiOlusturCommandValidator() => RuleFor(x => x.Model).NotNull().SetValidator(new FinansGiderKalemiKaydetModelValidator());
}

public sealed class FinansGiderKalemiGuncelleCommandValidator : AbstractValidator<FinansGiderKalemiGuncelleCommand>
{
    public FinansGiderKalemiGuncelleCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).NotNull().SetValidator(new FinansGiderKalemiKaydetModelValidator());
    }
}

public sealed class FinansUrunKaydetModelValidator : AbstractValidator<FinansUrunKaydetModel>
{
    public FinansUrunKaydetModelValidator()
    {
        RuleFor(x => x.Kod).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Ad).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FiyatlandirmaBirimi).IsInEnum();
        RuleFor(x => x.Sira).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BirimFiyat).GreaterThanOrEqualTo(0).When(x => x.BirimFiyat.HasValue);
        RuleFor(x => x.ParaBirimi)
            .Must(x => x is null || FinansValidationRules.Currency(x))
            .WithMessage("Para birimi üç karakter olmalıdır.");
        RuleFor(x => x.KdvOrani).InclusiveBetween(0, 100).When(x => x.KdvOrani.HasValue);
        RuleForEach(x => x.Eslesmeler).ChildRules(match =>
        {
            match.RuleFor(x => x.IsTuru).IsInEnum();
            match.RuleFor(x => x.SandikAdi).MaximumLength(250);
            match.RuleFor(x => x.SandikTipi).MaximumLength(100);
            match.RuleFor(x => x.Boy).GreaterThan(0).When(x => x.Boy.HasValue);
            match.RuleFor(x => x.En).GreaterThan(0).When(x => x.En.HasValue);
            match.RuleFor(x => x.Yukseklik).GreaterThan(0).When(x => x.Yukseklik.HasValue);
        });
    }
}

public sealed class FinansUrunOlusturCommandValidator : AbstractValidator<FinansUrunOlusturCommand>
{
    public FinansUrunOlusturCommandValidator() => RuleFor(x => x.Model).NotNull().SetValidator(new FinansUrunKaydetModelValidator());
}

public sealed class FinansUrunGuncelleCommandValidator : AbstractValidator<FinansUrunGuncelleCommand>
{
    public FinansUrunGuncelleCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).NotNull().SetValidator(new FinansUrunKaydetModelValidator());
    }
}
