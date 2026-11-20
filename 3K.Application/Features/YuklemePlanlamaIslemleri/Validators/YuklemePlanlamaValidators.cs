using FluentValidation;
using _3K.Application.Features.YuklemePlanlamaIslemleri.Commands;
using _3K.Application.Features.YuklemePlanlamaIslemleri.DTOs;
using _3K.Application.Features.YuklemePlanlamaIslemleri.Queries;
using _3K.Core.Models;

namespace _3K.Application.Features.YuklemePlanlamaIslemleri.Validators;

/// <summary>
/// Plan başlığının ortak kuralları (oluşturma ve kaydetme komutlarına <c>Include</c> edilir). İç sınıftır; tek başına
/// kayda girmez.
/// </summary>
internal sealed class YuklemePlaniBaslikValidator : AbstractValidator<IYuklemePlaniBasligi>
{
    public static readonly string ProjeSayisiMesaji = $"Bir planda en fazla {YuklemePlaniKurali.EnFazlaProje} proje olabilir.";

    public YuklemePlaniBaslikValidator()
    {
        RuleFor(x => x.Ad).NotEmpty().WithMessage("Plan adı zorunludur.")
            .MaximumLength(YuklemePlaniKurali.AdEnFazla).WithMessage($"Plan adı en fazla {YuklemePlaniKurali.AdEnFazla} karakter olabilir.");
        RuleFor(x => x.CalismaBicimi).IsInEnum().WithMessage("Çalışma biçimi geçersiz.");
        RuleFor(x => x.TasimaAilesiId).GreaterThan(0).WithMessage("Taşıma ailesi seçilmelidir.");
        RuleFor(x => x.ProjeKarisimi).IsInEnum().WithMessage("Proje karışımı tercihi geçersiz.");
        RuleFor(x => x.Aciklama).MaximumLength(YuklemePlaniKurali.AciklamaEnFazla)
            .WithMessage($"Açıklama en fazla {YuklemePlaniKurali.AciklamaEnFazla} karakter olabilir.");
        // Cascade Stop: gövdede null gelen liste Must'a ulaşıp 500 üretmesin, 400 dönsün.
        RuleFor(x => x.ProjeIdleri).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("En az bir proje seçilmelidir.")
            .Must(p => p.Distinct().Count() <= YuklemePlaniKurali.EnFazlaProje).WithMessage(ProjeSayisiMesaji)
            .Must(p => p.All(id => id > 0)).WithMessage("Proje kimliği geçersiz.");
    }
}

public sealed class YuklemePlaniPlanHesaplaCommandValidator : AbstractValidator<YuklemePlaniPlanHesaplaCommand>
{
    public YuklemePlaniPlanHesaplaCommandValidator()
    {
        RuleFor(x => x.PlanId).GreaterThan(0).When(x => x.PlanId != null).WithMessage("Plan kimliği geçersiz.");
        RuleFor(x => x.BeklenenRevizyonNo).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(RevizyonEksik)
            .GreaterThan(0).WithMessage(RevizyonEksik)
            .When(x => x.PlanId != null);
        RuleFor(x => x.TasimaAilesiId).GreaterThan(0).WithMessage("Taşıma ailesi seçilmelidir.");
        RuleFor(x => x.ProjeIdleri).ProjeSecimi();
        RuleFor(x => x.Ayarlar.TipIdleri).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(YuklemePlaniKurali.TipSecimiMesaji)
            .Must(YuklemePlaniKurali.TipIdleriGecerli).WithMessage(YuklemePlaniKurali.TipSecimiMesaji)
            .When(x => x.Ayarlar != null);
        RuleFor(x => x.Ayarlar).Cascade(CascadeMode.Stop).NotNull().WithMessage("Plan ayarları eksik.").Must(YuklemePlaniKurali.AyarlarGecerli)
            .WithMessage(YuklemePlaniKaydetCommandValidator.AyarlarMesaji);
        RuleFor(x => x.LokasyonTercihi).Must(YuklemePlaniKurali.HesapLokasyonTercihiGecerli).WithMessage(YuklemePlaniKurali.LokasyonTercihiMesaji);
        // Sandık seçimleri isteğe bağlıdır (boşsa bütün sandıklar dahil ve varsayılan izinli); gönderilirse kaydetmedeki
        // kurallarla doğrulanır. Cascade Stop: boş öge sonraki kurallarda 500 üretmesin, 400 dönsün.
        When(x => x.SandikYukleri != null, () =>
        {
            RuleFor(x => x.SandikYukleri!).Cascade(CascadeMode.Stop)
                .Must(y => y.All(s => s != null)).WithMessage(PlanSandikYukuDtoValidator.BosOgeMesaji)
                .Must(y => y.Count <= YuklemePlaniKurali.EnFazlaYuk).WithMessage(YuklemePlaniKurali.YukSiniriMesaji)
                .Must(PlanSandikYukuDtoValidator.Tekil).WithMessage(YuklemePlaniKurali.SandikTekrariMesaji);
            RuleForEach(x => x.SandikYukleri).SetValidator(new PlanSandikYukuDtoValidator());
        });
    }

