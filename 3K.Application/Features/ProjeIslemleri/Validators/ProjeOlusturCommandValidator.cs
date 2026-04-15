using FluentValidation;
using _3K.Application.Features.ProjeIslemleri.Commands;

namespace _3K.Application.Features.ProjeIslemleri.Validators
{
    public class ProjeOlusturCommandValidator : AbstractValidator<ProjeOlusturCommand>
    {
        public ProjeOlusturCommandValidator()
        {
            RuleFor(x => x.ProjeNo).NotEmpty().WithMessage("Proje numarası boş olamaz.");
            RuleFor(x => x.Musteri).NotEmpty().WithMessage("Müşteri adı boş olamaz.");
        }
    }
}
