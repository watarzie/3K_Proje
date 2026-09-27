using FluentValidation;
using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Validators;

public sealed class FinansGiderKaydetModelValidator : AbstractValidator<FinansGiderKaydetModel>
{
    public FinansGiderKaydetModelValidator()
    {
        RuleFor(x => x.Tarih).Must(FinansTarihKurallari.GecerliTarih);
        RuleFor(x => x.FinansDonemi).Must(FinansTarihKurallari.GecerliTarih);
        RuleFor(x => x.KategoriId).GreaterThan(0);
        RuleFor(x => x.AltKategori).MaximumLength(200);
        RuleFor(x => x.FirmaVeyaKisi).MaximumLength(250);
        RuleFor(x => x.Aciklama).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Miktar).GreaterThan(0);
        RuleFor(x => x.Birim).NotEmpty().MaximumLength(30);
        RuleFor(x => x.BirimFiyat).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ParaBirimi).Must(FinansValidationRules.Currency);
        RuleFor(x => x.KdvOrani).InclusiveBetween(0, 100);
        RuleFor(x => x.ManuelProjeNo).MaximumLength(100);
        RuleFor(x => x).Must(x => !(x.ProjeId.HasValue && !string.IsNullOrWhiteSpace(x.ManuelProjeNo)))
            .WithMessage("Sistem projesi ile manuel proje aynı anda seçilemez.");
    }
}

public sealed class FinansGiderOlusturCommandValidator : AbstractValidator<FinansGiderOlusturCommand>
{
    public FinansGiderOlusturCommandValidator() => RuleFor(x => x.Model).NotNull().SetValidator(new FinansGiderKaydetModelValidator());
}

public sealed class FinansGiderGuncelleCommandValidator : AbstractValidator<FinansGiderGuncelleCommand>
{
    public FinansGiderGuncelleCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).NotNull().SetValidator(new FinansGiderKaydetModelValidator());
    }
}

public sealed class FinansGiderIptalCommandValidator : AbstractValidator<FinansGiderIptalCommand>
{
    public FinansGiderIptalCommandValidator() { RuleFor(x => x.Id).GreaterThan(0); RuleFor(x => x.Aciklama).Must(FinansGerekceKurali.Gecerli).WithMessage(FinansGerekceKurali.Mesaj); }
}