    private const string RevizyonEksik = "Plan revizyonu eksik; sayfayı yenileyin.";
}

/// <summary>Yük önizlemesi: proje seçimi tek adımlı hesapla aynı kuraldır.</summary>
public sealed class PlanYukOnizlemeQueryValidator : AbstractValidator<PlanYukOnizlemeQuery>
{
    public PlanYukOnizlemeQueryValidator() => RuleFor(x => x.ProjeIdleri).ProjeSecimi();
}

/// <summary>Plan isteklerinin ortak proje seçimi kuralı.</summary>
internal static class YuklemePlaniKuralUzantilari
{
    /// <summary>En az bir proje, sınır kadar farklı proje ve pozitif kimlik. Cascade Stop: null liste 500 değil 400 olur.</summary>
    public static void ProjeSecimi<T>(this IRuleBuilderInitial<T, List<int>> kural) =>
        kural.Cascade(CascadeMode.Stop).NotEmpty().WithMessage("En az bir proje seçilmelidir.")
            .Must(p => p.Distinct().Count() <= YuklemePlaniKurali.EnFazlaProje).WithMessage(YuklemePlaniBaslikValidator.ProjeSayisiMesaji)
            .Must(p => p.All(id => id > 0)).WithMessage("Proje kimliği geçersiz.");
}

/// <summary>
/// Sandık yükünün kullanıcı seçimi (kaydetme ve tek adımlı hesap aynı kuralları kullanır): geçerli sandık kimliği, üst yük
/// sınırı ve "elle değişti" bayrakları.
/// </summary>
public sealed class PlanSandikYukuDtoValidator : AbstractValidator<PlanSandikYukuDto>
{
    internal const string BosOgeMesaji = "Sandık yük listesinde boş kayıt var.";

    public PlanSandikYukuDtoValidator()
    {
        RuleFor(s => s.SandikId).GreaterThan(0).WithMessage("Sandık kimliği geçersiz.");
        RuleFor(s => s.UstYukSiniriKg).Must(YukIzinKurali.UstYukSiniriGecerli).WithMessage(YukIzinKurali.UstYukSiniriMesaji);
        RuleFor(s => s.ElleDegisenIzinler).Must(YukIzinKurali.ElleDegisenGecerli).WithMessage(YukIzinKurali.ElleDegisenMesaji);
    }

    /// <summary>Bir sandığın seçimi listede bir kez geçer.</summary>
    internal static bool Tekil(List<PlanSandikYukuDto> yukler) =>
        YuklemePlaniKurali.SandikSecimleriTekil(yukler.Select(s => s.SandikId).ToList());
}

public sealed class YuklemePlaniOlusturCommandValidator : AbstractValidator<YuklemePlaniOlusturCommand>
{
    public YuklemePlaniOlusturCommandValidator() => Include(new YuklemePlaniBaslikValidator());
}

