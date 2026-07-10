using _3K.Application.Features.BildirimIslemleri.Commands;
using _3K.Application.Features.BildirimIslemleri.Queries;
using _3K.Core.Enums;
using FluentValidation;

namespace _3K.Application.Features.BildirimIslemleri.Validators
{
    public class GetOkunmamisBildirimlerQueryValidator : AbstractValidator<GetOkunmamisBildirimlerQuery>
    {
        public GetOkunmamisBildirimlerQueryValidator()
        {
            RuleFor(query => query.Limit).InclusiveBetween(1, 50);
        }
    }

    public class GetBildirimlerQueryValidator : AbstractValidator<GetBildirimlerQuery>
    {
        private static readonly string[] GecerliDurumlar = ["tumu", "okunmus", "okunmamis"];

        public GetBildirimlerQueryValidator()
        {
            RuleFor(query => query.Durum)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Must(durum => GecerliDurumlar.Contains(durum.Trim(), StringComparer.OrdinalIgnoreCase))
                .WithMessage("Durum; tumu, okunmus veya okunmamis olmalıdır.");

            RuleFor(query => query.Sayfa)
                .InclusiveBetween(1, 1_000_000);

            RuleFor(query => query.SayfaBoyutu)
                .InclusiveBetween(1, 100);

            RuleFor(query => query.TipId)
                .Must(tipId => !tipId.HasValue || Enum.IsDefined(typeof(BildirimTipi), tipId.Value))
                .WithMessage("Geçersiz bildirim tipi.");

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

    public class GetBildirimDetayiQueryValidator : AbstractValidator<GetBildirimDetayiQuery>
    {
        public GetBildirimDetayiQueryValidator()
        {
            RuleFor(query => query.BildirimId).GreaterThan(0);
        }
    }

    public class BildirimiOkunduIsaretleCommandValidator : AbstractValidator<BildirimiOkunduIsaretleCommand>
    {
        public BildirimiOkunduIsaretleCommandValidator()
        {
            RuleFor(command => command.BildirimId).GreaterThan(0);
        }
    }

    public class BildirimAbonelikleriniGuncelleCommandValidator
        : AbstractValidator<BildirimAbonelikleriniGuncelleCommand>
    {
        public BildirimAbonelikleriniGuncelleCommandValidator()
        {
            RuleForEach(command => command.CekiYuklendiAliciIdleri).GreaterThan(0);
            RuleForEach(command => command.CekiRevizyonuAliciIdleri).GreaterThan(0);
        }
    }
}
