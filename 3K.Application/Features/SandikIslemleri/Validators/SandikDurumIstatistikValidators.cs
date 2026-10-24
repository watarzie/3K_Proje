using FluentValidation;
using _3K.Application.Features.SandikIslemleri.Queries;
using _3K.Core.Models;

namespace _3K.Application.Features.SandikIslemleri.Validators
{
    public class GetSandikDurumGecmisiQueryValidator : AbstractValidator<GetSandikDurumGecmisiQuery>
    {
        public GetSandikDurumGecmisiQueryValidator()
        {
            RuleFor(x => x.SandikId).GreaterThan(0).WithMessage("Sandık seçilmelidir.");
        }
    }

    public class SandikDurumIstatistigiQueryValidator : AbstractValidator<SandikDurumIstatistigiQuery>
    {
        public SandikDurumIstatistigiQueryValidator()
        {
            RuleFor(x => x)
                .Must(x => SandikDurumOlayKurali.TarihAraligiGecerli(x.Baslangic, x.Bitis))
                .WithMessage("Başlangıç tarihi bitiş tarihinden sonra olamaz.");
        }
    }

    public class SandikDurumIstatistigiExcelQueryValidator : AbstractValidator<SandikDurumIstatistigiExcelQuery>
    {
        public SandikDurumIstatistigiExcelQueryValidator()
        {
            RuleFor(x => x)
                .Must(x => SandikDurumOlayKurali.TarihAraligiGecerli(x.Baslangic, x.Bitis))
                .WithMessage("Başlangıç tarihi bitiş tarihinden sonra olamaz.");
        }
    }

    /// <summary>Sayfalı istatistik tabloları: tarih aralığı ve sayfa/boyut sınırı.</summary>
    public abstract class SandikDurumIstatistikSayfaValidator<TQuery, T> : AbstractValidator<TQuery>
        where TQuery : SandikDurumIstatistikSayfaQuery<T>
    {
        protected SandikDurumIstatistikSayfaValidator()
        {
            RuleFor(x => x)
                .Must(x => SandikDurumOlayKurali.TarihAraligiGecerli(x.Baslangic, x.Bitis))
                .WithMessage("Başlangıç tarihi bitiş tarihinden sonra olamaz.");
            RuleFor(x => x.Sayfa).GreaterThan(0).When(x => x.Sayfa.HasValue).WithMessage("Sayfa 1'den küçük olamaz.");
            RuleFor(x => x.Boyut).InclusiveBetween(1, SayfaKurali.EnFazlaBoyut).When(x => x.Boyut.HasValue)
                .WithMessage($"Sayfa boyutu 1 ile {SayfaKurali.EnFazlaBoyut} arasında olmalıdır.");
        }
    }

    public class SandikDurumProjeleriQueryValidator : SandikDurumIstatistikSayfaValidator<SandikDurumProjeleriQuery, SandikDurumProjeSatiri>;
    public class SandikDurumMusterileriQueryValidator : SandikDurumIstatistikSayfaValidator<SandikDurumMusterileriQuery, SandikDurumMusteriSatiri>;
    public class SandikDurumKisileriQueryValidator : SandikDurumIstatistikSayfaValidator<SandikDurumKisileriQuery, SandikDurumKisiSatiri>;
    public class SandikDurumOlaylariQueryValidator : SandikDurumIstatistikSayfaValidator<SandikDurumOlaylariQuery, SandikDurumOlayModeli>;
}
