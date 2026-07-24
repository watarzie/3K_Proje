using FluentValidation;
using _3K.Application.Features.PdfIslemleri.Queries;

namespace _3K.Application.Features.PdfIslemleri.Validators
{
    public sealed class GetTopluEksikUrunlerRaporuQueryValidator
        : AbstractValidator<GetTopluEksikUrunlerRaporuQuery>
    {
        public GetTopluEksikUrunlerRaporuQueryValidator()
        {
            RuleFor(query => query.ProjeIds)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("En az bir proje seçilmelidir.")
                .Must(projeIds => projeIds.Count <= 25)
                .WithMessage("Tek seferde en fazla 25 proje için rapor alınabilir.")
                .Must(projeIds => projeIds.All(id => id > 0))
                .WithMessage("Proje kimlikleri pozitif olmalıdır.")
                .Must(projeIds => projeIds.Distinct().Count() == projeIds.Count)
                .WithMessage("Aynı proje birden fazla kez seçilemez.");

            RuleFor(query => query.DosyaTuru)
                .IsInEnum()
                .WithMessage("Geçersiz rapor dosya türü.");
        }
    }
}
