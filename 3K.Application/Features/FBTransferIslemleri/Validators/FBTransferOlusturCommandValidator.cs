using FluentValidation;
using _3K.Application.Features.FBTransferIslemleri.Commands;

namespace _3K.Application.Features.FBTransferIslemleri.Validators
{
    public class FBTransferOlusturCommandValidator : AbstractValidator<FBTransferOlusturCommand>
    {
        public FBTransferOlusturCommandValidator()
        {
            RuleFor(x => x.CekiSatiriId).GreaterThan(0).WithMessage("Geçerli bir ürün ID belirtilmeli.");
            RuleFor(x => x.AsilFB).NotEmpty().WithMessage("Asıl FB belirtilmeli.");
            RuleFor(x => x.AlinanFB).NotEmpty().WithMessage("Alınan FB belirtilmeli.");
            RuleFor(x => x.Miktar).GreaterThan(0).WithMessage("Miktar 0'dan büyük olmalı.");
        }
    }
}