public sealed class YuklemePlaniKaydetCommandValidator : AbstractValidator<YuklemePlaniKaydetCommand>
{
    internal static readonly string AyarlarMesaji =
        $"Yükler arası boşluk 0–{YuklemePlaniKurali.YerlesimBosluguEnFazlaMm} mm, ağırlık merkezi sapma sınırı boş ya da %1–{YuklemePlaniKurali.AgirlikMerkeziSapmaEnFazlaYuzde} olmalıdır.";

    public YuklemePlaniKaydetCommandValidator()
    {
        RuleFor(x => x.PlanId).GreaterThan(0).WithMessage("Plan seçilmelidir.");
        RuleFor(x => x.BeklenenRevizyonNo).GreaterThan(0).WithMessage("Planın revizyon numarası eksik; sayfayı yenileyin.");
        Include(new YuklemePlaniBaslikValidator());
        RuleFor(x => x.RevizyonAciklamasi).MaximumLength(YuklemePlaniKurali.AciklamaEnFazla)
            .WithMessage($"Revizyon açıklaması en fazla {YuklemePlaniKurali.AciklamaEnFazla} karakter olabilir.");
        // Gövdede açık null gelen liste 500 değil doğrulama hatası olur (boş liste "araç yok" demektir).
        RuleFor(x => x.Ayarlar.MevcutAraclar).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(YuklemePlaniKurali.MevcutAraclarMesaji)
            .Must(YuklemePlaniKurali.MevcutAraclarGecerli).WithMessage(YuklemePlaniKurali.MevcutAraclarMesaji)
            .When(x => x.Ayarlar != null);
        RuleFor(x => x.Ayarlar.TipIdleri).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(YuklemePlaniKurali.TipSecimiMesaji)
            .Must(YuklemePlaniKurali.TipIdleriGecerli).WithMessage(YuklemePlaniKurali.TipSecimiMesaji)
            .When(x => x.Ayarlar != null);
        RuleFor(x => x.Ayarlar).Cascade(CascadeMode.Stop).NotNull().WithMessage("Plan ayarları eksik.").Must(YuklemePlaniKurali.AyarlarGecerli)
            .WithMessage(AyarlarMesaji);
        RuleFor(x => x.SandikYukleri).Cascade(CascadeMode.Stop).NotNull().WithMessage("Sandık yük listesi eksik.")
            .Must(PlanSandikYukuDtoValidator.Tekil).WithMessage(YuklemePlaniKurali.SandikTekrariMesaji);
        RuleFor(x => x.ManuelYukler).NotNull().WithMessage("Elle eklenen yük listesi eksik.");
        RuleForEach(x => x.SandikYukleri).SetValidator(new PlanSandikYukuDtoValidator());
        RuleForEach(x => x.ManuelYukler).SetValidator(new PlanManuelYukDtoValidator());
        RuleFor(x => x).Must(x => (x.SandikYukleri?.Count ?? 0) + (x.ManuelYukler ?? []).Sum(m => Math.Max(m.Adet, 0)) <= YuklemePlaniKurali.EnFazlaYuk)
            .WithMessage(YuklemePlaniKurali.YukSiniriMesaji);
    }
}

