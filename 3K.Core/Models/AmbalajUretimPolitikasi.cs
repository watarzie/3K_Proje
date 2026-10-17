using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>Kaynak sandıktan ambalaj kaydına aktarılan ad, cins ve milimetre dış ölçü.</summary>
public readonly record struct AmbalajKaynakSandikDegerleri(
    string? Ad,
    AmbalajSandikCinsi Cins,
    decimal Boy,
    decimal En,
    decimal Yukseklik);

public static class AmbalajUretimPolitikasi
{
    public static bool M3HesaplanabilirMi(AmbalajSandikCinsi cins) =>
        cins is AmbalajSandikCinsi.AhsapKapali or AmbalajSandikCinsi.Kafes;

    public static AmbalajSandikCinsi KaynakCinsi(int tipId) => tipId switch
    {
        (int)SandikTipi.AhsapKapali => AmbalajSandikCinsi.AhsapKapali,
        (int)SandikTipi.KatlanirSandik => AmbalajSandikCinsi.Katlanir,
        (int)SandikTipi.Kontrplak => AmbalajSandikCinsi.Kontrplak,
        _ => AmbalajSandikCinsi.Diger
    };

    /// <summary>
    /// Kaynak sandığın ambalaj kaydına taşıdığı ad, cins ve dış ölçü (boş ölçü 0). Senkronizasyon ve üretim
    /// formundaki kaynak tazelemesi aynı eşlemeyi kullanır.
    /// </summary>
    public static AmbalajKaynakSandikDegerleri KaynakDegerleri(Sandik sandik) =>
        new(sandik.Ad, KaynakCinsi(sandik.TipId), sandik.Boy ?? 0, sandik.En ?? 0, sandik.Yukseklik ?? 0);

    /// <summary>
    /// Kaynak sandık, kaydın finans fiyat eşleşmesini belirleyen ad, cins veya ölçüsünü değiştiriyor mu?
    /// PO geçmişi olan finans kaleminde bu değişiklik fiyat snapshot'ını bozacağı için reddedilir.
    /// </summary>
    public static bool KaynakFiyatEslesmesiniDegistirirMi(AmbalajUretimKaydi kayit, Sandik sandik)
    {
        var kaynak = KaynakDegerleri(sandik);
        return !string.Equals(kayit.Ad?.Trim(), kaynak.Ad?.Trim(), StringComparison.OrdinalIgnoreCase) ||
               kayit.SandikCinsi != kaynak.Cins ||
               kayit.Boy != kaynak.Boy ||
               kayit.En != kaynak.En ||
               kayit.Yukseklik != kaynak.Yukseklik;
    }

    // Kaynakta amaç kodu bulunmadığından iki sandık adı ayrı ayrı değerlendirilir.
    // Sandık numarası ekipman türünün kanıtı değildir.
    public static bool VarsayilanYapilmazMi(string? ad, string? ingilizceAd = null)
    {
        return HedefSandikMi(NormalAd(ad)) || HedefSandikMi(NormalAd(ingilizceAd));
    }

    private static string NormalAd(string? kaynakAd)
    {
        if (string.IsNullOrWhiteSpace(kaynakAd)) return string.Empty;
        var metin = string.Concat(kaynakAd.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
            .ToUpperInvariant().Replace('İ', 'I').Replace('ı', 'I');
        return string.Join(' ', Regex.Matches(metin, "[A-Z0-9]+").Select(m => m.Value));
    }

    private static bool HedefSandikMi(string normalAd) =>
        Regex.IsMatch(normalAd, @"(?:^| )(?:BUSHING|BUSING)") ||
        Regex.IsMatch(normalAd,
            @"^(?:(?:TRANSFORMATOR(?: YAGSIZ)?|TRANSFORMER)(?: (?:SANDIGI|SANDIK|CASE|CRATE))?|TRANSFORMER MAIN BODY)$");

    /// <summary>
    /// Kaynak sandığın üretim grubu (RR Q2). Grup tek doğruluk kaynağı olan <c>Tur</c> alanındadır ve kayıt
    /// ilk senkronda bir kez belirlenir: normal projede plan başladıktan (projenin ilk senkron kaydından)
    /// sonra oluşturulan sandık İlave'dir. Kayıtlı Normal/İlave sınıflandırması sonraki senkronlarda
    /// korunur; saha/yedek projelerde grup proje türüdür. Henüz kaydı olmayan sandık aynı kuralla okunur.
    /// </summary>
    public static AmbalajSandikTuru KaynakGrubu(
        AmbalajSandikTuru projeGrubu,
        AmbalajSandikTuru? kayitliGrup,
        DateTime? planBaslangici,
        DateTime sandikOlusturmaTarihi)
    {
        if (projeGrubu != AmbalajSandikTuru.Normal) return projeGrubu;
        if (kayitliGrup.HasValue)
            return kayitliGrup == AmbalajSandikTuru.Ilave ? AmbalajSandikTuru.Ilave : AmbalajSandikTuru.Normal;
        return planBaslangici.HasValue && sandikOlusturmaTarihi > planBaslangici.Value
            ? AmbalajSandikTuru.Ilave
            : AmbalajSandikTuru.Normal;
    }

    public static AmbalajUretimDurumu ProjeDurumu(IEnumerable<AmbalajUretimKaydi> kayitlar)
    {
        var gerekli = kayitlar.Where(k => !k.IptalMi && k.AmbalajaDahil).ToList();
        if (gerekli.Count == 0) return AmbalajUretimDurumu.Planlandi;
        if (gerekli.All(k => k.UretimDurumu == AmbalajUretimDurumu.Tamamlandi))
            return AmbalajUretimDurumu.Tamamlandi;
        return gerekli.Any(k => k.UretimDurumu != AmbalajUretimDurumu.Planlandi)
            ? AmbalajUretimDurumu.Uretimde : AmbalajUretimDurumu.Planlandi;
    }
}
