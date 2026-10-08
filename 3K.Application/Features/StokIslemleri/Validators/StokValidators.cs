using FluentValidation;
using _3K.Application.Features.StokIslemleri.Commands;
using _3K.Application.Features.StokIslemleri.Queries;
using _3K.Core.Models;

namespace _3K.Application.Features.StokIslemleri.Validators
{
    public class StokListeleQueryValidator : AbstractValidator<StokListeleQuery>
    {
        public StokListeleQueryValidator()
        {
            RuleFor(x => x.DurumId).Must(StokListeFiltresi.GecerliDurum).WithMessage("Geçersiz stok durumu.");
        }
    }

    public class StokKaydiOlusturCommandValidator : AbstractValidator<StokKaydiOlusturCommand>
    {
        public StokKaydiOlusturCommandValidator()
        {
            // Barkod (Malzeme Kodu) opsiyonel
            RuleFor(x => x.MalzemeAdi).NotEmpty().WithMessage("Malzeme adı boş olamaz.");
            RuleFor(x => x.Miktar).GreaterThan(0).WithMessage("Miktar 0'dan büyük olmalı.");
            RuleFor(x => x.BirimId).NotEmpty().WithMessage("Birim boş olamaz.");
            RuleFor(x => x.StokGirisNedeni).NotEmpty().WithMessage("Stok giriş nedeni zorunludur.");
        }
    }

    public class StokKaydiGuncelleCommandValidator : AbstractValidator<StokKaydiGuncelleCommand>
    {
        public StokKaydiGuncelleCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Geçerli bir stok ID'si girilmelidir.");
            RuleFor(x => x.MalzemeAdi).NotEmpty().WithMessage("Malzeme adı boş olamaz.");
            RuleFor(x => x.Miktar).GreaterThanOrEqualTo(0).WithMessage("Miktar negatif olamaz.");
            RuleFor(x => x.BirimId).NotEmpty().WithMessage("Birim boş olamaz.");
            RuleFor(x => x.StokGirisNedeni).NotEmpty().WithMessage("Stok giriş nedeni zorunludur.");
        }
    }

    public class StokKaydiSilCommandValidator : AbstractValidator<StokKaydiSilCommand>
    {
        public StokKaydiSilCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Geçerli bir stok ID'si girilmelidir.");
        }
    }
}
