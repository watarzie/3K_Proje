using FluentValidation;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Core.Enums;

namespace _3K.Application.Features.SandikIslemleri.Validators
{
    /// <summary>Sevkiyat kaydı korunarak açma (sevk sonrası düzeltme) gerekçe ister; gerekçe düzeltme kaydına yazılır.</summary>
    public class SandikKilidiAcCommandValidator : AbstractValidator<SandikKilidiAcCommand>
    {
        public SandikKilidiAcCommandValidator()
        {
            RuleFor(x => x.Aciklama)
                .NotEmpty().WithMessage(SevkSonrasiDuzeltmeGerekcesi.Mesaj)
                .MaximumLength(SevkSonrasiDuzeltmeGerekcesi.EnFazlaUzunluk).WithMessage(SevkSonrasiDuzeltmeGerekcesi.UzunlukMesaji)
                .When(x => x.KilitAcmaTipiId == (int)SevkiyatKilitAcmaTipi.SevkiyatKaydiKorunarakAc);
        }
    }

    public class ProjeKilidiAcCommandValidator : AbstractValidator<ProjeKilidiAcCommand>
    {
        public ProjeKilidiAcCommandValidator()
        {
            RuleFor(x => x.Aciklama)
                .NotEmpty().WithMessage(SevkSonrasiDuzeltmeGerekcesi.Mesaj)
                .MaximumLength(SevkSonrasiDuzeltmeGerekcesi.EnFazlaUzunluk).WithMessage(SevkSonrasiDuzeltmeGerekcesi.UzunlukMesaji)
                .When(x => x.KilitAcmaTipiId == (int)SevkiyatKilitAcmaTipi.SevkiyatKaydiKorunarakAc);
        }
    }

    internal static class SevkSonrasiDuzeltmeGerekcesi
    {
        public const int EnFazlaUzunluk = 1000;
        public const string Mesaj = "Sevk sonrası düzeltme için gerekçe yazın.";
        public const string UzunlukMesaji = "Gerekçe en fazla 1000 karakter olabilir.";
    }
}