public sealed class PlanManuelYukDtoValidator : AbstractValidator<PlanManuelYukDto>
{
    public PlanManuelYukDtoValidator()
    {
        RuleFor(x => x.Kod).NotEmpty().WithMessage("Elle eklenen yükün kodu zorunludur.")
            .MaximumLength(YuklemePlaniKurali.ManuelKodEnFazla).WithMessage($"Yük kodu en fazla {YuklemePlaniKurali.ManuelKodEnFazla} karakter olabilir.");
        RuleFor(x => x.Aciklama).MaximumLength(YuklemePlaniKurali.ManuelAciklamaEnFazla)
            .WithMessage($"Yük açıklaması en fazla {YuklemePlaniKurali.ManuelAciklamaEnFazla} karakter olabilir.");
        RuleFor(x => x.YukTuruId).Must(id => YukTuruKurali.Gecerli(id)).WithMessage(YukTuruKurali.Mesaj);
        RuleFor(x => x).Must(x => YukVeriKurali.ElleOlcuGecerli(x.BoyMm) && YukVeriKurali.ElleOlcuGecerli(x.EnMm) &&
                                  YukVeriKurali.ElleOlcuGecerli(x.YukseklikMm))
            .WithMessage($"Girilen ölçü sıfırdan büyük ve en fazla {YukVeriKurali.EnFazlaOlcuMm:0} mm olmalıdır.");
        RuleFor(x => x.BrutKg).Must(YukVeriKurali.ElleAgirlikGecerli)
            .WithMessage($"Girilen brüt ağırlık sıfırdan büyük ve en fazla {YukVeriKurali.EnFazlaAgirlikKg:0} kg olmalıdır.");
        RuleFor(x => x.Adet).InclusiveBetween(1, YuklemePlaniKurali.ManuelAdetEnFazla)
            .WithMessage($"Adet 1–{YuklemePlaniKurali.ManuelAdetEnFazla} arasında olmalıdır.");
        RuleFor(x => x.ProjeId).GreaterThan(0).When(x => x.ProjeId.HasValue).WithMessage("Proje kimliği geçersiz.");
        RuleFor(x => x.LokasyonId).GreaterThan(0).When(x => x.LokasyonId.HasValue).WithMessage("Lokasyon kimliği geçersiz.");
        RuleFor(x => x.OzelTasimaNotu).MaximumLength(YuklemePlaniKurali.OzelTasimaNotuEnFazla)
            .WithMessage($"Özel taşıma notu en fazla {YuklemePlaniKurali.OzelTasimaNotuEnFazla} karakter olabilir.");
        RuleFor(x => x).Must(x => (x.Birimler ?? []).All(b => b.BirimNo >= 1 && b.BirimNo <= x.Adet) &&
                                  (x.Birimler ?? []).Select(b => b.BirimNo).Distinct().Count() == (x.Birimler ?? []).Count)
            .WithMessage("Birim numaraları 1'den adede kadar ve tekil olmalıdır.");
        RuleForEach(x => x.Birimler).ChildRules(b =>
        {
            b.RuleFor(y => y.UstYukSiniriKg).Must(YukIzinKurali.UstYukSiniriGecerli).WithMessage(YukIzinKurali.UstYukSiniriMesaji);
            b.RuleFor(y => y.ElleDegisenIzinler).Must(YukIzinKurali.ElleDegisenGecerli).WithMessage(YukIzinKurali.ElleDegisenMesaji);
        });
    }
}

public sealed class YuklemePlaniKaynaktanGuncelleCommandValidator : AbstractValidator<YuklemePlaniKaynaktanGuncelleCommand>
{
    public YuklemePlaniKaynaktanGuncelleCommandValidator()
    {
        RuleFor(x => x.PlanId).GreaterThan(0).WithMessage("Plan seçilmelidir.");
        RuleFor(x => x.BeklenenRevizyonNo).GreaterThan(0).WithMessage("Planın revizyon numarası eksik; sayfayı yenileyin.");
    }
}

public sealed class YuklemePlaniHesaplaCommandValidator : AbstractValidator<YuklemePlaniHesaplaCommand>
{
    public YuklemePlaniHesaplaCommandValidator()
    {
        RuleFor(x => x.PlanId).GreaterThan(0).WithMessage("Plan seçilmelidir.");
        RuleFor(x => x.BeklenenRevizyonNo).GreaterThan(0).WithMessage("Planın revizyon numarası eksik; sayfayı yenileyin.");
        RuleFor(x => x.LokasyonTercihi).IsInEnum().WithMessage("Lokasyon tercihi geçersiz.");
    }
}

public sealed class YuklemePlaniGetirQueryValidator : AbstractValidator<YuklemePlaniGetirQuery>
{
    public YuklemePlaniGetirQueryValidator() => RuleFor(x => x.PlanId).GreaterThan(0).WithMessage("Plan seçilmelidir.");
}

