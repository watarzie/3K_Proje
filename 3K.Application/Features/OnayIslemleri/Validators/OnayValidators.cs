using FluentValidation;
using _3K.Application.Features.OnayIslemleri.Commands;
using _3K.Application.Features.OnayIslemleri.Queries;

namespace _3K.Application.Features.OnayIslemleri.Validators
{
    public sealed class IslemOnaylaCommandValidator : AbstractValidator<IslemOnaylaCommand>
    {
        public IslemOnaylaCommandValidator()
        {
            RuleFor(command => command.OnayBekleyenIslemId).GreaterThan(0);
        }
    }

    public sealed class IslemReddetCommandValidator : AbstractValidator<IslemReddetCommand>
    {
        public IslemReddetCommandValidator()
        {
            RuleFor(command => command.OnayBekleyenIslemId).GreaterThan(0);
            RuleFor(command => command.RedAciklamasi)
                .NotEmpty()
                .MaximumLength(1000);
        }
    }

    public sealed class GetOnayGecmisiQueryValidator : AbstractValidator<GetOnayGecmisiQuery>
    {
        private static readonly string[] GecerliKapsamlar =
            ["tumu", "kararVerdiklerim", "taleplerim", "bekleyenler"];
        private static readonly string[] GecerliDurumlar =
            ["tumu", "bekliyor", "onaylandi", "reddedildi"];
        private static readonly string[] GecerliCalistirmaDurumlari =
            ["tumu", "bilinmiyor", "bekliyor", "calisiyor", "basarili", "basarisiz", "atlandi"];

        public GetOnayGecmisiQueryValidator()
        {
            RuleFor(query => query.Kapsam)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Must(value => GecerliKapsamlar.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase))
                .WithMessage("Kapsam; tumu, kararVerdiklerim, taleplerim veya bekleyenler olmalıdır.");

            RuleFor(query => query.Durum)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Must(value => GecerliDurumlar.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase))
                .WithMessage("Durum; tumu, bekliyor, onaylandi veya reddedildi olmalıdır.");

            RuleFor(query => query.CalistirmaDurumu)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Must(value => GecerliCalistirmaDurumlari.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase))
                .WithMessage("Geçersiz çalıştırma durumu.");

            RuleFor(query => query.Sayfa).InclusiveBetween(1, 1_000_000);
            RuleFor(query => query.SayfaBoyutu).InclusiveBetween(1, 100);
            RuleFor(query => query.Arama)
                .MaximumLength(200)
                .When(query => !string.IsNullOrWhiteSpace(query.Arama));

            RuleFor(query => query)
                .Must(query =>
                    !query.BaslangicTarihi.HasValue ||
                    !query.BitisTarihi.HasValue ||
                    query.BitisTarihi.Value >= query.BaslangicTarihi.Value)
                .WithMessage("Bitiş tarihi başlangıç tarihinden önce olamaz.");
        }
    }

    public sealed class GetOnayGecmisiDetayiQueryValidator
        : AbstractValidator<GetOnayGecmisiDetayiQuery>
    {
        public GetOnayGecmisiDetayiQueryValidator()
        {
            RuleFor(query => query.Id).GreaterThan(0);
        }
    }
}
