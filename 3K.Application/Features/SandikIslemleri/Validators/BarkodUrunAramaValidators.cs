using FluentValidation;
using _3K.Application.Features.SandikIslemleri.Queries;
using _3K.Application.Features.SevkiyatIslemleri.Queries;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Features.SandikIslemleri.Validators;

public sealed class BarkodUrunAraQueryValidator : AbstractValidator<BarkodUrunAraQuery>
{
    public BarkodUrunAraQueryValidator()
    {
        RuleFor(x => x.Barkod).Must(BarkodUrunAramaKurali.Gecerli)
            .WithMessage($"Aranacak barkodu girin (en fazla {BarkodUrunAramaKurali.EnFazlaUzunluk} karakter).");
        RuleFor(x => x.ProjeTipiId).Must(BarkodUrunAramaDogrulama.ProjeTipiGecerli).WithMessage("Proje tipi geçersiz.");
        RuleFor(x => x.ProjeArama).Must(BarkodUrunAramaDogrulama.ProjeAramaGecerli)
            .WithMessage($"Proje / müşteri araması en fazla {BarkodUrunAramaKurali.ProjeAramaEnFazlaUzunluk} karakter olabilir.");
        RuleFor(x => x.Sayfa).GreaterThan(0).WithMessage("Sayfa 1 veya büyük olmalıdır.");
        RuleFor(x => x.Boyut).InclusiveBetween(1, BarkodUrunAramaKurali.EnFazlaSayfaBoyutu)
            .WithMessage($"Sayfa boyutu 1 ile {BarkodUrunAramaKurali.EnFazlaSayfaBoyutu} arasında olmalıdır.");
    }
}

public sealed class BarkodUrunAramaRaporuQueryValidator : AbstractValidator<BarkodUrunAramaRaporuQuery>
{
    public BarkodUrunAramaRaporuQueryValidator()
    {
        RuleFor(x => x.Barkod).Must(BarkodUrunAramaKurali.Gecerli)
            .WithMessage($"Aranacak barkodu girin (en fazla {BarkodUrunAramaKurali.EnFazlaUzunluk} karakter).");
        RuleFor(x => x.ProjeTipiId).Must(BarkodUrunAramaDogrulama.ProjeTipiGecerli).WithMessage("Proje tipi geçersiz.");
        RuleFor(x => x.ProjeArama).Must(BarkodUrunAramaDogrulama.ProjeAramaGecerli)
            .WithMessage($"Proje / müşteri araması en fazla {BarkodUrunAramaKurali.ProjeAramaEnFazlaUzunluk} karakter olabilir.");
        RuleFor(x => x.Format).Must(f => SevkiyatRaporBicimleri.Coz(f) is not null)
            .WithMessage("Rapor biçimi pdf veya excel olmalıdır.");
    }
}

internal static class BarkodUrunAramaDogrulama
{
    /// <summary>Boş (bütün tipler) veya tanımlı proje tipi.</summary>
    public static bool ProjeTipiGecerli(int? projeTipiId) =>
        projeTipiId is null or 0 || Enum.IsDefined(typeof(ProjeTipi), projeTipiId.Value);

    public static bool ProjeAramaGecerli(string? metin) =>
        (metin?.Trim().Length ?? 0) <= BarkodUrunAramaKurali.ProjeAramaEnFazlaUzunluk;
}
