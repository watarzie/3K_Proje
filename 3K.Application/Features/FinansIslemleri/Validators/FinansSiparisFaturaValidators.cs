using FluentValidation;
using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Validators;

public sealed class FinansSiparisOlusturModelValidator : AbstractValidator<FinansSiparisOlusturModel>
{
    public FinansSiparisOlusturModelValidator()
    {
        RuleFor(x => x.PoNumarasi).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SiparisTarihi).Must(FinansTarihKurallari.GecerliTarih);
        RuleFor(x => x.Aciklama).MaximumLength(2000);
        RuleFor(x => x.Kalemler).NotEmpty();
        RuleForEach(x => x.Kalemler).ChildRules(line =>
        {
            line.RuleFor(x => x.IsKaydiId).GreaterThan(0);
            line.RuleFor(x => x.Adet).GreaterThanOrEqualTo(0);
            line.RuleFor(x => x.M3).GreaterThanOrEqualTo(0);
            line.RuleFor(x => x).Must(x => x.NetTutar > 0 || x.Adet > 0 || x.M3 > 0).WithMessage("Sipariş kalemi net tutarı veya miktarı sıfırdan büyük olmalıdır.");
            line.RuleFor(x => x.NetTutar).GreaterThan(0).When(x => x.NetTutar.HasValue);
            line.RuleFor(x => x.BirimFiyat).GreaterThan(0).When(x => x.BirimFiyat.HasValue);
            line.RuleFor(x => x.KdvOrani).InclusiveBetween(0, 100).When(x => x.KdvOrani.HasValue);
            line.RuleFor(x => x.ParaBirimi).Must(x => x is null || FinansValidationRules.Currency(x));
        });
    }
}

public sealed class FinansSiparisOlusturCommandValidator : AbstractValidator<FinansSiparisOlusturCommand>
{
    public FinansSiparisOlusturCommandValidator() => RuleFor(x => x.Model).NotNull().SetValidator(new FinansSiparisOlusturModelValidator());
}

public sealed class FinansSiparisGuncelleModelValidator : AbstractValidator<FinansSiparisGuncelleModel>
{
    public FinansSiparisGuncelleModelValidator()
    {
        RuleFor(x => x.PoNumarasi).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SiparisTarihi).Must(FinansTarihKurallari.GecerliTarih);
        RuleFor(x => x.Aciklama).MaximumLength(2000);
        RuleFor(x => x.Gerekce).Must(FinansGerekceKurali.Gecerli).WithMessage(FinansGerekceKurali.Mesaj).When(x => x.Kalemler is not null);
    }
}

public sealed class FinansSiparisGuncelleCommandValidator : AbstractValidator<FinansSiparisGuncelleCommand>
{
    public FinansSiparisGuncelleCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).NotNull().SetValidator(new FinansSiparisGuncelleModelValidator());
    }
}

public sealed class FinansSiparisIptalCommandValidator : AbstractValidator<FinansSiparisIptalCommand>
{
    public FinansSiparisIptalCommandValidator() { RuleFor(x => x.Id).GreaterThan(0); RuleFor(x => x.Aciklama).Must(FinansGerekceKurali.Gecerli).WithMessage(FinansGerekceKurali.Mesaj); }
}

