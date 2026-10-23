using FluentValidation;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Application.Features.ProjeIslemleri.Queries;

namespace _3K.Application.Features.ProjeIslemleri.Validators
{
    public class OtomatikSahaAktarimOnizleQueryValidator : AbstractValidator<OtomatikSahaAktarimOnizleQuery>
    {
        public OtomatikSahaAktarimOnizleQueryValidator()
        {
            RuleFor(x => x.ProjeId).GreaterThan(0).WithMessage("Proje seçilmelidir.");
            RuleFor(x => x.HedefSahaProjeId).GreaterThan(0).When(x => x.HedefSahaProjeId.HasValue)
                .WithMessage("Hedef saha projesi geçersiz.");
            RuleFor(x => x.HedefSahaProjeId).Null().When(x => x.YeniProje)
                .WithMessage("Yeni saha projesi seçildiğinde mevcut saha projesi gönderilmez.");
        }
    }

    public class OtomatikSahayaAktarCommandValidator : AbstractValidator<OtomatikSahayaAktarCommand>
    {
        public OtomatikSahayaAktarCommandValidator()
        {
            RuleFor(x => x.ProjeId).GreaterThan(0).WithMessage("Proje seçilmelidir.");
            RuleFor(x => x.HedefSahaProjeId).GreaterThan(0).When(x => x.HedefSahaProjeId.HasValue)
                .WithMessage("Hedef saha projesi geçersiz.");
            RuleFor(x => x.HedefSahaProjeId).Null().When(x => x.YeniProje)
                .WithMessage("Yeni saha projesi seçildiğinde mevcut saha projesi gönderilmez.");
            RuleFor(x => x.PlanOzeti).NotEmpty().WithMessage("Önizleme bilgisi eksik; önizlemeyi yenileyin.")
                .MaximumLength(64).WithMessage("Önizleme bilgisi geçersiz; önizlemeyi yenileyin.");
            RuleFor(x => x.YeniProjeNo).MaximumLength(100).WithMessage("Proje numarası en fazla 100 karakter olabilir.");
            RuleFor(x => x.Aciklama).MaximumLength(500).WithMessage("Açıklama en fazla 500 karakter olabilir.");
        }
    }

    public class SahadanSahayaTasimaOnizleQueryValidator : AbstractValidator<SahadanSahayaTasimaOnizleQuery>
    {
        public SahadanSahayaTasimaOnizleQueryValidator()
        {
            RuleFor(x => x.KaynakSahaProjeId).GreaterThan(0).WithMessage("Kaynak saha projesi seçilmelidir.");
            RuleFor(x => x.HedefSahaProjeId).GreaterThan(0).WithMessage("Hedef saha projesi seçilmelidir.");
            RuleFor(x => x.YeniSandikNo).MaximumLength(100).WithMessage("Sandık numarası en fazla 100 karakter olabilir.");
        }
    }

    public class SahadanSahayaTasiCommandValidator : AbstractValidator<SahadanSahayaTasiCommand>
    {
        public SahadanSahayaTasiCommandValidator()
        {
            RuleFor(x => x.IslemAnahtari).NotEqual(Guid.Empty).WithMessage("İşlem anahtarı eksik; ekranı yenileyip tekrar deneyin.");
            RuleFor(x => x.KaynakSahaProjeId).GreaterThan(0).WithMessage("Kaynak saha projesi seçilmelidir.");
            RuleFor(x => x.HedefSahaProjeId).GreaterThan(0).WithMessage("Hedef saha projesi seçilmelidir.");
            RuleFor(x => x.YeniSandikNo).MaximumLength(100).WithMessage("Sandık numarası en fazla 100 karakter olabilir.");
            RuleFor(x => x.Aciklama).MaximumLength(500).WithMessage("Açıklama en fazla 500 karakter olabilir.");
        }
    }

    public class SahaAktarimiGeriAlCommandValidator : AbstractValidator<SahaAktarimiGeriAlCommand>
    {
        public SahaAktarimiGeriAlCommandValidator()
        {
            RuleFor(x => x.SahaAktarimId).GreaterThan(0).WithMessage("Geri alınacak aktarım seçilmelidir.");
            RuleFor(x => x.Aciklama).MaximumLength(500).WithMessage("Açıklama en fazla 500 karakter olabilir.");
        }
    }
}
