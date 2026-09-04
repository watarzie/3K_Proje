using FluentValidation;
using _3K.Application.Features.AmbalajIslemleri.Queries;

namespace _3K.Application.Features.AmbalajIslemleri.Validators
{
    public sealed class GetAmbalajPlanlamaProjeleriQueryValidator
        : AbstractValidator<GetAmbalajPlanlamaProjeleriQuery>
    {
        public GetAmbalajPlanlamaProjeleriQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.ProjeTipiId).InclusiveBetween(1, 3).When(x => x.ProjeTipiId.HasValue);
            RuleFor(x => x.Grup).InclusiveBetween(1, 3);
            RuleFor(x => x.Arama).MaximumLength(200);
        }
    }

    public sealed class GetAmbalajBagimsizSandiklarQueryValidator
        : AbstractValidator<GetAmbalajBagimsizSandiklarQuery>
    {
        public GetAmbalajBagimsizSandiklarQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.Tur).Must(x => x is 2 or 3 or 4 or 5)
                .When(x => x.Tur.HasValue)
                .WithMessage("Özel sandık türü geçersiz.");
            RuleFor(x => x.Arama).MaximumLength(200);
        }
    }

    public sealed class GetAmbalajProjeSandikSecenekleriQueryValidator
        : AbstractValidator<GetAmbalajProjeSandikSecenekleriQuery>
    {
        public GetAmbalajProjeSandikSecenekleriQueryValidator()
        {
            RuleFor(x => x.ProjeId).GreaterThan(0);
        }
    }

    public sealed class GetAmbalajProjeleriQueryValidator : AbstractValidator<GetAmbalajProjeleriQuery>
    {
        public GetAmbalajProjeleriQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.ProjeTipiId).GreaterThan(0).When(x => x.ProjeTipiId.HasValue);
            RuleFor(x => x.Arama).MaximumLength(200);
        }
    }

    public sealed class GetAmbalajUretimKayitlariQueryValidator : AbstractValidator<GetAmbalajUretimKayitlariQuery>
    {
        public GetAmbalajUretimKayitlariQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
            RuleFor(x => x.ProjeId).GreaterThan(0).When(x => x.ProjeId.HasValue);
            RuleFor(x => x.Ay).InclusiveBetween(1, 12).When(x => x.Ay.HasValue);
            RuleFor(x => x.Yil).InclusiveBetween(2000, 2100).When(x => x.Yil.HasValue);
            RuleFor(x => x).Must(x => !x.BaslangicTarihi.HasValue || !x.BitisTarihi.HasValue ||
                                         x.BaslangicTarihi.Value <= x.BitisTarihi.Value)
                .WithMessage("Başlangıç tarihi bitiş tarihinden sonra olamaz.");
            RuleFor(x => x.Arama).MaximumLength(200);
            RuleFor(x => x.ManuelProjeNo).MaximumLength(100);
            RuleFor(x => x.TalepEdenKisi).MaximumLength(200);
            RuleFor(x => x.TalepEdenBolum).MaximumLength(200);
            RuleFor(x => x.TalimatVeren).MaximumLength(200);
            RuleFor(x => x.FirinPartiNo).MaximumLength(100);
        }
    }

    public sealed class GetAmbalajUretimKaydiDetayQueryValidator : AbstractValidator<GetAmbalajUretimKaydiDetayQuery>
    {
        public GetAmbalajUretimKaydiDetayQueryValidator() => RuleFor(x => x.Id).GreaterThan(0);
    }

    public sealed class GetAmbalajRaporQueryValidator : AbstractValidator<GetAmbalajRaporQuery>
    {
        public GetAmbalajRaporQueryValidator()
        {
            RuleFor(x => x.ProjeId).GreaterThan(0).When(x => x.ProjeId.HasValue);
            RuleFor(x => x.Ay).InclusiveBetween(1, 12).When(x => x.Ay.HasValue);
            RuleFor(x => x.Yil).InclusiveBetween(2000, 2100).When(x => x.Yil.HasValue);
            RuleFor(x => x).Must(x => !x.BaslangicTarihi.HasValue || !x.BitisTarihi.HasValue ||
                                         x.BaslangicTarihi.Value <= x.BitisTarihi.Value)
                .WithMessage("Başlangıç tarihi bitiş tarihinden sonra olamaz.");
        }
    }

    public sealed class GetAmbalajRaporDosyasiQueryValidator : AbstractValidator<GetAmbalajRaporDosyasiQuery>
    {
        public GetAmbalajRaporDosyasiQueryValidator()
        {
            RuleFor(x => x.ProjeId).GreaterThan(0).When(x => x.ProjeId.HasValue);
            RuleFor(x => x.Ay).InclusiveBetween(1, 12).When(x => x.Ay.HasValue);
            RuleFor(x => x.Yil).InclusiveBetween(2000, 2100).When(x => x.Yil.HasValue);
            RuleFor(x => x).Must(x => !x.BaslangicTarihi.HasValue || !x.BitisTarihi.HasValue ||
                                         x.BaslangicTarihi.Value <= x.BitisTarihi.Value)
                .WithMessage("Başlangıç tarihi bitiş tarihinden sonra olamaz.");
            RuleFor(x => x.Format)
                .Must(x => string.Equals(x, "xlsx", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(x, "pdf", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Rapor formatı xlsx veya pdf olmalıdır.");
        }
    }

    public sealed class GetAmbalajUretimFormuQueryValidator : AbstractValidator<GetAmbalajUretimFormuQuery>
    {
        public GetAmbalajUretimFormuQueryValidator()
        {
            RuleFor(x => x).Must(x => new[] { x.KayitId.HasValue, x.ProjeId.HasValue, !string.IsNullOrWhiteSpace(x.ManuelProjeNo) }.Count(v => v) == 1)
                .WithMessage("Üretim formu için kayıt, proje veya manuel proje numarasından yalnız biri belirtilmelidir.");
            RuleFor(x => x.KayitId).GreaterThan(0).When(x => x.KayitId.HasValue);
            RuleFor(x => x.ProjeId).GreaterThan(0).When(x => x.ProjeId.HasValue);
        }
    }

    public sealed class GetAmbalajUretimFormuDosyasiQueryValidator : AbstractValidator<GetAmbalajUretimFormuDosyasiQuery>
    {
        public GetAmbalajUretimFormuDosyasiQueryValidator()
        {
            RuleFor(x => x).Must(x => new[]
                {
                    x.KayitId.HasValue,
                    x.ProjeId.HasValue,
                    !string.IsNullOrWhiteSpace(x.ManuelProjeNo),
                    x.KayitIdleri is { Count: > 0 }
                }.Count(v => v) == 1)
                .WithMessage("Üretim formu için kayıt, proje, manuel proje numarası veya seçili kayıt listesinden yalnız biri belirtilmelidir.");
            RuleFor(x => x.KayitId).GreaterThan(0).When(x => x.KayitId.HasValue);
            RuleFor(x => x.ProjeId).GreaterThan(0).When(x => x.ProjeId.HasValue);
            RuleFor(x => x.ManuelProjeNo).MaximumLength(100);
            RuleFor(x => x.KayitIdleri)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .Must(ids => ids.Distinct().Count() <= GetAmbalajUretimFormuDosyasiQuery.EnFazlaSecilebilirKayit)
                .WithMessage($"Tek seferde en fazla {GetAmbalajUretimFormuDosyasiQuery.EnFazlaSecilebilirKayit} sandık seçilebilir.");
            RuleForEach(x => x.KayitIdleri)
                .GreaterThan(0)
                .WithMessage("Seçili kayıt kimlikleri sıfırdan büyük olmalıdır.")
                .When(x => x.KayitIdleri != null);
            RuleFor(x => x.Format)
                .Must(x => string.Equals(x, "xlsx", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(x, "pdf", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Üretim formu formatı xlsx veya pdf olmalıdır.");
        }
    }
}
