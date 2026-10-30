using FluentValidation;
using _3K.Application.Features.SandikIslemleri.Queries;
using _3K.Core.Models;

namespace _3K.Application.Features.SandikIslemleri.Validators;

public sealed class BarkodEtiketProjeleriQueryValidator : AbstractValidator<BarkodEtiketProjeleriQuery>
{
    public BarkodEtiketProjeleriQueryValidator()
    {
        RuleFor(x => x.Arama).Must(BarkodEtiketKurali.AramaGecerli).WithMessage(BarkodEtiketDogrulama.AramaMesaji);
        RuleFor(x => x.Sayfa).Must(BarkodEtiketDogrulama.SayfaGecerli).WithMessage("Sayfa 1 veya büyük olmalıdır.");
        RuleFor(x => x.Boyut).Must(BarkodEtiketDogrulama.BoyutGecerli).WithMessage(BarkodEtiketDogrulama.BoyutMesaji);
    }
}

public sealed class BarkodEtiketProjeQueryValidator : AbstractValidator<BarkodEtiketProjeQuery>
{
    public BarkodEtiketProjeQueryValidator()
    {
        RuleFor(x => x.ProjeId).GreaterThan(0).WithMessage("Proje seçin.");
    }
}

public sealed class BarkodEtiketSandiklariQueryValidator : AbstractValidator<BarkodEtiketSandiklariQuery>
{
    public BarkodEtiketSandiklariQueryValidator()
    {
        RuleFor(x => x.ProjeId).GreaterThan(0).WithMessage("Proje seçin.");
        RuleFor(x => x.Arama).Must(BarkodEtiketKurali.AramaGecerli).WithMessage(BarkodEtiketDogrulama.AramaMesaji);
        RuleFor(x => x.Sayfa).Must(BarkodEtiketDogrulama.SayfaGecerli).WithMessage("Sayfa 1 veya büyük olmalıdır.");
        RuleFor(x => x.Boyut).Must(BarkodEtiketDogrulama.BoyutGecerli).WithMessage(BarkodEtiketDogrulama.BoyutMesaji);
    }
}

public sealed class BarkodEtiketUrunleriQueryValidator : AbstractValidator<BarkodEtiketUrunleriQuery>
{
    public BarkodEtiketUrunleriQueryValidator()
    {
        RuleFor(x => x.ProjeId).GreaterThan(0).WithMessage("Proje seçin.");
        RuleFor(x => x.SandikId).Must(id => id is null or > 0).WithMessage("Sandık geçersiz.");
        RuleFor(x => x.Arama).Must(BarkodEtiketKurali.AramaGecerli).WithMessage(BarkodEtiketDogrulama.AramaMesaji);
        RuleFor(x => x.Sayfa).Must(BarkodEtiketDogrulama.SayfaGecerli).WithMessage("Sayfa 1 veya büyük olmalıdır.");
        RuleFor(x => x.Boyut).Must(BarkodEtiketDogrulama.BoyutGecerli).WithMessage(BarkodEtiketDogrulama.BoyutMesaji);
    }
}

public sealed class SandikEtiketiOlusturQueryValidator : AbstractValidator<SandikEtiketiOlusturQuery>
{
    public SandikEtiketiOlusturQueryValidator()
    {
        RuleFor(x => x.ProjeId).GreaterThan(0).WithMessage("Proje seçin.");
        RuleFor(x => x.Bicim).IsInEnum().WithMessage("Etiket biçimi PDF veya EZPL olmalıdır.");
        RuleFor(x => x).Must(x => BarkodEtiketKurali.TarihGecerli(x.Yil, x.Ay))
            .WithName("Tarih").WithMessage("Etiket tarihi için geçerli bir ay ve yıl seçin.");
        RuleFor(x => x.Secimler).Must(s => s is not null && s.All(x => x is not null)).WithMessage("Seçilen sandık geçersiz.");
        RuleFor(x => x.Secimler).Must(s => BarkodEtiketKurali.AdetlerGecerli(s.Select(x => x.Adet).ToList()))
            .When(x => x.Secimler is not null && x.Secimler.All(s => s is not null))
            .WithMessage(BarkodEtiketDogrulama.SecimMesaji("sandık"));
        RuleFor(x => x.Secimler).Must(s => s.Select(x => x.SandikId).Distinct().Count() == s.Count)
            .When(x => x.Secimler is not null && x.Secimler.All(s => s is not null))
            .WithMessage("Aynı sandık birden fazla seçilmiş; adedini artırın.");
        RuleForEach(x => x.Secimler).Must(s => s is not null && s.SandikId > 0).WithMessage("Seçilen sandık geçersiz.");
    }
}

public sealed class UrunEtiketiOlusturQueryValidator : AbstractValidator<UrunEtiketiOlusturQuery>
{
    public UrunEtiketiOlusturQueryValidator()
    {
        RuleFor(x => x.ProjeId).GreaterThan(0).WithMessage("Proje seçin.");
        RuleFor(x => x.Bicim).IsInEnum().WithMessage("Etiket biçimi PDF veya EZPL olmalıdır.");
        RuleFor(x => x.Secimler).Must(s => s is not null && s.All(x => x is not null)).WithMessage("Seçilen ürün geçersiz.");
        RuleFor(x => x.Secimler).Must(s => BarkodEtiketKurali.AdetlerGecerli(s.Select(x => x.Adet).ToList()))
            .When(x => x.Secimler is not null && x.Secimler.All(s => s is not null))
            .WithMessage(BarkodEtiketDogrulama.SecimMesaji("ürün"));
        RuleFor(x => x.Secimler).Must(s => s.Select(x => (x.Kaynak, x.Id)).Distinct().Count() == s.Count)
            .When(x => x.Secimler is not null && x.Secimler.All(s => s is not null))
            .WithMessage("Aynı ürün birden fazla seçilmiş; adedini artırın.");
        RuleForEach(x => x.Secimler).Must(s => s is not null && s.Id > 0 && Enum.IsDefined(s.Kaynak))
            .WithMessage("Seçilen ürün geçersiz.");
        RuleForEach(x => x.Secimler).Must(s => s is null || BarkodEtiketKurali.RemarksGecerli(s.Remarks))
            .WithMessage($"REMARKS en fazla {BarkodEtiketKurali.RemarksEnFazlaUzunluk} karakter olabilir.");
    }
}

internal static class BarkodEtiketDogrulama
{
    public static readonly string AramaMesaji = $"Arama en fazla {BarkodEtiketKurali.AramaEnFazlaUzunluk} karakter olabilir.";
    public static readonly string BoyutMesaji = $"Sayfa boyutu 1 ile {SayfaKurali.EnFazlaBoyut} arasında olmalıdır.";

    public static bool SayfaGecerli(int? sayfa) => sayfa is null or > 0;

    public static bool BoyutGecerli(int? boyut) => boyut is null || (boyut >= 1 && boyut <= SayfaKurali.EnFazlaBoyut);

    public static string SecimMesaji(string tur) =>
        $"En az bir {tur} seçin; her biri için 1–{BarkodEtiketKurali.EnFazlaAdet} adet, " +
        $"toplam en fazla {BarkodEtiketKurali.EnFazlaToplamEtiket} etiket.";
}
