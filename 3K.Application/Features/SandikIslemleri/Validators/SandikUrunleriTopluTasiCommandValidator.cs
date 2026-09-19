using FluentValidation;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Services;

namespace _3K.Application.Features.SandikIslemleri.Validators;

public sealed class SandikUrunleriTopluTasiCommandValidator : AbstractValidator<SandikUrunleriTopluTasiCommand>
{
    public SandikUrunleriTopluTasiCommandValidator()
    {
        RuleFor(x => x.ProjeId).GreaterThan(0);
        RuleFor(x => x.KaynakSandikId).GreaterThan(0);
        RuleFor(x => x.HedefSandikId).GreaterThan(0).NotEqual(x => x.KaynakSandikId);
        RuleFor(x => x.IslemAnahtari).NotEmpty();
        RuleFor(x => x.Satirlar).NotNull().Must(x => x is { Count: > 0 and <= 250 })
            .WithMessage("Tek işlemde 1 ile 250 arasında içerik taşınabilir.");
        RuleFor(x => x.Satirlar).Must(x => x != null && x.All(s => s != null) &&
            x.Select(s => s.KaynakSandikIcerikId).Distinct().Count() == x.Count)
            .WithMessage("Aynı sandık içeriği birden fazla kez seçilemez.");
        RuleForEach(x => x.Satirlar).ChildRules(s =>
        {
            s.RuleFor(x => x.KaynakSandikIcerikId).GreaterThan(0);
            s.RuleFor(x => x.TasinanAdet).GreaterThan(0)
                .Must(SandikUrunTasimaIslemi.MiktarHassasiyetiGecerliMi)
                .WithMessage("Taşınan miktar en fazla 14 tam ve 4 ondalık basamak içerebilir.");
        });
    }
}