public sealed class YuklemePlanlariQueryValidator : AbstractValidator<YuklemePlanlariQuery>
{
    public YuklemePlanlariQueryValidator() =>
        RuleFor(x => x.Arama).MaximumLength(100).WithMessage("Arama en fazla 100 karakter olabilir.");
}

public sealed class PlanProjeAdaylariQueryValidator : AbstractValidator<PlanProjeAdaylariQuery>
{
    public PlanProjeAdaylariQueryValidator() =>
        RuleFor(x => x.Arama).MaximumLength(100).WithMessage("Arama en fazla 100 karakter olabilir.");
}

public sealed class EkipmanKonfigurasyonlariQueryValidator : AbstractValidator<EkipmanKonfigurasyonlariQuery>
{
    public EkipmanKonfigurasyonlariQueryValidator()
    {
        RuleFor(x => x.Arama).MaximumLength(100).WithMessage("Arama en fazla 100 karakter olabilir.");
        RuleFor(x => x.DogrulamaDurumu).IsInEnum().When(x => x.DogrulamaDurumu.HasValue).WithMessage("Doğrulama durumu geçersiz.");
    }
}

public sealed class EkipmanKonfigurasyonGetirQueryValidator : AbstractValidator<EkipmanKonfigurasyonGetirQuery>
{
    public EkipmanKonfigurasyonGetirQueryValidator() => RuleFor(x => x.Id).GreaterThan(0).WithMessage("Konfigürasyon seçilmelidir.");
}

/// <summary>İçe aktarım dosyası: .xlsx, boş değil ve en fazla 10 MB.</summary>
internal static class EkipmanDosyaKurallari
{
    public static readonly string BoyutMesaji = $"Dosya boş olamaz ve en fazla {EkipmanKitapligiKurali.DosyaEnFazlaBayt / (1024 * 1024)} MB olabilir.";
    public const string UzantiMesaji = "Yalnız .xlsx dosyası yüklenebilir.";

    public static bool UzantiGecerli(string dosyaAdi) => dosyaAdi.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase);
    public static bool BoyutGecerli(long boyut) => boyut is > 0 and <= EkipmanKitapligiKurali.DosyaEnFazlaBayt;
}

public sealed class EkipmanIceAktarOnizleQueryValidator : AbstractValidator<EkipmanIceAktarOnizleQuery>
{
    public EkipmanIceAktarOnizleQueryValidator()
    {
        RuleFor(x => x.DosyaAdi).Must(EkipmanDosyaKurallari.UzantiGecerli).WithMessage(EkipmanDosyaKurallari.UzantiMesaji);
        RuleFor(x => x.DosyaBoyutu).Must(EkipmanDosyaKurallari.BoyutGecerli).WithMessage(EkipmanDosyaKurallari.BoyutMesaji);
    }
}

public sealed class EkipmanIceAktarCommandValidator : AbstractValidator<EkipmanIceAktarCommand>
{
    public EkipmanIceAktarCommandValidator()
    {
        RuleFor(x => x.DosyaAdi).Must(EkipmanDosyaKurallari.UzantiGecerli).WithMessage(EkipmanDosyaKurallari.UzantiMesaji);
        RuleFor(x => x.DosyaBoyutu).Must(EkipmanDosyaKurallari.BoyutGecerli).WithMessage(EkipmanDosyaKurallari.BoyutMesaji);
    }
}

