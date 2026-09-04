using FluentValidation;
using _3K.Application.Features.AmbalajIslemleri.Commands;
using _3K.Core.Enums;

namespace _3K.Application.Features.AmbalajIslemleri.Validators
{
    internal static class AmbalajValidatorKurallari
    {
        public static void KayitAlanlariniDogrula<T>(AbstractValidator<T> validator)
            where T : IAmbalajKayitAlanlari
        {
            validator.RuleFor(x => x)
                .Must(x => GecerliProjeBaglantisi(x))
                .WithMessage("Mevcut bir ProjeId veya manuel proje numarası ve adı birlikte belirtilmelidir.");

            validator.RuleFor(x => x.Tur).IsInEnum().WithMessage("Sandık türü geçersizdir.");
            validator.RuleFor(x => x.SandikCinsi).IsInEnum().WithMessage("Sandık cinsi geçersizdir.");
            validator.RuleFor(x => x.SandikNo).NotEmpty().MaximumLength(100);
            validator.RuleFor(x => x.Ad).MaximumLength(250);
            validator.RuleFor(x => x.ManuelProjeNo).MaximumLength(100);
            validator.RuleFor(x => x.ManuelProjeAdi).MaximumLength(250);
            validator.RuleFor(x => x.DigerSandikCinsi).MaximumLength(100);
            validator.RuleFor(x => x.Adet).InclusiveBetween(1, 100_000);
            validator.RuleFor(x => x.Boy).GreaterThan(0).LessThanOrEqualTo(100_000);
            validator.RuleFor(x => x.En).GreaterThan(0).LessThanOrEqualTo(100_000);
            validator.RuleFor(x => x.Yukseklik).GreaterThan(0).LessThanOrEqualTo(100_000);
            validator.RuleFor(x => x.UstKayitId)
                .NotNull().When(x => x.Tur == AmbalajSandikTuru.Ic)
                .WithMessage("İç sandık için üst sandık zorunludur.");
            validator.RuleFor(x => x.UstKayitId)
                .Null().When(x => x.Tur != AmbalajSandikTuru.Ic)
                .WithMessage("Yalnız iç sandıklar bir üst sandığa bağlanabilir.");
            validator.RuleFor(x => x.DigerSandikCinsi)
                .NotEmpty().When(x => x.SandikCinsi == AmbalajSandikCinsi.Diger)
                .WithMessage("Diğer sandık cinsi açıklanmalıdır.");
            validator.RuleFor(x => x.KullanimAmaci).MaximumLength(250);
            validator.RuleFor(x => x.TalepEdenKisi).MaximumLength(200);
            validator.RuleFor(x => x.TalepEdenBolum).MaximumLength(200);
            validator.RuleFor(x => x.TalimatVeren).MaximumLength(200);
            validator.RuleFor(x => x.FirinPartiNo).MaximumLength(100);
            validator.RuleFor(x => x.Aciklama).MaximumLength(2000);
        }

        private static bool GecerliProjeBaglantisi(IAmbalajKayitAlanlari kayit)
        {
            var mevcutProje = kayit.ProjeId > 0 &&
                               string.IsNullOrWhiteSpace(kayit.ManuelProjeNo) &&
                               string.IsNullOrWhiteSpace(kayit.ManuelProjeAdi);
            var manuelProje = !kayit.ProjeId.HasValue &&
                              !string.IsNullOrWhiteSpace(kayit.ManuelProjeNo) &&
                              !string.IsNullOrWhiteSpace(kayit.ManuelProjeAdi);
            return mevcutProje || manuelProje;
        }
    }

    public sealed class AmbalajUretimKaydiOlusturCommandValidator : AbstractValidator<AmbalajUretimKaydiOlusturCommand>
    {
        public AmbalajUretimKaydiOlusturCommandValidator()
        {
            AmbalajValidatorKurallari.KayitAlanlariniDogrula(this);
            RuleFor(x => x.KaynakModul).IsInEnum();
            RuleFor(x => x.KaynakModul)
                .Must(x => x is AmbalajKaynakModulu.Manuel or AmbalajKaynakModulu.Diger)
                .WithMessage("Modül kaynaklı sandıklar manuel oluşturulamaz; kaynak senkronizasyonu kullanılmalıdır.");
            RuleFor(x => x)
                .Must(x => !x.UretimeAlindi || x.AmbalajaDahil)
                .WithMessage("Ambalaja dahil olmayan kayıt üretime alınamaz.");
        }
    }

    public sealed class AmbalajUretimKaydiGuncelleCommandValidator : AbstractValidator<AmbalajUretimKaydiGuncelleCommand>
    {
        public AmbalajUretimKaydiGuncelleCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            AmbalajValidatorKurallari.KayitAlanlariniDogrula(this);
        }
    }

    public sealed class AmbalajUretimSecimGuncelleCommandValidator : AbstractValidator<AmbalajUretimSecimGuncelleCommand>
    {
        public AmbalajUretimSecimGuncelleCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x).Must(x => !x.UretimeAlindi || x.AmbalajaDahil)
                .WithMessage("Ambalaja dahil olmayan kayıt üretime alınamaz.");
            RuleFor(x => x.Aciklama).MaximumLength(1000);
        }
    }

    public sealed class AmbalajUretimDurumuGuncelleCommandValidator : AbstractValidator<AmbalajUretimDurumuGuncelleCommand>
    {
        public AmbalajUretimDurumuGuncelleCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Durum).IsInEnum();
            RuleFor(x => x.FirinPartiNo).MaximumLength(100);
            RuleFor(x => x.Aciklama).MaximumLength(1000);
        }
    }

    public sealed class AmbalajM3OverrideGuncelleCommandValidator : AbstractValidator<AmbalajM3OverrideGuncelleCommand>
    {
        public AmbalajM3OverrideGuncelleCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.M3Override).GreaterThanOrEqualTo(0).When(x => x.M3Override.HasValue);
            RuleFor(x => x.Neden).NotEmpty().MaximumLength(500);
        }
    }

    public sealed class AmbalajSarfOraniGuncelleCommandValidator : AbstractValidator<AmbalajSarfOraniGuncelleCommand>
    {
        public AmbalajSarfOraniGuncelleCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.SarfOrani).InclusiveBetween(0, 1)
                .WithMessage("Sarf oranı 0 ile 1 arasında olmalıdır (örn. %11 için 0.11).");
            RuleFor(x => x.Neden).NotEmpty().MaximumLength(1000);
        }
    }

    public sealed class AmbalajUretimKaydiIptalEtCommandValidator : AbstractValidator<AmbalajUretimKaydiIptalEtCommand>
    {
        public AmbalajUretimKaydiIptalEtCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Neden).NotEmpty().MaximumLength(500);
        }
    }

    public sealed class AmbalajUretimKaydiAktiflestirCommandValidator : AbstractValidator<AmbalajUretimKaydiAktiflestirCommand>
    {
        public AmbalajUretimKaydiAktiflestirCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Aciklama).MaximumLength(1000);
        }
    }

    public sealed class AmbalajKaynaklariSenkronizeEtCommandValidator : AbstractValidator<AmbalajKaynaklariSenkronizeEtCommand>
    {
        public AmbalajKaynaklariSenkronizeEtCommandValidator() =>
            RuleFor(x => x.ProjeId).GreaterThan(0);
    }
}
