using FluentValidation;
using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Application.Features.FinansIslemleri.Queries;
using _3K.Core.Enums;
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

public sealed class FinansDuzenliIsDonemUretCommandValidator : AbstractValidator<FinansDuzenliIsDonemUretCommand>
{
    public FinansDuzenliIsDonemUretCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.BitisTarihi).Must(FinansTarihKurallari.GecerliTarih).WithMessage("Geçerli bir bitiş tarihi girin.");
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
        RuleFor(x => x.GecerlilikBaslangici!.Value).Must(FinansTarihKurallari.GecerliTarih)
            .WithMessage("Tarife geçerlilik başlangıcı geçerli bir tarih olmalıdır.")
            .When(x => x.GecerlilikBaslangici.HasValue);
        // Malzemeli tarife (ahşap/kontra) m³ fiyatıdır: net = ahşap m³ × ahşap fiyatı + kontra m³ × kontra fiyatı.
        RuleFor(x => x.FiyatlandirmaBirimi)
            .Equal(FinansFiyatlandirmaBirimi.Metrekup)
            .When(x => x.Eslesmeler?.Any(e => e?.Malzeme is not null) == true)
            .WithMessage("Ahşap veya kontra malzemeli tarife m³ ile fiyatlanır.");
        RuleForEach(x => x.Eslesmeler).ChildRules(match =>
        {
            match.RuleFor(x => x.IsTuru).IsInEnum();
            match.RuleFor(x => x.SandikAdi).MaximumLength(250);
            match.RuleFor(x => x.SandikTipi).MaximumLength(100);
            // Katlanır sandık elle fiyatlanır; yeni katlanır eşleşmesi tanımlanamaz. Önceden kaydedilmiş eşleşme (Id dolu)
            // fiyatlamada zaten yok sayılır; ürünün diğer güncellemelerini kilitlememek için reddedilmez.
            match.RuleFor(x => x.SandikTipi)
                .Must(x => !FinansKatlanirKurali.SandikTipiMi(x))
                .When(x => x.Aktif && x.Id is null)
                .WithMessage(FinansKatlanirKurali.TarifeMesaji);
            match.RuleFor(x => x.Malzeme)
                .Must(x => x is null or FinansSandikMalzemesi.Ahsap or FinansSandikMalzemesi.Kontra)
                .WithMessage("Tarife malzemesi Ahşap veya Kontra olmalıdır.");
        });
    }
}

public sealed class FinansUrunYenidenFiyatlaCommandValidator : AbstractValidator<FinansUrunYenidenFiyatlaCommand>
{
    public FinansUrunYenidenFiyatlaCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Aciklama).Must(FinansGerekceKurali.Gecerli).WithMessage(FinansGerekceKurali.Mesaj);
    }
}

/// <summary>Fiyat ve KDV sınırları servisteki <see cref="FinansTarifeDuzeltmeKurali.Dogrula"/> ile tek kaynaktan denetlenir.</summary>
public sealed class FinansTarifeDuzeltCommandValidator : AbstractValidator<FinansTarifeDuzeltCommand>
{
    public FinansTarifeDuzeltCommandValidator()
    {
        RuleFor(x => x.UrunId).GreaterThan(0);
        RuleFor(x => x.TarifeId).GreaterThan(0);
        RuleFor(x => x.Model).NotNull();
        RuleFor(x => x.Model.Aciklama).Must(FinansGerekceKurali.Gecerli).WithMessage(FinansGerekceKurali.Mesaj)
            .When(x => x.Model is not null);
    }
}

public sealed class FinansTarifeDuzeltmeOnizlemeQueryValidator : AbstractValidator<FinansTarifeDuzeltmeOnizlemeQuery>
{
    public FinansTarifeDuzeltmeOnizlemeQueryValidator()
    {
        RuleFor(x => x.UrunId).GreaterThan(0);
        RuleFor(x => x.TarifeId).GreaterThan(0);
    }
}

public sealed class FinansParaBirimiKaydetModelValidator : AbstractValidator<FinansParaBirimiKaydetModel>
{
    public FinansParaBirimiKaydetModelValidator()
    {
        RuleFor(x => x.Kod).NotEmpty().Must(x => x is not null && x.Trim().Length == 3 && x.Trim().All(char.IsAsciiLetter))
            .WithMessage("Para birimi kodu üç harften oluşmalıdır (ISO-4217, ör. USD).");
        RuleFor(x => x.Ad).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Sembol).MaximumLength(10);
        RuleFor(x => x.Sira).InclusiveBetween(0, 9999);
    }
}

public sealed class FinansParaBirimiOlusturCommandValidator : AbstractValidator<FinansParaBirimiOlusturCommand>
{
    public FinansParaBirimiOlusturCommandValidator() => RuleFor(x => x.Model).NotNull().SetValidator(new FinansParaBirimiKaydetModelValidator());
}

public sealed class FinansParaBirimiGuncelleCommandValidator : AbstractValidator<FinansParaBirimiGuncelleCommand>
{
    public FinansParaBirimiGuncelleCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).NotNull().SetValidator(new FinansParaBirimiKaydetModelValidator());
    }
}

public sealed class FinansParametreleriKaydetCommandValidator : AbstractValidator<FinansParametreleriKaydetCommand>
{
    public FinansParametreleriKaydetCommandValidator()
    {
        RuleFor(x => x.Model).NotNull();
        RuleFor(x => x.Model.VarsayilanVadeGunu).InclusiveBetween(0, FinansVadeKurallari.EnFazlaVadeGunu).When(x => x.Model is not null);
        RuleFor(x => x.Model.VadeUyariGunu).InclusiveBetween(0, FinansVadeKurallari.EnFazlaUyariGunu).When(x => x.Model is not null);
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