public sealed class FinansFaturaOlusturModelValidator : AbstractValidator<FinansFaturaOlusturModel>
{
    public FinansFaturaOlusturModelValidator()
    {
        RuleFor(x => x.SiparisId).GreaterThan(0);
        RuleFor(x => x.FaturaNumarasi).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FaturaTarihi).Must(FinansTarihKurallari.GecerliTarih);
        RuleFor(x => x.Aciklama).MaximumLength(2000);
        AddDocumentTotalRules();
        RuleFor(x => x.Kalemler).NotEmpty();
        RuleForEach(x => x.Kalemler).ChildRules(line =>
        {
            line.RuleFor(x => x.SiparisKalemiId).GreaterThan(0);
            line.RuleFor(x => x.Adet).GreaterThanOrEqualTo(0);
            line.RuleFor(x => x.M3).GreaterThanOrEqualTo(0);
            line.RuleFor(x => x).Must(x => x.NetTutar > 0 || x.Adet > 0 || x.M3 > 0).WithMessage("Fatura kalemi net tutarı veya miktarı sıfırdan büyük olmalıdır.");
            line.RuleFor(x => x.NetTutar).GreaterThan(0).When(x => x.NetTutar.HasValue);
        });
    }

    private void AddDocumentTotalRules()
    {
        RuleFor(x => x.BelgeParaBirimi).Must(x => x is null || FinansValidationRules.Currency(x));
        RuleFor(x => x.BelgeNetTutar).GreaterThanOrEqualTo(0).When(x => x.BelgeNetTutar.HasValue);
        RuleFor(x => x.BelgeKdvTutari).GreaterThanOrEqualTo(0).When(x => x.BelgeKdvTutari.HasValue);
        RuleFor(x => x.BelgeToplamTutar).GreaterThanOrEqualTo(0).When(x => x.BelgeToplamTutar.HasValue);
        RuleFor(x => x.MutabakatAciklamasi).MaximumLength(1000);
        RuleFor(x => x).Must(x => FinansValidationRules.CompleteDocumentTotals(
                x.BelgeParaBirimi, x.BelgeNetTutar, x.BelgeKdvTutari, x.BelgeToplamTutar))
            .WithMessage("Belge para birimi, net, KDV ve brüt toplam alanları birlikte girilmelidir.");
        RuleFor(x => x).Must(x => FinansValidationRules.BalancedDocumentTotals(
                x.BelgeNetTutar, x.BelgeKdvTutari, x.BelgeToplamTutar))
            .WithMessage("Belge net + KDV toplamı brüt toplamla eşleşmelidir.");
    }
}

public sealed class FinansFaturaOlusturCommandValidator : AbstractValidator<FinansFaturaOlusturCommand>
{
    public FinansFaturaOlusturCommandValidator() => RuleFor(x => x.Model).NotNull().SetValidator(new FinansFaturaOlusturModelValidator());
}

public sealed class FinansFaturaGuncelleModelValidator : AbstractValidator<FinansFaturaGuncelleModel>
{
    public FinansFaturaGuncelleModelValidator()
    {
        RuleFor(x => x.FaturaNumarasi).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FaturaTarihi).Must(FinansTarihKurallari.GecerliTarih);
        RuleFor(x => x.Aciklama).MaximumLength(2000);
        RuleFor(x => x.Gerekce).Must(FinansGerekceKurali.Gecerli).WithMessage(FinansGerekceKurali.Mesaj).When(x => x.Kalemler is not null);
        When(x => !x.BelgeMutabakatiniKoru, () =>
        {
            RuleFor(x => x.BelgeParaBirimi).Must(x => x is null || FinansValidationRules.Currency(x));
            RuleFor(x => x.BelgeNetTutar).GreaterThanOrEqualTo(0).When(x => x.BelgeNetTutar.HasValue);
            RuleFor(x => x.BelgeKdvTutari).GreaterThanOrEqualTo(0).When(x => x.BelgeKdvTutari.HasValue);
            RuleFor(x => x.BelgeToplamTutar).GreaterThanOrEqualTo(0).When(x => x.BelgeToplamTutar.HasValue);
            RuleFor(x => x.MutabakatAciklamasi).MaximumLength(1000);
            RuleFor(x => x).Must(x => FinansValidationRules.CompleteDocumentTotals(
                    x.BelgeParaBirimi, x.BelgeNetTutar, x.BelgeKdvTutari, x.BelgeToplamTutar))
                .WithMessage("Belge para birimi, net, KDV ve brüt toplam alanları birlikte girilmelidir.");
            RuleFor(x => x).Must(x => FinansValidationRules.BalancedDocumentTotals(
                    x.BelgeNetTutar, x.BelgeKdvTutari, x.BelgeToplamTutar))
                .WithMessage("Belge net + KDV toplamı brüt toplamla eşleşmelidir.");
        });
    }
}

public sealed class FinansFaturaGuncelleCommandValidator : AbstractValidator<FinansFaturaGuncelleCommand>
{
    public FinansFaturaGuncelleCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).NotNull().SetValidator(new FinansFaturaGuncelleModelValidator());
    }
}

public sealed class FinansFaturaIptalCommandValidator : AbstractValidator<FinansFaturaIptalCommand>
{
    public FinansFaturaIptalCommandValidator() { RuleFor(x => x.Id).GreaterThan(0); RuleFor(x => x.Aciklama).Must(FinansGerekceKurali.Gecerli).WithMessage(FinansGerekceKurali.Mesaj); }
}
