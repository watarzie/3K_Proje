using FluentValidation;
using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Validators
{
    internal static class FinansValidationRules
    {
        public static bool Currency(string? value) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length == 3;
        public static bool ValidDate(DateTime value) => value.Year is >= 2000 and <= 2200;
        public static bool CompleteDocumentTotals(string? currency, decimal? net, decimal? vat, decimal? total)
        {
            var any = !string.IsNullOrWhiteSpace(currency) || net.HasValue || vat.HasValue || total.HasValue;
            return !any || (!string.IsNullOrWhiteSpace(currency) && net.HasValue && vat.HasValue && total.HasValue);
        }
        public static bool BalancedDocumentTotals(decimal? net, decimal? vat, decimal? total)
            => !net.HasValue || !vat.HasValue || !total.HasValue || Math.Abs(net.Value + vat.Value - total.Value) <= 0.02m;
    }

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
            RuleFor(x => x.UretimTarihi).Must(FinansValidationRules.ValidDate).WithMessage("Geçerli bir üretim tarihi girilmelidir.");
            RuleFor(x => x.FinansDonemi).Must(FinansValidationRules.ValidDate).WithMessage("Geçerli bir finans dönemi girilmelidir.");
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

    public sealed class FinansIsKaydiOlusturCommandValidator : AbstractValidator<FinansIsKaydiOlusturCommand>
    {
        public FinansIsKaydiOlusturCommandValidator() => RuleFor(x => x.Model).NotNull().SetValidator(new FinansIsKaydiKaydetModelValidator());
    }

    public sealed class FinansIsKaydiGuncelleCommandValidator : AbstractValidator<FinansIsKaydiGuncelleCommand>
    {
        public FinansIsKaydiGuncelleCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Model).NotNull().SetValidator(new FinansIsKaydiKaydetModelValidator());
        }
    }

    public sealed class FinansUretimAktarimModelValidator : AbstractValidator<FinansUretimAktarimModel>
    {
        public FinansUretimAktarimModelValidator()
        {
            RuleFor(x => x.KaynakTuru).NotEmpty().MaximumLength(50);
            RuleFor(x => x.KaynakKayitId).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ProjeNo).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Musteri).MaximumLength(250);
            RuleFor(x => x.IsTuru).IsInEnum();
            RuleFor(x => x.IsAdi).NotEmpty().MaximumLength(250);
            RuleFor(x => x.Aciklama).MaximumLength(2000);
            RuleFor(x => x.TalepEdenKisi).MaximumLength(200);
            RuleFor(x => x.TalepEdenBolum).MaximumLength(200);
            RuleFor(x => x.SandikNo).MaximumLength(100);
            RuleFor(x => x.SandikAdi).MaximumLength(250);
            RuleFor(x => x.SandikTipi).MaximumLength(100);
            RuleFor(x => x.Adet).GreaterThan(0).When(x => x.KaynakAktif);
            RuleFor(x => x.BirimM3).GreaterThanOrEqualTo(0);
            RuleFor(x => x.UretimTarihi).Must(FinansValidationRules.ValidDate);
            RuleFor(x => x.FinansDonemi).Must(FinansValidationRules.ValidDate);
        }
    }

    public sealed class FinansUretimAktarCommandValidator : AbstractValidator<FinansUretimAktarCommand>
    {
        public FinansUretimAktarCommandValidator()
        {
            RuleFor(x => x.Kayitlar).NotNull();
            RuleForEach(x => x.Kayitlar).SetValidator(new FinansUretimAktarimModelValidator());
        }
    }

    public sealed class FinansSiparisOlusturModelValidator : AbstractValidator<FinansSiparisOlusturModel>
    {
        public FinansSiparisOlusturModelValidator()
        {
            RuleFor(x => x.PoNumarasi).NotEmpty().MaximumLength(100);
            RuleFor(x => x.SiparisTarihi).Must(FinansValidationRules.ValidDate);
            RuleFor(x => x.Aciklama).MaximumLength(2000);
            RuleFor(x => x.Kalemler).NotEmpty();
            RuleForEach(x => x.Kalemler).ChildRules(line =>
            {
                line.RuleFor(x => x.IsKaydiId).GreaterThan(0);
                line.RuleFor(x => x.Adet).GreaterThanOrEqualTo(0);
                line.RuleFor(x => x.M3).GreaterThanOrEqualTo(0);
                line.RuleFor(x => x).Must(x => x.Adet > 0 || x.M3 > 0).WithMessage("Sipariş kalemi miktarı sıfırdan büyük olmalıdır.");
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
            RuleFor(x => x.SiparisTarihi).Must(FinansValidationRules.ValidDate);
            RuleFor(x => x.Aciklama).MaximumLength(2000);
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

    public sealed class FinansFaturaOlusturModelValidator : AbstractValidator<FinansFaturaOlusturModel>
    {
        public FinansFaturaOlusturModelValidator()
        {
            RuleFor(x => x.SiparisId).GreaterThan(0);
            RuleFor(x => x.FaturaNumarasi).NotEmpty().MaximumLength(100);
            RuleFor(x => x.FaturaTarihi).Must(FinansValidationRules.ValidDate);
            RuleFor(x => x.Aciklama).MaximumLength(2000);
            AddDocumentTotalRules();
            RuleFor(x => x.Kalemler).NotEmpty();
            RuleForEach(x => x.Kalemler).ChildRules(line =>
            {
                line.RuleFor(x => x.SiparisKalemiId).GreaterThan(0);
                line.RuleFor(x => x.Adet).GreaterThanOrEqualTo(0);
                line.RuleFor(x => x.M3).GreaterThanOrEqualTo(0);
                line.RuleFor(x => x).Must(x => x.Adet > 0 || x.M3 > 0).WithMessage("Fatura kalemi miktarı sıfırdan büyük olmalıdır.");
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
            RuleFor(x => x.FaturaTarihi).Must(FinansValidationRules.ValidDate);
            RuleFor(x => x.Aciklama).MaximumLength(2000);
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
            RuleFor(x => x.BaslangicTarihi).Must(FinansValidationRules.ValidDate);
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

    public sealed class FinansGiderKaydetModelValidator : AbstractValidator<FinansGiderKaydetModel>
    {
        public FinansGiderKaydetModelValidator()
        {
            RuleFor(x => x.Tarih).Must(FinansValidationRules.ValidDate);
            RuleFor(x => x.FinansDonemi).Must(FinansValidationRules.ValidDate);
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

    public sealed class FinansGideriKutuphaneyeKaydetCommandValidator : AbstractValidator<FinansGideriKutuphaneyeKaydetCommand>
    {
        public FinansGideriKutuphaneyeKaydetCommandValidator()
        {
            RuleFor(x => x.GiderId).GreaterThan(0);
            RuleFor(x => x.Model).NotNull().DependentRules(() =>
            {
                RuleFor(x => x.Model.Kod).NotEmpty().MaximumLength(50);
                RuleFor(x => x.Model.Ad).NotEmpty().MaximumLength(150);
            });
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

    public sealed class FinansFiyatTarifesiKaydetModelValidator : AbstractValidator<FinansFiyatTarifesiKaydetModel>
    {
        public FinansFiyatTarifesiKaydetModelValidator()
        {
            RuleFor(x => x.FinansUrunId).GreaterThan(0);
            RuleFor(x => x.Yil).InclusiveBetween(2000, 2200);
            RuleFor(x => x.GecerlilikBaslangici).Must(FinansValidationRules.ValidDate);
            RuleFor(x => x.GecerlilikBitisi).GreaterThanOrEqualTo(x => x.GecerlilikBaslangici);
            RuleFor(x => x.BirimFiyat).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ParaBirimi).Must(FinansValidationRules.Currency);
            RuleFor(x => x.KdvOrani).InclusiveBetween(0, 100);
            RuleFor(x => x).Must(x => x.GecerlilikBaslangici.Year == x.Yil && x.GecerlilikBitisi.Year == x.Yil)
                .WithMessage("Tarife tarihleri tarife yılı içinde olmalıdır.");
        }
    }

    public sealed class FinansFiyatTarifesiOlusturCommandValidator : AbstractValidator<FinansFiyatTarifesiOlusturCommand>
    {
        public FinansFiyatTarifesiOlusturCommandValidator() => RuleFor(x => x.Model).NotNull().SetValidator(new FinansFiyatTarifesiKaydetModelValidator());
    }

    public sealed class FinansFiyatTarifesiGuncelleCommandValidator : AbstractValidator<FinansFiyatTarifesiGuncelleCommand>
    {
        public FinansFiyatTarifesiGuncelleCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Model).NotNull().SetValidator(new FinansFiyatTarifesiKaydetModelValidator());
        }
    }

    public sealed class FinansIptalCommandValidators :
        AbstractValidator<FinansIsKaydiIptalCommand>
    {
        public FinansIptalCommandValidators()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Aciklama).NotEmpty().MaximumLength(1000);
        }
    }

    public sealed class FinansSiparisIptalCommandValidator : AbstractValidator<FinansSiparisIptalCommand>
    {
        public FinansSiparisIptalCommandValidator() { RuleFor(x => x.Id).GreaterThan(0); RuleFor(x => x.Aciklama).NotEmpty().MaximumLength(1000); }
    }

    public sealed class FinansFaturaIptalCommandValidator : AbstractValidator<FinansFaturaIptalCommand>
    {
        public FinansFaturaIptalCommandValidator() { RuleFor(x => x.Id).GreaterThan(0); RuleFor(x => x.Aciklama).NotEmpty().MaximumLength(1000); }
    }

    public sealed class FinansGiderIptalCommandValidator : AbstractValidator<FinansGiderIptalCommand>
    {
        public FinansGiderIptalCommandValidator() { RuleFor(x => x.Id).GreaterThan(0); RuleFor(x => x.Aciklama).NotEmpty().MaximumLength(1000); }
    }
}
