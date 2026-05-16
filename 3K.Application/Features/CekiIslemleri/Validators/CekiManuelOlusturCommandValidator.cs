using FluentValidation;
using _3K.Application.Features.CekiIslemleri.Commands;

namespace _3K.Application.Features.CekiIslemleri.Validators
{
    public class CekiManuelOlusturCommandValidator : AbstractValidator<CekiManuelOlusturCommand>
    {
        public CekiManuelOlusturCommandValidator()
        {
            RuleFor(x => x.ProjeNo).NotEmpty().WithMessage("Proje no zorunludur.");
            RuleFor(x => x.ProjeTipiId).GreaterThan(0).WithMessage("Proje tipi zorunludur.");
            RuleFor(x => x.Satirlar).NotEmpty().WithMessage("En az bir ürün satırı girilmelidir.");

            RuleForEach(x => x.Sandiklar).ChildRules(sandik =>
            {
                sandik.RuleFor(x => x.SandikNo).NotEmpty().WithMessage("Sandık no zorunludur.");
            });

            RuleForEach(x => x.Satirlar).ChildRules(satir =>
            {
                satir.RuleFor(x => x.Aciklama).NotEmpty().WithMessage("Ürün açıklaması zorunludur.");
                satir.RuleFor(x => x.SandikNo).NotEmpty().WithMessage("Ürün satırı için sandık no zorunludur.");
                satir.RuleFor(x => x.IstenenAdet).GreaterThan(0).WithMessage("Miktar 0'dan büyük olmalıdır.");
                satir.RuleFor(x => x.BirimId)
                    .GreaterThan(0)
                    .When(x => x.BirimId.HasValue)
                    .WithMessage("Birim seçimi geçersiz.");
            });
        }
    }
}
