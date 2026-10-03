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
        RuleFor(x => x.ParaBirimi).Must(x => x is null || FinansValidationRules.Currency(x));
        RuleFor(x => x.BelgeNetTutar).GreaterThan(0).When(x => x.BelgeNetTutar.HasValue);
        RuleFor(x => x).Must(x => x.Kalemler.Count > 0 || x.Gruplar?.Count > 0)
            .WithMessage("Sipariş en az bir iş kalemi içermelidir.");
        RuleForEach(x => x.Kalemler).SetValidator(new FinansSiparisDagitimModelValidator());
        RuleForEach(x => x.Gruplar).SetValidator(new FinansSiparisGrupDagitimModelValidator());
    }
}

public sealed class FinansSiparisDagitimModelValidator : AbstractValidator<FinansSiparisDagitimModel>
{
    public FinansSiparisDagitimModelValidator()
    {
        RuleFor(x => x.IsKaydiId).GreaterThan(0);
        RuleFor(x => x.Adet).GreaterThanOrEqualTo(0);
        RuleFor(x => x.M3).GreaterThanOrEqualTo(0);
        RuleFor(x => x).Must(x => x.NetTutar > 0 || x.Adet > 0 || x.M3 > 0).WithMessage("Sipariş kalemi net tutarı veya miktarı sıfırdan büyük olmalıdır.");
        RuleFor(x => x.NetTutar).GreaterThan(0).When(x => x.NetTutar.HasValue);
        RuleFor(x => x.BirimFiyat).GreaterThan(0).When(x => x.BirimFiyat.HasValue);
        RuleFor(x => x.KdvOrani).InclusiveBetween(0, 100).When(x => x.KdvOrani.HasValue);
        RuleFor(x => x.ParaBirimi).Must(x => x is null || FinansValidationRules.Currency(x));
    }
}

public sealed class FinansSiparisGrupDagitimModelValidator : AbstractValidator<FinansSiparisGrupDagitimModel>
{
    public FinansSiparisGrupDagitimModelValidator()
    {
        RuleFor(x => x.IsKaydiIds).NotEmpty().Must(x => x.Count <= 2000)
            .WithMessage("Bir grup en fazla 2000 iş kalemi içerebilir.");
        RuleForEach(x => x.IsKaydiIds).GreaterThan(0);
        RuleFor(x => x.NetTutar).GreaterThan(0);
    }
}

public sealed class FinansSiparisOlusturCommandValidator : AbstractValidator<FinansSiparisOlusturCommand>
{
    public FinansSiparisOlusturCommandValidator() => RuleFor(x => x.Model).NotNull().SetValidator(new FinansSiparisOlusturModelValidator());
}

public sealed class FinansSiparisKalemEkleCommandValidator : AbstractValidator<FinansSiparisKalemEkleCommand>
{
    public FinansSiparisKalemEkleCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).NotNull();
        RuleFor(x => x.Model).Must(x => x is null || x.Kalemler?.Count > 0 || x.Gruplar?.Count > 0)
            .WithMessage("PO'ya eklenecek en az bir iş kalemi seçilmelidir.");
        RuleForEach(x => x.Model.Kalemler).SetValidator(new FinansSiparisDagitimModelValidator()).When(x => x.Model?.Kalemler is not null);
        RuleForEach(x => x.Model.Gruplar).SetValidator(new FinansSiparisGrupDagitimModelValidator()).When(x => x.Model?.Gruplar is not null);
    }
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
        RuleFor(x => x.VadeTarihi).Must(x => FinansTarihKurallari.GecerliTarih(x!.Value))
            .GreaterThanOrEqualTo(x => x.FaturaTarihi.Date).WithMessage("Vade tarihi fatura tarihinden önce olamaz.")
            .When(x => x.VadeTarihi.HasValue);
        RuleFor(x => x.Aciklama).MaximumLength(2000);
        AddDocumentTotalRules();
        RuleFor(x => x).Must(x => x.Kalemler.Count > 0 || x.Gruplar?.Count > 0)
            .WithMessage("En az bir fatura kalemi seçilmelidir.");
        RuleForEach(x => x.Kalemler).ChildRules(line =>
        {
            line.RuleFor(x => x.SiparisKalemiId).GreaterThan(0);
            line.RuleFor(x => x.Adet).GreaterThanOrEqualTo(0);
            line.RuleFor(x => x.M3).GreaterThanOrEqualTo(0);
            line.RuleFor(x => x).Must(x => x.NetTutar > 0 || x.Adet > 0 || x.M3 > 0).WithMessage("Fatura kalemi net tutarı veya miktarı sıfırdan büyük olmalıdır.");
            line.RuleFor(x => x.NetTutar).GreaterThan(0).When(x => x.NetTutar.HasValue);
        });
        RuleForEach(x => x.Gruplar).ChildRules(group =>
        {
            group.RuleFor(x => x.SiparisKalemiIds).NotEmpty();
            group.RuleForEach(x => x.SiparisKalemiIds).GreaterThan(0);
            group.RuleFor(x => x.NetTutar).GreaterThan(0);
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
        RuleFor(x => x.VadeTarihi).Must(x => FinansTarihKurallari.GecerliTarih(x!.Value))
            .GreaterThanOrEqualTo(x => x.FaturaTarihi.Date).WithMessage("Vade tarihi fatura tarihinden önce olamaz.")
            .When(x => x.VadeTarihi.HasValue);
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

public sealed class FinansFaturaOdemeCommandValidator : AbstractValidator<FinansFaturaOdemeCommand>
{
    public FinansFaturaOdemeCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).NotNull();
        RuleFor(x => x.Model.OdemeTarihi).NotNull().WithMessage("Ödeme tarihi zorunludur.")
            .Must(x => x.HasValue && FinansTarihKurallari.GecerliTarih(x.Value)).WithMessage("Geçerli bir ödeme tarihi girin.")
            .When(x => x.Model is { Odendi: true });
        RuleFor(x => x.Model.Aciklama).MaximumLength(1000).When(x => x.Model is not null);
    }
}

public sealed class FinansFaturaIptalCommandValidator : AbstractValidator<FinansFaturaIptalCommand>
{
    public FinansFaturaIptalCommandValidator() { RuleFor(x => x.Id).GreaterThan(0); RuleFor(x => x.Aciklama).Must(FinansGerekceKurali.Gecerli).WithMessage(FinansGerekceKurali.Mesaj); }
}
