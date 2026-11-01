using System.Globalization;

namespace _3K.Core.Models;

/// <summary>
/// Çekideki koli (sandık) numarasının tek biçimi: çeki yüklemesi ve revizyonu sandığı bu biçimdeki numarayla açar,
/// Ambalaj da sandığın çekiden gelip gelmediğini aynı biçimle karşılaştırır. Boşluklar ve uzun tireler temizlenir,
/// tam sayı değer ("05", "1.0") sade sayıya ("5", "1") çevrilir.
/// </summary>
public static class KoliNoKurali
{
    /// <summary>Koli numarasını çeki yüklemesindeki biçime getirir; boş değer boş metindir.</summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var text = value.Trim()
            .Replace('\u00A0', ' ')
            .Replace(" ", string.Empty)
            .Replace('\u2013', '-')
            .Replace('\u2014', '-');

        if (!text.Contains(',') && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var numericValue)
            && numericValue == decimal.Truncate(numericValue))
        {
            // Elle girilen uzun sayısal numara (ör. 10+ hane) int'e sığmaz; taşma yerine aynı sade sayı yazılır.
            return numericValue is >= int.MinValue and <= int.MaxValue
                ? ((int)numericValue).ToString(CultureInfo.InvariantCulture)
                : numericValue.ToString("0", CultureInfo.InvariantCulture);
        }

        return text;
    }

    /// <summary>
    /// Projede sıradaki sandık numarası: sayısal numaraların (çeki biçiminde; "05" = 5) en büyüğü + 1. Sayısal olmayan
    /// numaralar ("2-2", "5010-04", "1-SH", "M-7") sıraya katılmaz; hiç sayısal numara yoksa "1".
    /// </summary>
    public static string SiradakiNumara(IEnumerable<string?> mevcutNolar)
    {
        ArgumentNullException.ThrowIfNull(mevcutNolar);
        var enBuyuk = 0L;
        foreach (var no in mevcutNolar)
        {
            if (long.TryParse(Normalize(no), NumberStyles.None, CultureInfo.InvariantCulture, out var sayi) && sayi > enBuyuk)
                enBuyuk = sayi;
        }
        return (enBuyuk + 1).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Yeni açılacak sandığın numarası (28.10.2026, mükerrer görünen "2-2" eki yerine): istenen numara boş değil ve
    /// <paramref name="kullanilanNolar"/> içinde yoksa istenen numara, aksi halde <see cref="SiradakiNumara"/>.
    /// Küme çeki biçimindeki (<see cref="Normalize"/>) numaraları tutar; seçilen numara kümeye eklenir, aynı işlemde
    /// açılan sandıklar birbirinin numarasını almaz.
    /// </summary>
    public static string BosNumaraYaDaSiradaki(string? istenenNo, ISet<string> kullanilanNolar)
    {
        ArgumentNullException.ThrowIfNull(kullanilanNolar);
        var istenen = istenenNo?.Trim() ?? string.Empty;
        if (istenen.Length > 0 && kullanilanNolar.Add(Normalize(istenen)))
            return istenen;

        var siradaki = SiradakiNumara(kullanilanNolar);
        kullanilanNolar.Add(siradaki);
        return siradaki;
    }
}
