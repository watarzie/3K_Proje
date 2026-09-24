using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Core.Models;

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
