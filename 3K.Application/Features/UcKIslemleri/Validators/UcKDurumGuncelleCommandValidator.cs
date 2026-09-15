using FluentValidation;
using _3K.Application.Features.UcKIslemleri.Commands;

namespace _3K.Application.Features.UcKIslemleri.Validators
{
    public class UcKDurumGuncelleCommandValidator : AbstractValidator<UcKDurumGuncelleCommand>
    {
        public UcKDurumGuncelleCommandValidator()
        {
            RuleFor(x => x.CekiSatiriId).GreaterThan(0).WithMessage("Geçerli bir ürün ID belirtilmeli.");
            RuleFor(x => x.ProjeId).GreaterThan(0).WithMessage("Geçerli bir proje ID belirtilmeli.");
            RuleFor(x => x.KarsilamaTipiId).GreaterThan(0).WithMessage("Karşılama tipi belirtilmeli.");
            RuleFor(x => x.GelenAdet)
                .PrecisionScale(18, 4, false)
                .When(x => x.GelenAdet.HasValue)
                .WithMessage("İşlem miktarı en fazla 14 tam ve 4 ondalık basamak içerebilir.");
        }
    }
}
