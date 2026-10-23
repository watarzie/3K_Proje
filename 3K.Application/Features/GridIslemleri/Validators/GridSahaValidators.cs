using FluentValidation;
using _3K.Application.Features.GridIslemleri.Queries;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Models;

namespace _3K.Application.Features.GridIslemleri.Validators
{
    public sealed class GetGridSahaUrunleriQueryValidator : AbstractValidator<GetGridSahaUrunleriQuery>
    {
        public GetGridSahaUrunleriQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThan(0).WithMessage("Sayfa numarası 1'den küçük olamaz.");
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("Sayfa boyutu 1 ile 100 arasında olmalıdır.");
            RuleFor(x => x.Arama).MaximumLength(100).WithMessage("Arama metni en fazla 100 karakter olabilir.");
            RuleFor(x => x.Kapsam).Must(GridSahaKapsami.Gecerli).WithMessage("Geçersiz liste kapsamı.");
            RuleFor(x => x.ProjeTipiId)
                .Must(tip => !tip.HasValue || Enum.IsDefined(typeof(ProjeTipi), tip.Value))
                .WithMessage("Geçersiz proje tipi.");
            RuleFor(x => x.GridDurumuId)
                .Must(durum => !durum.HasValue || GridSahaBekleyenKurali.GridGelmisDurum(durum.Value))
                .WithMessage("Grid Sahası yalnız Tam Geldi ve Eksik Geldi durumlarını listeler.");
            RuleFor(x => x.Siralama)
                .Must(siralama => string.IsNullOrWhiteSpace(siralama) || GridSahaSiralama.Gecerli(siralama))
                .WithMessage("Geçersiz sıralama alanı.");
        }
    }

    public sealed class GetGridMalKabulGecmisiQueryValidator : AbstractValidator<GetGridMalKabulGecmisiQuery>
    {
        public GetGridMalKabulGecmisiQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThan(0).WithMessage("Sayfa numarası 1'den küçük olamaz.");
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("Sayfa boyutu 1 ile 100 arasında olmalıdır.");
            RuleFor(x => x.Arama).MaximumLength(100).WithMessage("Arama metni en fazla 100 karakter olabilir.");
            RuleFor(x => x.Tur).Must(GridMalKabulTuru.Gecerli).WithMessage("Geçersiz kayıt türü.");
            RuleFor(x => x)
                .Must(x => MakulTarih(x.Baslangic) && MakulTarih(x.Bitis))
                .WithMessage("Tarih 2000–2100 aralığında olmalıdır.");
            // Aralık kuralları uygulanan aralıkla denetlenir: yalnız başlangıç verilse bitiş bugündür, sınır delinmez.
            When(x => MakulTarih(x.Baslangic) && MakulTarih(x.Bitis), () =>
            {
                RuleFor(x => x)
                    .Must(x => x.EtkinAralik(TurkeyTime.Now) is var (baslangic, bitis) && baslangic <= bitis)
                    .WithMessage("Tarih aralığı geçersiz: başlangıç bitişten sonra olamaz.");
                RuleFor(x => x)
                    .Must(x => x.EtkinAralik(TurkeyTime.Now) is var (baslangic, bitis) && (bitis - baslangic).TotalDays <= 366)
                    .WithMessage("Tarih aralığı en fazla bir yıl olabilir.");
            });
        }

        private static bool MakulTarih(DateTime? tarih) =>
            !tarih.HasValue ||
            (tarih.Value >= GetGridMalKabulGecmisiQuery.EnErkenTarih && tarih.Value <= GetGridMalKabulGecmisiQuery.EnGecTarih);
    }
}
