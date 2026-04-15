using FluentValidation;
using _3K.Application.Features.StokIslemleri.Commands;

namespace _3K.Application.Features.StokIslemleri.Validators
{
    public class StokKarsilaCommandValidator : AbstractValidator<StokKarsilaCommand>
    {
        public StokKarsilaCommandValidator()
        {
            RuleFor(x => x.CekiSatiriId).GreaterThan(0).WithMessage("Geçerli bir ürün ID belirtilmeli.");
            RuleFor(x => x.StokKaydiId).GreaterThan(0).WithMessage("Geçerli bir stok ID belirtilmeli.");
            RuleFor(x => x.Miktar).GreaterThan(0).WithMessage("Miktar 0'dan büyük olmalı.");
        }
    }

    public class StokKaydiOlusturCommandValidator : AbstractValidator<StokKaydiOlusturCommand>
    {
        public StokKaydiOlusturCommandValidator()
        {
            RuleFor(x => x.MalzemeKodu).NotEmpty().WithMessage("Malzeme kodu boş olamaz.");
            RuleFor(x => x.MalzemeAdi).NotEmpty().WithMessage("Malzeme adı boş olamaz.");
            RuleFor(x => x.Miktar).GreaterThan(0).WithMessage("Miktar 0'dan büyük olmalı.");
            RuleFor(x => x.Birim).NotEmpty().WithMessage("Birim boş olamaz.");
        }
    }
}
