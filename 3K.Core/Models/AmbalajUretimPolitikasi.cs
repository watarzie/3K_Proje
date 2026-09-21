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

    // Kaynakta amaç kodu bulunmadığından yalnız açık ad terimleri değerlendirilir.
    // Sandık numarası hiçbir zaman ekipman türünün kanıtı değildir.
    public static bool VarsayilanYapilmazMi(string? ad, string? ingilizceAd = null)
    {
        var metin = string.Concat($"{ad} {ingilizceAd}".Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
            .ToUpperInvariant().Replace('İ', 'I').Replace('ı', 'I');
        var kelimeler = Regex.Matches(metin, "[A-Z0-9]+").Select(m => m.Value).ToHashSet();
        if (kelimeler.Overlaps(["TRAFO", "TRANSFORMATOR", "TRANSFORMER"])) return true;
        return kelimeler.Overlaps(["BUSHING", "BUSING", "BUSINGLER", "BUSHINGS"]) &&
               kelimeler.Overlaps(["AH", "YG", "HV", "LV"]);
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
