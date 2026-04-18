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
            // Barkod (Malzeme Kodu) opsiyonel
            RuleFor(x => x.MalzemeAdi).NotEmpty().WithMessage("Malzeme adı boş olamaz.");
            RuleFor(x => x.KaynakProje).NotEmpty().WithMessage("Malzemenin artan/iade olduğu kaynak proje adı zorunludur.");
            RuleFor(x => x.Miktar).GreaterThan(0).WithMessage("Miktar 0'dan büyük olmalı.");
            RuleFor(x => x.Birim).NotEmpty().WithMessage("Birim boş olamaz.");
            RuleFor(x => x.StokGirisNedeni).NotEmpty().WithMessage("Stok giriş nedeni zorunludur.");
        }
    }

    public class StokKaydiGuncelleCommandValidator : AbstractValidator<StokKaydiGuncelleCommand>
    {
        public StokKaydiGuncelleCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Geçerli bir stok ID'si girilmelidir.");
            RuleFor(x => x.MalzemeAdi).NotEmpty().WithMessage("Malzeme adı boş olamaz.");
            RuleFor(x => x.KaynakProje).NotEmpty().WithMessage("Kaynak proje adı zorunludur.");
            RuleFor(x => x.Miktar).GreaterThanOrEqualTo(0).WithMessage("Miktar negatif olamaz.");
            RuleFor(x => x.Birim).NotEmpty().WithMessage("Birim boş olamaz.");
            RuleFor(x => x.StokGirisNedeni).NotEmpty().WithMessage("Stok giriş nedeni zorunludur.");
        }
    }
}
