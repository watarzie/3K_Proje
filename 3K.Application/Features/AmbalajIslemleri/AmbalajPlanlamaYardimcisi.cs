using System.Text.RegularExpressions;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Features.AmbalajIslemleri;

internal static partial class AmbalajPlanlamaYardimcisi
{
    public static AmbalajPlanlamaPlanDto PlanDtoOlustur(
        Proje proje,
        string projeTipiMetni,
        IReadOnlyList<Sandik> sandiklar,
        IReadOnlyList<AmbalajUretimKaydi> kayitlar,
        int? grup = null)
    {
        var aktifKayitlar = kayitlar.Where(k => !k.IptalMi && !k.BagimsizKayitMi).ToList();
        var kaynakMap = aktifKayitlar
            .Where(k => k.KaynakKayitId.HasValue)
            .GroupBy(k => k.KaynakKayitId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(k => k.Id).First());
        var planBaslangici = aktifKayitlar
            .Where(k => k.KaynakKayitId.HasValue)
            .Select(k => (DateTime?)k.CreatedDate)
            .Min();

        var kaynaklar = grup == 3
            ? []
            : sandiklar
                .OrderBy(s => SandikSiraAnahtari(s.SandikNo))
                .ThenBy(s => s.SandikNo, StringComparer.OrdinalIgnoreCase)
                .Select(s => KaynakKalemDtoOlustur(s, kaynakMap.GetValueOrDefault(s.Id), planBaslangici))
                .ToList();
        var manueller = aktifKayitlar
            .Where(k => !k.KaynakKayitId.HasValue)
            .OrderBy(k => (int)k.Tur)
            .ThenBy(k => SandikSiraAnahtari(k.SandikNo))
            .ThenBy(k => k.SandikNo, StringComparer.OrdinalIgnoreCase)
            .Select(KalemDtoOlustur)
            .ToList();
        var tumKalemler = kaynaklar.Concat(manueller).ToList();

        var normal = GrupKayitlari(aktifKayitlar, AmbalajSandikTuru.Normal);
        var ilave = GrupKayitlari(aktifKayitlar, AmbalajSandikTuru.Ilave);
        var ic = GrupKayitlari(aktifKayitlar, AmbalajSandikTuru.Ic);
        return new AmbalajPlanlamaPlanDto(
            proje.Id,
            proje.ProjeNo,
            proje.FBNo,
            proje.Musteri,
            proje.ProjeTipiId,
            projeTipiMetni,
            normal.FirstOrDefault(k => !string.IsNullOrWhiteSpace(k.FirinPartiNo))?.FirinPartiNo,
            ilave.FirstOrDefault(k => !string.IsNullOrWhiteSpace(k.FirinPartiNo))?.FirinPartiNo,
            ic.FirstOrDefault(k => !string.IsNullOrWhiteSpace(k.FirinPartiNo))?.FirinPartiNo,
            GrupDurumu(normal),
            GrupDurumu(ilave),
            GrupDurumu(ic),
            tumKalemler,
            tumKalemler.Where(k => k.UretimeAlindi).Sum(k => k.Adet),
            tumKalemler.Where(k => k.UretimeAlindi).Sum(k => k.HacimM3));
    }

