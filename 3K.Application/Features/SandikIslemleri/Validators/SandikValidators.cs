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
        }
    }

    public class SahaYedekMalzemeEkleCommandValidator : AbstractValidator<SahaYedekMalzemeEkleCommand>
    {
        public SahaYedekMalzemeEkleCommandValidator()
        {
            RuleFor(x => x.ProjeId).GreaterThan(0).WithMessage("Geçerli bir proje ID belirtilmeli.");
            RuleFor(x => x.SandikId).GreaterThan(0).WithMessage("Geçerli bir sandık ID belirtilmeli.");
            RuleFor(x => x.Isim).NotEmpty().WithMessage("Malzeme ismi zorunludur.");
            RuleFor(x => x.Miktar).GreaterThan(0).WithMessage("Miktar 0'dan büyük olmalı.");
        }
    }

    public class SandikSevkEtCommandValidator : AbstractValidator<SandikSevkEtCommand>
    {
        public SandikSevkEtCommandValidator()
        {
            RuleFor(x => x.ProjeId).GreaterThan(0).WithMessage("Geçerli bir proje ID belirtilmeli.");
            RuleFor(x => x.SandikId).GreaterThan(0).WithMessage("Geçerli bir sandık ID belirtilmeli.");
        }
    }

    public class SandikUrunTasiCommandValidator : AbstractValidator<SandikUrunTasiCommand>
    {
        public SandikUrunTasiCommandValidator()
        {
            RuleFor(x => x.KaynakSandikIcerikId)
                .GreaterThan(0).WithMessage("Geçerli bir kaynak sandık içeriği belirtilmeli.");
            RuleFor(x => x.HedefSandikId)
                .GreaterThan(0).WithMessage("Geçerli bir hedef sandık belirtilmeli.");
            RuleFor(x => x.ProjeId)
                .GreaterThan(0).WithMessage("Geçerli bir proje belirtilmeli.");
            RuleFor(x => x.TasinanAdet)
                .GreaterThan(0).WithMessage("Taşınan miktar 0'dan büyük olmalıdır.");
            RuleFor(x => x.TasinanAdet)
                .PrecisionScale(18, 4, false)
                .WithMessage("Taşınan miktar en fazla 14 tam ve 4 ondalık basamak içerebilir.");
            RuleFor(x => x.IslemAnahtari)
                .NotEmpty()
                .WithMessage("İşlem anahtarı zorunludur.");
        }
    }
}
