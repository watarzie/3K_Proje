using FluentValidation;
using _3K.Application.Features.GridIslemleri.Commands;
using _3K.Core.Enums;

namespace _3K.Application.Features.GridIslemleri.Validators
{
    public class GridDurumGuncelleCommandValidator : AbstractValidator<GridDurumGuncelleCommand>
    {
        public GridDurumGuncelleCommandValidator()
        {
            RuleFor(x => x.CekiSatiriId).GreaterThan(0).WithMessage("Geçerli bir ürün ID belirtilmeli.");
            RuleFor(x => x.ProjeId).GreaterThan(0).WithMessage("Geçerli bir proje ID belirtilmeli.");
            RuleFor(x => x.YeniDurumId).GreaterThan(0).WithMessage("Yeni durum belirtilmeli.");
            RuleFor(x => x)
                .Must(x => !x.SevkMiktari.HasValue ||
                           x.SevkMiktari.Value <= 0 ||
                           x.GridSevkDurumuId == (int)GridSevkDurum.SevkEdildi)
                .WithMessage("Sevk adeti girildiyse Grid sevk durumu 'Sevk Edildi' seçilmelidir.");
        }
    }

    public class GridTopluSevkCommandValidator : AbstractValidator<GridTopluSevkCommand>
    {
        public GridTopluSevkCommandValidator()
        {
            RuleFor(x => x.ProjeId).GreaterThan(0).WithMessage("Geçerli bir proje ID belirtilmeli.");
            RuleFor(x => x.CekiSatiriIdler).NotEmpty().WithMessage("En az bir ürün seçilmeli.");
        }
    }
}
