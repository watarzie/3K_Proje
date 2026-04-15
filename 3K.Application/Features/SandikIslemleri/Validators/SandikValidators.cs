using FluentValidation;
using _3K.Application.Features.SandikIslemleri.Commands;

namespace _3K.Application.Features.SandikIslemleri.Validators
{
    public class UrunGuncelleCommandValidator : AbstractValidator<UrunGuncelleCommand>
    {
        public UrunGuncelleCommandValidator()
        {
            RuleFor(x => x.CekiSatiriId)
                .GreaterThan(0).WithMessage("Geçerli bir ürün ID belirtilmeli.");

            RuleFor(x => x.SandikId)
                .GreaterThan(0).WithMessage("Geçerli bir sandık ID belirtilmeli.");

            RuleFor(x => x.KonulanAdet)
                .GreaterThanOrEqualTo(0).When(x => x.KonulanAdet.HasValue)
                .WithMessage("Konulan adet negatif olamaz.");

            RuleFor(x => x.EksikAdet)
                .GreaterThanOrEqualTo(0).When(x => x.EksikAdet.HasValue)
                .WithMessage("Eksik adet negatif olamaz.");

            RuleFor(x => x.ProjeId)
                .GreaterThan(0).WithMessage("Geçerli bir proje ID belirtilmeli.");

            RuleFor(x => x.KullaniciId)
                .GreaterThan(0).WithMessage("Geçerli bir kullanıcı ID belirtilmeli.");
        }
    }

    public class StoktanKarsilaCommandValidator : AbstractValidator<StoktanKarsilaCommand>
    {
        public StoktanKarsilaCommandValidator()
        {
            RuleFor(x => x.CekiSatiriId).GreaterThan(0).WithMessage("Geçerli bir ürün ID belirtilmeli.");
            RuleFor(x => x.StokKaydiId).GreaterThan(0).WithMessage("Geçerli bir stok kaydı ID belirtilmeli.");
            RuleFor(x => x.KarsilananAdet).GreaterThan(0).WithMessage("Karşılanan adet 0'dan büyük olmalı.");
            RuleFor(x => x.ProjeId).GreaterThan(0).WithMessage("Geçerli bir proje ID belirtilmeli.");
        }
    }

    public class ManuelUrunEkleCommandValidator : AbstractValidator<ManuelUrunEkleCommand>
    {
        public ManuelUrunEkleCommandValidator()
        {
            RuleFor(x => x.SandikId).GreaterThan(0).WithMessage("Sandık ID belirtilmeli.");
            RuleFor(x => x.ProjeId).GreaterThan(0).WithMessage("Proje ID belirtilmeli.");
            RuleFor(x => x.BarkodNo).NotEmpty().WithMessage("Barkod numarası boş olamaz.");
            RuleFor(x => x.Aciklama).NotEmpty().WithMessage("Açıklama boş olamaz.");
            RuleFor(x => x.IstenenAdet).GreaterThan(0).WithMessage("İstenen adet 0'dan büyük olmalı.");
        }
    }

    public class UrunIptalCommandValidator : AbstractValidator<UrunIptalCommand>
    {
        public UrunIptalCommandValidator()
        {
            RuleFor(x => x.CekiSatiriId).GreaterThan(0).WithMessage("Geçerli bir ürün ID belirtilmeli.");
            RuleFor(x => x.Neden).NotEmpty().WithMessage("İptal nedeni belirtilmeli.");
        }
    }

    public class FiiliSandikDegistirCommandValidator : AbstractValidator<FiiliSandikDegistirCommand>
    {
        public FiiliSandikDegistirCommandValidator()
        {
            RuleFor(x => x.CekiSatiriId).GreaterThan(0).WithMessage("Geçerli bir ürün ID belirtilmeli.");
            RuleFor(x => x.YeniFiiliSandikNo).NotEmpty().WithMessage("Yeni sandık numarası belirtilmeli.");
            RuleFor(x => x.ProjeId).GreaterThan(0).WithMessage("Proje ID belirtilmeli.");
        }
    }

    public class FBDenKarsilaCommandValidator : AbstractValidator<FBDenKarsilaCommand>
    {
        public FBDenKarsilaCommandValidator()
        {
            RuleFor(x => x.CekiSatiriId).GreaterThan(0).WithMessage("Geçerli bir ürün ID belirtilmeli.");
            RuleFor(x => x.AsilFB).NotEmpty().WithMessage("Asıl FB belirtilmeli.");
            RuleFor(x => x.AlinanFB).NotEmpty().WithMessage("Alınan FB belirtilmeli.");
            RuleFor(x => x.KarsilananAdet).GreaterThan(0).WithMessage("Karşılanan adet 0'dan büyük olmalı.");
        }
    }

    // === YENİ VALIDATOR ===
    public class SandikEkleCommandValidator : AbstractValidator<SandikEkleCommand>
    {
        public SandikEkleCommandValidator()
        {
            RuleFor(x => x.ProjeId).GreaterThan(0).WithMessage("Geçerli bir proje ID belirtilmeli.");
            RuleFor(x => x.SandikNo).NotEmpty().WithMessage("Sandık numarası boş olamaz.");
            RuleFor(x => x.Tip).NotEmpty().WithMessage("Sandık tipi belirtilmeli.");
        }
    }
}