    public static AmbalajPlanlamaProjeOzetDto ProjeOzetDtoOlustur(
        Proje proje,
        string projeTipiMetni,
        IReadOnlyList<Sandik> sandiklar,
        IReadOnlyList<AmbalajUretimKaydi> kayitlar)
    {
        var plan = PlanDtoOlustur(proje, projeTipiMetni, sandiklar, kayitlar);
        var ambalajKaynaklari = plan.Kalemler.Where(k => k.KaynakSandikId.HasValue && k.AmbalajaDahilMi != false).ToList();
        var seciliKaynaklar = ambalajKaynaklari.Where(k => k.UretimeAlindi).ToList();
        var olculu = ambalajKaynaklari.Where(Olculu).ToList();
        var eksikler = seciliKaynaklar.Where(k => !Olculu(k)).Select(k => k.SandikNo).ToList();
        var manueller = plan.Kalemler.Where(k => !k.KaynakSandikId.HasValue && k.UretimeAlindi).ToList();
        var projeKaynaklari = ambalajKaynaklari.Where(k => k.Tur == 1).ToList();
        var ilaveKaynaklari = ambalajKaynaklari.Where(k => k.Tur == 2).ToList();

        return new AmbalajPlanlamaProjeOzetDto(
            proje.Id,
            proje.ProjeNo,
            proje.FBNo,
            proje.Musteri,
            proje.ProjeTipiId,
            projeTipiMetni,
            ambalajKaynaklari.Sum(k => k.Adet),
            olculu.Count,
            eksikler.Count,
            eksikler,
            olculu.Sum(k => k.HacimM3),
            plan.FirinPartiNo,
            seciliKaynaklar.Sum(k => k.Adet) + manueller.Sum(k => k.Adet),
            ilaveKaynaklari.Count + manueller.Count(k => k.Tur == 2),
            manueller.Count(k => k.Tur == 3),
            seciliKaynaklar.Sum(k => k.HacimM3) + manueller.Sum(k => k.HacimM3),
            plan.ProjeSandiklariDurumId,
            plan.IlaveSandiklarDurumId,
            plan.IcSandiklarDurumId,
            plan.IlaveFirinPartiNo,
            plan.IcSandikFirinPartiNo,
            projeKaynaklari.Sum(k => k.Adet) + manueller.Where(k => k.Tur == 1).Sum(k => k.Adet),
            projeKaynaklari.Where(k => k.UretimeAlindi).Sum(k => k.HacimM3) + manueller.Where(k => k.Tur == 1).Sum(k => k.HacimM3),
            ilaveKaynaklari.Where(k => k.UretimeAlindi).Sum(k => k.HacimM3) + manueller.Where(k => k.Tur == 2).Sum(k => k.HacimM3),
            manueller.Where(k => k.Tur == 3).Sum(k => k.HacimM3));
    }

    public static AmbalajPlanlamaKalemDto KalemDtoOlustur(AmbalajUretimKaydi kayit) =>
        new(
            kayit.Id,
            kayit.KaynakKayitId,
            kayit.UstKayitId,
            kayit.IcSandikSablonId,
            GrupTuruneDonustur(kayit.Tur),
            TurMetni(kayit.Tur, kayit.KaynakKayitId.HasValue),
            kayit.UretimeAlindi,
            kayit.SandikNo,
            kayit.Ad,
            SandikTipiMetni(kayit.SandikCinsi, kayit.DigerSandikCinsi),
            kayit.Adet,
            kayit.Boy,
            kayit.En,
            kayit.Yukseklik,
            kayit.KullanimAmaci,
            kayit.TalimatVeren,
            kayit.Aciklama,
            kayit.M3Override ?? kayit.HesaplananToplamM3,
            kayit.AmbalajaDahil,
            false);

    public static AmbalajBagimsizSandikDto BagimsizDtoOlustur(
        AmbalajUretimKaydi kayit,
        Proje? proje,
        AmbalajUretimKaydi? ustKayit,
        Sandik? kaynakSandik) =>
        new(
            kayit.Id,
            OzelTurId(kayit.Tur),
            OzelSandikTurMetni(kayit.Tur),
            kayit.ProjeId,
            proje?.ProjeNo,
            proje?.Musteri,
            kaynakSandik?.Id,
            kaynakSandik?.SandikNo,
            kaynakSandik?.Ad,
            ustKayit?.KaynakKayitId,
            kayit.IcSandikSablonId,
            ustKayit?.SandikNo,
            ustKayit?.Ad,
            kayit.UretimeAlindi,
            kayit.SandikNo,
            kayit.Ad ?? string.Empty,
            SandikTipiMetni(kayit.SandikCinsi, kayit.DigerSandikCinsi),
            kayit.Adet,
            kayit.Boy,
            kayit.En,
            kayit.Yukseklik,
            kayit.KullanimAmaci,
            kayit.TalimatVeren,
            kayit.Aciklama,
            kayit.M3Override ?? kayit.HesaplananToplamM3);

