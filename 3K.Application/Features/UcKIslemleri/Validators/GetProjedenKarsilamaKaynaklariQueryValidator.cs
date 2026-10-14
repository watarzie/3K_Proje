using FluentValidation;
using _3K.Application.Features.UcKIslemleri.Queries;

namespace _3K.Application.Features.UcKIslemleri.Validators
{
    public class GetProjedenKarsilamaKaynaklariQueryValidator : AbstractValidator<GetProjedenKarsilamaKaynaklariQuery>
    {
        public GetProjedenKarsilamaKaynaklariQueryValidator()
        {
            RuleFor(x => x.HedefProjeId).GreaterThan(0).WithMessage("Geçerli bir proje ID belirtilmeli.");
            RuleFor(x => x.KaynakProjeId).GreaterThan(0).WithMessage("Geçerli bir kaynak proje ID belirtilmeli.");
            RuleFor(x => x.KaynakProjeId)
                .NotEqual(x => x.HedefProjeId)
                .When(x => x.KaynakProjeId > 0)
                .WithMessage("Bir proje kendi ürünlerinden karşılama yapamaz.");
        }
    }
}
