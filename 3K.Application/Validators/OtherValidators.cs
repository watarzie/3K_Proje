using FluentValidation;
using _3K.Application.Features.AuthIslemleri.Commands;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Application.Features.StokIslemleri.Commands;
using _3K.Application.Features.FBTransferIslemleri.Commands;

namespace _3K.Application.Validators
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage("Email adresi boş olamaz.")
                .EmailAddress().WithMessage("Geçerli bir email formatı girilmeli.");
            RuleFor(x => x.Sifre).NotEmpty().WithMessage("Şifre boş olamaz.")
                .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalı.");
        }
    }

    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.AdSoyad).NotEmpty().WithMessage("Ad Soyad boş olamaz.");
            RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Geçerli bir email adresi girilmeli.");
            RuleFor(x => x.Sifre).NotEmpty().MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalı.");
            RuleFor(x => x.Rol).NotEmpty().WithMessage("Rol belirtilmeli.");
        }
    }

    public class ProjeOlusturCommandValidator : AbstractValidator<ProjeOlusturCommand>
    {
        public ProjeOlusturCommandValidator()
        {
            RuleFor(x => x.ProjeNo).NotEmpty().WithMessage("Proje numarası boş olamaz.");
            RuleFor(x => x.Musteri).NotEmpty().WithMessage("Müşteri adı boş olamaz.");
        }
    }

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