    public static AmbalajSandikCinsi SandikCinsiCoz(string? sandikTipi) => sandikTipi?.Trim() switch
    {
        "Ahşap Kapalı" => AmbalajSandikCinsi.AhsapKapali,
        "Kafes Sandık" => AmbalajSandikCinsi.Kafes,
        "Kontrplak Sandık" => AmbalajSandikCinsi.Kontrplak,
        "Katlanır Sandık" => AmbalajSandikCinsi.Katlanir,
        _ => AmbalajSandikCinsi.Diger
    };

    public static string SandikTipiMetni(AmbalajSandikCinsi cins, string? diger = null) => cins switch
    {
        AmbalajSandikCinsi.AhsapKapali => "Ahşap Kapalı",
        AmbalajSandikCinsi.Kafes => "Kafes Sandık",
        AmbalajSandikCinsi.Kontrplak => "Kontrplak Sandık",
        AmbalajSandikCinsi.Katlanir => "Katlanır Sandık",
        _ => string.IsNullOrWhiteSpace(diger) ? "Diğer" : diger
    };

    public static bool GecerliSandikTipi(string? value) => value is
        "Ahşap Kapalı" or "Kafes Sandık" or "Kontrplak Sandık" or "Katlanır Sandık";

    public static int SandikAdediHesapla(string? sandikNo)
    {
        var match = SandikAraligiRegex().Match(sandikNo ?? string.Empty);
        if (!match.Success) return 1;
        var baslangic = int.Parse(match.Groups[1].Value);
        var bitis = int.Parse(match.Groups[2].Value);
        return bitis >= baslangic ? bitis - baslangic + 1 : 1;
    }

    public static string OzelSandikTurMetni(AmbalajSandikTuru tur) => tur switch
    {
        AmbalajSandikTuru.Ilave => "İlave",
        AmbalajSandikTuru.Ic => "İç Sandık",
        AmbalajSandikTuru.Saha => "Saha",
        AmbalajSandikTuru.Yedek => "Yedek",
        _ => "Bilinmiyor"
    };

    public static int OzelTurId(AmbalajSandikTuru tur) => tur switch
    {
        AmbalajSandikTuru.Ilave => 2,
        AmbalajSandikTuru.Ic => 3,
        AmbalajSandikTuru.Saha => 4,
        AmbalajSandikTuru.Yedek => 5,
        _ => 0
    };

    public static AmbalajSandikTuru OzelTurCoz(int tur) => tur switch
    {
        2 => AmbalajSandikTuru.Ilave,
        3 => AmbalajSandikTuru.Ic,
        4 => AmbalajSandikTuru.Saha,
        5 => AmbalajSandikTuru.Yedek,
        _ => throw new ArgumentOutOfRangeException(nameof(tur))
    };

    private static AmbalajPlanlamaKalemDto KaynakKalemDtoOlustur(
        Sandik sandik,
        AmbalajUretimKaydi? kayit,
        DateTime? planBaslangici)
    {
        var tur = kayit?.Tur == AmbalajSandikTuru.Ilave ||
                  (kayit == null && planBaslangici.HasValue && sandik.CreatedDate > planBaslangici.Value)
            ? 2
            : 1;
        var dahil = kayit?.AmbalajaDahil ?? true;
        var secili = dahil && (kayit?.UretimeAlindi ?? false);
        var hacim = KaynakSandikToplamHacmiHesapla(
            sandik.Ad,
            sandik.AdIngilizce,
            sandik.SandikNo,
            sandik.Boy,
            sandik.En,
            sandik.Yukseklik);
        return new AmbalajPlanlamaKalemDto(
            kayit?.Id ?? 0,
            sandik.Id,
            null,
            null,
            tur,
            tur == 2 ? "İlave Sandık" : "Proje Sandığı",
            secili,
            sandik.SandikNo,
            sandik.Ad,
            kayit == null ? "Ahşap Kapalı" : SandikTipiMetni(kayit.SandikCinsi, kayit.DigerSandikCinsi),
            SandikAdediHesapla(sandik.SandikNo),
            sandik.Boy ?? 0,
            sandik.En ?? 0,
            sandik.Yukseklik ?? 0,
            kayit?.KullanimAmaci,
            kayit?.TalimatVeren,
            kayit?.Aciklama,
            hacim,
            dahil,
            AmbalajKarariOneriliyor(sandik));
    }

