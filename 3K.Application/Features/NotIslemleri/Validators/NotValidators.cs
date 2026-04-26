using FluentValidation;
using _3K.Application.Features.NotIslemleri.Commands;
using _3K.Application.Features.NotIslemleri.Queries;
using _3K.Application.Features.UcKIslemleri.Commands;

namespace _3K.Application.Features.NotIslemleri.Validators
{
    public class NotEkleCommandValidator : AbstractValidator<NotEkleCommand>
    {
        private static readonly string[] GecerliReferansTipleri = { "CekiSatiri", "Sandik", "Proje" };

        public NotEkleCommandValidator()
        {
            RuleFor(x => x.Icerik)
                .NotEmpty().WithMessage("Not içeriği boş olamaz.")
                .MaximumLength(2000).WithMessage("Not içeriği 2000 karakteri aşamaz.");

            RuleFor(x => x.BagliReferansTipi)
                .NotEmpty().WithMessage("Referans tipi belirtilmeli.")
                .Must(t => GecerliReferansTipleri.Contains(t))
                .WithMessage("Geçersiz referans tipi. (CekiSatiri, Sandik, Proje)");

            RuleFor(x => x.BagliReferansId)
                .GreaterThan(0).WithMessage("Geçerli bir referans ID belirtilmeli.");

            RuleFor(x => x.ProjeId)
                .GreaterThan(0).WithMessage("Geçerli bir proje ID belirtilmeli.");
        }
    }

    public class GetNotlarQueryValidator : AbstractValidator<GetNotlarQuery>
    {
        public GetNotlarQueryValidator()
        {
            RuleFor(x => x.BagliReferansTipi).NotEmpty().WithMessage("Referans tipi belirtilmeli.");
            RuleFor(x => x.BagliReferansId).GreaterThan(0).WithMessage("Geçerli bir referans ID belirtilmeli.");
        }
    }

    public class TopluDurumGuncelleCommandValidator : AbstractValidator<TopluDurumGuncelleCommand>
    {
        public TopluDurumGuncelleCommandValidator()
        {
            RuleFor(x => x.ProjeId).GreaterThan(0).WithMessage("Geçerli bir proje ID belirtilmeli.");
            RuleFor(x => x.CekiSatiriIdler).NotEmpty().WithMessage("En az bir ürün seçilmeli.");
        }
    }
}