/// <summary>
/// Ekrandan girilen konfigürasyonun ortak kuralları (ekleme ve güncelleme komutlarına <c>Include</c> edilir): Excel'deki
/// konfigürasyon satırının zorunlu alanları ve sınırları. Metin kırpılmış haliyle ölçülür. İç sınıftır; tek başına kayda
/// girmez.
/// </summary>
internal sealed class EkipmanKonfigurasyonKaydiValidator : AbstractValidator<EkipmanKonfigurasyonKaydiIstegi>
{
    public EkipmanKonfigurasyonKaydiValidator()
    {
        RuleFor(x => x.Kod).Must(v => EkipmanKitapligiKurali.ZorunluMetinGecerli(v, EkipmanKitapligiKurali.KodEnFazla))
            .WithMessage($"Ekipman kodu zorunludur ve en fazla {EkipmanKitapligiKurali.KodEnFazla} karakter olabilir.");
        RuleFor(x => x.Ad).Must(v => EkipmanKitapligiKurali.ZorunluMetinGecerli(v, EkipmanKitapligiKurali.AdEnFazla))
            .WithMessage($"Ekipman adı zorunludur ve en fazla {EkipmanKitapligiKurali.AdEnFazla} karakter olabilir.");
        RuleFor(x => x.TipId).GreaterThan(0).WithMessage("Ekipman tipi seçilmelidir.");
        RuleFor(x => x.Uretici).Must(v => EkipmanKitapligiKurali.ZorunluMetinGecerli(v, EkipmanKitapligiKurali.UreticiEnFazla))
            .WithMessage($"Üretici zorunludur ve en fazla {EkipmanKitapligiKurali.UreticiEnFazla} karakter olabilir.");
        RuleFor(x => x.ModelAdi).Must(v => EkipmanKitapligiKurali.ZorunluMetinGecerli(v, EkipmanKitapligiKurali.ModelEnFazla))
            .WithMessage($"Model adı zorunludur ve en fazla {EkipmanKitapligiKurali.ModelEnFazla} karakter olabilir.");
        RuleFor(x => x.KaynakTuru).IsInEnum().WithMessage("Kaynak türü geçersiz.");
        RuleFor(x => x.KaynakAciklama).Must(v => EkipmanKitapligiKurali.IstegeBagliMetinGecerli(v, EkipmanKitapligiKurali.KaynakAciklamaEnFazla))
            .WithMessage($"Kaynak açıklaması en fazla {EkipmanKitapligiKurali.KaynakAciklamaEnFazla} karakter olabilir.");
        RuleFor(x => x.KaynakTarihi).Must(EkipmanKitapligiKurali.KaynakTarihiGecerli).WithMessage(EkipmanKitapligiKurali.KaynakTarihiMesaji);
        RuleFor(x => x.Aciklama).Must(v => EkipmanKitapligiKurali.IstegeBagliMetinGecerli(v, EkipmanKitapligiKurali.AciklamaEnFazla))
            .WithMessage($"Açıklama en fazla {EkipmanKitapligiKurali.AciklamaEnFazla} karakter olabilir.");
        // Cascade Stop: gövdede null gelen teknik veri uzunluk kuralında 500 değil 400 olur.
        RuleFor(x => x.TeknikVeriJson).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Teknik veri zorunludur.")
            .MaximumLength(EkipmanKitapligiKurali.TeknikVeriJsonEnFazla).WithMessage("Teknik veri çok büyük.");
    }
}

public sealed class EkipmanKonfigurasyonEkleCommandValidator : AbstractValidator<EkipmanKonfigurasyonEkleCommand>
{
    public EkipmanKonfigurasyonEkleCommandValidator() => Include(new EkipmanKonfigurasyonKaydiValidator());
}

public sealed class EkipmanKonfigurasyonGuncelleCommandValidator : AbstractValidator<EkipmanKonfigurasyonGuncelleCommand>
{
    public EkipmanKonfigurasyonGuncelleCommandValidator()
    {
        RuleFor(x => x.KonfigurasyonId).GreaterThan(0).WithMessage("Konfigürasyon seçilmelidir.");
        RuleFor(x => x.BeklenenRevizyonNo).GreaterThan(0).WithMessage("Ekipmanın revizyon numarası eksik; sayfayı yenileyin.");
        Include(new EkipmanKonfigurasyonKaydiValidator());
    }
}

public sealed class EkipmanDogrulaCommandValidator : AbstractValidator<EkipmanDogrulaCommand>
{
    public EkipmanDogrulaCommandValidator()
    {
        RuleFor(x => x.KonfigurasyonId).GreaterThan(0).WithMessage("Konfigürasyon seçilmelidir.");
        RuleFor(x => x.RevizyonNo).GreaterThan(0).WithMessage("Revizyon seçilmelidir.");
    }
}