    internal static decimal KaynakSandikToplamHacmiHesapla(
        string? ad,
        string? adIngilizce,
        string? sandikNo,
        decimal? boy,
        decimal? en,
        decimal? yukseklik)
    {
        if (boy is not > 92m || en is not > 92m || yukseklik is not > 255m)
            return 0;
        var profil = AmbalajAyakProfiliBelirleyici.Belirle(ad, adIngilizce);
        var birimHacim = AmbalajHesaplayici.M3OzetiHesapla(
            boy.Value - 92m,
            en.Value - 92m,
            yukseklik.Value - 255m,
            1,
            ayakProfili: profil,
            ayakHesapBoyu: boy.Value).HesaplananToplamM3;
        return birimHacim * SandikAdediHesapla(sandikNo);
    }

    private static IReadOnlyList<AmbalajUretimKaydi> GrupKayitlari(
        IEnumerable<AmbalajUretimKaydi> kayitlar,
        AmbalajSandikTuru tur) => kayitlar.Where(k => k.Tur == tur).ToList();

    private static int GrupDurumu(IEnumerable<AmbalajUretimKaydi> kayitlar)
    {
        var secili = kayitlar.Where(k => k.UretimeAlindi).ToList();
        return secili.Count == 0 ? 1 : (int)secili.Max(k => k.UretimDurumu);
    }

    private static int GrupTuruneDonustur(AmbalajSandikTuru tur) => tur switch
    {
        AmbalajSandikTuru.Ilave => 2,
        AmbalajSandikTuru.Ic => 3,
        _ => 1
    };

    private static string TurMetni(AmbalajSandikTuru tur, bool kaynakli) => tur switch
    {
        AmbalajSandikTuru.Ilave => "İlave Sandık",
        AmbalajSandikTuru.Ic => "İç Sandık",
        _ => kaynakli ? "Proje Sandığı" : "Manuel Proje Sandığı"
    };

    private static bool Olculu(AmbalajPlanlamaKalemDto kalem) =>
        kalem.Boy > 0 && kalem.En > 0 && kalem.Yukseklik > 0;

    private static bool AmbalajKarariOneriliyor(Sandik sandik)
    {
        if (int.TryParse(sandik.SandikNo.Trim(), out var sandikNo) && sandikNo == 1)
            return true;
        var ad = $"{sandik.Ad} {sandik.AdIngilizce}".ToUpperInvariant()
            .Replace('İ', 'I')
            .Replace('Ş', 'S');
        return ad.Contains("BUSHING", StringComparison.Ordinal) ||
               ad.Contains("BUSING", StringComparison.Ordinal) ||
               ad.Contains("PARAFUD", StringComparison.Ordinal) ||
               ad.Contains("SURGE ARRESTER", StringComparison.Ordinal);
    }

    internal static int SandikSiraAnahtari(string? sandikNo)
    {
        var match = IlkSayiRegex().Match(sandikNo ?? string.Empty);
        return match.Success && int.TryParse(match.Value, out var sayi) ? sayi : int.MaxValue;
    }

    [GeneratedRegex(@"^(\d+)\s*-\s*(\d+)$")]
    private static partial Regex SandikAraligiRegex();

    [GeneratedRegex(@"\d+")]
    private static partial Regex IlkSayiRegex();
}
