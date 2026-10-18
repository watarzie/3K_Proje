using FluentValidation;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Core.Models;

namespace _3K.Application.Features.ProjeIslemleri.Validators
{
    public class ProjeFotografBaglantisiKaydetCommandValidator : AbstractValidator<ProjeFotografBaglantisiKaydetCommand>
    {
        public ProjeFotografBaglantisiKaydetCommandValidator()
        {
            RuleFor(x => x.ProjeId).GreaterThan(0).WithMessage("Proje seçilmelidir.");
            RuleFor(x => x.BaglantiId).GreaterThan(0).When(x => x.BaglantiId.HasValue).WithMessage("Fotoğraf bağlantısı seçilmelidir.");
            RuleFor(x => x.Adres)
                .Must(ProjeFotografBaglantisiKurali.Gecerli)
                .WithMessage($"Geçerli bir web adresi girin (ör. https://ornek.com/proje); en fazla {ProjeFotografBaglantisiKurali.EnFazlaUzunluk} karakter.");
            RuleFor(x => x.Aciklama)
                .Must(ProjeFotografBaglantisiKurali.AciklamaGecerli)
                .WithMessage($"Açıklama en fazla {ProjeFotografBaglantisiKurali.AciklamaEnFazlaUzunluk} karakter olabilir.");
        }
    }

    public class ProjeFotografBaglantisiSilCommandValidator : AbstractValidator<ProjeFotografBaglantisiSilCommand>
    {
        public ProjeFotografBaglantisiSilCommandValidator()
        {
            RuleFor(x => x.ProjeId).GreaterThan(0).WithMessage("Proje seçilmelidir.");
            RuleFor(x => x.BaglantiId).GreaterThan(0).WithMessage("Fotoğraf bağlantısı seçilmelidir.");
        }
    }
}
